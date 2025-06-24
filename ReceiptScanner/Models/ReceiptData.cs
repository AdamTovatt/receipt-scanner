namespace ReceiptScanner.Models
{
    public class ReceiptData
    {
        public List<TextDetection> DetectedTexts { get; set; } = new List<TextDetection>();
        public string? Error { get; set; }
    }
} 