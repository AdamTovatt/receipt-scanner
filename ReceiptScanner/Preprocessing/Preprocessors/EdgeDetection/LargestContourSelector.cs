using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public class LargestContourSelector : IContourSelector
    {
        private readonly EdgeDetectionConfig _config;

        public LargestContourSelector(EdgeDetectionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public Point[]? SelectBestContour(Point[][] contours, int imageWidth, int imageHeight)
        {
            if (contours.Length == 0)
                return null;

            Point[] largestContour = contours[0];
            double maxArea = Cv2.ContourArea(largestContour);

            for (int i = 1; i < contours.Length; i++)
            {
                double area = Cv2.ContourArea(contours[i]);
                if (area > maxArea)
                {
                    maxArea = area;
                    largestContour = contours[i];
                }
            }

            // Only return if the contour is reasonably large (relative to image size)
            double minArea = Math.Max(_config.MinContourArea, imageWidth * imageHeight * _config.MinAreaRatio);
            if (maxArea < minArea)
                return null;

            return largestContour;
        }
    }
}