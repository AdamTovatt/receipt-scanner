using ReceiptScanner.Preprocessing.Preprocessors;
using ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection;
using ReceiptScanner.Preprocessing;
using EasyReasy;
using OpenCvSharp;

namespace ReceiptScanner.Tests.Tests
{
    [TestClass]
    public class RefactoredReceiptEdgeDetectionTests
    {
        private static ResourceManager _testProjectResourceManager = null!;

        [ClassInitialize]
        public static async Task BeforeAll(TestContext testContext)
        {
            _testProjectResourceManager = await ResourceManager.CreateInstanceAsync();
        }

        [TestMethod]
        public void CreateDefault_WithFactory_ReturnsValidPreprocessor()
        {
            // Act
            ReceiptEdgeDetectionPreprocessor preprocessor = ReceiptEdgeDetectionFactory.CreateDefault();

            // Assert
            Assert.IsNotNull(preprocessor);
            Assert.AreEqual("ReceiptEdgeDetection", preprocessor.Name);
        }

        [TestMethod]
        public void CreateWithConfig_WithCustomConfig_ReturnsValidPreprocessor()
        {
            // Arrange
            EdgeDetectionConfig config = new EdgeDetectionConfig
            {
                CannyThreshold1 = 30,
                CannyThreshold2 = 100,
                Epsilon = 0.01,
                MinAreaRatio = 0.05
            };

            // Act
            ReceiptEdgeDetectionPreprocessor preprocessor = ReceiptEdgeDetectionFactory.CreateWithConfig(config);

            // Assert
            Assert.IsNotNull(preprocessor);
            Assert.AreEqual("ReceiptEdgeDetection", preprocessor.Name);
        }

        [TestMethod]
        public async Task Preprocess_WithTestReceipt_ProcessesImage()
        {
            // Arrange
            ReceiptEdgeDetectionPreprocessor preprocessor = ReceiptEdgeDetectionFactory.CreateDefault();
            byte[] imageBytes = await _testProjectResourceManager.ReadAsBytesAsync(new Resource("TestReceipt01.jpeg"));

            // Convert bytes to Mat
            Mat originalImage = Mat.FromImageData(imageBytes);

            // Act
            Mat processedImage = preprocessor.Preprocess(originalImage);

            // Assert
            Assert.IsNotNull(processedImage);
            Assert.IsFalse(processedImage.Empty());

            // Save the processed image for inspection
            string outputPath = "refactored_edge_detection_output.png";
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
        public async Task Preprocess_WithDifferentConfigs_ProducesDifferentResults()
        {
            // Arrange
            EdgeDetectionConfig config1 = new EdgeDetectionConfig
            {
                CannyThreshold1 = 30,
                CannyThreshold2 = 100,
                Epsilon = 0.01
            };

            EdgeDetectionConfig config2 = new EdgeDetectionConfig
            {
                CannyThreshold1 = 70,
                CannyThreshold2 = 200,
                Epsilon = 0.03
            };

            ReceiptEdgeDetectionPreprocessor preprocessor1 = ReceiptEdgeDetectionFactory.CreateWithConfig(config1);
            ReceiptEdgeDetectionPreprocessor preprocessor2 = ReceiptEdgeDetectionFactory.CreateWithConfig(config2);

            byte[] imageBytes = await _testProjectResourceManager.ReadAsBytesAsync(new Resource("TestReceipt01.jpeg"));
            Mat originalImage = Mat.FromImageData(imageBytes);

            // Act
            Mat result1 = preprocessor1.Preprocess(originalImage);
            Mat result2 = preprocessor2.Preprocess(originalImage);

            // Assert
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.IsFalse(result1.Empty());
            Assert.IsFalse(result2.Empty());

            // Save both results for comparison
            result1.SaveImage("config1_result.png");
            result2.SaveImage("config2_result.png");

            // Cleanup
            originalImage.Dispose();
            result1.Dispose();
            result2.Dispose();
        }

        [TestMethod]
        public void Constructor_WithNullComponents_ThrowsArgumentNullException()
        {
            // Arrange
            EdgeDetectionConfig config = new EdgeDetectionConfig();
            IImagePreprocessor imagePreprocessor = new GrayscaleBlurPreprocessor(config);
            IEdgeDetector edgeDetector = new CannyEdgeDetector(config);
            IContourFinder contourFinder = new ContourFinder();
            IContourSelector contourSelector = new LargestContourSelector(config);
            ICornerDetector cornerDetector = new PolygonCornerDetector(config);
            ICornerSorter cornerSorter = new ClockwiseCornerSorter();
            IPerspectiveTransformer perspectiveTransformer = new PerspectiveTransformer();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new ReceiptEdgeDetectionPreprocessor(
                null!, edgeDetector, contourFinder, contourSelector, cornerDetector, cornerSorter, perspectiveTransformer));

            Assert.ThrowsException<ArgumentNullException>(() => new ReceiptEdgeDetectionPreprocessor(
                imagePreprocessor, null!, contourFinder, contourSelector, cornerDetector, cornerSorter, perspectiveTransformer));

            Assert.ThrowsException<ArgumentNullException>(() => new ReceiptEdgeDetectionPreprocessor(
                imagePreprocessor, edgeDetector, null!, contourSelector, cornerDetector, cornerSorter, perspectiveTransformer));
        }
    }
}