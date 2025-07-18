using System.Reflection;
using EasyReasy.Tests.TestProviders;
using EasyReasy.Tests.TestResourceCollections;

namespace EasyReasy.Tests
{
    [TestClass]
    public class ResourceManagerResourceAccessTests
    {
        [TestMethod]
        public async Task ReadAsStringAsync_WithValidResource_ReturnsContent()
        {
            // Arrange
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true, "shared-test.txt");
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(SharedTestResourceCollection));
            ResourceManager manager = await ResourceManager.CreateInstanceAsync(Assembly.GetExecutingAssembly(), predefinedProvider);
            Resource testResource = SharedTestResourceCollection.TestResource;

            // Act
            string result = await manager.ReadAsStringAsync(testResource);

            // Assert
            Assert.AreEqual("Test content", result);
        }

        [TestMethod]
        public async Task ReadAsBytesAsync_WithValidResource_ReturnsBytes()
        {
            // Arrange
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true, "shared-test.txt");
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(SharedTestResourceCollection));
            ResourceManager manager = await ResourceManager.CreateInstanceAsync(Assembly.GetExecutingAssembly(), predefinedProvider);
            Resource testResource = SharedTestResourceCollection.TestResource;

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
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true, "shared-test.txt");
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(SharedTestResourceCollection));

            ResourceManager manager = await ResourceManager.CreateInstanceAsync(Assembly.GetExecutingAssembly(), predefinedProvider);
            Resource testResource = SharedTestResourceCollection.TestResource;

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
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true, "shared-test.txt");
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(SharedTestResourceCollection));

            ResourceManager manager = await ResourceManager.CreateInstanceAsync(Assembly.GetExecutingAssembly(), predefinedProvider);
            Resource testResource = SharedTestResourceCollection.TestResource;

            // Act
            bool result = await manager.ResourceExistsAsync(testResource);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ResourceExistsAsync_WithNonExistingResource_ThrowsInvalidOperationException()
        {
            // Arrange
            ParameterizedFakeResourceProvider fakeProvider = new ParameterizedFakeResourceProvider("test", true, "shared-test.txt");
            PredefinedResourceProvider predefinedProvider = fakeProvider.AsPredefinedFor(typeof(SharedTestResourceCollection));
            ResourceManager manager = await ResourceManager.CreateInstanceAsync(Assembly.GetExecutingAssembly(), predefinedProvider);
            Resource testResource = new Resource("nonexistent.txt");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await manager.ResourceExistsAsync(testResource));
        }
    }
} 