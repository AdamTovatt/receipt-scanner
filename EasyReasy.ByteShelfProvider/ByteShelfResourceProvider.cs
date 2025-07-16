using System.Text;
using ByteShelfClient;
using ByteShelfCommon;
using EasyReasy;

namespace EasyReasy.ByteShelfProvider
{
    /// <summary>
    /// Provides access to resources via ByteShelf, mapping Resource.Path directories to subtenant hierarchy.
    /// </summary>
    public class ByteShelfResourceProvider : IResourceProvider
    {
        private readonly IShelfFileProvider shelfProvider;
        private readonly string? rootSubTenantId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteShelfResourceProvider"/> class.
        /// </summary>
        /// <param name="baseUrl">The ByteShelf API base URL.</param>
        /// <param name="apiKey">The API key for ByteShelf.</param>
        /// <param name="rootSubTenantId">Optional root subtenant ID to start from.</param>
        public ByteShelfResourceProvider(
            string baseUrl,
            string apiKey,
            string? rootSubTenantId = null)
        {
            HttpClient httpClient = HttpShelfFileProvider.CreateHttpClient(baseUrl);
            shelfProvider = new HttpShelfFileProvider(httpClient, apiKey);
            this.rootSubTenantId = rootSubTenantId;
        }

        public async Task<bool> ResourceExistsAsync(Resource resource)
        {
            (string? subTenantId, string fileName) = await ResolveSubTenantAndFileName(resource.Path);

            IEnumerable<ShelfFileMetadata> files = subTenantId == null
                ? await shelfProvider.GetFilesAsync()
                : await shelfProvider.GetFilesForTenantAsync(subTenantId);

            // Find file by display name (OriginalFilename)
            return files.Any(f => string.Equals(f.OriginalFilename, fileName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<Stream> GetResourceStreamAsync(Resource resource)
        {
            (string? subTenantId, string fileName) = await ResolveSubTenantAndFileName(resource.Path);

            // Find file by display name (OriginalFilename)
            IEnumerable<ShelfFileMetadata> files = subTenantId == null
                ? await shelfProvider.GetFilesAsync()
                : await shelfProvider.GetFilesForTenantAsync(subTenantId);

            ShelfFileMetadata? file = files.FirstOrDefault(f => string.Equals(f.OriginalFilename, fileName, StringComparison.OrdinalIgnoreCase));

            if (file == null)
                throw new FileNotFoundException($"File '{resource.Path}' not found in ByteShelf.");

            ShelfFile shelfFile = subTenantId == null
                ? await shelfProvider.ReadFileAsync(file.Id)
                : await shelfProvider.ReadFileForTenantAsync(subTenantId, file.Id);

            return shelfFile.GetContentStream();
        }

        public async Task<byte[]> ReadAsBytesAsync(Resource resource)
        {
            using (Stream stream = await GetResourceStreamAsync(resource))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    return ms.ToArray();
                }
            }
        }

        public async Task<string> ReadAsStringAsync(Resource resource)
        {
            using (Stream stream = await GetResourceStreamAsync(resource))
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        /// <summary>
        /// Resolves the subtenant hierarchy and file name from a resource path.
        /// Only traverses existing subtenants. Throws if any segment is missing.
        /// </summary>
        private async Task<(string? subTenantId, string fileName)> ResolveSubTenantAndFileName(string resourcePath)
        {
            string[] parts = resourcePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                throw new ArgumentException("Resource path is empty.", nameof(resourcePath));

            string fileName = parts[^1];

            if (parts.Length == 1)
                return (rootSubTenantId, fileName);

            // Traverse subtenant hierarchy by display name, fail if not found
            string? currentSubTenantId = rootSubTenantId;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                string segment = parts[i];

                Dictionary<string, TenantInfoResponse> subTenants = currentSubTenantId == null
                    ? await shelfProvider.GetSubTenantsAsync()
                    : await shelfProvider.GetSubTenantsUnderSubTenantAsync(currentSubTenantId);

                // Find subtenant by display name
                KeyValuePair<string, TenantInfoResponse>? found = subTenants.FirstOrDefault(st => string.Equals(st.Value.DisplayName, segment, StringComparison.OrdinalIgnoreCase));

                if (found.HasValue && !string.IsNullOrEmpty(found.Value.Key))
                {
                    currentSubTenantId = found.Value.Key;
                }
                else
                {
                    throw new DirectoryNotFoundException($"Subtenant '{segment}' not found in path '{resourcePath}'.");
                }
            }

            return (currentSubTenantId, fileName);
        }

        /// <summary>
        /// Creates a PredefinedResourceProvider for the given resource collection type using ByteShelfResourceProvider.
        /// </summary>
        /// <param name="resourceCollectionType">The resource collection type.</param>
        /// <param name="baseUrl">The ByteShelf API base URL.</param>
        /// <param name="apiKey">The API key for ByteShelf.</param>
        /// <returns>A PredefinedResourceProvider for the specified resource collection type.</returns>
        public static PredefinedResourceProvider Create(Type resourceCollectionType, string baseUrl, string apiKey)
        {
            return new PredefinedResourceProvider(
                resourceCollectionType,
                new ByteShelfResourceProvider(baseUrl, apiKey)
            );
        }
    }
}