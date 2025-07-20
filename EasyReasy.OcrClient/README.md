# EasyReasy.OcrClient

A client library for the Receipt Scanner API that provides easy access to OCR functionality.

## Usage

### Basic Usage

```csharp
using EasyReasy.OcrClient;

// Create a client
using OcrClient client = new OcrClient("https://your-api-url.com");

// Scan a receipt with default preprocessing
byte[] imageBytes = File.ReadAllBytes("receipt.jpg");
OcrResult result = await client.ScanReceiptAsync(imageBytes);

if (result.Error != null)
{
    Console.WriteLine($"Error: {result.Error}");
}
else
{
    foreach (TextDetection detection in result.DetectedTexts)
    {
        Console.WriteLine($"Text: {detection.Text}");
    }
}
```

### Using Custom Preprocessors

```csharp
// Get available preprocessors
List<string> availablePreprocessors = await client.GetAvailablePreprocessorsAsync();
Console.WriteLine($"Available preprocessors: {string.Join(", ", availablePreprocessors)}");

// Scan with specific preprocessors
List<string> preprocessors = new List<string> { "Threshold", "RotationCorrection" };
OcrResult result = await client.ScanReceiptAsync(imageBytes, preprocessors);
```

### Using HttpClient Injection

```csharp
using HttpClient httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromMinutes(2);

using OcrClient client = new OcrClient(httpClient);
OcrResult result = await client.ScanReceiptAsync(imageBytes);
```

### Using API Key Authentication

```csharp
// Create client with API key
using OcrClient client = new OcrClient("https://your-api-url.com", "your-api-key");

// Or add API key to existing HttpClient
using HttpClient httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("X-API-Key", "your-api-key");
using OcrClient client = new OcrClient(httpClient);
```

### Health Check

```csharp
bool isHealthy = await client.PingAsync();
Console.WriteLine($"API is healthy: {isHealthy}");
```

## Available Methods

- `ScanReceiptAsync(byte[] imageBytes)` - Scan with default preprocessing
- `ScanReceiptAsync(byte[] imageBytes, List<string> preprocessors)` - Scan with custom preprocessors
- `GetAvailablePreprocessorsAsync()` - Get list of available preprocessors
- `PingAsync()` - Health check for the API

## Authentication

The API supports API key authentication via the `X-API-Key` header. To enable authentication:

1. Set the `RECEIPT_SCANNER_API_KEY` environment variable on the server
2. Include the API key in your requests using the `X-API-Key` header

If no API key is configured on the server, all requests are allowed. The following endpoints are always accessible without authentication:
- `/ping` - Health check
- `/swagger` - API documentation
- `/health` - Health check

## Models

- `OcrResult` - Contains detected texts and any errors
- `TextDetection` - Individual text detection with bounding box
- `Point` - X,Y coordinates for bounding box
- `ScanReceiptRequest` - Request model for specifying preprocessors 