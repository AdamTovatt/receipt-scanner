using OpenCvSharp;

namespace ReceiptScanner.Services.CornerDetection
{
    public class ContourProcessingService
    {
        public OpenCvSharp.Point? FindCentroidFromBinaryImage(Mat binaryImage)
        {
            if (binaryImage == null || binaryImage.Empty())
                return null;

            // Find contours
            Cv2.FindContours(
                binaryImage,
                out OpenCvSharp.Point[][] contours,
                out _,
                RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0)
                return null;

            // Find the contour with the largest area
            Point[] largestContour = contours
                .OrderByDescending(c => Cv2.ContourArea(c))
                .First();

            // Calculate centroid using moments
            Moments moments = Cv2.Moments(largestContour);

            if (moments.M00 == 0)
                return null;

            int centroidX = (int)(moments.M10 / moments.M00);
            int centroidY = (int)(moments.M01 / moments.M00);

            return new OpenCvSharp.Point(centroidX, centroidY);
        }

        public List<OpenCvSharp.Point[]> FindContours(Mat binaryImage)
        {
            if (binaryImage == null || binaryImage.Empty())
                return new List<OpenCvSharp.Point[]>();

            Cv2.FindContours(
                binaryImage,
                out OpenCvSharp.Point[][] contours,
                out _,
                RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            // Filter out empty contours
            return contours.Where(c => c.Length > 0).ToList();
        }

        public OpenCvSharp.Point? GetCentroidOfLargestContour(List<OpenCvSharp.Point[]> contours)
        {
            if (contours.Count == 0)
                return null;

            Point[] largestContour = contours
                .OrderByDescending(c => Cv2.ContourArea(c))
                .First();

            Moments moments = Cv2.Moments(largestContour);

            if (moments.M00 == 0)
                return null;

            int centroidX = (int)(moments.M10 / moments.M00);
            int centroidY = (int)(moments.M01 / moments.M00);

            return new OpenCvSharp.Point(centroidX, centroidY);
        }
    }
}