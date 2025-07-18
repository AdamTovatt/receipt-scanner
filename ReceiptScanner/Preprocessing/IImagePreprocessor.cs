using OpenCvSharp;

namespace ReceiptScanner.Preprocessing
{
    public interface IImagePreprocessor
    {
        Mat Preprocess(Mat image);
        string Name { get; }
    }
}