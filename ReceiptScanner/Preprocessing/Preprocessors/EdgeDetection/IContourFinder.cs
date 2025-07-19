using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public interface IContourFinder
    {
        Point[][] FindContours(Mat edgeImage);
    }
}