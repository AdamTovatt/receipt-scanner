using OpenCvSharp;
using Point = OpenCvSharp.Point;

namespace ReceiptScanner.Preprocessing.Preprocessors
{
    /// <summary>
    /// Image preprocessor that detects horizontal lines in an image using HoughLinesP.
    /// Currently draws detected horizontal lines for debugging purposes.
    /// </summary>
    public class HorizontalLineDetectionPreprocessor : IImagePreprocessor
    {
        private readonly double _minLineLength;
        private readonly double _maxLineGap;
        private readonly int _threshold;
        private readonly double _angleTolerance;

        /// <summary>
        /// Initializes a new instance of the <see cref="HorizontalLineDetectionPreprocessor"/> class.
        /// </summary>
        /// <param name="minLineLength">The minimum length of line to detect (in pixels).</param>
        /// <param name="maxLineGap">The maximum gap between line segments to treat them as a single line (in pixels).</param>
        /// <param name="threshold">The accumulator threshold parameter for HoughLinesP.</param>
        /// <param name="angleTolerance">The tolerance for considering a line horizontal (in degrees).</param>
        public HorizontalLineDetectionPreprocessor(double minLineLength = 80.0, double maxLineGap = 50.0, int threshold = 80, double angleTolerance = 5.0)
        {
            _minLineLength = minLineLength;
            _maxLineGap = maxLineGap;
            _threshold = threshold;
            _angleTolerance = angleTolerance;
        }

        /// <summary>
        /// Gets the name of this preprocessor.
        /// </summary>
        public string Name => "HorizontalLineDetection";

        /// <summary>
        /// Preprocesses the input image by detecting horizontal lines and drawing them for debugging.
        /// </summary>
        /// <param name="image">The input image to process.</param>
        /// <returns>The image with detected horizontal lines drawn on it.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="image"/> is null or empty.</exception>
        public Mat Preprocess(Mat image)
        {
            if (image == null || image.Empty())
                throw new ArgumentException("Input image cannot be null or empty", nameof(image));

            // Convert to grayscale if the image is in color
            Mat grayImage = new Mat();
            if (image.Channels() == 3)
            {
                Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                grayImage = image.Clone();
            }

            // Apply Canny edge detection with higher thresholds to reduce noise
            Mat edges = new Mat();
            Cv2.Canny(grayImage, edges, 100, 200);

            // Detect lines using HoughLinesP
            LineSegmentPoint[] lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, _threshold, _minLineLength, _maxLineGap);

            // Filter for horizontal lines and merge nearby lines
            Mat result = image.Clone();
            List<LineSegmentPoint> horizontalLines = new List<LineSegmentPoint>();

            foreach (LineSegmentPoint line in lines)
            {
                // Calculate the angle of the line
                double angle = Math.Atan2(line.P2.Y - line.P1.Y, line.P2.X - line.P1.X) * 180 / Math.PI;

                // Normalize angle to 0-180 degrees
                if (angle < 0)
                    angle += 180;

                // Check if the line is approximately horizontal (within tolerance)
                if (angle <= _angleTolerance || angle >= (180 - _angleTolerance))
                {
                    // Additional filtering: check if the line is long enough relative to image width
                    double lineLength = Math.Sqrt(Math.Pow(line.P2.X - line.P1.X, 2) + Math.Pow(line.P2.Y - line.P1.Y, 2));
                    double minLengthRatio = 0.15; // Line should be at least 15% of image width

                    if (lineLength >= image.Width * minLengthRatio)
                    {
                        // Check line continuity to filter out text
                        if (IsLineContinuous(line, grayImage))
                        {
                            horizontalLines.Add(line);
                        }
                    }
                }
            }

            // Draw bounding polygons in sampled colors for debugging
            List<Point> allPoints = GetAllPointsFromLines(horizontalLines);
            List<List<Point>> pointGroups = GroupPointsByYCoordinate(allPoints);
            List<List<LineSegmentPoint>> boundingPolygons = CreateBoundingPolygonsFromGroups(pointGroups);

            foreach (List<LineSegmentPoint> polygon in boundingPolygons)
            {
                foreach (LineSegmentPoint line in polygon)
                {
                    // Draw the line with per-pixel color sampling
                    DrawLineWithSampledColors(line, image, result);
                }
            }

            // Cleanup
            grayImage.Dispose();
            edges.Dispose();

            return result;
        }

