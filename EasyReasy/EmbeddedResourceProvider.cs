using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Reflection;

namespace EasyReasy
{
    /// <summary>
    /// Provides access to embedded resources within an assembly.
    /// </summary>
    public class EmbeddedResourceProvider : IResourceProvider
    {
        private readonly EmbeddedFileProvider fileProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedResourceProvider"/> class.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resources. If null, uses the executing assembly.</param>
        public EmbeddedResourceProvider(Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();
            fileProvider = new EmbeddedFileProvider(assembly);
        }

        /// <summary>
        /// Checks if the specified resource exists in the embedded assembly.
        /// </summary>
        /// <param name="resource">The resource to check.</param>
        /// <returns>True if the resource exists; otherwise, false.</returns>
        public async Task<bool> ResourceExistsAsync(Resource resource)
        {
            await Task.CompletedTask;
            IFileInfo fileInfo = GetFileInfo(resource);
            return fileInfo.Exists;
        }

        /// <summary>
        /// Gets a stream for reading the specified embedded resource.
        /// </summary>
        /// <param name="resource">The resource to read.</param>
        /// <returns>A stream for reading the resource.</returns>
        public async Task<Stream> GetResourceStreamAsync(Resource resource)
        {
            await Task.CompletedTask;
            IFileInfo fileInfo = GetFileInfo(resource);

            if (!fileInfo.Exists)
                throw new FileNotFoundException($"Resource '{resource.Path}' not found.");

            return fileInfo.CreateReadStream();
        }

        /// <summary>
        /// Reads the specified embedded resource as a byte array.
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
        /// Reads the specified embedded resource as a string.
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

        private IFileInfo GetFileInfo(Resource resource)
        {
            string path = resource.Path;
            IFileInfo fileInfo = fileProvider.GetFileInfo(path);

            if (fileInfo.Exists)
            {
                return fileInfo;
            }

            return fileProvider.GetFileInfo($"Resources/{path}");
        }
    }
}