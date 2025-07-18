using OpenCvSharp;
using ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection;

namespace ReceiptScanner.Preprocessing.Preprocessors
{
    public class ReceiptEdgeDetectionPreprocessor : IImagePreprocessor
    {
        private readonly IImagePreprocessor _imagePreprocessor;
        private readonly IEdgeDetector _edgeDetector;
        private readonly IContourFinder _contourFinder;
        private readonly IContourSelector _contourSelector;
        private readonly ICornerDetector _cornerDetector;
        private readonly ICornerSorter _cornerSorter;
        private readonly IPerspectiveTransformer _perspectiveTransformer;

        public ReceiptEdgeDetectionPreprocessor(
            IImagePreprocessor imagePreprocessor,
            IEdgeDetector edgeDetector,
            IContourFinder contourFinder,
            IContourSelector contourSelector,
            ICornerDetector cornerDetector,
            ICornerSorter cornerSorter,
            IPerspectiveTransformer perspectiveTransformer)
        {
            _imagePreprocessor = imagePreprocessor ?? throw new ArgumentNullException(nameof(imagePreprocessor));
            _edgeDetector = edgeDetector ?? throw new ArgumentNullException(nameof(edgeDetector));
            _contourFinder = contourFinder ?? throw new ArgumentNullException(nameof(contourFinder));
            _contourSelector = contourSelector ?? throw new ArgumentNullException(nameof(contourSelector));
            _cornerDetector = cornerDetector ?? throw new ArgumentNullException(nameof(cornerDetector));
            _cornerSorter = cornerSorter ?? throw new ArgumentNullException(nameof(cornerSorter));
            _perspectiveTransformer = perspectiveTransformer ?? throw new ArgumentNullException(nameof(perspectiveTransformer));
        }

        public string Name => "ReceiptEdgeDetection";

        public Mat Preprocess(Mat image)
        {
            if (image.Empty())
                throw new ArgumentException(nameof(image));

            // Step 1: Preprocess the image (grayscale + blur)
            Mat preprocessedImage = _imagePreprocessor.Preprocess(image);

            // Step 2: Detect edges
            Mat edgeImage = _edgeDetector.DetectEdges(preprocessedImage);

            // Step 3: Find contours
            Point[][] contours = _contourFinder.FindContours(edgeImage);

            // Step 4: Select the best contour
            Point[]? bestContour = _contourSelector.SelectBestContour(contours, image.Width, image.Height);

            if (bestContour == null)
            {
                // Clean up intermediate images
                preprocessedImage.Dispose();
                edgeImage.Dispose();
                return image.Clone();
            }

            // Step 5: Detect corners
            Point[]? corners = _cornerDetector.DetectCorners(bestContour);

            if (corners == null)
            {
                // Clean up intermediate images
                preprocessedImage.Dispose();
                edgeImage.Dispose();
                return image.Clone();
            }

            // Step 6: Sort corners
            Point[] sortedCorners = _cornerSorter.SortCorners(corners);

            // Step 7: Apply perspective transform
            Mat result = _perspectiveTransformer.TransformPerspective(image, sortedCorners);

            // Clean up intermediate images
            preprocessedImage.Dispose();
            edgeImage.Dispose();

            return result;
        }
    }
}