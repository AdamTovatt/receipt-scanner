using EasyReasy;

namespace ReceiptScanner.ResourceProviders
{
    /// <summary>
    /// Provides access to resources via HTTP with local caching.
    /// </summary>
    public class HttpResourceProvider : IResourceProvider
    {
        private readonly HttpClient httpClient;
        private readonly string cacheDirectory;
        private readonly string baseUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResourceProvider"/> class.
        /// </summary>
        /// <param name="baseUrl">The base URL for HTTP requests.</param>
        /// <param name="cacheDirectory">The directory to cache downloaded resources. Defaults to "cache".</param>
        public HttpResourceProvider(string baseUrl, string cacheDirectory = "cache")
        {
            this.baseUrl = baseUrl.TrimEnd('/');
            this.cacheDirectory = cacheDirectory;
            httpClient = new HttpClient();

            // Ensure cache directory exists
            Directory.CreateDirectory(cacheDirectory);
        }

        /// <summary>
        /// Checks if the specified resource exists in the local cache.
        /// </summary>
        /// <param name="resource">The resource to check.</param>
        /// <returns>True if the resource exists in cache; otherwise, false.</returns>
        public async Task<bool> ResourceExistsAsync(Resource resource)
        {
            await Task.CompletedTask;
            string cachePath = GetCachePath(resource);
            return File.Exists(cachePath);
        }

        /// <summary>
        /// Gets a stream for reading the specified HTTP resource, downloading it if not cached.
        /// </summary>
        /// <param name="resource">The resource to read.</param>
        /// <returns>A stream for reading the resource.</returns>
        public async Task<Stream> GetResourceStreamAsync(Resource resource)
        {
            string cachePath = GetCachePath(resource);

            // If not cached, download it
            if (!File.Exists(cachePath))
            {
                await DownloadResourceAsync(resource, cachePath);
            }

            return File.OpenRead(cachePath);
        }

        /// <summary>
        /// Reads the specified HTTP resource as a byte array, downloading it if not cached.
        /// </summary>
        /// <param name="resource">The resource to read.</param>
        /// <returns>The resource content as a byte array.</returns>
        public async Task<byte[]> ReadAsBytesAsync(Resource resource)
        {
            using (Stream resourceStream = await GetResourceStreamAsync(resource))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await resourceStream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Reads the specified HTTP resource as a string, downloading it if not cached.
        /// </summary>
        /// <param name="resource">The resource to read.</param>
        /// <returns>The resource content as a string.</returns>
        public async Task<string> ReadAsStringAsync(Resource resource)
        {
            using (Stream resourceStream = await GetResourceStreamAsync(resource))
            {
                using (StreamReader reader = new StreamReader(resourceStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        private string GetCachePath(Resource resource)
        {
            return Path.Combine(cacheDirectory, resource.Path.Replace('/', Path.DirectorySeparatorChar));
        }

        private async Task DownloadResourceAsync(Resource resource, string cachePath)
        {
            string url = $"{baseUrl}/{resource.Path}";

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

            using (HttpResponseMessage response = await httpClient.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                using (FileStream fileStream = File.Create(cachePath))
                {
                    await contentStream.CopyToAsync(fileStream);
                }
            }
        }

        /// <summary>
        /// Disposes the HTTP client.
        /// </summary>
        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}