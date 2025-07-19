using EasyReasy;
using EasyReasy.ByteShelfProvider;
using EasyReasy.EnvironmentVariables;
using OpenCvSharp;
using ReceiptScanner.Models;
using ReceiptScanner.Services.CornerDetection;
using ReceiptScanner.Tests.Configuration;
using System.Reflection;

namespace ReceiptScannerTests
{
    [TestClass]
    public class CornerDetectionTests
    {
        private static ResourceManager _mainProjectResourceManager = null!;

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
            PredefinedResourceProvider predefinedByteShelfProvider = ByteShelfResourceProvider.CreatePredefined(typeof(ReceiptScanner.Resources.Models), url, apiKey, cache: fileSystemCache);

            Assembly mainProjectAssembly = Assembly.GetAssembly(typeof(ReceiptScanner.Program)) ?? throw new Exception($"Could not find the assembly of {nameof(ReceiptScanner.Program)}");
            _mainProjectResourceManager = await ResourceManager.CreateInstanceAsync(mainProjectAssembly, predefinedByteShelfProvider);
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
            Assert.IsTrue(result.Confidence >= 0.0 && result.Confidence <= 1.0);

            // Log results for debugging
            Console.WriteLine($"Corner detection confidence: {result.Confidence}");
            Console.WriteLine($"TopLeft: ({result.TopLeft.X}, {result.TopLeft.Y})");
            Console.WriteLine($"TopRight: ({result.TopRight.X}, {result.TopRight.Y})");
            Console.WriteLine($"BottomLeft: ({result.BottomLeft.X}, {result.BottomLeft.Y})");
            Console.WriteLine($"BottomRight: ({result.BottomRight.X}, {result.BottomRight.Y})");

            // Cleanup
            testImage.Dispose();
            cornerDetectionService.Dispose();
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