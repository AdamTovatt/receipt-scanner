using ByteShelfClient;
using ByteShelfCommon;

namespace EasyReasy.ByteShelfProvider.Tests
{
    [TestClass]
    public class ByteShelfResourceProviderIntegrationTests
    {
        private const string BaseUrl = "https://localhost:7249/";
        private const string ApiKey = "easyreasy";
        private static string? sub1Id;
        private static string? sub2Id;
        private static string? sub2aId;
        private static string? sub3Id;
        private static string? sub4Id;
        private static string? sub4aId;

        private static readonly List<Guid> createdFileIds = new List<Guid>();
        private static readonly List<string> createdSubTenantIds = new List<string>();
        private static IShelfFileProvider? classShelfProvider;

        // Add a simple mock cache for testing
        private class MockResourceCache : IResourceCache
        {
            private readonly Dictionary<string, byte[]> cache = new();

            public Task<bool> ExistsAsync(string resourcePath)
            {
                return Task.FromResult(cache.ContainsKey(resourcePath));
            }

            public Task<Stream> GetStreamAsync(string resourcePath)
            {
                if (!cache.ContainsKey(resourcePath))
                    throw new FileNotFoundException($"Resource '{resourcePath}' not found in cache.");
                
                return Task.FromResult<Stream>(new MemoryStream(cache[resourcePath]));
            }

            public Task StoreAsync(string resourcePath, Stream content)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    content.CopyTo(ms);
                    cache[resourcePath] = ms.ToArray();
                }
                return Task.CompletedTask;
            }

            public Task StoreAsync(string resourcePath, byte[] content)
            {
                cache[resourcePath] = content;
                return Task.CompletedTask;
            }

            public Task StoreAsync(string resourcePath, string content)
            {
                cache[resourcePath] = System.Text.Encoding.UTF8.GetBytes(content);
                return Task.CompletedTask;
            }

