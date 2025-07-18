using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public class ContourFinder : IContourFinder
    {
        public Point[][] FindContours(Mat edgeImage)
        {
            if (edgeImage.Empty())
                throw new ArgumentException("Edge image cannot be empty", nameof(edgeImage));

            // Find contours
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(edgeImage, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            return contours;
        }
    }
} 