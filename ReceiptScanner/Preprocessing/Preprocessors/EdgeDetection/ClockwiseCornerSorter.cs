using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public class ClockwiseCornerSorter : ICornerSorter
    {
        public Point[] SortCorners(Point[] corners)
        {
            if (corners.Length != 4)
                throw new ArgumentException("Must have exactly 4 corners", nameof(corners));

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
    }
}