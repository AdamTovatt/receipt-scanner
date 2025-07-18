using Microsoft.AspNetCore.Http;
using Moq;
using ReceiptScanner.Models;
using ReceiptScanner.Services;
using Microsoft.ML.OnnxRuntime;
using ReceiptScanner;
using EasyReasy;

namespace ReceiptScannerTests
{
    [TestClass]
    public class ReceiptScannerServiceTests
    {
        private static ResourceManager _resourceManager = null!;

        [ClassInitialize]
        public static async Task BeforeAll(TestContext testContext)
        {
            _resourceManager = await ResourceManager.CreateInstanceAsync();
        }

        [TestMethod]
        public async Task InspectOnnxModel_PrintModelInfo()
        {
            try
            {
                // Load model bytes
                byte[] modelBytes = await _resourceManager.ReadAsBytesAsync(Resources.Models.PgNetModel);
                Console.WriteLine($"Model size: {modelBytes.Length / (1024 * 1024):F2} MB");

                // Create inference session to inspect model
                using InferenceSession session = new InferenceSession(modelBytes);

                // Print input information
                Console.WriteLine("\n=== MODEL INPUTS ===");
                foreach (KeyValuePair<string, NodeMetadata> input in session.InputMetadata)
                {
                    Console.WriteLine($"Input: {input.Key}");
                    Console.WriteLine($"  Type: {input.Value.ElementType}");
                    Console.WriteLine($"  Shape: {string.Join("x", input.Value.Dimensions.ToArray())}");
                }

                // Print output information
                Console.WriteLine("\n=== MODEL OUTPUTS ===");
                foreach (KeyValuePair<string, NodeMetadata> output in session.OutputMetadata)
                {
                    Console.WriteLine($"Output: {output.Key}");
                    Console.WriteLine($"  Type: {output.Value.ElementType}");
                    Console.WriteLine($"  Shape: {string.Join("x", output.Value.Dimensions.ToArray())}");
                }

                // Print model metadata
                Console.WriteLine("\n=== MODEL METADATA ===");
                ModelMetadata? metadata = session.ModelMetadata;
                if (metadata != null)
                {
                    Console.WriteLine($"Description: {metadata.Description}");
                    Console.WriteLine($"Domain: {metadata.Domain}");
                    Console.WriteLine($"Graph Name: {metadata.GraphName}");
                    Console.WriteLine($"Producer: {metadata.ProducerName}");
                    Console.WriteLine($"Version: {metadata.Version}");
                }

                // Print session options
                Console.WriteLine("\n=== SESSION INFO ===");
                Console.WriteLine($"Session created successfully");
                Console.WriteLine($"Input count: {session.InputMetadata.Count}");
                Console.WriteLine($"Output count: {session.OutputMetadata.Count}");

                // Test with a dummy input to see if model runs
                Console.WriteLine("\n=== TESTING MODEL INFERENCE ===");
                TestModelInference(session);

                Assert.IsTrue(true, "Model inspection completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inspecting model: {ex.Message}");
                Assert.Fail($"Model inspection failed: {ex.Message}");
            }
        }

        private void TestModelInference(InferenceSession session)
        {
            try
            {
                // Get first input info
                KeyValuePair<string, NodeMetadata> firstInput = session.InputMetadata.First();
                string inputName = firstInput.Key;
                int[] inputShape = firstInput.Value.Dimensions.ToArray();

                Console.WriteLine($"Testing with input '{inputName}' shape: {string.Join("x", inputShape)}");

                // Create a dummy input tensor
                int totalElements = 1;
                foreach (int dim in inputShape)
                {
                    if (dim > 0)
                        totalElements *= dim;
                    else
                        totalElements *= 1; // Handle dynamic dimensions
                }

                float[] dummyData = new float[totalElements];
                for (int i = 0; i < dummyData.Length; i++)
                {
                    dummyData[i] = 0.1f; // Small non-zero values
                }

                // Create input tensor
                Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float> inputTensor = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(dummyData, inputShape);
                List<NamedOnnxValue> inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, inputTensor) };

                // Run inference
                using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);
                Console.WriteLine($"Inference successful! Got {results.Count} outputs:");

                foreach (DisposableNamedOnnxValue result in results)
                {
                    Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> tensor = result.AsTensor<float>();
                    Console.WriteLine($"  {result.Name}: {string.Join("x", tensor.Dimensions.ToArray())} = {tensor.Length} elements");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Inference test failed: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task ScanReceiptAsync_NullFile_ReturnsError()
        {
            // Arrange
            Mock<IModelService> modelServiceMock = new Mock<IModelService>();
            ReceiptScannerService service = new ReceiptScannerService(modelServiceMock.Object, _resourceManager);

            // Act
            ReceiptData result = await service.ScanReceiptAsync(null!);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrEmpty(result.Error));
        }

        [TestMethod]
        public async Task ScanReceiptAsync_EmptyFile_ReturnsError()
        {
            // Arrange
            Mock<IModelService> modelServiceMock = new Mock<IModelService>();
            ReceiptScannerService service = new ReceiptScannerService(modelServiceMock.Object, _resourceManager);
            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0);

            // Act
            ReceiptData result = await service.ScanReceiptAsync(fileMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrEmpty(result.Error));
        }

        [TestMethod]
        public async Task ScanReceiptAsync_InvalidFileType_ReturnsError()
        {
            // Arrange
            Mock<IModelService> modelServiceMock = new Mock<IModelService>();
            ReceiptScannerService service = new ReceiptScannerService(modelServiceMock.Object, _resourceManager);
            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100);
            fileMock.Setup(f => f.FileName).Returns("test.txt");

            // Act
            ReceiptData result = await service.ScanReceiptAsync(fileMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrEmpty(result.Error));
        }
    }
}