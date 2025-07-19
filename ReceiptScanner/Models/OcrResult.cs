namespace ReceiptScanner.Models
{
    public class OcrResult
    {
        public List<TextDetection> DetectedTexts { get; set; } = new List<TextDetection>();
        public string? Error { get; set; }
    }
}