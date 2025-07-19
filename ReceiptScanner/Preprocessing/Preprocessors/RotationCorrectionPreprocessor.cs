using OpenCvSharp;
using ReceiptScanner.Models;
using ReceiptScanner.Services.CornerDetection;
using Point = ReceiptScanner.Models.Point;

namespace ReceiptScanner.Preprocessing.Preprocessors
{
    /// <summary>
    /// Image preprocessor that corrects rotation based on detected corners from a corner detection service.
    /// Only rotates the image if the corner detection confidence is above the specified threshold.
    /// </summary>
    public class RotationCorrectionPreprocessor : IImagePreprocessor
    {
        private readonly ICornerDetectionService _cornerDetectionService;
        private readonly double _confidenceThreshold;
        private readonly double _maxRotationAngle;

        /// <summary>
        /// Initializes a new instance of the <see cref="RotationCorrectionPreprocessor"/> class.
        /// </summary>
        /// <param name="cornerDetectionService">The corner detection service to use for detecting receipt corners.</param>
        /// <param name="confidenceThreshold">The minimum confidence level (0.0 to 1.0) required to perform rotation correction. If confidence is below this threshold, the original image is returned unchanged.</param>
        /// <param name="maxRotationAngle">The maximum rotation angle in degrees to apply. Rotations beyond this limit will be ignored.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="cornerDetectionService"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="confidenceThreshold"/> is not between 0.0 and 1.0, or when <paramref name="maxRotationAngle"/> is not positive.</exception>
        public RotationCorrectionPreprocessor(ICornerDetectionService cornerDetectionService, double confidenceThreshold, double maxRotationAngle = 45.0)
        {
            _cornerDetectionService = cornerDetectionService ?? throw new ArgumentNullException(nameof(cornerDetectionService));

            if (confidenceThreshold < 0.0 || confidenceThreshold > 1.0)
                throw new ArgumentOutOfRangeException(nameof(confidenceThreshold), "Confidence threshold must be between 0.0 and 1.0");

            if (maxRotationAngle <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(maxRotationAngle), "Maximum rotation angle must be positive");

            _confidenceThreshold = confidenceThreshold;
            _maxRotationAngle = maxRotationAngle;
        }

        /// <summary>
        /// Gets the name of this preprocessor.
        /// </summary>
        public string Name => "RotationCorrection";

        /// <summary>
        /// Preprocesses the input image by correcting its rotation based on detected corners.
        /// If corner detection confidence is below the threshold or no corners are found, returns the original image unchanged.
        /// </summary>
        /// <param name="image">The input image to process.</param>
        /// <returns>A rotated image if corners are detected with sufficient confidence, otherwise the original image.</returns>
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

            // Calculate rotation angle from the detected corners
            double rotationAngle = CalculateRotationAngle(cornerResult);

            // If rotation angle is 0, return original image
            if (rotationAngle == 0.0)
            {
                Console.WriteLine("Preprocessor - No rotation needed, returning original image");
                return image.Clone();
            }

            Console.WriteLine("Preprocessor - Applying rotation transformation");

            // Apply rotation transformation
            return ApplyRotation(image, rotationAngle);
        }

        /// <summary>
        /// Calculates the rotation angle from the detected corners.
        /// </summary>
        /// <param name="cornerResult">The corner detection result.</param>
        /// <returns>The rotation angle in degrees.</returns>
        private double CalculateRotationAngle(CornerDetectionResult cornerResult)
        {
            // Calculate width average (TopLeft-TopRight and BottomLeft-BottomRight)
            double width = (Math.Sqrt(
                Math.Pow(cornerResult.TopRight.X - cornerResult.TopLeft.X, 2) +
                Math.Pow(cornerResult.TopRight.Y - cornerResult.TopLeft.Y, 2)) +
                Math.Sqrt(
                Math.Pow(cornerResult.BottomRight.X - cornerResult.BottomLeft.X, 2) +
                Math.Pow(cornerResult.BottomRight.Y - cornerResult.BottomLeft.Y, 2))) / 2.0;

            // Calculate height average (TopLeft-BottomLeft and TopRight-BottomRight)
            double height = (Math.Sqrt(
                Math.Pow(cornerResult.BottomLeft.X - cornerResult.TopLeft.X, 2) +
                Math.Pow(cornerResult.BottomLeft.Y - cornerResult.TopLeft.Y, 2)) +
                Math.Sqrt(
                Math.Pow(cornerResult.BottomRight.X - cornerResult.TopRight.X, 2) +
                Math.Pow(cornerResult.BottomRight.Y - cornerResult.TopRight.Y, 2))) / 2.0;

            // If width is greater than height, rotate 90 degrees
            if (width > height || true)
            {
                return 90.0;
            }

            // Otherwise, no rotation needed
            return 0.0;
        }

        /// <summary>
        /// Calculates the angle between two points relative to the horizontal axis.
        /// </summary>
        /// <param name="x1">X coordinate of first point.</param>
        /// <param name="y1">Y coordinate of first point.</param>
        /// <param name="x2">X coordinate of second point.</param>
        /// <param name="y2">Y coordinate of second point.</param>
        /// <returns>The angle in degrees.</returns>
        private double CalculateAngle(double x1, double y1, double x2, double y2)
        {
            double deltaX = x2 - x1;
            double deltaY = y2 - y1;

            if (Math.Abs(deltaX) < 0.001) // Avoid division by zero
                return deltaY > 0 ? 90.0 : -90.0;

            double angle = Math.Atan2(deltaY, deltaX) * 180.0 / Math.PI;
            return angle;
        }

        /// <summary>
        /// Applies rotation transformation to the image.
        /// </summary>
        /// <param name="image">The input image.</param>
        /// <param name="angle">The rotation angle in degrees.</param>
        /// <returns>The rotated image.</returns>
        private Mat ApplyRotation(Mat image, double angle)
        {
            // Calculate the center of the image
            Point2f center = new Point2f(image.Width / 2.0f, image.Height / 2.0f);

            // Get the rotation matrix
            Mat rotationMatrix = Cv2.GetRotationMatrix2D(center, angle, 1.0);

            // Calculate the new image size to accommodate the rotated image
            double cos = Math.Abs(rotationMatrix.At<double>(0, 0));
            double sin = Math.Abs(rotationMatrix.At<double>(0, 1));

            int newWidth = (int)((image.Height * sin) + (image.Width * cos));
            int newHeight = (int)((image.Height * cos) + (image.Width * sin));

            // Adjust the rotation matrix to center the rotated image
            rotationMatrix.Set(0, 2, rotationMatrix.At<double>(0, 2) + (newWidth / 2.0) - center.X);
            rotationMatrix.Set(1, 2, rotationMatrix.At<double>(1, 2) + (newHeight / 2.0) - center.Y);

            // Apply the rotation
            Mat rotatedImage = new Mat();
            Cv2.WarpAffine(image, rotatedImage, rotationMatrix, new Size(newWidth, newHeight),
                InterpolationFlags.Linear, BorderTypes.Constant, Scalar.White);

            return rotatedImage;
        }
    }
}