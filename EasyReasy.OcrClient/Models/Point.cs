using System.Text.Json.Serialization;

namespace EasyReasy.OcrClient.Models
{
    public class Point
    {
        [JsonPropertyName("x")]
        public int X { get; set; }
        
        [JsonPropertyName("y")]
        public int Y { get; set; }
    }
} 