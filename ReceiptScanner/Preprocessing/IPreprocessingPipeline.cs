using OpenCvSharp;

namespace ReceiptScanner.Preprocessing
{
    public interface IPreprocessingPipeline
    {
        void AddPreprocessor(IImagePreprocessor preprocessor);
        Mat Preprocess(Mat input);
    }
}
