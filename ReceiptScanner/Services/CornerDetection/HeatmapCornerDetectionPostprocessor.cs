using OpenCvSharp;
using ReceiptScanner.Models;

namespace ReceiptScanner.Services.CornerDetection
{
    /// <summary>
    /// Service for postprocessing heatmap-based corner detection results.
    /// </summary>
    public class HeatmapCornerDetectionPostprocessor
    {
        private readonly ContourProcessingService _contourProcessor;
        private const double HeatmapThreshold = 0.3;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeatmapCornerDetectionPostprocessor"/> class.
        /// </summary>
        public HeatmapCornerDetectionPostprocessor()
        {
            _contourProcessor = new ContourProcessingService();
        }

        /// <summary>
        /// Postprocesses heatmap inference results to extract corner coordinates.
        /// </summary>
        /// <param name="inferenceResult">The heatmap inference result from the model.</param>
        /// <param name="preprocessingResult">The preprocessing result containing metadata.</param>
        /// <returns>The corner detection result with four corner coordinates and confidence score.</returns>
        public CornerDetectionResult PostprocessHeatmaps(
            HeatmapInferenceResult inferenceResult,
            PreprocessingResult preprocessingResult)
        {
            List<Models.Point> corners = new List<Models.Point>();
            double confidence = 0.0;

            // Extract heatmaps from the model output
            List<Mat> heatmaps = ExtractHeatmaps(inferenceResult.Heatmaps, inferenceResult.HeatmapShape);

            // Process each corner heatmap
            for (int i = 0; i < 4; i++)
            {
                if (i < heatmaps.Count)
                {
                    OpenCvSharp.Point? corner = ProcessSingleHeatmap(heatmaps[i], preprocessingResult.OriginalSize);
                    if (corner.HasValue)
                    {
                        corners.Add(new Models.Point(corner.Value.X, corner.Value.Y));
                        confidence += 0.25; // Each detected corner adds 25% confidence
                    }
                }
            }

            // Apply center crop alignment if needed
            if (preprocessingResult.CenterCropAlign.X != 0 || preprocessingResult.CenterCropAlign.Y != 0)
            {
                for (int i = 0; i < corners.Count; i++)
                {
                    corners[i] = new Models.Point(
                        corners[i].X + preprocessingResult.CenterCropAlign.X,
                        corners[i].Y + preprocessingResult.CenterCropAlign.Y
                    );
                }
            }

            // Ensure we have exactly 4 corners
            while (corners.Count < 4)
            {
                corners.Add(new Models.Point(0, 0));
            }

            return new CornerDetectionResult
            {
                TopLeft = corners.Count > 0 ? corners[0] : new Models.Point(),
                TopRight = corners.Count > 1 ? corners[1] : new Models.Point(),
                BottomLeft = corners.Count > 2 ? corners[2] : new Models.Point(),
                BottomRight = corners.Count > 3 ? corners[3] : new Models.Point(),
                Confidence = confidence
            };
        }

        private List<Mat> ExtractHeatmaps(float[] heatmapData, int[] heatmapShape)
        {
            List<Mat> heatmaps = new List<Mat>();

            // Expected shape: [1, 4, H, W] for heatmap model
            if (heatmapShape.Length != 4 || heatmapShape[0] != 1 || heatmapShape[1] != 4)
            {
                throw new ArgumentException($"Unexpected heatmap shape: [{string.Join(", ", heatmapShape)}]");
            }

            int height = heatmapShape[2];
            int width = heatmapShape[3];
            int heatmapSize = height * width;

            for (int i = 0; i < 4; i++)
            {
                Mat heatmap = new Mat(height, width, MatType.CV_32F);
                float[] heatmapArray = new float[heatmapSize];

                // Extract the i-th heatmap from the flattened data
                Array.Copy(heatmapData, i * heatmapSize, heatmapArray, 0, heatmapSize);

                // Copy data to Mat
                heatmap.SetArray(heatmapArray);
                heatmaps.Add(heatmap);
            }

            return heatmaps;
        }

        private OpenCvSharp.Point? ProcessSingleHeatmap(Mat heatmap, Size originalSize)
        {
            // Resize heatmap to original image size
            Mat resizedHeatmap = new Mat();
            Cv2.Resize(heatmap, resizedHeatmap, originalSize);

            // Apply threshold
            Mat thresholdedHeatmap = new Mat();
            Cv2.Threshold(resizedHeatmap, thresholdedHeatmap, HeatmapThreshold, 1.0, ThresholdTypes.Binary);

            // Convert to uint8 for contour detection
            Mat binaryImage = new Mat();
            thresholdedHeatmap.ConvertTo(binaryImage, MatType.CV_8U, 255);

            // Find centroid
            OpenCvSharp.Point? centroid = _contourProcessor.FindCentroidFromBinaryImage(binaryImage);

            // Cleanup
            resizedHeatmap.Dispose();
            thresholdedHeatmap.Dispose();
            binaryImage.Dispose();

            return centroid;
        }
    }
}