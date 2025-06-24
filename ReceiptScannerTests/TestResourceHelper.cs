using Microsoft.Extensions.FileProviders;
using System.Reflection;
using System.Text;

namespace ReceiptScannerTests
{
    public class TestResourceHelper
    {
        private static readonly Lazy<TestResourceHelper> instance = new Lazy<TestResourceHelper>(() => new TestResourceHelper());
        public static TestResourceHelper Instance => instance.Value;

        private readonly EmbeddedFileProvider fileProvider;

        private TestResourceHelper()
        {
            fileProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
        }

        public async Task<string> ReadAsStringAsync(TestResource resource)
        {
            using (Stream resourceStream = GetFileStream(resource))
            {
                using (StreamReader reader = new StreamReader(resourceStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        public async Task<byte[]> ReadAsBytesAsync(TestResource resource)
        {
            using (Stream resourceStream = GetFileStream(resource))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await resourceStream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        public Stream GetFileStream(TestResource resource)
        {
            return GetFileInfo(resource).CreateReadStream();
        }

        public IFileInfo GetFileInfo(TestResource resource)
        {
            string fullPath = resource.Path;
            IFileInfo fileInfo = fileProvider.GetFileInfo(fullPath);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"Test resource '{fullPath}' not found.");
            }

            return fileInfo;
        }
    }
} 