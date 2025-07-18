using ReceiptScanner.Models;

namespace ReceiptScanner.Services
{
    public interface IReceiptScannerService
    {
        Task<ReceiptData> ScanReceiptAsync(IFormFile imageFile);
    }
} 