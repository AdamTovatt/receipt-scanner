using EasyReasy;
using OpenCvSharp;
using ReceiptScanner.Models;

namespace ReceiptScanner.Services.CornerDetection
{
    /// <summary>
    /// Service for detecting receipt corners using heatmap-based deep learning models.
    /// </summary>
    public class HeatmapCornerDetectionService : ICornerDetectionService, IDisposable
    {
        private readonly ResourceManager _resourceManager;
        private readonly CornerDetectionPreprocessingService _preprocessingService;
        private readonly CornerDetectionModelInferenceService _inferenceService;
        private readonly HeatmapCornerDetectionPostprocessor _postprocessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeatmapCornerDetectionService"/> class.
        /// </summary>
        /// <param name="resourceManager">The resource manager for loading model resources.</param>
        public HeatmapCornerDetectionService(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _preprocessingService = new CornerDetectionPreprocessingService();
            _inferenceService = new CornerDetectionModelInferenceService(
                resourceManager,
                Resources.CornerDetection.FastVitSa24HEBifpn256Fp32);
            _postprocessor = new HeatmapCornerDetectionPostprocessor();
        }

        /// <summary>
        /// Detects the four corners of a receipt in the given image using heatmap-based inference.
        /// </summary>
        /// <param name="image">The input image to process.</param>
        /// <returns>A result containing the detected corner coordinates and confidence score.</returns>
        public CornerDetectionResult DetectCorners(Mat image)
        {
            try
            {
                // Step 1: Preprocess the image
                PreprocessingResult preprocessingResult = _preprocessingService.PreprocessImage(image);

                // Step 2: Run ONNX inference
                HeatmapInferenceResult inferenceResult = _inferenceService.RunHeatmapInference(preprocessingResult.InputTensor);

                // Step 3: Postprocess the heatmaps
                CornerDetectionResult result = _postprocessor.PostprocessHeatmaps(inferenceResult, preprocessingResult);

                // Cleanup
                preprocessingResult.InputTensor.Dispose();

                return result;
            }
            catch (Exception ex)
            {
                // Return a result indicating failure
                return new CornerDetectionResult
                {
                    TopLeft = new Models.Point(),
                    TopRight = new Models.Point(),
                    BottomLeft = new Models.Point(),
                    BottomRight = new Models.Point(),
                    Confidence = -1.0
                };
            }
        }

        /// <summary>
        /// Disposes of the inference service resources.
        /// </summary>
        public void Dispose()
        {
            _inferenceService?.Dispose();
        }
    }
}
