﻿using System.Text.Json.Serialization;

namespace ReceiptScanner.Models
{
    public class TextDetection
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("boundingBox")]
        public List<Point> BoundingBox { get; set; } = new List<Point>();
    }
}
