using System.Text.Json.Serialization;

namespace EasyReasy.OcrClient.Models
{
    public class OcrResult
    {
        [JsonPropertyName("detectedTexts")]
        public List<TextDetection> DetectedTexts { get; set; } = new List<TextDetection>();
        
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
} 