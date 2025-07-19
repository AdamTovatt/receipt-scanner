namespace ReceiptScanner.Models
{
    public class CornerDetectionResult
    {
        public Point TopLeft { get; set; }
        public Point TopRight { get; set; }
        public Point BottomLeft { get; set; }
        public Point BottomRight { get; set; }

        public double Confidence { get; set; }

        public bool CornersFound => Confidence >= 0.5;

        public CornerDetectionResult()
        {
            TopLeft = new Point();
            TopRight = new Point();
            BottomLeft = new Point();
            BottomRight = new Point();
            Confidence = 0.0;
        }

        public CornerDetectionResult(Point topLeft, Point topRight, Point bottomLeft, Point bottomRight, double confidence)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
            Confidence = confidence;
        }
    }
}