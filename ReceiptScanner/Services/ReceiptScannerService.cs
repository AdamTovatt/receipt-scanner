using ReceiptScanner.Models;
using Microsoft.ML.OnnxRuntime;
using EasyReasy;

namespace ReceiptScanner.Services
{
    public class ReceiptScannerService : IReceiptScannerService
    {
        private readonly IModelService _modelService;
        private readonly ResourceManager _resourceManager;
        private PGNetPredictor? _predictor;

        public ReceiptScannerService(IModelService modelService, ResourceManager resourceManager)
        {
            _modelService = modelService;
            _resourceManager = resourceManager;
        }

        public async Task<ReceiptData> ScanReceiptAsync(IFormFile imageFile)
        {
            try
            {
                if (imageFile == null || imageFile.Length == 0)
                {
                    return new ReceiptData { Error = "No image file provided" };
                }

                // Validate file type
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
                string fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return new ReceiptData { Error = "Invalid file type. Please upload a JPG, PNG, or BMP image." };
                }

                // Initialize predictor if not already done
                if (_predictor == null)
                {
                    InferenceSession model = await _modelService.GetModelAsync();
                    byte[] modelBytes = await _modelService.GetModelBytesAsync();
                    _predictor = new PGNetPredictor(modelBytes, _resourceManager);
                }

                // Process the image with OCR
                using Stream imageStream = imageFile.OpenReadStream();
                ReceiptData result = await _predictor.ProcessImageAsync(imageStream);

                return result;
            }
            catch (Exception ex)
            {
                return new ReceiptData { Error = $"Error processing receipt: {ex.Message}" };
            }
        }
    }
} 