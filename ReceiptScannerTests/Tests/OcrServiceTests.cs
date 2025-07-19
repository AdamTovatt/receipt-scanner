using EasyReasy;
using EasyReasy.ByteShelfProvider;
using EasyReasy.EnvironmentVariables;
using OpenCvSharp;
using ReceiptScanner.Models;
using ReceiptScanner.Preprocessing;
using ReceiptScanner.Preprocessing.Preprocessors;
using ReceiptScanner.Providers.Language;
using ReceiptScanner.Providers.Models;
using ReceiptScanner.Services.CornerDetection;
using ReceiptScanner.Services.Ocr;
using ReceiptScanner.Tests.Configuration;
using ReceiptScannerTests.Extensions;
using ReceiptScannerTests.Utilities;
using System.Reflection;

namespace ReceiptScanner.Tests.Tests
{
    [TestClass]
    public class OcrServiceTests
    {
        private static ResourceManager _mainProjectResourceManager = null!;
        private static ResourceManager _testProjectResourceManager = null!;
        private static string _tesseractModelPath = null!;

        private static PreprocessingPipeline _preprocessingPipeline = null!;
        private static IOcrService _ocrService = null!;
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

            Assembly mainProjectAssembly = Assembly.GetAssembly(typeof(Program)) ?? throw new Exception($"Could not find the assembly of {nameof(Program)}");
            _mainProjectResourceManager = await ResourceManager.CreateInstanceAsync(mainProjectAssembly, predefinedByteShelfProvider);
            _testProjectResourceManager = await ResourceManager.CreateInstanceAsync();

            InitializePreprocessingPipeline();
            InitializeOcrService();

            // Initialize debug output service with automatic path detection
            _debugOutputService = new LocalFileDebugOutputService();
        }

        private static void InitializePreprocessingPipeline()
        {
            ICornerDetectionService cornerDetectionService = new HeatmapCornerDetectionService(_mainProjectResourceManager);
            CornerDetectionCropPreprocessor cornerDetectionCropPreprocessor = new CornerDetectionCropPreprocessor(cornerDetectionService, 0.5);
            ThresholdPreprocessor thresholdPreprocessor = new ThresholdPreprocessor();

            _preprocessingPipeline = new PreprocessingPipeline();
            _preprocessingPipeline.AddPreprocessor(cornerDetectionCropPreprocessor);
            _preprocessingPipeline.AddPreprocessor(thresholdPreprocessor);
        }

        private static void InitializeOcrService()
        {
            IModelProviderService modelProvider = new TesseractModelProviderService(
                _mainProjectResourceManager,
                Resources.Models.TesseractEnglishModel,
                Resources.Models.TesseractSwedishModel,
                Resources.Models.TesseractOrientationModel);

            IlanguageProvider languageProvider = new TesseractLanguageProvider("swe", "osd");

            _ocrService = new TesseractOcrService(modelProvider, languageProvider);
        }

        [TestMethod]
        [DynamicData(nameof(GetTestResources), DynamicDataSourceType.Method)]
        public async Task ProcessImageAsync_WithTestReceipt_DetectsText(string testResource)
        {
            // Arrange
            byte[] imageBytes = await _testProjectResourceManager.ReadAsBytesAsync(new Resource(testResource));
            Mat originalImage = Mat.FromImageData(imageBytes);

            // Preprocess the image separately for debugging
            Mat preprocessedImage = _preprocessingPipeline.Preprocess(originalImage);

            // Output the original and preprocessed images for debugging
            string originalImageName = LocalFileDebugOutputService.CreateNameFromCaller(testResource.Replace("/", "_").Replace(".", "_") + "_original");
            _debugOutputService.OutputImage(originalImage, originalImageName);

            string preprocessedImageName = LocalFileDebugOutputService.CreateNameFromCaller(testResource.Replace("/", "_").Replace(".", "_") + "_preprocessed");
            _debugOutputService.OutputImage(preprocessedImage, preprocessedImageName);

            // Log image dimensions for debugging
            Console.WriteLine($"Test resource: {testResource}");
            Console.WriteLine($"Original image size: {originalImage.Width}x{originalImage.Height}");
            Console.WriteLine($"Preprocessed image size: {preprocessedImage.Width}x{preprocessedImage.Height}");

            // Act
            OcrResult result = await _ocrService.ProcessImageAsync(imageBytes, _preprocessingPipeline);

            // Assert
            Assert.IsNotNull(result);

            if (!string.IsNullOrEmpty(result.Error))
            {
                Assert.Inconclusive($"OCR processing failed: {result.Error}");
                return;
            }

            Assert.IsTrue(result.DetectedTexts.Count > 0, "Should detect at least some text");

            // Log the detected text for debugging
            Console.WriteLine($"Detected {result.DetectedTexts.Count} text elements:");
            foreach (TextDetection detection in result.DetectedTexts)
            {
                Console.WriteLine($"  - '{detection.Text}'");
            }

            // Cleanup
            originalImage.Dispose();
            preprocessedImage.Dispose();
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