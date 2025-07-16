using ReceiptScanner.Models;
using ReceiptScanner.Services;
using Microsoft.AspNetCore.Mvc;
using EasyReasy;

namespace ReceiptScanner.Controllers
{
    [ApiController]
    [Route("")]
    public class ReceiptController : ControllerBase
    {
        private readonly IReceiptScannerService _receiptService;
        private readonly ResourceManager _resourceManager;

        public ReceiptController(IReceiptScannerService receiptService, ResourceManager resourceManager)
        {
            _receiptService = receiptService;
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
        public async Task<ReceiptData> ScanReceipt(IFormFile imageFile)
        {
            return await _receiptService.ScanReceiptAsync(imageFile);
        }
    }
} 