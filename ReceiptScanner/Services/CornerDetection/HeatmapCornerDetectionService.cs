using EasyReasy;
using OpenCvSharp;
using ReceiptScanner.Models;

namespace ReceiptScanner.Services.CornerDetection
{
    public class HeatmapCornerDetectionService : ICornerDetectionService, IDisposable
    {
        private readonly ResourceManager _resourceManager;
        private readonly CornerDetectionPreprocessingService _preprocessingService;
        private readonly CornerDetectionModelInferenceService _inferenceService;
        private readonly HeatmapCornerDetectionPostprocessor _postprocessor;

        public HeatmapCornerDetectionService(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _preprocessingService = new CornerDetectionPreprocessingService();
            _inferenceService = new CornerDetectionModelInferenceService(
                resourceManager,
                Resources.CornerDetection.FastVitSa24HEBifpn256Fp32);
            _postprocessor = new HeatmapCornerDetectionPostprocessor();
        }

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
            catch
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

        public void Dispose()
        {
            _inferenceService?.Dispose();
        }
    }
}
