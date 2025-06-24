using Microsoft.Extensions.FileProviders;
using System.Reflection;
using System.Text;

namespace ReceiptScanner.Resources
{
    public class ResourceHelper
    {
        private static readonly Lazy<ResourceHelper> instance = new Lazy<ResourceHelper>(() => new ResourceHelper());
        public static ResourceHelper Instance => instance.Value;

        private readonly EmbeddedFileProvider fileProvider;

        private ResourceHelper()
        {
            fileProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
        }

        public string GetContentType(Resource resource)
        {
            string extension = Path.GetExtension(resource.Path).ToLowerInvariant();

            return extension switch
            {
                ".html" => "text/html",
                ".otf" => "font/otf",
                ".ttf" => "font/ttf",
                ".svg" => "image/svg+xml",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".pdf" => "application/pdf",
                ".cer" => "application/x-x509-ca-cert",
                ".onnx" => "application/octet-stream",
                _ => "application/octet-stream"
            };
        }

        public async Task<string> ReadAsStringAsync(Resource resource)
        {
            using (Stream resourceStream = GetFileStream(resource))
            {
                using (StreamReader reader = new StreamReader(resourceStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        public async Task<byte[]> ReadAsBytesAsync(Resource resource)
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

        public Stream GetFileStream(Resource resource)
        {
            return GetFileInfo(resource).CreateReadStream();
        }

        public IFileInfo GetFileInfo(Resource resource)
        {
            string fullPath = $"Resources/{resource.Path}";
            IFileInfo fileInfo = fileProvider.GetFileInfo(fullPath);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"Resource '{fullPath}' not found.");
            }

            return fileInfo;
        }

        public void VerifyResourceMappings()
        {
            StringBuilder errors = new StringBuilder();

            // 1. Ensure all mapped resources exist as embedded files
            HashSet<string> mappedResources = GetAllResourcePaths();
            foreach (string resourcePath in mappedResources)
            {
                string fullPath = $"Resources/{resourcePath}";
                IFileInfo fileInfo = fileProvider.GetFileInfo(fullPath);
                if (!fileInfo.Exists)
                {
                    errors.AppendLine($"❌ Missing Embedded Resource: {fullPath}");
                }
            }

            // 2. Ensure all embedded resources have a corresponding mapping
            HashSet<string> embeddedResources = GetEmbeddedResourcePaths();
            foreach (string embeddedFile in embeddedResources)
            {
                if (!mappedResources.Contains(embeddedFile))
                {
                    errors.AppendLine($"❌ Unmapped Embedded File: {embeddedFile}");
                }
            }

            // Throw exception if there are any mismatches
            if (errors.Length > 0)
            {
                StringBuilder errorMessageBuilder = new StringBuilder($"Resource mapping integrity check failed:\n{errors}");
                errorMessageBuilder.AppendLine("If an embedded resource is missing it means that an embedded resources from the Resources folder has been removed while the mapping for it in the Resource class remains.");
                errorMessageBuilder.AppendLine("If there is an unmapped embedded file it means that there is an embedded resource in the Resources folder that has not been mapped correctly in the Resource class.");
                errorMessageBuilder.AppendLine("This error message and check is to make sure that all expected embedded resources in the Resources folder exist.");
                errorMessageBuilder.AppendLine("It is performed when the tests run and on api startup to ensure no unexpected error can occur in the middle of resonding to a request or similar.");

                throw new InvalidOperationException(errorMessageBuilder.ToString());
            }
        }

        private static HashSet<string> GetAllResourcePaths()
        {
            HashSet<string> paths = new HashSet<string>();

            Type[] categories = typeof(Resource).GetNestedTypes();
            foreach (Type category in categories)
            {
                FieldInfo[] fields = category.GetFields(BindingFlags.Public | BindingFlags.Static);
                foreach (FieldInfo field in fields)
                {
                    object? fieldValue = field.GetValue(null);
                    if (fieldValue is Resource resource)
                    {
                        paths.Add(resource.Path);
                    }
                }
            }

            return paths;
        }

        private HashSet<string> GetAllResourceFolders()
        {
            HashSet<string> folders = new HashSet<string>();

            foreach (Type category in typeof(Resource).GetNestedTypes())
            {
                if (category.GetFields(BindingFlags.Public | BindingFlags.Static).Any(x => x.GetValue(null) is Resource))
                    folders.Add(category.Name);
            }

            return folders;
        }

        private HashSet<string> GetEmbeddedResourcePaths()
        {
            HashSet<string> embeddedResources = new HashSet<string>();

            Assembly assembly = Assembly.GetExecutingAssembly();
            string assemblyName = assembly.GetName().Name!; // Get namespace prefix

            // Get all embedded resources in the assembly
            string[] resourceNames = assembly.GetManifestResourceNames();

            // Collect known folder names from ResourcePath mappings
            HashSet<string> knownFolders = GetAllResourceFolders();

            foreach (string resource in resourceNames)
            {
                if (resource.StartsWith($"{assemblyName}.Resources"))
                {
                    // Remove "ReceiptScanner.Resources." prefix
                    string relativePath = resource.Substring($"{assemblyName}.Resources.".Length);

                    // Split on dots and try to reconstruct folder/file structure
                    string[] parts = relativePath.Split('.');
                    List<string> processedParts = new List<string>();

                    for (int i = 0; i < parts.Length; i++)
                    {
                        string currentPart = parts[i];
                        string constructedCheckPath = string.Join("/", processedParts) + "/" + currentPart;
                        if (processedParts.Count == 0 && constructedCheckPath.Length > 1) constructedCheckPath = constructedCheckPath.Substring(1);

                        // If this is a known folder, we treat it as a directory and use a "/"
                        if (knownFolders.Contains(constructedCheckPath))
                        {
                            processedParts.Add(currentPart);
                        }
                        else
                        {
                            // Assume remaining parts belong to the filename
                            string filename = string.Join(".", parts.Skip(i));
                            processedParts.Add(filename);
                            break;
                        }
                    }

                    string normalizedPath = string.Join("/", processedParts);
                    embeddedResources.Add(normalizedPath);
                }
            }

            return embeddedResources;
        }
    }
} 