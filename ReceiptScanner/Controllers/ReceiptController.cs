using EasyReasy;
using Microsoft.AspNetCore.Mvc;
using ReceiptScanner.Models;
using ReceiptScanner.Preprocessing;
using ReceiptScanner.Preprocessing.Preprocessors;
using ReceiptScanner.Services.Ocr;

namespace ReceiptScanner.Controllers
{
    [ApiController]
    [Route("")]
    public class ReceiptController : ControllerBase
    {
        private readonly IOcrService _ocrService;
        private readonly ResourceManager _resourceManager;
        private readonly IEnumerable<IImagePreprocessor> _preprocessors;

        public ReceiptController(IOcrService ocrService, ResourceManager resourceManager, IEnumerable<IImagePreprocessor> preprocessors)
        {
            _ocrService = ocrService;
            _resourceManager = resourceManager;
            _preprocessors = preprocessors;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "Receipt Scanner API is running", timestamp = DateTime.UtcNow });
        }

        [HttpPost("scan-with-preprocessors")]
        public async Task<OcrResult> ScanReceiptWithPreprocessors(IFormFile imageFile, [FromBody] ScanReceiptRequest request)
        {
            // Create preprocessing pipeline based on requested preprocessors
            PreprocessingPipeline preprocessingPipeline = new PreprocessingPipeline();
            
            // Create a dictionary of available preprocessors by name
            Dictionary<string, IImagePreprocessor> availablePreprocessors = _preprocessors.ToDictionary(p => p.Name, p => p);
            
            // Add requested preprocessors to the pipeline
            foreach (string preprocessorName in request.Preprocessors)
            {
                if (availablePreprocessors.TryGetValue(preprocessorName, out IImagePreprocessor? preprocessor))
                {
                    preprocessingPipeline.AddPreprocessor(preprocessor);
                }
                else
                {
                    return new OcrResult 
                    { 
                        Error = $"Unknown preprocessor: {preprocessorName}. Available preprocessors: {string.Join(", ", availablePreprocessors.Keys)}" 
                    };
                }
            }

            // Convert the uploaded file to byte array
            using MemoryStream memoryStream = new MemoryStream();
            await imageFile.CopyToAsync(memoryStream);
            byte[] imageBytes = memoryStream.ToArray();

            // Process the image using the OCR service
            return await _ocrService.ProcessImageAsync(imageBytes, preprocessingPipeline);
        }

        [HttpGet("preprocessors")]
        public IActionResult GetAvailablePreprocessors()
        {
            List<string> availablePreprocessors = _preprocessors.Select(p => p.Name).ToList();
            return Ok(new { preprocessors = availablePreprocessors });
        }
    }
}