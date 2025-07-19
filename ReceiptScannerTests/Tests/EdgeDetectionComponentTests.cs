using ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection;
using ReceiptScanner.Preprocessing;
using OpenCvSharp;

namespace ReceiptScannerTests.Tests
{
    [TestClass]
    public class EdgeDetectionComponentTests
    {
        [TestMethod]
        public void GrayscaleBlurPreprocessor_WithValidImage_ReturnsProcessedImage()
        {
            // Arrange
            EdgeDetectionConfig config = new EdgeDetectionConfig();
            IImagePreprocessor preprocessor = new GrayscaleBlurPreprocessor(config);

            Mat testImage = CreateTestImage(100, 100);

            // Act
            Mat result = preprocessor.Preprocess(testImage);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Empty());
            Assert.AreEqual(100, result.Width);
            Assert.AreEqual(100, result.Height);

            // Cleanup
            testImage.Dispose();
            result.Dispose();
        }

        [TestMethod]
        public void CannyEdgeDetector_WithValidImage_ReturnsEdgeImage()
        {
            // Arrange
            EdgeDetectionConfig config = new EdgeDetectionConfig();
            IEdgeDetector detector = new CannyEdgeDetector(config);

            Mat testImage = CreateTestImage(100, 100);

            // Act
            Mat result = detector.DetectEdges(testImage);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Empty());

            // Cleanup
            testImage.Dispose();
            result.Dispose();
        }

        [TestMethod]
        public void ContourFinder_WithEdgeImage_ReturnsContours()
        {
            // Arrange
            IContourFinder finder = new ContourFinder();
            Mat edgeImage = CreateEdgeImage(100, 100);

            // Act
            Point[][] contours = finder.FindContours(edgeImage);

            // Assert
            Assert.IsNotNull(contours);

            // Cleanup
            edgeImage.Dispose();
        }

        [TestMethod]
        public void LargestContourSelector_WithValidContours_ReturnsBestContour()
        {
            // Arrange
            EdgeDetectionConfig config = new EdgeDetectionConfig();
            IContourSelector selector = new LargestContourSelector(config);

            Point[][] contours = new Point[][]
            {
                new Point[] { new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10) },
                new Point[] { new Point(0, 0), new Point(20, 0), new Point(20, 20), new Point(0, 20) }
            };

            // Act
            Point[]? result = selector.SelectBestContour(contours, 100, 100);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Length);
        }

        [TestMethod]
        public void PolygonCornerDetector_WithValidContour_ReturnsCorners()
        {
            // Arrange
            EdgeDetectionConfig config = new EdgeDetectionConfig();
            ICornerDetector detector = new PolygonCornerDetector(config);

            Point[] contour = new Point[]
            {
                new Point(0, 0),
                new Point(10, 0),
                new Point(10, 10),
                new Point(0, 10)
            };

            // Act
            Point[]? result = detector.DetectCorners(contour);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Length);
        }

        [TestMethod]
        public void ClockwiseCornerSorter_WithValidCorners_ReturnsSortedCorners()
        {
            // Arrange
            ICornerSorter sorter = new ClockwiseCornerSorter();

            Point[] corners = new Point[]
            {
                new Point(10, 10),
                new Point(0, 0),
                new Point(10, 0),
                new Point(0, 10)
            };

            // Act
            Point[] result = sorter.SortCorners(corners);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Length);
        }

        [TestMethod]
        public void PerspectiveTransformer_WithValidCorners_ReturnsTransformedImage()
        {
            // Arrange
            IPerspectiveTransformer transformer = new PerspectiveTransformer();
            Mat testImage = CreateTestImage(100, 100);

            Point[] corners = new Point[]
            {
                new Point(0, 0),
                new Point(50, 0),
                new Point(50, 50),
                new Point(0, 50)
            };

            // Act
            Mat result = transformer.TransformPerspective(testImage, corners);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Empty());

            // Cleanup
            testImage.Dispose();
            result.Dispose();
        }

        private Mat CreateTestImage(int width, int height)
        {
            Mat image = new Mat(height, width, MatType.CV_8UC3);
            image.SetTo(new Scalar(255, 255, 255)); // White background
            return image;
        }

        private Mat CreateEdgeImage(int width, int height)
        {
            Mat image = new Mat(height, width, MatType.CV_8UC1);
            image.SetTo(new Scalar(0)); // Black background

            // Draw a simple rectangle to create edges
            Cv2.Rectangle(image, new Point(10, 10), new Point(90, 90), new Scalar(255), 2);

            return image;
        }
    }
}