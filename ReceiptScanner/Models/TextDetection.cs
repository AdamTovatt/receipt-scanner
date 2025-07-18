namespace ReceiptScanner.Models
{
    public class TextDetection
    {
        public string Text { get; set; } = string.Empty;
        public List<Point> BoundingBox { get; set; } = new List<Point>();
    }
}
