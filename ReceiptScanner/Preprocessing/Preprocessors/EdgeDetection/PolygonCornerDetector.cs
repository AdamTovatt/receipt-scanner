using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public class PolygonCornerDetector : ICornerDetector
    {
        private readonly EdgeDetectionConfig _config;

        public PolygonCornerDetector(EdgeDetectionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public Point[]? DetectCorners(Point[] contour)
        {
            if (contour.Length < 4)
                return null;

            // Approximate the contour to a polygon
            Point[] approxContour = Cv2.ApproxPolyDP(contour, _config.Epsilon * Cv2.ArcLength(contour, true), true);

            // If we have 4 points, we can use them as corners
            if (approxContour.Length == 4)
                return approxContour;

            return null;
        }
    }
} 