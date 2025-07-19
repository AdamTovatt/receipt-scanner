using EasyReasy;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace ReceiptScanner.Services.CornerDetection
{
    public class CornerDetectionModelInferenceService : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _inputName;

        public CornerDetectionModelInferenceService(ResourceManager resourceManager, Resource modelResource)
        {
            byte[] modelBytes = resourceManager.ReadAsBytesAsync(modelResource).GetAwaiter().GetResult();
            _session = new InferenceSession(modelBytes);
            _inputName = _session.InputNames.First();
        }

        public HeatmapInferenceResult RunHeatmapInference(Mat inputMat)
        {
            // Convert Mat to float array for ONNX Runtime
            float[] inputData = new float[inputMat.Total()];
            inputMat.GetArray(out inputData);

            // Create input tensor
            NodeMetadata inputMeta = _session.InputMetadata[_inputName];
            DenseTensor<float> onnxInputTensor = new DenseTensor<float>(inputData, inputMeta.Dimensions);

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

        public PointInferenceResult RunPointInference(Mat inputMat)
        {
            // Convert Mat to float array for ONNX Runtime
            float[] inputData = new float[inputMat.Total()];
            inputMat.GetArray(out inputData);

            // Create input tensor
            NodeMetadata inputMeta = _session.InputMetadata[_inputName];
            DenseTensor<float> onnxInputTensor = new DenseTensor<float>(inputData, inputMeta.Dimensions);

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

        public void Dispose()
        {
            _session?.Dispose();
        }
    }

    public class HeatmapInferenceResult
    {
        public float[] Heatmaps { get; set; } = Array.Empty<float>();
        public int[] HeatmapShape { get; set; } = Array.Empty<int>();
    }

    public class PointInferenceResult
    {
        public float[] Points { get; set; } = Array.Empty<float>();
        public float HasObject { get; set; }
    }
}