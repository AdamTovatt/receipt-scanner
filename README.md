# Classification API

A .NET 8 Web API for stool classification using ML.NET and ONNX Runtime. This API provides image classification capabilities with a pre-trained EfficientNet-B0 model.

## 🏗️ Project Structure

```
ClassificationApi/
├── Controllers/
│   └── ClassificationController.cs          # API endpoints
├── Models/
│   ├── ClassificationResponse.cs            # Response DTOs
│   └── StoolType.cs                         # Stool type enum
├── Resources/
│   ├── Models/
│   │   └── stool_model.onnx                 # Embedded ONNX model
│   ├── Resource.cs                          # Resource management
│   └── ResourceHelper.cs                    # Resource loading utilities
├── Services/
│   ├── IModelService.cs                     # Model service interface
│   ├── ModelService.cs                      # ONNX model loading
│   ├── IStoolClassificationService.cs       # Classification service interface
│   └── StoolClassificationService.cs        # Image processing & inference
├── Program.cs                               # Application startup
└── ClassificationApi.csproj                 # Project configuration

ClassificationApiTests/                      # Unit tests
├── ResourceHelperTests.cs                   # Resource system tests
└── StoolClassificationServiceTests.cs       # Service tests
```

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code

### Setup
1. Clone the repository
2. Navigate to the project directory
3. Run the application:
   ```bash
   dotnet run --project ClassificationApi
   ```
4. The API will be available at `https://localhost:5001` (or the configured port)

### Running Tests
```bash
dotnet test
```

## 🚀 Deployment

### Raspberry Pi Deployment

For deployment on Raspberry Pi (ARM64), use the following build command:

```bash
dotnet publish -c Release -r linux-arm64 --self-contained true
```

This creates a self-contained deployment that includes the .NET runtime, making it suitable for Raspberry Pi systems.

### Port Configuration

You can specify a custom port when running the application:

```bash
# Using environment variable
ASPNETCORE_URLS=http://localhost:8000 dotnet run --project ClassificationApi

# Or set the environment variable first
export ASPNETCORE_URLS=http://localhost:8000
dotnet run --project ClassificationApi
```

For production deployments, you might want to bind to all interfaces:

```bash
ASPNETCORE_URLS=http://0.0.0.0:8000 dotnet run --project ClassificationApi
```

## API Documentation

### Base URL
```
https://localhost:5001
```
Or whatever you have it hosted at.

### Endpoints

#### GET /

Serves the embedded frontend HTML interface.

#### GET /ping

Tests API connectivity and returns server status.

**Response:**
```json
{
  "message": "API is running",
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

#### POST /predict

Classifies an uploaded image and returns the predicted stool type.

**Request:**
- **Method:** POST
- **Content-Type:** `multipart/form-data`
- **Body:** Form data with an image file

**cURL Example:**
```bash
curl -X POST "https://localhost:5001/predict" \
  -H "Content-Type: multipart/form-data" \
  -F "imageFile=@path/to/your/image.jpg"
```

**JavaScript/Fetch Example:**
```javascript
const formData = new FormData();
formData.append('imageFile', imageFile); // imageFile is a File object

const response = await fetch('https://localhost:5001/predict', {
  method: 'POST',
  body: formData
});

