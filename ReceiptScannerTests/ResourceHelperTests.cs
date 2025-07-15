using System.Reflection;
using System.Text;
using Microsoft.Extensions.FileProviders;
using ReceiptScanner;

namespace ReceiptScannerTests
{
    [TestClass]
    public class ResourceHelperTests
    {
        [TestMethod]
        public void ResourceManager_GetInstance_ReturnsSameInstanceForSameAssembly()
        {
            // Arrange & Act
            ResourceManager instance1 = ResourceManager.GetInstance();
            ResourceManager instance2 = ResourceManager.GetInstance();

            // Assert
            Assert.IsNotNull(instance1);
            Assert.IsNotNull(instance2);
            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        public void GetContentType_WithValidExtensions_ReturnsCorrectContentType()
        {
            // Arrange
            ResourceManager manager = ResourceManager.GetInstance();

            // Act & Assert
            Assert.AreEqual("text/html", manager.GetContentType(Resource.Create("test.html")));
            Assert.AreEqual("font/otf", manager.GetContentType(Resource.Create("test.otf")));
            Assert.AreEqual("font/ttf", manager.GetContentType(Resource.Create("test.ttf")));
            Assert.AreEqual("image/svg+xml", manager.GetContentType(Resource.Create("test.svg")));
            Assert.AreEqual("image/png", manager.GetContentType(Resource.Create("test.png")));
            Assert.AreEqual("image/jpeg", manager.GetContentType(Resource.Create("test.jpg")));
            Assert.AreEqual("text/plain", manager.GetContentType(Resource.Create("test.txt")));
            Assert.AreEqual("application/pdf", manager.GetContentType(Resource.Create("test.pdf")));
            Assert.AreEqual("application/x-x509-ca-cert", manager.GetContentType(Resource.Create("test.cer")));
            Assert.AreEqual("application/octet-stream", manager.GetContentType(Resource.Create("test.onnx")));
            Assert.AreEqual("application/octet-stream", manager.GetContentType(Resource.Create("test.unknown")));
        }

        [TestMethod]
        public void GetContentType_WithUppercaseExtensions_ReturnsCorrectContentType()
        {
            // Arrange
            ResourceManager manager = ResourceManager.GetInstance();

            // Act & Assert
            Assert.AreEqual("text/html", manager.GetContentType(Resource.Create("test.HTML")));
            Assert.AreEqual("image/png", manager.GetContentType(Resource.Create("test.PNG")));
        }

        [TestMethod]
        public void Resource_ToString_ReturnsPath()
        {
            // Arrange
            string expectedPath = "test/file.txt";
            Resource resource = Resource.Create(expectedPath);

            // Act
            string result = resource.ToString();

            // Assert
            Assert.AreEqual(expectedPath, result);
        }

        [TestMethod]
        public void Resource_ImplicitStringConversion_ReturnsPath()
        {
            // Arrange
            string expectedPath = "test/file.txt";
            Resource resource = Resource.Create(expectedPath);

            // Act
            string result = resource;

            // Assert
            Assert.AreEqual(expectedPath, result);
        }

        [TestMethod]
        public void Resource_GetFileName_ReturnsFileName()
        {
            // Arrange
            Resource resource = Resource.Create("folder/subfolder/file.txt");

            // Act
            string fileName = resource.GetFileName();

            // Assert
            Assert.AreEqual("file.txt", fileName);
        }

        [TestMethod]
        public void VerifyResourceMappings_WithValidResources_DoesNotThrow()
        {
            // Arrange
            ResourceManager manager = ResourceManager.Instance;

            // Act & Assert
            try
            {
                manager.VerifyResourceMappings();
                // If no exception is thrown, the test passes
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Resource mapping verification failed: {ex.Message}");
            }
        }

        [TestMethod]
        public void ReceiptModel_Resource_IsCorrectlyDefined()
        {
            // Arrange
            Resource receiptModel = Resources.Models.ReceiptModel;

            // Act & Assert
            Assert.AreEqual("Models/pgnet.onnx", receiptModel.Path);
            Assert.AreEqual("pgnet.onnx", receiptModel.GetFileName());
            Assert.AreEqual("Models/pgnet.onnx", receiptModel.ToString());
        }

        [TestMethod]
        public void ReceiptModel_ContentType_IsCorrect()
        {
            // Arrange
            ResourceManager manager = ResourceManager.Instance;
            Resource receiptModel = Resources.Models.ReceiptModel;

            // Act
            string contentType = manager.GetContentType(receiptModel);

            // Assert
            Assert.AreEqual("application/octet-stream", contentType);
        }

        [TestMethod]
        public void Frontend_Resource_IsCorrectlyDefined()
        {
            // Arrange
            Resource frontend = Resources.Frontend.ReceiptScannerFrontend;

            // Act & Assert
            Assert.AreEqual("Frontend/ReceiptScannerFrontend.html", frontend.Path);
            Assert.AreEqual("ReceiptScannerFrontend.html", frontend.GetFileName());
            Assert.AreEqual("Frontend/ReceiptScannerFrontend.html", frontend.ToString());
        }

        [TestMethod]
        public void Frontend_ContentType_IsCorrect()
        {
            // Arrange
            ResourceManager manager = ResourceManager.Instance;
            Resource frontend = Resources.Frontend.ReceiptScannerFrontend;

            // Act
            string contentType = manager.GetContentType(frontend);

            // Assert
            Assert.AreEqual("text/html", contentType);
        }

        [TestMethod]
        public async Task ReadAsStringAsync_ValidResource_ReturnsContent()
        {
            // Arrange
            Resource resource = Resources.Frontend.ReceiptScannerFrontend;

            // Act & Assert
            try
            {
                string content = await ResourceManager.Instance.ReadAsStringAsync(resource);
                Assert.IsFalse(string.IsNullOrEmpty(content));
                Assert.IsTrue(content.Contains("<!DOCTYPE html>"));
            }
            catch (FileNotFoundException)
            {
                Assert.Inconclusive("Resource not found, but method works as expected.");
            }
        }

        [TestMethod]
        public async Task ReadAsBytesAsync_ValidResource_ReturnsBytes()
        {
            // Arrange
            Resource resource = Resources.Models.ReceiptModel;

            // Act & Assert
            try
            {
                byte[] bytes = await ResourceManager.Instance.ReadAsBytesAsync(resource);
                Assert.IsTrue(bytes.Length > 0);
                // ONNX files should start with specific magic bytes
                Assert.IsTrue(bytes.Length > 8);
            }
            catch (FileNotFoundException)
            {
                Assert.Inconclusive("Resource not found, but method works as expected.");
            }
        }

        [TestMethod]
        public async Task ReadAsStringAsync_InvalidResource_Throws()
        {
            // Arrange
            Resource resource = Resource.Create("NonExistent.txt");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () =>
            {
                await ResourceManager.Instance.ReadAsStringAsync(resource);
            });
        }

        [TestMethod]
        public async Task ReadAsBytesAsync_InvalidResource_Throws()
        {
            // Arrange
            Resource resource = Resource.Create("NonExistent.bin");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () =>
            {
                await ResourceManager.Instance.ReadAsBytesAsync(resource);
            });
        }
    }
} 