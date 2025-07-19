using EasyReasy;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace ReceiptScanner.Services.CornerDetection
{
    /// <summary>
    /// Service for running ONNX model inference for corner detection.
    /// </summary>
    public class CornerDetectionModelInferenceService : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _inputName;

        /// <summary>
        /// Initializes a new instance of the <see cref="CornerDetectionModelInferenceService"/> class.
        /// </summary>
        /// <param name="resourceManager">The resource manager for loading model resources.</param>
        /// <param name="modelResource">The model resource to load.</param>
        public CornerDetectionModelInferenceService(ResourceManager resourceManager, Resource modelResource)
        {
            byte[] modelBytes = resourceManager.ReadAsBytesAsync(modelResource).GetAwaiter().GetResult();
            _session = new InferenceSession(modelBytes);
            _inputName = _session.InputNames.First();
        }

        /// <summary>
        /// Runs heatmap-based inference on the input image.
        /// </summary>
        /// <param name="inputMat">The preprocessed input image tensor.</param>
        /// <returns>The heatmap inference result containing corner heatmaps.</returns>
        public HeatmapInferenceResult RunHeatmapInference(Mat inputMat)
        {
            // Convert Mat to float array for ONNX Runtime
            float[] inputData = new float[inputMat.Total()];
            inputMat.GetArray(out inputData);

            // Create input tensor with correct dimensions (1, 3, 256, 256)
            int[] dimensions = new int[] { 1, 3, 256, 256 };
            DenseTensor<float> onnxInputTensor = new DenseTensor<float>(inputData, dimensions);

            // Create input container
            List<NamedOnnxValue> inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputName, onnxInputTensor)
            };

            // Run inference
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs);

            // Extract heatmap outputs
            DisposableNamedOnnxValue heatmapOutput = results.First();
            float[] heatmapData = heatmapOutput.AsEnumerable<float>().ToArray();
            int[] heatmapShape = heatmapOutput.AsTensor<float>().Dimensions.ToArray();

            return new HeatmapInferenceResult
            {
                Heatmaps = heatmapData,
                HeatmapShape = heatmapShape
            };
        }

        /// <summary>
        /// Runs point-based inference on the input image.
        /// </summary>
        /// <param name="inputMat">The preprocessed input image tensor.</param>
        /// <returns>The point inference result containing corner coordinates.</returns>
        public PointInferenceResult RunPointInference(Mat inputMat)
        {
            // Convert Mat to float array for ONNX Runtime
            float[] inputData = new float[inputMat.Total()];
            inputMat.GetArray(out inputData);

            // Create input tensor with correct dimensions (1, 3, 256, 256)
            int[] dimensions = new int[] { 1, 3, 256, 256 };
            DenseTensor<float> onnxInputTensor = new DenseTensor<float>(inputData, dimensions);

            // Create input container
            List<NamedOnnxValue> inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputName, onnxInputTensor)
            };

            // Run inference
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs);

            // Extract outputs
            DisposableNamedOnnxValue pointsOutput = results.First(r => r.Name.Contains("points"));
            DisposableNamedOnnxValue hasObjOutput = results.First(r => r.Name.Contains("has_obj"));

            float[] pointsData = pointsOutput.AsEnumerable<float>().ToArray();
            float[] hasObjData = hasObjOutput.AsEnumerable<float>().ToArray();

            return new PointInferenceResult
            {
                Points = pointsData,
                HasObject = hasObjData[0]
            };
        }

        /// <summary>
        /// Disposes of the ONNX inference session.
        /// </summary>
        public void Dispose()
        {
            _session?.Dispose();
        }
    }

    /// <summary>
    /// Result of heatmap-based corner detection inference.
    /// </summary>
    public class HeatmapInferenceResult
    {
        /// <summary>
        /// Gets or sets the heatmap data as a flattened array.
        /// </summary>
        public float[] Heatmaps { get; set; } = Array.Empty<float>();

        /// <summary>
        /// Gets or sets the shape of the heatmap tensor.
        /// </summary>
        public int[] HeatmapShape { get; set; } = Array.Empty<int>();
    }

    /// <summary>
    /// Result of point-based corner detection inference.
    /// </summary>
    public class PointInferenceResult
    {
        /// <summary>
        /// Gets or sets the corner point coordinates as a flattened array.
        /// </summary>
        public float[] Points { get; set; } = Array.Empty<float>();

        /// <summary>
        /// Gets or sets the confidence score indicating if an object was detected.
        /// </summary>
        public float HasObject { get; set; }
    }
}