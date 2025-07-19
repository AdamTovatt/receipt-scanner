using OpenCvSharp;

namespace ReceiptScanner.Services.CornerDetection
{
    /// <summary>
    /// Service for processing contours and finding centroids in binary images.
    /// </summary>
    public class ContourProcessingService
    {
        /// <summary>
        /// Finds the centroid of the largest contour in a binary image.
        /// </summary>
        /// <param name="binaryImage">The binary image to process.</param>
        /// <returns>The centroid point, or null if no contours are found.</returns>
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

        /// <summary>
        /// Finds all contours in a binary image.
        /// </summary>
        /// <param name="binaryImage">The binary image to process.</param>
        /// <returns>A list of contour point arrays.</returns>
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

        /// <summary>
        /// Gets the centroid of the largest contour from a list of contours.
        /// </summary>
        /// <param name="contours">The list of contours to process.</param>
        /// <returns>The centroid point of the largest contour, or null if no contours are provided.</returns>
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