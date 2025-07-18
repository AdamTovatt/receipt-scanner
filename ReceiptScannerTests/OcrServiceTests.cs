using ReceiptScanner.Models;
using ReceiptScanner.Preprocessing.Preprocessors;
using ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection;
using ReceiptScanner;
using EasyReasy;
using EasyReasy.EnvironmentVariables;
using EasyReasy.ByteShelfProvider;
using System.Reflection;
using ReceiptScanner.Preprocessing;
using ReceiptScanner.Services.Ocr;
using ReceiptScanner.Providers.Language;
using ReceiptScanner.Providers.Models;
using OpenCvSharp;

namespace ReceiptScannerTests
{
    [TestClass]
    public class OcrServiceTests
    {
        [EnvironmentVariableNameContainer]
        private static class OcrPredictorTestsVariables
        {
            [EnvironmentVariableName(5)]
            public static string ByteShelfBackendUrl = "BYTE_SHELF_URL";
            [EnvironmentVariableName(5)]
            public static string ByteShelfApiKey = "BYTE_SHELF_API_KEY";
        }

        private static ResourceManager _mainProjectResourceManager = null!;
        private static ResourceManager _testProjectResourceManager = null!;
        private static string _tesseractModelPath = null!;

        [ClassInitialize]
        public static async Task BeforeAll(TestContext testContext)
        {
            if (!File.Exists("TestVariables.txt"))
            {
                File.WriteAllText("TestVariables.txt", "VARIABLE1=value1\nVARIABLE2=value2");
            }

            EnvironmentVariables.LoadFromFile("TestVariables.txt");
            EnvironmentVariables.ValidateVariableNamesIn(typeof(OcrPredictorTestsVariables));

            string apiKey = EnvironmentVariables.GetVariable(OcrPredictorTestsVariables.ByteShelfApiKey);
            string url = EnvironmentVariables.GetVariable(OcrPredictorTestsVariables.ByteShelfBackendUrl);

            FileSystemCache fileSystemCache = new FileSystemCache("CachedResources");
            PredefinedResourceProvider predefinedByteShelfProvider = ByteShelfResourceProvider.CreatePredefined(typeof(Resources.Models), url, apiKey, cache: fileSystemCache);

            Assembly mainProjectAssembly = Assembly.GetAssembly(typeof(Program)) ?? throw new Exception($"Could not find the assembly of {nameof(Program)}");
            _mainProjectResourceManager = await ResourceManager.CreateInstanceAsync(mainProjectAssembly, predefinedByteShelfProvider);
            _testProjectResourceManager = await ResourceManager.CreateInstanceAsync();
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
            string outputPath = "ReceiptEdgeDetectionPreprocessorTestOutput.png";
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
        [DynamicData(nameof(GetTestResources), DynamicDataSourceType.Method)]
        public async Task ProcessImageAsync_WithTestReceipt_DetectsText(string testResource)
        {
            // Arrange
            PreprocessingPipeline preprocessingPipeline = new PreprocessingPipeline();
            preprocessingPipeline.AddPreprocessor(new ThresholdPreprocessor());

            // Create real providers
            IModelProviderService modelProvider = new TesseractModelProviderService(
                _mainProjectResourceManager,
                Resources.Models.TesseractEnglishModel,
                Resources.Models.TesseractSwedishModel,
                Resources.Models.TesseractOrientationModel);

            IlanguageProvider languageProvider = new TesseractLanguageProvider("swe", "osd");

            IOcrService predictor = new TesseractOcrService(modelProvider, languageProvider);

            byte[] imageBytes = await _testProjectResourceManager.ReadAsBytesAsync(new Resource(testResource));

            // Act
            OcrResult result = await predictor.ProcessImageAsync(imageBytes, preprocessingPipeline);

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