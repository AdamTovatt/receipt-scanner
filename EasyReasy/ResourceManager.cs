using System.Reflection;
using System.Text;

namespace EasyReasy
{
    /// <summary>
    /// Manages resource collections and their associated providers.
    /// </summary>
    public class ResourceManager
    {
        private readonly Dictionary<Type, IResourceProvider> providers = new Dictionary<Type, IResourceProvider>();
        private readonly Dictionary<Type, List<Resource>> resourceCollections = new Dictionary<Type, List<Resource>>();
        private readonly Assembly assembly;

        /// <summary>
        /// Creates a new instance of <see cref="ResourceManager"/> for the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to discover resource collections from. If null, uses the executing assembly.</param>
        /// <returns>A new <see cref="ResourceManager"/> instance.</returns>
        public static ResourceManager CreateInstance(Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();
            ResourceManager instance = new ResourceManager(assembly);
            instance.DiscoverResourceCollections();
            instance.VerifyResourceMappings();
            return instance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceManager"/> class.
        /// </summary>
        /// <param name="assembly">The assembly to discover resource collections from.</param>
        private ResourceManager(Assembly assembly)
        {
            this.assembly = assembly;
        }

        /// <summary>
        /// Registers a provider for a specific resource collection type.
        /// </summary>
        /// <typeparam name="T">The resource collection type.</typeparam>
        /// <param name="provider">The provider to register.</param>
        public void RegisterProvider<T>(IResourceProvider provider) where T : class
        {
            providers[typeof(T)] = provider;
        }

        /// <summary>
        /// Reads the specified resource as a string.
        /// </summary>
        /// <param name="resource">The resource to read.</param>
        /// <returns>The resource content as a string.</returns>
        public async Task<string> ReadAsStringAsync(Resource resource)
        {
            IResourceProvider provider = GetProviderForResource(resource);
            return await provider.ReadAsStringAsync(resource);
        }

        /// <summary>
        /// Reads the specified resource as a byte array.
        /// </summary>
        /// <param name="resource">The resource to read.</param>
        /// <returns>The resource content as a byte array.</returns>
        public async Task<byte[]> ReadAsBytesAsync(Resource resource)
        {
            IResourceProvider provider = GetProviderForResource(resource);
            return await provider.ReadAsBytesAsync(resource);
        }

        /// <summary>
        /// Gets a stream for reading the specified resource.
        /// </summary>
        /// <param name="resource">The resource to read.</param>
        /// <returns>A stream for reading the resource.</returns>
        public async Task<Stream> GetResourceStreamAsync(Resource resource)
        {
            IResourceProvider provider = GetProviderForResource(resource);
            return await provider.GetResourceStreamAsync(resource);
        }

        /// <summary>
        /// Gets the content type for the specified resource.
        /// </summary>
        /// <param name="resource">The resource to get the content type for.</param>
        /// <returns>The content type of the resource.</returns>
        public string GetContentType(Resource resource)
        {
            return resource.GetContentType();
        }

        /// <summary>
        /// Checks if the specified resource exists.
        /// </summary>
        /// <param name="resource">The resource to check.</param>
        /// <returns>True if the resource exists; otherwise, false.</returns>
        public async Task<bool> ResourceExistsAsync(Resource resource)
        {
            IResourceProvider provider = GetProviderForResource(resource);
            return await provider.ResourceExistsAsync(resource);
        }

        /// <summary>
        /// Verifies that all mapped resources exist and are accessible.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when resource mapping integrity check fails.</exception>
        public void VerifyResourceMappings()
        {
            StringBuilder errors = new StringBuilder();

            foreach (KeyValuePair<Type, List<Resource>> collection in resourceCollections)
            {
                Type collectionType = collection.Key;
                List<Resource> resources = collection.Value;

                if (!providers.ContainsKey(collectionType))
                {
                    errors.AppendLine($"❌ No provider registered for resource collection: {collectionType.Name}");
                    continue;
                }

                IResourceProvider provider = providers[collectionType];

                foreach (Resource resource in resources)
                {
                    try
                    {
                        bool exists = provider.ResourceExistsAsync(resource).Result;
                        if (!exists)
                        {
                            errors.AppendLine($"❌ Resource not found: {resource.Path} (Collection: {collectionType.Name})");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.AppendLine($"❌ Error checking resource {resource.Path}: {ex.Message}");
                    }
                }
            }

            if (errors.Length > 0)
            {
                throw new InvalidOperationException($"Resource mapping integrity check failed:\n{errors}");
            }
        }

        private void DiscoverResourceCollections()
        {
            foreach (Type type in assembly.GetTypes())
            {
                ResourceCollectionAttribute? attribute = type.GetCustomAttribute<ResourceCollectionAttribute>();
                if (attribute != null)
                {
                    // Find all static Resource fields in this type
                    List<Resource> resources = new List<Resource>();

                    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (field.FieldType == typeof(Resource))
                        {
                            object? value = field.GetValue(null);
                            if (value is Resource resource)
                            {
                                resources.Add(resource);
                            }
                        }
                    }

                    resourceCollections[type] = resources;

                    // Auto-register the provider if it's a concrete type
                    if (attribute.ProviderType.IsClass && !attribute.ProviderType.IsAbstract)
                    {
                        IResourceProvider provider = (IResourceProvider)Activator.CreateInstance(attribute.ProviderType)!;
                        providers[type] = provider;
                    }
                }
            }
        }

        private IResourceProvider GetProviderForResource(Resource resource)
        {
            // Find which collection this resource belongs to
            foreach (KeyValuePair<Type, List<Resource>> collection in resourceCollections)
            {
                if (collection.Value.Contains(resource))
                {
                    if (providers.TryGetValue(collection.Key, out IResourceProvider? provider))
                    {
                        return provider;
                    }

                    throw new InvalidOperationException($"No provider registered for resource collection: {collection.Key.Name}");
                }
            }

            throw new InvalidOperationException($"Resource not found in any collection: {resource.Path}");
        }
    }
}