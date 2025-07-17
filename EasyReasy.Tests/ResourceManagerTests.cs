using System.Reflection;

namespace EasyReasy.Tests
{
    [TestClass]
    public class ResourceManagerTests
    {
        [TestMethod]
        public void CreateInstance_WithAssembly_ReturnsValidInstance()
        {
            // Arrange & Act
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true);
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(TestResourceCollection));
            ResourceManager manager = ResourceManager.CreateInstance(Assembly.GetExecutingAssembly(), predefinedProvider);

            // Assert
            Assert.IsNotNull(manager);
        }

        [TestMethod]
        public void CreateInstance_WithoutAssembly_ReturnsValidInstance()
        {
            // Arrange & Act
            ResourceManager manager = ResourceManager.CreateInstance();

            // Assert
            Assert.IsNotNull(manager);
        }

        [TestMethod]
        public void CreateInstance_WithPredefinedProviders_RegistersProviders()
        {
            // Arrange
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true);
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(TestResourceCollection));

            // Act
            ResourceManager manager = ResourceManager.CreateInstance(Assembly.GetExecutingAssembly(), predefinedProvider);

            // Assert
            Assert.IsNotNull(manager);
        }

        [TestMethod]
        public async Task ReadAsStringAsync_WithValidResource_ReturnsContent()
        {
            // Arrange
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true);
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(TestResourceCollection));
            ResourceManager manager = ResourceManager.CreateInstance(Assembly.GetExecutingAssembly(), predefinedProvider);
            Resource testResource = TestResourceCollection.TestResource;

            // Act
            string result = await manager.ReadAsStringAsync(testResource);

            // Assert
            Assert.AreEqual("Test content", result);
        }

        [TestMethod]
        public async Task ReadAsBytesAsync_WithValidResource_ReturnsBytes()
        {
            // Arrange
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true);
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(TestResourceCollection));
            ResourceManager manager = ResourceManager.CreateInstance(Assembly.GetExecutingAssembly(), predefinedProvider);
            Resource testResource = TestResourceCollection.TestResource;

            // Act
            byte[] result = await manager.ReadAsBytesAsync(testResource);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public async Task GetResourceStreamAsync_WithValidResource_ReturnsStream()
        {
            // Arrange
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true);
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(TestResourceCollection));

            ResourceManager manager = ResourceManager.CreateInstance(Assembly.GetExecutingAssembly(), predefinedProvider);
            Resource testResource = TestResourceCollection.TestResource;

            // Act
            Stream result = await manager.GetResourceStreamAsync(testResource);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.CanRead);
        }

        [TestMethod]
        public async Task ResourceExistsAsync_WithExistingResource_ReturnsTrue()
        {
            // Arrange
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true);
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(TestResourceCollection));

            ResourceManager manager = ResourceManager.CreateInstance(Assembly.GetExecutingAssembly(), predefinedProvider);
            Resource testResource = TestResourceCollection.TestResource;

            // Act
            bool result = await manager.ResourceExistsAsync(testResource);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ResourceExistsAsync_WithNonExistingResource_ThrowsInvalidOperationException()
        {
            // Arrange
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true);
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(TestResourceCollection));
            ResourceManager manager = ResourceManager.CreateInstance(Assembly.GetExecutingAssembly(), predefinedProvider);
            Resource testResource = new Resource("nonexistent.txt");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await manager.ResourceExistsAsync(testResource));
        }

        [TestMethod]
        public void GetContentType_WithValidResource_ReturnsContentType()
        {
            // Arrange
            Resource testResource = new Resource("test.txt");

            // Act
            string result = testResource.GetContentType();

            // Assert
            Assert.AreEqual("text/plain", result);
        }

        [TestMethod]
        public void GetContentType_WithImageResource_ReturnsImageContentType()
        {
            // Arrange
            Resource testResource = new Resource("test.jpg");

            // Act
            string result = testResource.GetContentType();

            // Assert
            Assert.AreEqual("image/jpeg", result);
        }

        [TestMethod]
        public void GetContentType_WithUnknownExtension_ReturnsOctetStream()
        {
            // Arrange
            Resource testResource = new Resource("test.unknown");

            // Act
            string result = testResource.GetContentType();

            // Assert
            Assert.AreEqual("application/octet-stream", result);
        }

        [TestMethod]
        public void VerifyResourceMappings_WithValidProviders_DoesNotThrow()
        {
            // Arrange
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true);
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(TestResourceCollection));
            ResourceManager manager = ResourceManager.CreateInstance(Assembly.GetExecutingAssembly(), predefinedProvider);

            // Act & Assert
            manager.VerifyResourceMappings(); // Should not throw when providers are properly registered
        }

        [TestMethod]
        public void VerifyResourceMappings_WithAutoRegisteredProviders_DoesNotThrow()
        {
            // Arrange
            // Provide predefined provider for TestResourceCollection (which needs it)
            // AutoRegisteredResourceCollection should be auto-registered
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true);
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(TestResourceCollection));
            ResourceManager manager = ResourceManager.CreateInstance(Assembly.GetExecutingAssembly(), predefinedProvider);

            // Act & Assert
            // Should not throw because:
            // - TestResourceCollection has predefined provider
            // - AutoRegisteredResourceCollection is auto-registered (FakeResourceProvider has parameterless constructor)
            manager.VerifyResourceMappings();
        }

        [TestMethod]
        public void VerifyResourceMappings_WithMissingProvider_ThrowsException()
        {
            // Arrange & Act & Assert
            // Create manager with no predefined providers
            // AutoRegisteredResourceCollection will be auto-registered (FakeResourceProvider has parameterless constructor)
            // TestResourceCollection will NOT be auto-registered (ParameterizedFakeResourceProvider has constructor parameters)
            // Should throw because TestResourceCollection has no registered provider
            Assert.ThrowsException<InvalidOperationException>(() => ResourceManager.CreateInstance(Assembly.GetExecutingAssembly()));
        }

        // Test resource collection that can be auto-registered (parameterless constructor)
        [ResourceCollection(typeof(FakeResourceProvider))]
        public static class AutoRegisteredResourceCollection
        {
            public static readonly Resource TestResource = new Resource("auto.txt");
        }

        // Test resource collection that requires a predefined provider (constructor parameters)
        [ResourceCollection(typeof(ParameterizedFakeResourceProvider))]
        public static class TestResourceCollection
        {
            public static readonly Resource TestResource = new Resource("test.txt");
        }

        // Fake resource provider for testing that requires constructor parameters
        private class ParameterizedFakeResourceProvider : IResourceProvider
        {
            private readonly string _basePath;
            private readonly bool _shouldExist;

            public ParameterizedFakeResourceProvider(string basePath, bool shouldExist = true)
            {
                _basePath = basePath;
                _shouldExist = shouldExist;
            }

            public async Task<bool> ResourceExistsAsync(Resource resource)
            {
                await Task.CompletedTask;
                return _shouldExist && resource.Path == "test.txt";
            }

            public async Task<Stream> GetResourceStreamAsync(Resource resource)
            {
                await Task.CompletedTask;
                if (_shouldExist && resource.Path == "test.txt")
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes("Test content");
                    return new MemoryStream(bytes);
                }
                throw new FileNotFoundException($"Resource '{resource.Path}' not found.");
            }

            public async Task<byte[]> ReadAsBytesAsync(Resource resource)
            {
                using Stream stream = await GetResourceStreamAsync(resource);
                using MemoryStream memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }

            public async Task<string> ReadAsStringAsync(Resource resource)
            {
                using Stream stream = await GetResourceStreamAsync(resource);
                using StreamReader reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
        }

        // Simple fake resource provider for testing (no constructor parameters)
        private class FakeResourceProvider : IResourceProvider
        {
            public async Task<bool> ResourceExistsAsync(Resource resource)
            {
                await Task.CompletedTask;
                return resource.Path == "test.txt" || resource.Path == "auto.txt";
            }

            public async Task<Stream> GetResourceStreamAsync(Resource resource)
            {
                await Task.CompletedTask;
                if (resource.Path == "test.txt" || resource.Path == "auto.txt")
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes("Test content");
                    return new MemoryStream(bytes);
                }
                throw new FileNotFoundException($"Resource '{resource.Path}' not found.");
            }

            public async Task<byte[]> ReadAsBytesAsync(Resource resource)
            {
                using Stream stream = await GetResourceStreamAsync(resource);
                using MemoryStream memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }

            public async Task<string> ReadAsStringAsync(Resource resource)
            {
                using Stream stream = await GetResourceStreamAsync(resource);
                using StreamReader reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
        }
    }
}