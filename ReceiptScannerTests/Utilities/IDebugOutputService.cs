using ReceiptScannerTests.Models;

namespace ReceiptScannerTests.Utilities
{
    public interface IDebugOutputService
    {
        void OutputImageWithPoints(OpenCvSharp.Mat image, List<DebugPoint> points, string imageName);
        void OutputImage(OpenCvSharp.Mat image, string imageName);
    }
}