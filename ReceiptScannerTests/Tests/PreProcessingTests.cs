using ReceiptScanner.Preprocessing.Preprocessors;
using ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection;
using EasyReasy;
using System.Reflection;
using OpenCvSharp;
using ReceiptScanner.Tests.Configuration;

namespace ReceiptScanner.Tests.Tests
{
    [TestClass]
    public class PreProcessingTests
    {
        private const string receiptEdgeDetectionOutputDirectory = "ReceiptEdgeDetection";

        private static ResourceManager _testProjectResourceManager = null!;

        [ClassInitialize]
        public static async Task BeforeAll(TestContext testContext)
        {
            _testProjectResourceManager = await ResourceManager.CreateInstanceAsync();

            Directory.CreateDirectory(receiptEdgeDetectionOutputDirectory);
        }

        [TestMethod]
        [DynamicData(nameof(GetTestResources), DynamicDataSourceType.Method)]
        public async Task Preprocess_WithTestReceipt_DetectsAndCropsReceipt(string testResource)
        {
            // Arrange
            ReceiptEdgeDetectionPreprocessor preprocessor = ReceiptEdgeDetectionFactory.CreateDefault();
            byte[] imageBytes = await _testProjectResourceManager.ReadAsBytesAsync(new Resource(testResource));

            // Convert bytes to Mat
            Mat originalImage = Mat.FromImageData(imageBytes);

            // Act
            Mat processedImage = preprocessor.Preprocess(originalImage);

            // Assert
            Assert.IsNotNull(processedImage);
            Assert.IsFalse(processedImage.Empty());

            // Save the processed image for inspection
            string outputPath = Path.Combine(receiptEdgeDetectionOutputDirectory, Path.GetFileName(testResource));
            processedImage.SaveImage(outputPath);
            Console.WriteLine($"Processed image saved to: {Path.GetFullPath(outputPath)}");

            // Log image dimensions for comparison
            Console.WriteLine($"Original image size: {originalImage.Width}x{originalImage.Height}");
            Console.WriteLine($"Processed image size: {processedImage.Width}x{processedImage.Height}");

            // Cleanup
            originalImage.Dispose();
            processedImage.Dispose();
        }

        [TestMethod]
        public void Constructor_WithDefaultParameters_CreatesInstance()
        {
            // Act
            ReceiptEdgeDetectionPreprocessor preprocessor = ReceiptEdgeDetectionFactory.CreateDefault();

            // Assert
            Assert.IsNotNull(preprocessor);
            Assert.AreEqual("ReceiptEdgeDetection", preprocessor.Name);
        }

        [TestMethod]
        public void Constructor_WithCustomParameters_CreatesInstance()
        {
            // Arrange
            EdgeDetectionConfig config = new EdgeDetectionConfig
            {
                CannyThreshold1 = 30,
                CannyThreshold2 = 100,
                Epsilon = 0.01
            };

            // Act
            ReceiptEdgeDetectionPreprocessor preprocessor = ReceiptEdgeDetectionFactory.CreateWithConfig(config);

            // Assert
            Assert.IsNotNull(preprocessor);
            Assert.AreEqual("ReceiptEdgeDetection", preprocessor.Name);
        }

        [TestMethod]
        public void Preprocess_WithEmptyImage_ThrowsArgumentException()
        {
            // Arrange
            ReceiptEdgeDetectionPreprocessor preprocessor = ReceiptEdgeDetectionFactory.CreateDefault();
            Mat emptyImage = new Mat();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => preprocessor.Preprocess(emptyImage));

            // Cleanup
            emptyImage.Dispose();
        }

        public static IEnumerable<object[]> GetTestResources()
        {
            Console.WriteLine("GetTestResources method called");

            // Create ResourceManager with EmbeddedResourceProvider for test resources
            EmbeddedResourceProvider embeddedProvider = new EmbeddedResourceProvider(Assembly.GetExecutingAssembly());
            PredefinedResourceProvider predefinedProvider = embeddedProvider.AsPredefinedFor(typeof(TestResources.TestFiles));

            ResourceManager resourceManager = ResourceManager.CreateInstanceAsync(predefinedProvider).GetAwaiter().GetResult();
            List<Resource> testResources = resourceManager.GetResourcesForCollection(typeof(TestResources.TestFiles));

            foreach (Resource resource in testResources)
            {
                Console.WriteLine($"Yielding resource path: {resource.Path}");
                yield return new object[] { resource.Path };
            }
        }
    }
}