const result = await response.json();
```

**Response Format:**

**Success Response (200 OK):**
```json
{
  "predictedType": "type-1",
  "confidence": 0.95,
  "error": null
}
```

**Error Response (200 OK with error):**
```json
{
  "predictedType": null,
  "confidence": 0.0,
  "error": "Classification failed: Invalid image format"
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `predictedType` | string | The predicted stool type (type-1 through type-7) |
| `confidence` | number | Confidence score between 0.0 and 1.0 |
| `error` | string | Error message if classification failed (null on success) |

### Stool Types

The model can classify images into 7 different stool types:
- `type-1` - Separate hard lumps
- `type-2` - Sausage-like but lumpy
- `type-3` - Sausage-like with cracks
- `type-4` - Sausage-like, smooth and soft
- `type-5` - Soft blobs with clear-cut edges
- `type-6` - Mushy consistency with ragged edges
- `type-7` - Entirely liquid

### Supported Image Formats

The API supports all image formats that SixLabors.ImageSharp can process:
- JPEG (.jpg, .jpeg)
- PNG (.png)
- GIF (.gif)
- BMP (.bmp)
- TIFF (.tiff)
- WebP (.webp)

### Image Processing

- Images are automatically resized to 224x224 pixels
- RGB color space conversion is applied
- ImageNet normalization is performed
- No file size limits (images are processed in memory)

## 🔧 Frontend Integration Guide

### File Upload Implementation

#### HTML Form
```html
<form id="uploadForm" enctype="multipart/form-data">
  <input type="file" id="imageFile" name="imageFile" accept="image/*" required>
  <button type="submit">Classify Image</button>
</form>
<div id="result"></div>
```

#### JavaScript Implementation
```javascript
document.getElementById('uploadForm').addEventListener('submit', async (e) => {
  e.preventDefault();
  
  const fileInput = document.getElementById('imageFile');
  const file = fileInput.files[0];
  
  if (!file) {
    alert('Please select an image file');
    return;
  }
  
  const formData = new FormData();
  formData.append('imageFile', file);
  
  try {
    const response = await fetch('/predict', {
      method: 'POST',
      body: formData
    });
    
    const result = await response.json();
    
    if (result.error) {
      document.getElementById('result').innerHTML = 
        `<p style="color: red;">Error: ${result.error}</p>`;
    } else {
      document.getElementById('result').innerHTML = 
        `<p>Predicted Type: ${result.predictedType}</p>
         <p>Confidence: ${(result.confidence * 100).toFixed(1)}%</p>`;
    }
  } catch (error) {
    document.getElementById('result').innerHTML = 
      `<p style="color: red;">Network error: ${error.message}</p>`;
  }
});
```

#### React Implementation
```jsx
import React, { useState } from 'react';

function ImageClassifier() {
  const [file, setFile] = useState(null);
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const handleFileChange = (e) => {
    setFile(e.target.files[0]);
    setResult(null);
    setError(null);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!file) return;

    setLoading(true);
    setError(null);

    const formData = new FormData();
    formData.append('imageFile', file);

    try {
      const response = await fetch('/predict', {
        method: 'POST',
        body: formData,
      });

      const data = await response.json();

      if (data.error) {
        setError(data.error);
        setResult(null);
      } else {
        setResult(data);
        setError(null);
      }
    } catch (err) {
      setError('Network error: ' + err.message);
      setResult(null);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <form onSubmit={handleSubmit}>
        <input
          type="file"
          accept="image/*"
          onChange={handleFileChange}
          required
        />
        <button type="submit" disabled={loading}>
          {loading ? 'Classifying...' : 'Classify Image'}
        </button>
      </form>

      {error && <p style={{ color: 'red' }}>Error: {error}</p>}
      
      {result && (
        <div>
          <h3>Results:</h3>
          <p>Type: {result.predictedType}</p>
          <p>Confidence: {(result.confidence * 100).toFixed(1)}%</p>
        </div>
      )}
    </div>
  );
}
```

### Error Handling

The API returns errors in the response body rather than HTTP error codes. Always check the `error` field:

```javascript
const response = await fetch('/predict', {
  method: 'POST',
  body: formData
});

const result = await response.json();

if (result.error) {
  // Handle error
  console.error('Classification error:', result.error);
  // Show user-friendly error message
} else {
  // Handle success
  console.log('Prediction:', result.predictedType);
  console.log('Confidence:', result.confidence);
}
```

### Common Error Messages

| Error Message | Cause | Solution |
|---------------|-------|----------|
| "No image file provided or file is empty" | No file uploaded or empty file | Ensure a valid image file is selected |
| "Classification failed: Invalid image format" | Unsupported image format | Use JPEG, PNG, GIF, BMP, TIFF, or WebP |
| "Classification failed: Failed to load ONNX model" | Model loading error | Check server logs, restart application |
| "Network error: ..." | Network connectivity issues | Check API endpoint URL and network connection |

### Performance Considerations

- **Image Size**: Larger images take longer to process due to resizing
- **Concurrent Requests**: The API can handle multiple concurrent requests
- **Model Loading**: Model is loaded once and reused for all requests
- **Memory Usage**: Images are processed in memory, consider file size limits on frontend

### CORS Configuration

If your frontend is on a different domain, you may need to configure CORS in the API:

```csharp
// In Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Your frontend URL
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// In the middleware pipeline
app.UseCors("AllowFrontend");
```

## 🧪 Testing

### Unit Tests
```bash
dotnet test
```

### Manual Testing
1. Start the API: `dotnet run --project ClassificationApi`
2. Use tools like Postman or curl to test the `/predict` endpoint
3. Upload various image formats and sizes to verify functionality

### Test Images
- Use images of different stool types for comprehensive testing
- Test with various image formats (JPEG, PNG, etc.)
- Test with different image sizes and aspect ratios

## 🔧 Configuration

### Model Settings
The model configuration is hardcoded in `StoolClassificationService.cs`:
- Image size: 224x224 pixels
- Number of classes: 7
- Normalization: ImageNet mean/std values

### Resource Management
- Models are stored as embedded resources in `Resources/Models/`
- Use the `Resource` class to add new models
- Run `ResourceHelper.Instance.VerifyResourceMappings()` to validate resources

## 📝 Development Notes

### Adding New Models
1. Add the ONNX model file to `Resources/Models/`
2. Mark the file as "Embedded Resource" in project properties
3. Add a new resource in `Resource.cs`:
   ```csharp
   public static readonly Resource NewModel = new Resource("Models/new_model.onnx");
   ```
4. Update the service to use the new model

### Architecture Patterns
- **Dependency Injection**: Services are registered in `Program.cs`
- **Singleton Pattern**: ModelService is singleton for performance
- **Resource Management**: Embedded resources for deployment simplicity
- **Async/Await**: All operations are async for better performance

## 🤝 Contributing

1. Follow the existing code style (explicit types, no `var`)
2. Add unit tests for new functionality
3. Update this README for any API changes
4. Ensure all tests pass before submitting

## 📄 License

[Add your license information here] 