            public Task<DateTimeOffset?> GetCreationTimeAsync(string resourcePath)
            {
                return Task.FromResult<DateTimeOffset?>(null);
            }
        }

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
            // Initialize ByteShelf client
            HttpClient httpClient = HttpShelfFileProvider.CreateHttpClient(BaseUrl);
            classShelfProvider = new HttpShelfFileProvider(httpClient, ApiKey);

            // 1. rootfile.txt at root
            using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Root file content")))
            {
                Guid fileId = await classShelfProvider.WriteFileAsync("rootfile.txt", "text/plain", ms);
                createdFileIds.Add(fileId);
            }

            // 2. Sub1/file1.txt
            sub1Id = await classShelfProvider.CreateSubTenantAsync("Sub1");
            createdSubTenantIds.Add(sub1Id);
            using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Sub1 file1 content")))
            {
                Guid fileId = await classShelfProvider.WriteFileForTenantAsync(sub1Id, "file1.txt", "text/plain", ms);
                createdFileIds.Add(fileId);
            }

            // 3. Sub2/Sub2A/file2a.txt
            sub2Id = await classShelfProvider.CreateSubTenantAsync("Sub2");
            createdSubTenantIds.Add(sub2Id);
            sub2aId = await classShelfProvider.CreateSubTenantUnderSubTenantAsync(sub2Id, "Sub2A");
            createdSubTenantIds.Add(sub2aId);
            using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Sub2A file2a content")))
            {
                Guid fileId = await classShelfProvider.WriteFileForTenantAsync(sub2aId, "file2a.txt", "text/plain", ms);
                createdFileIds.Add(fileId);
            }

            // 4. Sub3/file3a.txt and Sub3/file3b.txt
            sub3Id = await classShelfProvider.CreateSubTenantAsync("Sub3");
            createdSubTenantIds.Add(sub3Id);
            using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Sub3 file3a content")))
            {
                Guid fileId = await classShelfProvider.WriteFileForTenantAsync(sub3Id, "file3a.txt", "text/plain", ms);
                createdFileIds.Add(fileId);
            }
            using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Sub3 file3b content")))
            {
                Guid fileId = await classShelfProvider.WriteFileForTenantAsync(sub3Id, "file3b.txt", "text/plain", ms);
                createdFileIds.Add(fileId);
            }

            // 5. Sub4/Sub4A/file4a1.txt and Sub4/Sub4A/file4a2.txt
            sub4Id = await classShelfProvider.CreateSubTenantAsync("Sub4");
            createdSubTenantIds.Add(sub4Id);
            sub4aId = await classShelfProvider.CreateSubTenantUnderSubTenantAsync(sub4Id, "Sub4A");
            createdSubTenantIds.Add(sub4aId);
            using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Sub4A file4a1 content")))
            {
                Guid fileId = await classShelfProvider.WriteFileForTenantAsync(sub4aId, "file4a1.txt", "text/plain", ms);
                createdFileIds.Add(fileId);
            }
            using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Sub4A file4a2 content")))
            {
                Guid fileId = await classShelfProvider.WriteFileForTenantAsync(sub4aId, "file4a2.txt", "text/plain", ms);
                createdFileIds.Add(fileId);
            }
        }

        [ClassCleanup]
        public static async Task ClassCleanup()
        {
            if (classShelfProvider == null)
                return;

            // Delete all created files
            foreach (Guid fileId in createdFileIds)
            {
                try { await classShelfProvider.DeleteFileAsync(fileId); } catch { }
            }

            // Delete all created subtenants (reverse order for nested)
            foreach (string subTenantId in createdSubTenantIds.AsEnumerable().Reverse())
            {
                try { await classShelfProvider.DeleteSubTenantAsync(subTenantId); } catch { }
            }
        }

        [TestMethod]
        public async Task ResourceExistsAsync_WithSubTenantHierarchy_ReturnsTrue()
        {
            // sub2/sub2a/file2a.txt
            ByteShelfResourceProvider provider = new ByteShelfResourceProvider(BaseUrl, ApiKey, sub2Id);
            Resource resource = new Resource("Sub2A/file2a.txt");
            bool result = await provider.ResourceExistsAsync(resource);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task GetResourceStreamAsync_WithSubTenantHierarchy_ReturnsCorrectStream()
        {
            // sub2/sub2a/file2a.txt
            ByteShelfResourceProvider provider = new ByteShelfResourceProvider(BaseUrl, ApiKey, sub2Id);
            Resource resource = new Resource("Sub2A/file2a.txt");
            using (Stream stream = await provider.GetResourceStreamAsync(resource))
            using (StreamReader reader = new StreamReader(stream))
            {
                string content = await reader.ReadToEndAsync();
                Assert.AreEqual("Sub2A file2a content", content);
            }
        }

        [TestMethod]
        public async Task GetResourceStreamAsync_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            ByteShelfResourceProvider provider = new ByteShelfResourceProvider(BaseUrl, ApiKey, sub1Id);
            Resource resource = new Resource("nonexistent.txt");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<FileNotFoundException>(() =>
                provider.GetResourceStreamAsync(resource));
        }

        [TestMethod]
        public async Task GetResourceStreamAsync_WithNonExistentSubTenant_ThrowsDirectoryNotFoundException()
        {
            // Arrange
            ByteShelfResourceProvider provider = new ByteShelfResourceProvider(BaseUrl, ApiKey, sub1Id);
            Resource resource = new Resource("NonExistentFolder/test.txt");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(() =>
                provider.GetResourceStreamAsync(resource));
        }

        // Test for root-level file
        [TestMethod]
        public async Task ResourceExistsAsync_RootLevelFile_ReturnsTrueAndCorrectContent()
        {
            ByteShelfResourceProvider provider = new ByteShelfResourceProvider(BaseUrl, ApiKey);
            Resource resource = new Resource("rootfile.txt");

            bool exists = await provider.ResourceExistsAsync(resource);
            Assert.IsTrue(exists);

            string content = await ((IResourceProvider)provider).ReadAsStringAsync(resource);
            Assert.AreEqual("Root file content", content);
        }

        // Parameterized test for subtenant files
        [DataTestMethod]
        [DataRow(null, "rootfile.txt", "Root file content")]
        [DataRow("sub1", "file1.txt", "Sub1 file1 content")]
        [DataRow("sub2a", "file2a.txt", "Sub2A file2a content")]
        [DataRow("sub3", "file3a.txt", "Sub3 file3a content")]
        [DataRow("sub3", "file3b.txt", "Sub3 file3b content")]
        [DataRow("sub4a", "file4a1.txt", "Sub4A file4a1 content")]
        [DataRow("sub4a", "file4a2.txt", "Sub4A file4a2 content")]
        public async Task ResourceExistsAsync_SubtenantFiles_ReturnsTrueAndCorrectContent(string subTenantKey, string fileName, string expectedContent)
        {
            string? subTenantId = subTenantKey switch
            {
                null => null,
                "sub1" => sub1Id,
                "sub2a" => sub2aId,
                "sub3" => sub3Id,
                "sub4a" => sub4aId,
                _ => throw new ArgumentException($"Unknown subtenant key: {subTenantKey}")
            };

            ByteShelfResourceProvider provider = new ByteShelfResourceProvider(BaseUrl, ApiKey, subTenantId);
            Resource resource = new Resource(fileName);

            bool exists = await provider.ResourceExistsAsync(resource);
            Assert.IsTrue(exists);

            string content = await ((IResourceProvider)provider).ReadAsStringAsync(resource);
            Assert.AreEqual(expectedContent, content);
        }

        [DataTestMethod]
        [DataRow("Sub1/file1.txt", "Sub1 file1 content")]
        [DataRow("Sub2/Sub2A/file2a.txt", "Sub2A file2a content")]
        [DataRow("Sub3/file3a.txt", "Sub3 file3a content")]
        [DataRow("Sub3/file3b.txt", "Sub3 file3b content")]
        [DataRow("Sub4/Sub4A/file4a1.txt", "Sub4A file4a1 content")]
        [DataRow("Sub4/Sub4A/file4a2.txt", "Sub4A file4a2 content")]
        public async Task ResourceProvider_ResolvesFilesByPath_FromRoot(string resourcePath, string expectedContent)
        {
            ByteShelfResourceProvider provider = new ByteShelfResourceProvider(BaseUrl, ApiKey);
            Resource resource = new Resource(resourcePath);
            bool exists = await provider.ResourceExistsAsync(resource);
            Assert.IsTrue(exists);
            string content = await ((IResourceProvider)provider).ReadAsStringAsync(resource);
            Assert.AreEqual(expectedContent, content);
        }

        [TestMethod]
        public async Task GetResourceStreamAsync_WithCaching_ReturnsCachedFile()
        {
            // Arrange
            MockResourceCache cache = new MockResourceCache();
            ByteShelfResourceProvider provider = new ByteShelfResourceProvider(BaseUrl, ApiKey, cache: cache);
            Resource resource = new Resource("Sub1/file1.txt");

            // Act - First call should download and cache
            string firstResult;
            using (Stream stream = await provider.GetResourceStreamAsync(resource))
            using (StreamReader reader = new StreamReader(stream))
            {
                firstResult = await reader.ReadToEndAsync();
            }

            // Act - Second call should use cache
            string secondResult;
            using (Stream stream = await provider.GetResourceStreamAsync(resource))
            using (StreamReader reader = new StreamReader(stream))
            {
                secondResult = await reader.ReadToEndAsync();
            }

            // Assert
            Assert.AreEqual("Sub1 file1 content", firstResult);
            Assert.AreEqual("Sub1 file1 content", secondResult);
            Assert.IsTrue(await cache.ExistsAsync("Sub1/file1.txt"));
        }
    }
}