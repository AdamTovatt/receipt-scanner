using System.Text.Json.Serialization;

namespace EasyReasy.OcrClient.Models
{
    public class ScanReceiptRequest
    {
        [JsonPropertyName("preprocessors")]
        public List<string> Preprocessors { get; set; } = new List<string>();
    }
} 