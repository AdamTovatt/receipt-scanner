using OpenCvSharp;

namespace ReceiptScannerTests.Models
{
    public class DebugPoint
    {
        public ReceiptScanner.Models.Point Point { get; set; }
        public Scalar Color { get; set; }
        public string Text { get; set; }

        public DebugPoint(ReceiptScanner.Models.Point point, Scalar color, string text)
        {
            Point = point;
            Color = color;
            Text = text;
        }

        public DebugPoint(ReceiptScanner.Models.Point point, Scalar color) : this(point, color, string.Empty)
        {
        }
    }
} 