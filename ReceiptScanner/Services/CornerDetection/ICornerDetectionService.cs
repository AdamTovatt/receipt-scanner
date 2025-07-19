using OpenCvSharp;
using ReceiptScanner.Models;

namespace ReceiptScanner.Services.CornerDetection
{
    public interface ICornerDetectionService
    {
        CornerDetectionResult DetectCorners(Mat image);
    }
}