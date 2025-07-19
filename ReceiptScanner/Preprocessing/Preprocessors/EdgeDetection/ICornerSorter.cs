using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public interface ICornerSorter
    {
        Point[] SortCorners(Point[] corners);
    }
}