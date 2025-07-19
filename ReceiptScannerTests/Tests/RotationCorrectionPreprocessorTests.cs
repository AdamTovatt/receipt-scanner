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
    public class RotationCorrectionPreprocessorTests
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
        public async Task RotationCorrectionPreprocessor_WithReceiptImage_CorrectsRotationCorrectly(string testResource)
        {
            // Arrange
            HeatmapCornerDetectionService cornerDetectionService = new HeatmapCornerDetectionService(_mainProjectResourceManager);
            RotationCorrectionPreprocessor rotationPreprocessor = new RotationCorrectionPreprocessor(cornerDetectionService, 0.5, 45.0);

            // Load image from test resources
            byte[] imageBytes = await _testProjectResourceManager.ReadAsBytesAsync(new Resource(testResource));
            Mat originalImage = Mat.FromImageData(imageBytes);

            // Act
            Mat rotatedImage = rotationPreprocessor.Preprocess(originalImage);

            // Assert
            _debugOutputService.OutputImage(rotatedImage, LocalFileDebugOutputService.CreateNameFromCaller(testResource));

            // Cleanup
            originalImage.Dispose();
            rotatedImage.Dispose();
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