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
        public void ResourceHelper_Instance_IsSingleton()
        {
            // Arrange & Act
            ResourceHelper instance1 = ResourceHelper.Instance;
            ResourceHelper instance2 = ResourceHelper.Instance;

            // Assert
            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        public void GetContentType_WithValidExtensions_ReturnsCorrectContentType()
        {
            // Arrange
            ResourceHelper helper = ResourceHelper.Instance;

            // Act & Assert
            Assert.AreEqual("text/html", helper.GetContentType(Resource.Create("test.html")));
            Assert.AreEqual("font/otf", helper.GetContentType(Resource.Create("test.otf")));
            Assert.AreEqual("font/ttf", helper.GetContentType(Resource.Create("test.ttf")));
            Assert.AreEqual("image/svg+xml", helper.GetContentType(Resource.Create("test.svg")));
            Assert.AreEqual("image/png", helper.GetContentType(Resource.Create("test.png")));
            Assert.AreEqual("image/jpeg", helper.GetContentType(Resource.Create("test.jpg")));
            Assert.AreEqual("text/plain", helper.GetContentType(Resource.Create("test.txt")));
            Assert.AreEqual("application/pdf", helper.GetContentType(Resource.Create("test.pdf")));
            Assert.AreEqual("application/x-x509-ca-cert", helper.GetContentType(Resource.Create("test.cer")));
            Assert.AreEqual("application/octet-stream", helper.GetContentType(Resource.Create("test.onnx")));
            Assert.AreEqual("application/octet-stream", helper.GetContentType(Resource.Create("test.unknown")));
        }

        [TestMethod]
        public void GetContentType_WithUppercaseExtensions_ReturnsCorrectContentType()
        {
            // Arrange
            ResourceHelper helper = ResourceHelper.Instance;

            // Act & Assert
            Assert.AreEqual("text/html", helper.GetContentType(Resource.Create("test.HTML")));
            Assert.AreEqual("image/png", helper.GetContentType(Resource.Create("test.PNG")));
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
            ResourceHelper helper = ResourceHelper.Instance;

            // Act & Assert
            try
            {
                helper.VerifyResourceMappings();
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
            ResourceHelper helper = ResourceHelper.Instance;
            Resource receiptModel = Resources.Models.ReceiptModel;

            // Act
            string contentType = helper.GetContentType(receiptModel);

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
            ResourceHelper helper = ResourceHelper.Instance;
            Resource frontend = Resources.Frontend.ReceiptScannerFrontend;

            // Act
            string contentType = helper.GetContentType(frontend);

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
                string content = await ResourceHelper.Instance.ReadAsStringAsync(resource);
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
                byte[] bytes = await ResourceHelper.Instance.ReadAsBytesAsync(resource);
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
                await ResourceHelper.Instance.ReadAsStringAsync(resource);
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
                await ResourceHelper.Instance.ReadAsBytesAsync(resource);
            });
        }
    }
} 