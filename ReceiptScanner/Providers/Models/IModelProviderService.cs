namespace ReceiptScanner.Providers.Models
{
    public interface IModelProviderService : IDisposable
    {
        Task<string> GetModelPathAsync();
    }
}