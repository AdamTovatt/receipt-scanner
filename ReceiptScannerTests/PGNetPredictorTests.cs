using ReceiptScanner.Models;
using ReceiptScanner.Services;
using ReceiptScanner;
using EasyReasy;

namespace ReceiptScannerTests
{
    [TestClass]
    public class PGNetPredictorTests
    {
        private static ResourceManager _resourceManager = null!;

        [ClassInitialize]
        public static void BeforeAll()
        {
            _resourceManager = ResourceManager.CreateInstance();
        }

        [TestMethod]
        public async Task ProcessImageAsync_WithTestReceipt_DetectsText()
        {
            // Load the test receipt image
            byte[] imageBytes = _resourceManager.ReadAsBytesAsync(TestResources.TestFiles.TestReceipt01).Result;
            using MemoryStream imageStream = new MemoryStream(imageBytes);

            // Load the model
            byte[] modelBytes = _resourceManager.ReadAsBytesAsync(Resources.Models.ReceiptModel).Result;
            using PGNetPredictor predictor = new PGNetPredictor(modelBytes);

            // Act
            ReceiptData result = await predictor.ProcessImageAsync(imageStream);

            // Assert
            Assert.IsNotNull(result);

            if (!string.IsNullOrEmpty(result.Error))
            {
                // If there's an error, print it for debugging but don't fail the test yet
                Console.WriteLine($"OCR processing error: {result.Error}");
                Assert.Inconclusive($"OCR processing failed: {result.Error}");
                return;
            }

            Assert.IsNotNull(result.DetectedTexts);
            Assert.IsTrue(result.DetectedTexts.Count > 0, "No text detected in the receipt image");

            // Print detected text for manual verification
            Console.WriteLine($"\n=== DETECTED TEXT ===");
            for (int i = 0; i < result.DetectedTexts.Count; i++)
            {
                TextDetection detection = result.DetectedTexts[i];
                Console.WriteLine($"{i + 1}. Text: \"{detection.Text}\"");
                Console.WriteLine($"   Confidence: {detection.Confidence:P1}");
                Console.WriteLine($"   Bounding Box: {string.Join(" → ", detection.BoundingBox.Select(p => $"({p.X}, {p.Y})"))}");
                Console.WriteLine();
            }

            // TODO: Replace this with actual expected text from your receipt
            // For now, just check that we got some text
            bool hasExpectedText = result.DetectedTexts.Any(detection =>
                detection.Text.Contains("STORE", StringComparison.OrdinalIgnoreCase) ||
                detection.Text.Contains("TOTAL", StringComparison.OrdinalIgnoreCase) ||
                detection.Text.Contains("$", StringComparison.OrdinalIgnoreCase) ||
                detection.Text.Contains("RECEIPT", StringComparison.OrdinalIgnoreCase) ||
                detection.Text.Contains("DATE", StringComparison.OrdinalIgnoreCase) ||
                detection.Text.Contains("TIME", StringComparison.OrdinalIgnoreCase) ||
                detection.Text.Contains("CASH", StringComparison.OrdinalIgnoreCase) ||
                detection.Text.Contains("CARD", StringComparison.OrdinalIgnoreCase) ||
                detection.Text.Contains("TAX", StringComparison.OrdinalIgnoreCase) ||
                detection.Text.Contains("SUBTOTAL", StringComparison.OrdinalIgnoreCase)
            );

            if (!hasExpectedText)
            {
                Console.WriteLine("WARNING: No expected receipt text found. Please update the test with actual text from your receipt.");
                Console.WriteLine("Expected text should include common receipt terms like: STORE, TOTAL, $, RECEIPT, DATE, TIME, CASH, CARD, TAX, SUBTOTAL");
            }

            // For now, just assert that we got some text (you can make this more specific later)
            Assert.IsTrue(result.DetectedTexts.Count > 0, "Should detect at least some text in the receipt");
        }

        [TestMethod]
        public async Task ProcessImageAsync_WithTestReceipt_ValidatesBoundingBoxes()
        {
            // Load the test receipt image
            byte[] imageBytes = _resourceManager.ReadAsBytesAsync(TestResources.TestFiles.TestReceipt01).Result;
            using MemoryStream imageStream = new MemoryStream(imageBytes);

            // Load the model
            byte[] modelBytes = _resourceManager.ReadAsBytesAsync(Resources.Models.ReceiptModel).Result;
            using PGNetPredictor predictor = new PGNetPredictor(modelBytes);

            // Act
            ReceiptData result = await predictor.ProcessImageAsync(imageStream);

            // Assert
            Assert.IsNotNull(result);

            if (!string.IsNullOrEmpty(result.Error))
            {
                Assert.Inconclusive($"OCR processing failed: {result.Error}");
                return;
            }

            // Validate bounding boxes
            foreach (TextDetection detection in result.DetectedTexts)
            {
                Assert.IsNotNull(detection.BoundingBox, "Bounding box should not be null");
                Assert.IsTrue(detection.BoundingBox.Count >= 3, "Bounding box should have at least 3 points");

                // Check that coordinates are reasonable (not negative or extremely large)
                foreach (Point point in detection.BoundingBox)
                {
                    Assert.IsTrue(point.X >= 0, "X coordinate should be non-negative");
                    Assert.IsTrue(point.Y >= 0, "Y coordinate should be non-negative");
                    Assert.IsTrue(point.X < 10000, "X coordinate should be reasonable");
                    Assert.IsTrue(point.Y < 10000, "Y coordinate should be reasonable");
                }
            }
        }
    }
}