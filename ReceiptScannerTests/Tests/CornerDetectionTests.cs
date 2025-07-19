using EasyReasy;
using EasyReasy.ByteShelfProvider;
using EasyReasy.EnvironmentVariables;
using OpenCvSharp;
using ReceiptScanner.Models;
using ReceiptScanner.Services.CornerDetection;
using ReceiptScanner.Tests.Configuration;
using ReceiptScannerTests.Extensions;
using ReceiptScannerTests.Models;
using ReceiptScannerTests.Utilities;
using System.Reflection;

namespace ReceiptScanner.Tests.Tests
{
    [TestClass]
    public class CornerDetectionTests
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
        public void HeatmapCornerDetection_WithTestImage_DetectsCorners()
        {
            // Arrange
            HeatmapCornerDetectionService cornerDetectionService = new HeatmapCornerDetectionService(_mainProjectResourceManager);

            // Create a simple test image (white rectangle on black background)
            Mat testImage = CreateTestImage();

            // Act
            CornerDetectionResult result = cornerDetectionService.DetectCorners(testImage);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Confidence >= 0.0 && result.Confidence <= 1.0, $"Expected confidence between 0 and 1 but actual value was: {result.Confidence}");

            // Log results for debugging
            Console.WriteLine($"Corner detection confidence: {result.Confidence}");
            Console.WriteLine($"TopLeft: ({result.TopLeft.X}, {result.TopLeft.Y})");
            Console.WriteLine($"TopRight: ({result.TopRight.X}, {result.TopRight.Y})");
            Console.WriteLine($"BottomLeft: ({result.BottomLeft.X}, {result.BottomLeft.Y})");
            Console.WriteLine($"BottomRight: ({result.BottomRight.X}, {result.BottomRight.Y})");

            // Create debug points and output image using extension method and automatic naming
            List<DebugPoint> debugPoints = result.CreateCornerDebugPoints();
            string imageName = LocalFileDebugOutputService.CreateNameFromCaller("testImage");
            _debugOutputService.OutputImageWithPoints(testImage, debugPoints, imageName);

            // Cleanup
            testImage.Dispose();
            cornerDetectionService.Dispose();
        }

        [TestMethod]
        [DynamicData(nameof(GetTestResources), DynamicDataSourceType.Method)]
        public async Task HeatmapCornerDetection_WithReceiptImage_DetectsCorners(string testResource)
        {
            // Arrange
            HeatmapCornerDetectionService cornerDetectionService = new HeatmapCornerDetectionService(_mainProjectResourceManager);

            // Load image from test resources
            byte[] imageBytes = await _testProjectResourceManager.ReadAsBytesAsync(new Resource(testResource));
            Mat receiptImage = Mat.FromImageData(imageBytes);

            // Act
            CornerDetectionResult result = cornerDetectionService.DetectCorners(receiptImage);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Confidence >= 0.0 && result.Confidence <= 1.0, $"Expected confidence between 0 and 1 but actual value was: {result.Confidence}");

            // Log results for debugging
            Console.WriteLine($"Test resource: {testResource}");
            Console.WriteLine($"Corner detection confidence: {result.Confidence}");
            Console.WriteLine($"TopLeft: ({result.TopLeft.X}, {result.TopLeft.Y})");
            Console.WriteLine($"TopRight: ({result.TopRight.X}, {result.TopRight.Y})");
            Console.WriteLine($"BottomLeft: ({result.BottomLeft.X}, {result.BottomLeft.Y})");
            Console.WriteLine($"BottomRight: ({result.BottomRight.X}, {result.BottomRight.Y})");

            // Create debug points and output image using extension method and automatic naming
            List<DebugPoint> debugPoints = result.CreateCornerDebugPoints();
            string imageName = LocalFileDebugOutputService.CreateNameFromCaller(testResource.Replace("/", "_").Replace(".", "_"));
            _debugOutputService.OutputImageWithPoints(receiptImage, debugPoints, imageName);

            // Cleanup
            receiptImage.Dispose();
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

        private Mat CreateTestImage()
        {
            // Create a 512x512 black image
            Mat image = new Mat(512, 512, MatType.CV_8UC3, Scalar.Black);

            // Draw a white rectangle (simulating a document)
            Rect documentRect = new Rect(100, 100, 300, 200);
            Cv2.Rectangle(image, documentRect, Scalar.White, -1);

            return image;
        }
    }
}