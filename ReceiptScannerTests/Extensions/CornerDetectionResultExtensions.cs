using OpenCvSharp;
using ReceiptScanner.Models;
using ReceiptScannerTests.Models;

namespace ReceiptScannerTests.Extensions
{
    public static class CornerDetectionResultExtensions
    {
        public static List<DebugPoint> CreateCornerDebugPoints(this CornerDetectionResult result)
        {
            List<DebugPoint> debugPoints = new List<DebugPoint>();

            // Define colors for each corner
            Scalar[] colors = new Scalar[]
            {
                new Scalar(0, 0, 255),   // Red for TopLeft
                new Scalar(0, 255, 0),   // Green for TopRight
                new Scalar(255, 0, 0),   // Blue for BottomLeft
                new Scalar(255, 255, 0)  // Cyan for BottomRight
            };

            string[] cornerNames = new string[] { "TopLeft", "TopRight", "BottomLeft", "BottomRight" };
            ReceiptScanner.Models.Point[] corners = new ReceiptScanner.Models.Point[]
            {
                result.TopLeft,
                result.TopRight,
                result.BottomLeft,
                result.BottomRight
            };

            // Create debug points for each corner
            for (int i = 0; i < corners.Length; i++)
            {
                ReceiptScanner.Models.Point corner = corners[i];
                Scalar color = colors[i];
                string name = cornerNames[i];

                debugPoints.Add(new DebugPoint(corner, color, name));
            }

            return debugPoints;
        }
    }
}