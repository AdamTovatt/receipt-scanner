using ReceiptScanner.Preprocessing.Preprocessors;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public static class ReceiptEdgeDetectionFactory
    {
        public static ReceiptEdgeDetectionPreprocessor CreateDefault()
        {
            EdgeDetectionConfig config = new EdgeDetectionConfig();
            return CreateWithConfig(config);
        }

        public static ReceiptEdgeDetectionPreprocessor CreateWithConfig(EdgeDetectionConfig config)
        {
            IImagePreprocessor imagePreprocessor = new GrayscaleBlurPreprocessor(config);
            IEdgeDetector edgeDetector = new CannyEdgeDetector(config);
            IContourFinder contourFinder = new ContourFinder();
            IContourSelector contourSelector = new LargestContourSelector(config);
            ICornerDetector cornerDetector = new PolygonCornerDetector(config);
            ICornerSorter cornerSorter = new ClockwiseCornerSorter();
            IPerspectiveTransformer perspectiveTransformer = new PerspectiveTransformer();

            return new ReceiptEdgeDetectionPreprocessor(
                imagePreprocessor,
                edgeDetector,
                contourFinder,
                contourSelector,
                cornerDetector,
                cornerSorter,
                perspectiveTransformer);
        }

        public static ReceiptEdgeDetectionPreprocessor CreateWithCustomComponents(
            IImagePreprocessor imagePreprocessor,
            IEdgeDetector edgeDetector,
            IContourFinder contourFinder,
            IContourSelector contourSelector,
            ICornerDetector cornerDetector,
            ICornerSorter cornerSorter,
            IPerspectiveTransformer perspectiveTransformer)
        {
            return new ReceiptEdgeDetectionPreprocessor(
                imagePreprocessor,
                edgeDetector,
                contourFinder,
                contourSelector,
                cornerDetector,
                cornerSorter,
                perspectiveTransformer);
        }
    }
} 