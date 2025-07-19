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
        public HorizontalLineDetectionPreprocessor(double minLineLength = 80.0, double maxLineGap = 50.0, int threshold = 80, double angleTolerance = 3.0)
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
            Cv2.Canny(grayImage, edges, 100, 200, 1);

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
                        if (IsLineContinuous(line, edges))
                        {
                            horizontalLines.Add(line);
                        }
                    }
                }
            }

            // Merge nearby horizontal lines that are at similar Y positions
            List<LineSegmentPoint> mergedLines = MergeNearbyHorizontalLines(horizontalLines, image.Height);

            // Draw the actual line contours instead of straight horizontal lines
            foreach (LineSegmentPoint line in mergedLines)
            {
                // Draw the actual line path, not a straight horizontal line
                Cv2.Line(result, line.P1, line.P2, Scalar.Red, 2);
            }

            // Also draw the original unmerged lines in blue for comparison
            foreach (LineSegmentPoint line in horizontalLines)
            {
                Cv2.Line(result, line.P1, line.P2, Scalar.Blue, 1);
            }

            // Cleanup
            grayImage.Dispose();
            edges.Dispose();

            return result;
        }

        /// <summary>
        /// Merges nearby horizontal lines that are at similar Y positions to reduce duplicate detections.
        /// </summary>
        /// <param name="lines">The list of horizontal lines to merge.</param>
        /// <param name="imageHeight">The height of the image for calculating merge tolerance.</param>
        /// <returns>A list of merged horizontal lines.</returns>
        private List<LineSegmentPoint> MergeNearbyHorizontalLines(List<LineSegmentPoint> lines, int imageHeight)
        {
            if (lines.Count == 0)
                return new List<LineSegmentPoint>();

            // Sort lines by Y position (average of start and end Y)
            List<LineSegmentPoint> sortedLines = lines.OrderBy(l => (l.P1.Y + l.P2.Y) / 2.0).ToList();

            List<LineSegmentPoint> mergedLines = new List<LineSegmentPoint>();
            double mergeTolerance = imageHeight * 0.01; // 1% of image height (very strict)

            foreach (LineSegmentPoint currentLine in sortedLines)
            {
                double currentY = (currentLine.P1.Y + currentLine.P2.Y) / 2.0;
                bool merged = false;

                // Check if this line can be merged with an existing line
                for (int i = 0; i < mergedLines.Count; i++)
                {
                    LineSegmentPoint existingLine = mergedLines[i];
                    double existingY = (existingLine.P1.Y + existingLine.P2.Y) / 2.0;

                    // If lines are at similar Y positions, merge them
                    if (Math.Abs(currentY - existingY) <= mergeTolerance)
                    {
                        // Instead of creating a straight horizontal line, 
                        // keep the line with the better contour
                        double existingLength = Math.Sqrt(Math.Pow(existingLine.P2.X - existingLine.P1.X, 2) +
                                                        Math.Pow(existingLine.P2.Y - existingLine.P1.Y, 2));
                        double currentLength = Math.Sqrt(Math.Pow(currentLine.P2.X - currentLine.P1.X, 2) +
                                                       Math.Pow(currentLine.P2.Y - currentLine.P1.Y, 2));

                        // Keep the longer line as it's more likely to be the main contour
                        if (currentLength > existingLength)
                        {
                            mergedLines[i] = currentLine;
                        }
                        merged = true;
                        break;
                    }
                }

                // If not merged, add as a new line
                if (!merged)
                {
                    mergedLines.Add(currentLine);
                }
            }

            return mergedLines;
        }

        /// <summary>
        /// Checks if a line is continuous (has few gaps) to filter out text.
        /// Text typically has gaps between characters, while real lines are more continuous.
        /// </summary>
        /// <param name="line">The line segment to check.</param>
        /// <param name="cannyEdges">The Canny edge detection result (binary image).</param>
        /// <returns>True if the line is continuous enough to be considered a real line.</returns>
        private bool IsLineContinuous(LineSegmentPoint line, Mat cannyEdges)
        {
            // Sample points along the line to check for edge pixels
            int numSamples = 100;
            int continuousCount = 0;
            int totalSamples = 0;

            cannyEdges.SaveImage("cannyEdges.jpg");

            for (int i = 0; i <= numSamples; i++)
            {
                double t = (double)i / numSamples;
                int x = (int)(line.P1.X + t * (line.P2.X - line.P1.X));
                int y = (int)(line.P1.Y + t * (line.P2.Y - line.P1.Y));

                // Ensure coordinates are within bounds
                if (x >= 0 && x < cannyEdges.Width && y >= 0 && y < cannyEdges.Height)
                {
                    totalSamples++;

                    // Check if there's an edge pixel at this position
                    byte pixelValue = cannyEdges.Get<byte>(y, x);
                    if (pixelValue > 0)
                    {
                        continuousCount++;
                    }
                }
            }

            // Calculate continuity ratio
            if (totalSamples == 0)
                return false;

            double continuityRatio = (double)continuousCount / totalSamples;

            // More lenient threshold: 35% continuity - allows more gaps but still filters out text
            return continuityRatio >= 0.35;
        }
    }
}