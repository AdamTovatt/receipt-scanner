using OpenCvSharp;
using ReceiptScanner.Models;

namespace ReceiptScanner.Services.CornerDetection
{
    /// <summary>
    /// Service interface for detecting corners in receipt images.
    /// </summary>
    public interface ICornerDetectionService
    {
        /// <summary>
        /// Detects the four corners of a receipt in the given image.
        /// </summary>
        /// <param name="image">The input image to process.</param>
        /// <returns>A result containing the detected corner coordinates and confidence score.</returns>
        CornerDetectionResult DetectCorners(Mat image);
    }
}