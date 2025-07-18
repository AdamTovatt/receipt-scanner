using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors
{
    public class ThresholdPreprocessor : IImagePreprocessor
    {
        private readonly double _threshold;
        private readonly double _maxValue;

        public ThresholdPreprocessor(double threshold = 180, double maxValue = 255)
        {
            _threshold = threshold;
            _maxValue = maxValue;
        }

        public string Name => "Threshold";

        public Mat Preprocess(Mat image)
        {
            // Convert to grayscale
            Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            // Apply threshold
            Mat binary = new Mat();
            Cv2.Threshold(gray, binary, _threshold, _maxValue, ThresholdTypes.Binary);

            // Clean up intermediate image
            gray.Dispose();

            return binary;
        }
    }
} 