using OpenCvSharp;
using ReceiptScanner.Models;

namespace ReceiptScanner.Services.CornerFinding
{
    public interface ICornerFindingService
    {
        CornerDetectionResult FindCorners(Mat image);
    }
} 