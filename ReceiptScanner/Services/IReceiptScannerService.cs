using ReceiptScanner.Models;
using Microsoft.AspNetCore.Http;

namespace ReceiptScanner.Services
{
    public interface IReceiptScannerService
    {
        Task<ReceiptData> ScanReceiptAsync(IFormFile imageFile);
    }
} 