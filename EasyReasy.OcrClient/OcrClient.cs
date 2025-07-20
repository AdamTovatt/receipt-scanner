using System.Text;
using System.Text.Json;
using EasyReasy.OcrClient.Models;

namespace EasyReasy.OcrClient
{
    public class OcrClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private bool _disposed = false;

        public OcrClient(string baseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
        }

        public OcrClient(string baseUrl, string apiKey)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }
        }

        public OcrClient(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
            }
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }

        public async Task<OcrResult> ScanReceiptAsync(byte[] imageBytes, List<string> preprocessors)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OcrClient));
            }

            if (imageBytes == null || imageBytes.Length == 0)
            {
                throw new ArgumentException("Image bytes cannot be null or empty", nameof(imageBytes));
            }

            // Create multipart form data
            using MultipartFormDataContent formData = new MultipartFormDataContent();
            
            // Add the image file
            using StreamContent imageContent = new StreamContent(new MemoryStream(imageBytes));
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            formData.Add(imageContent, "imageFile", "receipt.jpg");

            // Add the request body as JSON
            ScanReceiptRequest request = new ScanReceiptRequest
            {
                Preprocessors = preprocessors ?? new List<string>()
            };
            
            string requestJson = JsonSerializer.Serialize(request);
            using StringContent requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
            formData.Add(requestContent, "request");

            // Make the request
            HttpResponseMessage response = await _httpClient.PostAsync("/scan-with-preprocessors", formData);
            
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"HTTP request failed with status {response.StatusCode}: {errorContent}");
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            OcrResult? result = JsonSerializer.Deserialize<OcrResult>(responseContent);
            
            return result ?? new OcrResult { Error = "Failed to deserialize response" };
        }

        public async Task<OcrResult> ScanReceiptAsync(byte[] imageBytes)
        {
            return await ScanReceiptAsync(imageBytes, new List<string>());
        }

        public async Task<List<string>> GetAvailablePreprocessorsAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OcrClient));
            }

            HttpResponseMessage response = await _httpClient.GetAsync("/preprocessors");
            
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"HTTP request failed with status {response.StatusCode}: {errorContent}");
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            if (jsonElement.TryGetProperty("preprocessors", out JsonElement preprocessorsElement))
            {
                return JsonSerializer.Deserialize<List<string>>(preprocessorsElement.GetRawText()) ?? new List<string>();
            }
            
            return new List<string>();
        }

        public async Task<bool> PingAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OcrClient));
            }

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("/ping");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
} 