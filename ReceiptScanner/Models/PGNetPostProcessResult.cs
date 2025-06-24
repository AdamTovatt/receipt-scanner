namespace ReceiptScanner.Models
{
    public class PGNetPostProcessResult
    {
        public List<Point[]> BoundingBoxes { get; set; } = new List<Point[]>();
        public List<string> Texts { get; set; } = new List<string>();
    }
} 