        /// <summary>
        /// Extracts all points from detected lines and reconstructs the longest horizontal lines.
        /// </summary>
        /// <param name="lines">The list of detected line segments.</param>
        /// <param name="imageHeight">The height of the image for calculating Y tolerance.</param>
        /// <returns>A list of reconstructed horizontal lines.</returns>
        private List<LineSegmentPoint> ReconstructLongestHorizontalLines(List<LineSegmentPoint> lines, int imageHeight)
        {
            if (lines.Count == 0)
                return new List<LineSegmentPoint>();

            // Extract all points from the detected lines
            List<Point> allPoints = new List<Point>();
            foreach (LineSegmentPoint line in lines)
            {
                allPoints.Add(line.P1);
                allPoints.Add(line.P2);
            }

            // Group points by Y position (within tolerance)
            double yTolerance = imageHeight * 0.005; // 1% of image height
            List<List<Point>> yGroups = new List<List<Point>>();

            foreach (Point point in allPoints)
            {
                bool addedToGroup = false;

                // Check if point belongs to an existing Y group
                foreach (List<Point> group in yGroups)
                {
                    if (group.Count > 0)
                    {
                        double groupAvgY = group.Average(p => p.Y);
                        if (Math.Abs(point.Y - groupAvgY) <= yTolerance)
                        {
                            group.Add(point);
                            addedToGroup = true;
                            break;
                        }
                    }
                }

                // If not added to any group, create a new group
                if (!addedToGroup)
                {
                    yGroups.Add(new List<Point> { point });
                }
            }

            // For each Y group, find the longest line with original slope
            List<LineSegmentPoint> reconstructedLines = new List<LineSegmentPoint>();

            foreach (List<Point> yGroup in yGroups)
            {
                if (yGroup.Count < 2)
                    continue;

                // Find the original lines that contributed points to this group
                List<LineSegmentPoint> contributingLines = new List<LineSegmentPoint>();
                foreach (LineSegmentPoint line in lines)
                {
                    bool p1InGroup = yGroup.Any(p => Math.Abs(p.X - line.P1.X) <= 5 && Math.Abs(p.Y - line.P1.Y) <= 5);
                    bool p2InGroup = yGroup.Any(p => Math.Abs(p.X - line.P2.X) <= 5 && Math.Abs(p.Y - line.P2.Y) <= 5);

                    if (p1InGroup || p2InGroup)
                    {
                        contributingLines.Add(line);
                    }
                }

                if (contributingLines.Count == 0)
                    continue;

                // Calculate the average slope of contributing lines
                double avgSlope = contributingLines.Average(line =>
                {
                    double dx = line.P2.X - line.P1.X;
                    double dy = line.P2.Y - line.P1.Y;
                    return dx != 0 ? dy / dx : 0;
                });

                // Find the leftmost and rightmost points
                Point leftmost = yGroup.OrderBy(p => p.X).First();
                Point rightmost = yGroup.OrderBy(p => p.X).Last();

                // Calculate the Y coordinates for the reconstructed line using the average slope
                int y1 = leftmost.Y;
                int y2 = (int)(y1 + avgSlope * (rightmost.X - leftmost.X));

                // Create the longest line with original slope
                LineSegmentPoint longestLine = new LineSegmentPoint(
                    new Point(leftmost.X, y1),
                    new Point(rightmost.X, y2)
                );

                reconstructedLines.Add(longestLine);
            }

            return reconstructedLines;
        }

        /// <summary>
        /// Extracts all start and end points from a list of line segments.
        /// </summary>
        /// <param name="lines">The list of line segments.</param>
        /// <returns>A list of all unique points from the line segments.</returns>
        private List<Point> GetAllPointsFromLines(List<LineSegmentPoint> lines)
        {
            List<Point> allPoints = new List<Point>();

            foreach (LineSegmentPoint line in lines)
            {
                allPoints.Add(line.P1);
                allPoints.Add(line.P2);
            }

            return allPoints;
        }

        /// <summary>
        /// Groups points by Y coordinate using a fixed tolerance and improved clustering logic.
        /// </summary>
        /// <param name="points">The list of points to group.</param>
        /// <returns>A list of point groups, each containing points at similar Y coordinates.</returns>
        private List<List<Point>> GroupPointsByYCoordinate(List<Point> points)
        {
            if (points.Count == 0)
                return new List<List<Point>>();

            // Use a fixed tolerance of 20 pixels for grouping
            const int yTolerance = 20;
            
            // Sort points by Y coordinate for more predictable grouping
            List<Point> sortedPoints = points.OrderBy(p => p.Y).ToList();
            List<List<Point>> groups = new List<List<Point>>();
            
            foreach (Point point in sortedPoints)
            {
                bool addedToExistingGroup = false;
                
                // Try to add to an existing group
                foreach (List<Point> group in groups)
                {
                    if (group.Count > 0)
                    {
                        // Calculate the average Y coordinate of the group
                        double groupAvgY = group.Average(p => p.Y);
                        
                        // Check if this point is within tolerance of the group's average Y
                        if (Math.Abs(point.Y - groupAvgY) <= yTolerance)
                        {
                            group.Add(point);
                            addedToExistingGroup = true;
                            break;
                        }
                    }
                }
                
                // If not added to any existing group, create a new group
                if (!addedToExistingGroup)
                {
                    groups.Add(new List<Point> { point });
                }
            }
            
            // Filter out groups that are too small (likely noise)
            const int minGroupSize = 2;
            return groups.Where(group => group.Count >= minGroupSize).ToList();
        }

