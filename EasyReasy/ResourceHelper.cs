namespace EasyReasy
{
    /// <summary>
    /// Provides a simplified interface for accessing resources through the resource manager.
    /// </summary>
    public class ResourceHelper
    {
        private static readonly Lazy<ResourceHelper> instance = new Lazy<ResourceHelper>(() => new ResourceHelper());
        
        /// <summary>
        /// Gets the singleton instance of the resource helper.
        /// </summary>
        public static ResourceHelper Instance => instance.Value;

        private ResourceHelper()
        {
        }

        /// <summary>
        /// Gets the content type for the specified resource.
        /// </summary>
        /// <param name="resource">The resource to get the content type for.</param>
        /// <returns>The content type of the resource.</returns>
        public string GetContentType(Resource resource)
        {
            return ResourceManager.Instance.GetContentType(resource);
        }

        /// <summary>
        /// Reads the specified resource as a string.
        /// </summary>
        /// <param name="resource">The resource to read.</param>
        /// <returns>The resource content as a string.</returns>
        public async Task<string> ReadAsStringAsync(Resource resource)
        {
            return await ResourceManager.Instance.ReadAsStringAsync(resource);
        }

        /// <summary>
        /// Reads the specified resource as a byte array.
        /// </summary>
        /// <param name="resource">The resource to read.</param>
        /// <returns>The resource content as a byte array.</returns>
        public async Task<byte[]> ReadAsBytesAsync(Resource resource)
        {
            return await ResourceManager.Instance.ReadAsBytesAsync(resource);
        }

        /// <summary>
        /// Gets a stream for reading the specified resource.
        /// </summary>
        /// <param name="resource">The resource to read.</param>
        /// <returns>A stream for reading the resource.</returns>
        public async Task<Stream> GetFileStreamAsync(Resource resource)
        {
            return await ResourceManager.Instance.GetResourceStreamAsync(resource);
        }

        /// <summary>
        /// Checks if the specified resource exists.
        /// </summary>
        /// <param name="resource">The resource to check.</param>
        /// <returns>True if the resource exists; otherwise, false.</returns>
        public async Task<bool> ResourceExistsAsync(Resource resource)
        {
            return await ResourceManager.Instance.ResourceExistsAsync(resource);
        }

        /// <summary>
        /// Verifies that all mapped resources exist and are accessible.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when resource mapping integrity check fails.</exception>
        public void VerifyResourceMappings()
        {
            ResourceManager.Instance.VerifyResourceMappings();
        }

        /// <summary>
        /// Gets a stream for reading the specified resource (synchronous version).
        /// </summary>
        /// <param name="resource">The resource to read.</param>
        /// <returns>A stream for reading the resource.</returns>
        public Stream GetFileStream(Resource resource)
        {
            return GetFileStreamAsync(resource).Result;
        }
    }
} 