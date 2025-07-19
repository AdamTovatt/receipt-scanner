using EasyReasy;
using EasyReasy.ByteShelfProvider;
using EasyReasy.EnvironmentVariables;
using OpenCvSharp;
using ReceiptScanner.Models;
using ReceiptScanner.Preprocessing;
using ReceiptScanner.Preprocessing.Preprocessors;
using ReceiptScanner.Services.CornerDetection;
using ReceiptScanner.Tests.Configuration;
using ReceiptScannerTests.Extensions;
using ReceiptScannerTests.Models;
using ReceiptScannerTests.Utilities;
using System.Reflection;

namespace ReceiptScanner.Tests.Tests
{
    [TestClass]
    public class CornerDetectionCropPreprocessorTests
    {
        private static ResourceManager _mainProjectResourceManager = null!;
        private static ResourceManager _testProjectResourceManager = null!;
        private static IDebugOutputService _debugOutputService = null!;

        [ClassInitialize]
        public static async Task BeforeAll(TestContext testContext)
        {
            if (!File.Exists("TestVariables.txt"))
            {
                File.WriteAllText("TestVariables.txt", "VARIABLE1=value1\nVARIABLE2=value2");
            }

            EnvironmentVariables.LoadFromFile("TestVariables.txt");
            EnvironmentVariables.ValidateVariableNamesIn(typeof(TestEnvironmentVariables));

            string apiKey = EnvironmentVariables.GetVariable(TestEnvironmentVariables.ByteShelfApiKey);
            string url = EnvironmentVariables.GetVariable(TestEnvironmentVariables.ByteShelfBackendUrl);

            FileSystemCache fileSystemCache = new FileSystemCache("CachedResources");
            PredefinedResourceProvider predefinedByteShelfProvider = ByteShelfResourceProvider.CreatePredefined(typeof(Resources.Models), url, apiKey, cache: fileSystemCache);

            Assembly mainProjectAssembly = Assembly.GetAssembly(typeof(Program))!;
            _mainProjectResourceManager = await ResourceManager.CreateInstanceAsync(mainProjectAssembly, predefinedByteShelfProvider);
            _testProjectResourceManager = await ResourceManager.CreateInstanceAsync();

            // Initialize debug output service with automatic path detection
            _debugOutputService = new LocalFileDebugOutputService();
        }

        [TestMethod]
        [DynamicData(nameof(GetTestResources), DynamicDataSourceType.Method)]
        public async Task CornerDetectionCropPreprocessor_WithReceiptImage_CropsImageCorrectly(string testResource)
        {
            // Arrange
            HeatmapCornerDetectionService cornerDetectionService = new HeatmapCornerDetectionService(_mainProjectResourceManager);
            CornerDetectionCropPreprocessor cropPreprocessor = new CornerDetectionCropPreprocessor(cornerDetectionService, 0.5);

            // Load image from test resources
            byte[] imageBytes = await _testProjectResourceManager.ReadAsBytesAsync(new Resource(testResource));
            Mat originalImage = Mat.FromImageData(imageBytes);

            // Act
            Mat croppedImage = cropPreprocessor.Preprocess(originalImage);

            // Assert
            Assert.IsNotNull(croppedImage);
            Assert.IsFalse(croppedImage.Empty(), "Cropped image should not be empty");

            // Log results for debugging
            Console.WriteLine($"Test resource: {testResource}");
            Console.WriteLine($"Original image size: {originalImage.Width}x{originalImage.Height}");
            Console.WriteLine($"Cropped image size: {croppedImage.Width}x{croppedImage.Height}");

            // Verify that the cropped image is not larger than the original
            Assert.IsTrue(croppedImage.Width <= originalImage.Width, "Cropped image width should not exceed original width");
            Assert.IsTrue(croppedImage.Height <= originalImage.Height, "Cropped image height should not exceed original height");

            // If corners were detected and confidence was above threshold, the cropped image should be smaller
            CornerDetectionResult cornerResult = cornerDetectionService.DetectCorners(originalImage);
            if (cornerResult.Confidence >= 0.5 && cornerResult.CornersFound)
            {
                Console.WriteLine($"Corner detection confidence: {cornerResult.Confidence}");
                Console.WriteLine($"TopLeft: ({cornerResult.TopLeft.X}, {cornerResult.TopLeft.Y})");
                Console.WriteLine($"TopRight: ({cornerResult.TopRight.X}, {cornerResult.TopRight.Y})");
                Console.WriteLine($"BottomLeft: ({cornerResult.BottomLeft.X}, {cornerResult.BottomLeft.Y})");
                Console.WriteLine($"BottomRight: ({cornerResult.BottomRight.X}, {cornerResult.BottomRight.Y})");

                // Create debug points for the original image
                List<DebugPoint> debugPoints = cornerResult.CreateCornerDebugPoints();
                string originalImageName = LocalFileDebugOutputService.CreateNameFromCaller(testResource.Replace("/", "_").Replace(".", "_") + "_original");
                _debugOutputService.OutputImageWithPoints(originalImage, debugPoints, originalImageName);

                // Output the cropped image
                string croppedImageName = LocalFileDebugOutputService.CreateNameFromCaller(testResource.Replace("/", "_").Replace(".", "_") + "_cropped");
                _debugOutputService.OutputImage(croppedImage, croppedImageName);
            }
            else
            {
                Console.WriteLine($"No corners detected or confidence too low ({cornerResult.Confidence}), image should be unchanged");
                Assert.AreEqual(originalImage.Width, croppedImage.Width, "Image width should be unchanged when no corners detected");
                Assert.AreEqual(originalImage.Height, croppedImage.Height, "Image height should be unchanged when no corners detected");
            }

            // Cleanup
            originalImage.Dispose();
            croppedImage.Dispose();
            cornerDetectionService.Dispose();
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