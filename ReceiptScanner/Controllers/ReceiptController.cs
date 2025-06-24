using ReceiptScanner.Models;
using ReceiptScanner.Resources;
using ReceiptScanner.Services;
using Microsoft.AspNetCore.Mvc;

namespace ReceiptScanner.Controllers
{
    [ApiController]
    [Route("")]
    public class ReceiptController : ControllerBase
    {
        private readonly IReceiptScannerService _receiptService;

        public ReceiptController(IReceiptScannerService receiptService)
        {
            _receiptService = receiptService;
        }

        [HttpGet]
        public IActionResult GetFrontend()
        {
            try
            {
                string htmlContent = ResourceHelper.Instance.ReadAsStringAsync(Resource.Frontend.ReceiptScannerFrontend).Result;
                
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