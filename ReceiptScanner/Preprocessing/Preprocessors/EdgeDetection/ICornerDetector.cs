using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public interface ICornerDetector
    {
        Point[]? DetectCorners(Point[] contour);
    }
}