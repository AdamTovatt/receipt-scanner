using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public class GrayscaleBlurPreprocessor : IImagePreprocessor
    {
        private readonly EdgeDetectionConfig _config;

        public GrayscaleBlurPreprocessor(EdgeDetectionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public string Name => "GrayscaleBlur";

        public Mat Preprocess(Mat image)
        {
            if (image.Empty())
                throw new ArgumentException("Image cannot be empty", nameof(image));

            // Convert to grayscale
            Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            // Apply Gaussian blur to reduce noise
            Mat blurred = new Mat();
            Cv2.GaussianBlur(gray, blurred, new Size(_config.GaussianBlurKernelSize, _config.GaussianBlurKernelSize), _config.GaussianBlurSigma);

            // Clean up intermediate image
            gray.Dispose();

            return blurred;
        }
    }
} 