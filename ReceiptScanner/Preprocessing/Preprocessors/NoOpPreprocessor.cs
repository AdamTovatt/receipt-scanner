using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors
{
    public class NoOpPreprocessor : IImagePreprocessor
    {
        public string Name => "NoOp";

        public Mat Preprocess(Mat image)
        {
            return image.Clone();
        }
    }
} 