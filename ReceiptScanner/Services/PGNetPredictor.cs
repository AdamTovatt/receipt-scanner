using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ReceiptScanner.Models;

namespace ReceiptScanner.Services
{
    public class PGNetPredictor : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly List<string> _characterDict;
        private readonly PGNetPostProcessor _postProcessor;
        private Image<Rgb24>? _originalImage;
        private bool _disposed = false;

        // Constants from Python code
        private const int MaxSideLen = 768;
        private const float ScoreThreshold = 0.5f;
        private static readonly float[] Mean = { 0.485f, 0.456f, 0.406f };
        private static readonly float[] Std = { 0.229f, 0.224f, 0.225f };

        public PGNetPredictor(byte[] modelBytes)
        {
            _session = new InferenceSession(modelBytes);
            _characterDict = LoadCharacterDict();
            _postProcessor = new PGNetPostProcessor(_characterDict, ScoreThreshold, "totaltext");
        }

        private List<string> LoadCharacterDict()
        {
            string dictContent = ResourceManager.GetInstance().ReadAsStringAsync(Resources.Dictionaries.IC15Dict).Result;
            return dictContent.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public async Task<ReceiptData> ProcessImageAsync(Stream imageStream)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PGNetPredictor));
            }

            try
            {
                // Load and preprocess image
                (float[] preprocessedImage, int[] shapeList) = await PreprocessImageAsync(imageStream);
                
                // Run inference
                Dictionary<string, Tensor<float>> predictions = await PredictAsync(preprocessedImage);
                
                // Postprocess results using the proper postprocessor
                PGNetPostProcessResult postProcessResult = _postProcessor.Postprocess(predictions, shapeList);
                
                // Create result
                ReceiptData result = new ReceiptData();
                for (int i = 0; i < postProcessResult.Texts.Count; i++)
                {
                    result.DetectedTexts.Add(new TextDetection
                    {
                        Text = postProcessResult.Texts[i],
                        BoundingBox = postProcessResult.BoundingBoxes[i].Select(p => new Models.Point { X = p.X, Y = p.Y }).ToList(),
                        Confidence = 0.8 // This should come from the actual score
                    });
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return new ReceiptData { Error = $"OCR processing failed: {ex.Message}" };
            }
        }

        private async Task<(float[] image, int[] shapeList)> PreprocessImageAsync(Stream imageStream)
        {
            // Load image
            _originalImage = await Image.LoadAsync<Rgb24>(imageStream);
            Image<Rgb24> image = _originalImage.Clone();

            // E2EResizeForTest equivalent
            (int newWidth, int newHeight) = ResizeImage(image.Width, image.Height, MaxSideLen);
            image.Mutate(x => x.Resize(newWidth, newHeight));

            // Convert to tensor and normalize
            float[] tensor = new float[3 * newHeight * newWidth];
            int index = 0;

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    Rgb24 pixel = image[x, y];

                    // Normalize (same as Python code)
                    float r = (pixel.R / 255.0f - Mean[0]) / Std[0];
                    float g = (pixel.G / 255.0f - Mean[1]) / Std[1];
                    float b = (pixel.B / 255.0f - Mean[2]) / Std[2];

                    // CHW format
                    tensor[index] = r;
                    tensor[index + newHeight * newWidth] = g;
                    tensor[index + 2 * newHeight * newWidth] = b;
                    index++;
                }
            }

            // Shape list: [batch_size, 3, height, width]
            int[] shapeList = { 1, 3, newHeight, newWidth };

            return (tensor, shapeList);
        }

        private (int newWidth, int newHeight) ResizeImage(int width, int height, int maxSideLen)
        {
            float ratio = Math.Min((float)maxSideLen / width, (float)maxSideLen / height);
            int newWidth = (int)(width * ratio);
            int newHeight = (int)(height * ratio);
            
            // Ensure dimensions are multiples of 32 (common requirement for OCR models)
            newWidth = (newWidth / 32) * 32;
            newHeight = (newHeight / 32) * 32;
            
            return (newWidth, newHeight);
        }

        private async Task<Dictionary<string, Tensor<float>>> PredictAsync(float[] preprocessedImage)
        {
            await Task.CompletedTask;

            // Create input tensor
            DenseTensor<float> inputTensor = new DenseTensor<float>(preprocessedImage, new int[] { 1, 3, preprocessedImage.Length / 3, 1 });
            
            // Get input name from model
            string inputName = _session.InputNames.First();
            List<NamedOnnxValue> inputs = new List<NamedOnnxValue> 
            { 
                NamedOnnxValue.CreateFromTensor(inputName, inputTensor) 
            };

            // Run inference
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs);

            // Extract outputs
            Dictionary<string, Tensor<float>> predictions = new Dictionary<string, Tensor<float>>();
            string[] outputNames = { "f_border", "f_char", "f_direction", "f_score" };

            for (int i = 0; i < results.Count; i++)
            {
                DisposableNamedOnnxValue output = results.ElementAt(i);
                predictions[outputNames[i]] = output.AsTensor<float>();
            }

            return predictions;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _session?.Dispose();
                _originalImage?.Dispose();
                _disposed = true;
            }
        }
    }
}