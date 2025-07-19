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

        public ReceiptController(IOcrService ocrService, ResourceManager resourceManager)
        {
            _ocrService = ocrService;
            _resourceManager = resourceManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetFrontend()
        {
            try
            {
                string htmlContent = await _resourceManager.ReadAsStringAsync(Resources.Frontend.ReceiptScannerFrontend);

                // Get the external URL from forwarded headers, but hardcode https
                string scheme = "https"; // Hardcoded to fix mixed content
                string host = Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? Request.Host.Value;
                string prefix = Request.Headers["X-Forwarded-Prefix"].FirstOrDefault() ?? "";

                string requestUrl = $"{scheme}://{host}{prefix}";
                htmlContent = htmlContent.Replace("http://localhost:5001", requestUrl);

                return Content(htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                return NotFound($"Frontend not found: {ex.Message}");
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "Receipt Scanner API is running", timestamp = DateTime.UtcNow });
        }

        [HttpPost("scan")]
        public async Task<OcrResult> ScanReceipt(IFormFile imageFile)
        {
            // Create preprocessing pipeline like in the test
            PreprocessingPipeline preprocessingPipeline = new PreprocessingPipeline();
            preprocessingPipeline.AddPreprocessor(new ThresholdPreprocessor());

            // Convert the uploaded file to byte array
            using MemoryStream memoryStream = new MemoryStream();
            await imageFile.CopyToAsync(memoryStream);
            byte[] imageBytes = memoryStream.ToArray();

            // Process the image using the OCR service
            return await _ocrService.ProcessImageAsync(imageBytes, preprocessingPipeline);
        }
    }
}