        /// <summary>
        /// Creates line segments that form a bounding polygon around each group of points.
        /// </summary>
        /// <param name="pointGroups">The list of point groups.</param>
        /// <returns>A list of line segment groups, where each group forms a bounding polygon.</returns>
        private List<List<LineSegmentPoint>> CreateBoundingPolygonsFromGroups(List<List<Point>> pointGroups)
        {
            List<List<LineSegmentPoint>> polygonGroups = new List<List<LineSegmentPoint>>();

            foreach (List<Point> group in pointGroups)
            {
                if (group.Count < 3)
                    continue; // Skip groups with less than 3 points

                // Find the extreme points in a single pass
                Point leftmost = group[0];
                Point rightmost = group[0];
                Point topmost = group[0];
                Point bottommost = group[0];

                foreach (Point point in group)
                {
                    if (point.X < leftmost.X) leftmost = point;
                    if (point.X > rightmost.X) rightmost = point;
                    if (point.Y < topmost.Y) topmost = point;
                    if (point.Y > bottommost.Y) bottommost = point;
                }

                // Create line segments forming the bounding polygon
                List<LineSegmentPoint> polygonLines = new List<LineSegmentPoint>();

                // Always add the main bounding lines
                polygonLines.Add(new LineSegmentPoint(leftmost, rightmost)); // Top horizontal

                // Add vertical lines if we have enough spread
                if (leftmost != rightmost)
                {
                    polygonLines.Add(new LineSegmentPoint(leftmost, topmost));      // Left to top
                    polygonLines.Add(new LineSegmentPoint(leftmost, bottommost));    // Left to bottom
                    polygonLines.Add(new LineSegmentPoint(rightmost, topmost));      // Right to top
                    polygonLines.Add(new LineSegmentPoint(rightmost, bottommost));   // Right to bottom
                }

                polygonGroups.Add(polygonLines);
            }

            return polygonGroups;
        }

        /// <summary>
        /// Checks if a line is likely a real horizontal line by analyzing the average darkness.
        /// Real horizontal lines will be consistently darker, while text will be brighter due to white spaces.
        /// </summary>
        /// <param name="line">The line segment to check.</param>
        /// <param name="grayImage">The grayscale image to analyze.</param>
        /// <returns>True if the line is dark enough to be considered a real horizontal line.</returns>
        private bool IsLineContinuous(LineSegmentPoint line, Mat grayImage)
        {
            // Sample points along the line to calculate average darkness
            int numSamples = 100;
            double totalDarkness = 0.0;
            int validSamples = 0;

            for (int i = 0; i <= numSamples; i++)
            {
                double t = (double)i / numSamples;
                int x = (int)(line.P1.X + t * (line.P2.X - line.P1.X));
                int y = (int)(line.P1.Y + t * (line.P2.Y - line.P1.Y));

                // Ensure coordinates are within bounds
                if (x >= 0 && x < grayImage.Width && y >= 0 && y < grayImage.Height)
                {
                    validSamples++;

                    // Get the pixel value at this point (0 = black, 255 = white)
                    byte pixelValue = grayImage.Get<byte>(y, x);
                    totalDarkness += pixelValue; // Higher value = brighter (less dark)
                }
            }

            // Calculate average brightness
            if (validSamples == 0)
                return false;

            double averageBrightness = totalDarkness / validSamples;

            // Real horizontal lines should be darker (lower brightness values)
            // Text will be brighter due to white spaces between characters
            // Threshold: average brightness should be less than 150 (darker than 150/255)
            return averageBrightness < 150;
        }



