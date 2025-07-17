using EasyReasy;
using EasyReasy.ByteShelfProvider;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasyReasy.Tests
{
    [TestClass]
    public class ByteShelfResourceProviderTests
    {
        [TestMethod]
        public void Create_WithValidParameters_ReturnsPredefinedProvider()
        {
            // Arrange
            string baseUrl = "https://api.byteshelf.com";
            string apiKey = "test-api-key";

            // Act
            PredefinedResourceProvider provider = ByteShelfResourceProvider.Create(
                resourceCollectionType: typeof(TestResourceCollection),
                baseUrl: baseUrl,
                apiKey: apiKey);

            // Assert
            Assert.IsNotNull(provider);
            Assert.AreEqual(typeof(TestResourceCollection), provider.ResourceCollectionType);
            Assert.IsNotNull(provider.Provider);
            Assert.IsInstanceOfType(provider.Provider, typeof(ByteShelfResourceProvider));
        }

        [TestMethod]
        public void Create_WithNullBaseUrl_ThrowsArgumentException()
        {
            // Arrange
            string? baseUrl = null;
            string apiKey = "test-api-key";

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => ByteShelfResourceProvider.Create(
                resourceCollectionType: typeof(TestResourceCollection),
                baseUrl: baseUrl,
                apiKey: apiKey));
        }

        [TestMethod]
        public void Create_WithNullApiKey_ThrowsArgumentException()
        {
            // Arrange
            string baseUrl = "https://api.byteshelf.com";
            string? apiKey = null;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => ByteShelfResourceProvider.Create(
                resourceCollectionType: typeof(TestResourceCollection),
                baseUrl: baseUrl,
                apiKey: apiKey));
        }

        [TestMethod]
        public void Create_WithEmptyBaseUrl_ThrowsArgumentException()
        {
            // Arrange
            string baseUrl = "";
            string apiKey = "test-api-key";

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => ByteShelfResourceProvider.Create(
                resourceCollectionType: typeof(TestResourceCollection),
                baseUrl: baseUrl,
                apiKey: apiKey));
        }

        [TestMethod]
        public void Create_WithEmptyApiKey_ThrowsArgumentException()
        {
            // Arrange
            string baseUrl = "https://api.byteshelf.com";
            string apiKey = "";

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => ByteShelfResourceProvider.Create(
                resourceCollectionType: typeof(TestResourceCollection),
                baseUrl: baseUrl,
                apiKey: apiKey));
        }

        [TestMethod]
        public void Create_WithWhitespaceBaseUrl_ThrowsArgumentException()
        {
            // Arrange
            string baseUrl = "   ";
            string apiKey = "test-api-key";

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => ByteShelfResourceProvider.Create(
                resourceCollectionType: typeof(TestResourceCollection),
                baseUrl: baseUrl,
                apiKey: apiKey));
        }

        [TestMethod]
        public void Create_WithWhitespaceApiKey_ThrowsArgumentException()
        {
            // Arrange
            string baseUrl = "https://api.byteshelf.com";
            string apiKey = "   ";

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => ByteShelfResourceProvider.Create(
                resourceCollectionType: typeof(TestResourceCollection),
                baseUrl: baseUrl,
                apiKey: apiKey));
        }

        [TestMethod]
        public void Create_WithInvalidBaseUrl_ThrowsArgumentException()
        {
            // Arrange
            string baseUrl = "not-a-valid-url";
            string apiKey = "test-api-key";

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => ByteShelfResourceProvider.Create(
                resourceCollectionType: typeof(TestResourceCollection),
                baseUrl: baseUrl,
                apiKey: apiKey));
        }

        [TestMethod]
        public void Create_WithHttpBaseUrl_ThrowsArgumentException()
        {
            // Arrange
            string baseUrl = "http://api.byteshelf.com";
            string apiKey = "test-api-key";

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => ByteShelfResourceProvider.Create(
                resourceCollectionType: typeof(TestResourceCollection),
                baseUrl: baseUrl,
                apiKey: apiKey));
        }

        [TestMethod]
        public void Create_WithValidHttpsUrl_DoesNotThrow()
        {
            // Arrange
            string baseUrl = "https://api.byteshelf.com";
            string apiKey = "test-api-key";

            // Act & Assert
            PredefinedResourceProvider provider = ByteShelfResourceProvider.Create(
                resourceCollectionType: typeof(TestResourceCollection),
                baseUrl: baseUrl,
                apiKey: apiKey);

            Assert.IsNotNull(provider);
        }

        [TestMethod]
        public void Create_WithValidHttpsUrlWithPath_DoesNotThrow()
        {
            // Arrange
            string baseUrl = "https://api.byteshelf.com/v1";
            string apiKey = "test-api-key";

            // Act & Assert
            PredefinedResourceProvider provider = ByteShelfResourceProvider.Create(
                resourceCollectionType: typeof(TestResourceCollection),
                baseUrl: baseUrl,
                apiKey: apiKey);

            Assert.IsNotNull(provider);
        }

        [TestMethod]
        public void Create_WithValidHttpsUrlWithTrailingSlash_DoesNotThrow()
        {
            // Arrange
            string baseUrl = "https://api.byteshelf.com/";
            string apiKey = "test-api-key";

            // Act & Assert
            PredefinedResourceProvider provider = ByteShelfResourceProvider.Create(
                resourceCollectionType: typeof(TestResourceCollection),
                baseUrl: baseUrl,
                apiKey: apiKey);

            Assert.IsNotNull(provider);
        }

        // Test resource collection for testing
        public static class TestResourceCollection
        {
            public static readonly Resource TestResource = new Resource("test.txt");
        }
    }
} 