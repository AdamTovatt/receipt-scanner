using ReceiptScanner.Models;
using ReceiptScanner.Preprocessing;

namespace ReceiptScanner.Services.Ocr
{
    public interface IOcrService : IDisposable
    {
        Task<OcrResult> ProcessImageAsync(byte[] imageBytes, IPreprocessingPipeline preprocessingPipeline);
    }
}