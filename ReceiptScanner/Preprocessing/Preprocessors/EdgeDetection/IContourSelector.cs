using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public interface IContourSelector
    {
        Point[]? SelectBestContour(Point[][] contours, int imageWidth, int imageHeight);
    }
} 