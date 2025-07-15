using Microsoft.ML.OnnxRuntime;

namespace ReceiptScanner.Services
{
    public class ModelService : IModelService
    {
        private InferenceSession? _model;
        private byte[]? _modelBytes;
        private readonly SemaphoreSlim _lockObject = new SemaphoreSlim(1, 1);

        public bool IsModelLoaded => _model != null;

        public async Task<InferenceSession> GetModelAsync()
        {
            if (_model != null)
            {
                return _model;
            }

            await _lockObject.WaitAsync();
            try
            {
                // Double-check pattern after acquiring lock
                if (_model != null)
                {
                    return _model;
                }

                byte[] modelBytes = await ResourceHelper.Instance.ReadAsBytesAsync(Resources.Models.ReceiptModel);
                _modelBytes = modelBytes;
                _model = new InferenceSession(modelBytes);
                return _model;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load receipt model: {ex.Message}", ex);
            }
            finally
            {
                _lockObject.Release();
            }
        }

        public async Task<byte[]> GetModelBytesAsync()
        {
            if (_modelBytes != null)
            {
                return _modelBytes;
            }

            // If model bytes not loaded yet, load them
            await GetModelAsync();
            return _modelBytes!;
        }

        public void Dispose()
        {
            _model?.Dispose();
            _lockObject?.Dispose();
        }
    }
} 