using EasyReasy;
using EasyReasy.ByteShelfProvider;
using EasyReasy.EnvironmentVariables;
using OpenCvSharp;
using ReceiptScanner.Models;
using ReceiptScanner.Preprocessing;
using ReceiptScanner.Preprocessing.Preprocessors;
using ReceiptScanner.Tests.Configuration;
using ReceiptScannerTests.Extensions;
using ReceiptScannerTests.Models;
using ReceiptScannerTests.Utilities;
using System.Reflection;
using CvPoint = OpenCvSharp.Point;

namespace ReceiptScanner.Tests.Tests
{
    [TestClass]
    public class HorizontalLineDetectionPreprocessorTests
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
        public async Task HorizontalLineDetectionPreprocessor_WithReceiptImage_DetectsHorizontalLines(string testResource)
        {
            // Arrange
            ThresholdPreprocessor thresholdPreprocessor = new ThresholdPreprocessor();
            HorizontalLineDetectionPreprocessor lineDetectionPreprocessor = new HorizontalLineDetectionPreprocessor();

            // Load image from test resources
            byte[] imageBytes = await _testProjectResourceManager.ReadAsBytesAsync(new Resource(testResource));
            Mat originalImage = Mat.FromImageData(imageBytes);

            // Act
            // Mat thresholdedImage = thresholdPreprocessor.Preprocess(originalImage);
            Mat processedImage = lineDetectionPreprocessor.Preprocess(originalImage);

            // Assert
            Assert.IsNotNull(processedImage);
            Assert.IsFalse(processedImage.Empty(), "Processed image should not be empty");

            // Log results for debugging
            Console.WriteLine($"Test resource: {testResource}");
            Console.WriteLine($"Original image size: {originalImage.Width}x{originalImage.Height}");
            Console.WriteLine($"Processed image size: {processedImage.Width}x{processedImage.Height}");

            // Verify that the processed image has the same dimensions as the original
            Assert.AreEqual(originalImage.Width, processedImage.Width, "Processed image width should match original width");
            Assert.AreEqual(originalImage.Height, processedImage.Height, "Processed image height should match original height");

            // Output both images for debugging
            string originalImageName = LocalFileDebugOutputService.CreateNameFromCaller(testResource.Replace("/", "_").Replace(".", "_") + "_original");
            _debugOutputService.OutputImage(originalImage, originalImageName);

            string processedImageName = LocalFileDebugOutputService.CreateNameFromCaller(testResource.Replace("/", "_").Replace(".", "_") + "_horizontal_lines");
            _debugOutputService.OutputImage(processedImage, processedImageName);

            // Cleanup
            originalImage.Dispose();
            processedImage.Dispose();
        }

        [TestMethod]
        public void HorizontalLineDetectionPreprocessor_WithCustomParameters_DetectsLinesCorrectly()
        {
            // Arrange
            HorizontalLineDetectionPreprocessor strictPreprocessor = new HorizontalLineDetectionPreprocessor(
                minLineLength: 100.0,    // Longer lines
                maxLineGap: 2.0,         // Smaller gaps
                threshold: 20,            // Lower threshold (more sensitive)
                angleTolerance: 1.0      // Very strict horizontal detection
            );

            HorizontalLineDetectionPreprocessor lenientPreprocessor = new HorizontalLineDetectionPreprocessor(
                minLineLength: 20.0,     // Shorter lines
                maxLineGap: 15.0,        // Larger gaps
                threshold: 80,            // Higher threshold (less sensitive)
                angleTolerance: 10.0     // More lenient horizontal detection
            );

            // Create a simple test image with horizontal lines
            Mat testImage = CreateTestImageWithHorizontalLines();

            // Act
            Mat strictResult = strictPreprocessor.Preprocess(testImage);
            Mat lenientResult = lenientPreprocessor.Preprocess(testImage);

            // Assert
            Assert.IsNotNull(strictResult);
            Assert.IsNotNull(lenientResult);
            Assert.IsFalse(strictResult.Empty());
            Assert.IsFalse(lenientResult.Empty());

            // Output test images for debugging
            string testImageName = LocalFileDebugOutputService.CreateNameFromCaller("test_image_with_lines");
            _debugOutputService.OutputImage(testImage, testImageName);

            string strictResultName = LocalFileDebugOutputService.CreateNameFromCaller("strict_detection");
            _debugOutputService.OutputImage(strictResult, strictResultName);

            string lenientResultName = LocalFileDebugOutputService.CreateNameFromCaller("lenient_detection");
            _debugOutputService.OutputImage(lenientResult, lenientResultName);

            // Cleanup
            testImage.Dispose();
            strictResult.Dispose();
            lenientResult.Dispose();
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

        private Mat CreateTestImageWithHorizontalLines()
        {
            // Create a 512x512 white image
            Mat image = new Mat(512, 512, MatType.CV_8UC3, Scalar.White);

            // Draw some horizontal lines
            Cv2.Line(image, new CvPoint(50, 100), new CvPoint(450, 100), Scalar.Black, 2);
            Cv2.Line(image, new CvPoint(50, 200), new CvPoint(450, 200), Scalar.Black, 2);
            Cv2.Line(image, new CvPoint(50, 300), new CvPoint(450, 300), Scalar.Black, 2);
            Cv2.Line(image, new CvPoint(50, 400), new CvPoint(450, 400), Scalar.Black, 2);

            // Draw some diagonal lines (should not be detected as horizontal)
            Cv2.Line(image, new CvPoint(50, 150), new CvPoint(200, 250), Scalar.Black, 2);
            Cv2.Line(image, new CvPoint(300, 350), new CvPoint(450, 450), Scalar.Black, 2);

            // Draw some vertical lines (should not be detected as horizontal)
            Cv2.Line(image, new CvPoint(100, 50), new CvPoint(100, 450), Scalar.Black, 2);
            Cv2.Line(image, new CvPoint(400, 50), new CvPoint(400, 450), Scalar.Black, 2);

            return image;
        }
    }
}