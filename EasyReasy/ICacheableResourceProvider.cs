namespace EasyReasy
{
    /// <summary>
    /// Represents a resource provider that supports caching.
    /// </summary>
    public interface ICacheableResourceProvider
    {
        /// <summary>
        /// Gets the cache instance used by this resource provider.
        /// </summary>
        /// <returns>The cache instance, or null if no cache is configured.</returns>
        IResourceCache? GetCache();
    }
}