        /// <summary>
        /// Draws a line with per-pixel color sampling.
        /// </summary>
        /// <param name="line">The line segment to draw.</param>
        /// <param name="image">The image to sample colors from.</param>
        /// <param name="result">The Mat to draw on.</param>
        private void DrawLineWithSampledColors(LineSegmentPoint line, Mat image, Mat result)
        {
            int numSamples = (int)Math.Sqrt(Math.Pow(line.P2.X - line.P1.X, 2) + Math.Pow(line.P2.Y - line.P1.Y, 2));
            if (numSamples == 0) return; // Avoid division by zero

            const int offsetDistance = 6; // Distance above/below line to sample
            const int lineThickness = 4; // Thickness of the erasing line

            // First, sample the entire line globally (20 samples)
            Scalar globalLineColor = SampleGlobalLineColor(line, image, offsetDistance);

            for (int i = 0; i <= numSamples; i++)
            {
                double t = (double)i / numSamples;
                int x = (int)(line.P1.X + t * (line.P2.X - line.P1.X));
                int y = (int)(line.P1.Y + t * (line.P2.Y - line.P1.Y));

                // Ensure coordinates are within bounds
                if (x >= 0 && x < image.Width && y >= 0 && y < image.Height)
                {
                    // Sample colors from above and below this specific pixel
                    List<Vec3b> samples = new List<Vec3b>();

                    // Sample above the line
                    int yAbove = y - offsetDistance;
                    if (yAbove >= 0)
                    {
                        samples.Add(image.Get<Vec3b>(yAbove, x));
                    }

                    // Sample below the line
                    int yBelow = y + offsetDistance;
                    if (yBelow < image.Height)
                    {
                        samples.Add(image.Get<Vec3b>(yBelow, x));
                    }

                    // Calculate average color from local samples
                    if (samples.Count > 0)
                    {
                        double localAvgB = samples.Average(s => s[0]);
                        double localAvgG = samples.Average(s => s[1]);
                        double localAvgR = samples.Average(s => s[2]);
                        
                        // Combine global and local colors (50/50 blend)
                        double combinedB = (localAvgB + globalLineColor.Val0) * 0.5;
                        double combinedG = (localAvgG + globalLineColor.Val1) * 0.5;
                        double combinedR = (localAvgR + globalLineColor.Val2) * 0.5;
                        
                        Scalar sampledColor = new Scalar(combinedB, combinedG, combinedR);
                        
                        // Draw a thicker line by drawing multiple pixels around the point
                        for (int dy = -lineThickness; dy <= lineThickness; dy++)
                        {
                            for (int dx = -lineThickness; dx <= lineThickness; dx++)
                            {
                                int drawX = x + dx;
                                int drawY = y + dy;
                                
                                // Ensure the pixel to draw is within bounds
                                if (drawX >= 0 && drawX < image.Width && drawY >= 0 && drawY < image.Height)
                                {
                                    Cv2.Circle(result, new Point(drawX, drawY), 0, sampledColor, -1);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Samples the entire line globally to get an average background color.
        /// </summary>
        /// <param name="line">The line segment to sample.</param>
        /// <param name="image">The image to sample from.</param>
        /// <param name="offsetDistance">Distance above/below line to sample.</param>
        /// <returns>A Scalar representing the global average background color.</returns>
        private Scalar SampleGlobalLineColor(LineSegmentPoint line, Mat image, int offsetDistance)
        {
            const int globalSamples = 20;
            double totalB = 0.0, totalG = 0.0, totalR = 0.0;
            int validSamples = 0;

            for (int i = 0; i <= globalSamples; i++)
            {
                double t = (double)i / globalSamples;
                int x = (int)(line.P1.X + t * (line.P2.X - line.P1.X));
                int y = (int)(line.P1.Y + t * (line.P2.Y - line.P1.Y));

                // Sample colors from above and below this point
                List<Vec3b> samples = new List<Vec3b>();

                // Sample above the line
                int yAbove = y - offsetDistance;
                if (yAbove >= 0 && x >= 0 && x < image.Width)
                {
                    samples.Add(image.Get<Vec3b>(yAbove, x));
                }

                // Sample below the line
                int yBelow = y + offsetDistance;
                if (yBelow < image.Height && x >= 0 && x < image.Width)
                {
                    samples.Add(image.Get<Vec3b>(yBelow, x));
                }

                // Average the samples from above and below
                if (samples.Count > 0)
                {
                    validSamples++;
                    double avgB = samples.Average(s => s[0]);
                    double avgG = samples.Average(s => s[1]);
                    double avgR = samples.Average(s => s[2]);
                    
                    totalB += avgB;
                    totalG += avgG;
                    totalR += avgR;
                }
            }

            if (validSamples == 0)
                return new Scalar(255, 255, 255); // Fallback to white

            double finalAvgB = totalB / validSamples;
            double finalAvgG = totalG / validSamples;
            double finalAvgR = totalR / validSamples;

            return new Scalar(finalAvgB, finalAvgG, finalAvgR);
        }
    }
}