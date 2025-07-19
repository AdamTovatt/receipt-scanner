using OpenCvSharp;
using ReceiptScanner.Models;
using ReceiptScanner.Services.CornerDetection;
using Point = ReceiptScanner.Models.Point;

namespace ReceiptScanner.Preprocessing.Preprocessors
{
    /// <summary>
    /// Image preprocessor that crops an image based on detected corners from a corner detection service.
    /// Only crops the image if the corner detection confidence is above the specified threshold.
    /// </summary>
    public class CornerDetectionCropPreprocessor : IImagePreprocessor
    {
        private readonly ICornerDetectionService _cornerDetectionService;
        private readonly double _confidenceThreshold;

        /// <summary>
        /// Initializes a new instance of the <see cref="CornerDetectionCropPreprocessor"/> class.
        /// </summary>
        /// <param name="cornerDetectionService">The corner detection service to use for detecting receipt corners.</param>
        /// <param name="confidenceThreshold">The minimum confidence level (0.0 to 1.0) required to perform cropping. If confidence is below this threshold, the original image is returned unchanged.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="cornerDetectionService"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="confidenceThreshold"/> is not between 0.0 and 1.0.</exception>
        public CornerDetectionCropPreprocessor(ICornerDetectionService cornerDetectionService, double confidenceThreshold)
        {
            _cornerDetectionService = cornerDetectionService ?? throw new ArgumentNullException(nameof(cornerDetectionService));
            
            if (confidenceThreshold < 0.0 || confidenceThreshold > 1.0)
                throw new ArgumentOutOfRangeException(nameof(confidenceThreshold), "Confidence threshold must be between 0.0 and 1.0");
            
            _confidenceThreshold = confidenceThreshold;
        }

        /// <summary>
        /// Gets the name of this preprocessor.
        /// </summary>
        public string Name => "CornerDetectionCrop";

        /// <summary>
        /// Preprocesses the input image by cropping it based on detected corners.
        /// If corner detection confidence is below the threshold or no corners are found, returns the original image unchanged.
        /// </summary>
        /// <param name="image">The input image to process.</param>
        /// <returns>A cropped image if corners are detected with sufficient confidence, otherwise the original image.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="image"/> is null or empty.</exception>
        public Mat Preprocess(Mat image)
        {
            if (image == null || image.Empty())
                throw new ArgumentException("Input image cannot be null or empty", nameof(image));

            // Detect corners using the corner detection service
            CornerDetectionResult cornerResult = _cornerDetectionService.DetectCorners(image);

            // If confidence is below threshold or no corners found, return original image
            if (cornerResult.Confidence < _confidenceThreshold || !cornerResult.CornersFound)
                return image.Clone();

            // Get the bounding rectangle that encompasses all four corners
            Point[] corners = new Point[]
            {
                cornerResult.TopLeft,
                cornerResult.TopRight,
                cornerResult.BottomLeft,
                cornerResult.BottomRight
            };

            // Calculate the bounding rectangle
            int minX = corners.Min(p => p.X);
            int maxX = corners.Max(p => p.X);
            int minY = corners.Min(p => p.Y);
            int maxY = corners.Max(p => p.Y);

            // Ensure the rectangle is within image bounds
            minX = Math.Max(0, minX);
            maxX = Math.Min(image.Width - 1, maxX);
            minY = Math.Max(0, minY);
            maxY = Math.Min(image.Height - 1, maxY);

            // Create the region of interest (ROI)
            Rect cropRect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);

            // Crop the image using the ROI
            Mat croppedImage = new Mat(image, cropRect);

            return croppedImage.Clone();
        }
    }
}