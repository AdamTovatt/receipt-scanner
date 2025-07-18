using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public class CannyEdgeDetector : IEdgeDetector
    {
        private readonly EdgeDetectionConfig _config;

        public CannyEdgeDetector(EdgeDetectionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public Mat DetectEdges(Mat preprocessedImage)
        {
            if (preprocessedImage.Empty())
                throw new ArgumentException("Preprocessed image cannot be empty", nameof(preprocessedImage));

            // Apply Canny edge detection
            Mat edges = new Mat();
            Cv2.Canny(preprocessedImage, edges, _config.CannyThreshold1, _config.CannyThreshold2);

            return edges;
        }
    }
} 