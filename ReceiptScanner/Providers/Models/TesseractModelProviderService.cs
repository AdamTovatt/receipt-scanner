using EasyReasy;

namespace ReceiptScanner.Providers.Models
{
    public class TesseractModelProviderService : IModelProviderService
    {
        private readonly SemaphoreSlim _lockObject = new SemaphoreSlim(1, 1);
        private readonly ResourceManager _resourceManager;
        private readonly Resource[] _includedModels;
        private string? _cachedModelPath;

        public TesseractModelProviderService(ResourceManager resourceManager, params Resource[] includedModels)
        {
            if (includedModels.Length == 0)
                throw new ArgumentException("At least one included model should be provided", nameof(includedModels));

            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _includedModels = includedModels;
        }

        public async Task<string> GetModelPathAsync()
        {
            // Return cached path if available
            if (_cachedModelPath != null)
            {
                return _cachedModelPath;
            }

            await _lockObject.WaitAsync();
            try
            {
                // Double-check pattern after acquiring lock
                if (_cachedModelPath != null)
                {
                    return _cachedModelPath;
                }

                // Load all required models
                foreach (Resource model in _includedModels)
                {
                    await _resourceManager.GetResourceStreamAsync(model);
                }

                // Get the provider for the first model to determine cache location
                IResourceProvider modelProvider = _resourceManager.GetProviderForResource(_includedModels[0]);
                ICacheableResourceProvider? cacheableResourceProvider = modelProvider as ICacheableResourceProvider;

                if (cacheableResourceProvider == null)
                {
                    throw new InvalidOperationException($"Resource provider for {_includedModels[0]} is not cacheable");
                }

                FileSystemCache? fileSystemCache = cacheableResourceProvider.GetCache() as FileSystemCache;

                if (fileSystemCache == null)
                {
                    throw new InvalidOperationException($"Cache for {_includedModels[0]} is not a FileSystemCache");
                }

                _cachedModelPath = fileSystemCache.GetStorageDirectoryForFile(_includedModels[0]);
                return _cachedModelPath;
            }
            finally
            {
                _lockObject.Release();
            }
        }

        public void Dispose()
        {
            _lockObject?.Dispose();
        }
    }
}