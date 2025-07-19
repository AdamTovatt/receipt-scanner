using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public class PerspectiveTransformer : IPerspectiveTransformer
    {
        public Mat TransformPerspective(Mat image, Point[] corners)
        {
            if (image.Empty())
                throw new ArgumentException("Image cannot be empty", nameof(image));

            if (corners.Length != 4)
                throw new ArgumentException("Must have exactly 4 corners", nameof(corners));

            // Sort corners to get top-left, top-right, bottom-right, bottom-left
            Point[] sortedCorners = SortCorners(corners);

            // Calculate the width and height of the receipt
            double width1 = Distance(sortedCorners[0], sortedCorners[1]);
            double width2 = Distance(sortedCorners[2], sortedCorners[3]);
            double height1 = Distance(sortedCorners[0], sortedCorners[3]);
            double height2 = Distance(sortedCorners[1], sortedCorners[2]);

            double maxWidth = Math.Max(width1, width2);
            double maxHeight = Math.Max(height1, height2);

            // Define destination points for perspective transform
            Point2f[] dstPoints = new Point2f[]
            {
                new Point2f(0, 0),
                new Point2f((float)maxWidth, 0),
                new Point2f((float)maxWidth, (float)maxHeight),
                new Point2f(0, (float)maxHeight)
            };

            // Convert corners to Point2f for perspective transform
            Point2f[] srcPoints = new Point2f[]
            {
                new Point2f(sortedCorners[0].X, sortedCorners[0].Y),
                new Point2f(sortedCorners[1].X, sortedCorners[1].Y),
                new Point2f(sortedCorners[2].X, sortedCorners[2].Y),
                new Point2f(sortedCorners[3].X, sortedCorners[3].Y)
            };

            // Get perspective transform matrix
            Mat transformMatrix = Cv2.GetPerspectiveTransform(srcPoints, dstPoints);

            // Apply perspective transform
            Mat warped = new Mat();
            Cv2.WarpPerspective(image, warped, transformMatrix, new Size((int)maxWidth, (int)maxHeight));

            transformMatrix.Dispose();
            return warped;
        }

        private Point[] SortCorners(Point[] corners)
        {
            // Find the center point
            Point center = new Point(
                (int)corners.Average(p => p.X),
                (int)corners.Average(p => p.Y)
            );

            // Sort corners based on their position relative to center
            Point[] sorted = corners.OrderBy(p => Math.Atan2(p.Y - center.Y, p.X - center.X)).ToArray();

            // Ensure we have top-left, top-right, bottom-right, bottom-left order
            return new Point[] { sorted[0], sorted[1], sorted[2], sorted[3] };
        }

        private double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
    }
}