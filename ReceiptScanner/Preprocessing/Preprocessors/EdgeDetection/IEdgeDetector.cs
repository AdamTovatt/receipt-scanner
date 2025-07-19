using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public interface IEdgeDetector
    {
        Mat DetectEdges(Mat preprocessedImage);
    }
}