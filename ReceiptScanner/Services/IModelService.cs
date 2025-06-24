using Microsoft.ML.OnnxRuntime;

namespace ReceiptScanner.Services
{
    public interface IModelService
    {
        Task<InferenceSession> GetModelAsync();
        Task<byte[]> GetModelBytesAsync();
        bool IsModelLoaded { get; }
    }
} 