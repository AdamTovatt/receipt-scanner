using ReceiptScanner.Models;
using Tesseract;
using ReceiptScanner.Preprocessing;
using ReceiptScanner.Providers.Language;
using ReceiptScanner.Providers.Models;
using OpenCvSharp;

namespace ReceiptScanner.Services.Ocr
{
    public class TesseractOcrService : IOcrService
    {
        private readonly IModelProviderService _modelProvider;
        private readonly IlanguageProvider _languageProvider;
        private bool _disposed = false;

        public TesseractOcrService(IModelProviderService modelProvider, IlanguageProvider languageProvider)
        {
            _modelProvider = modelProvider ?? throw new ArgumentNullException(nameof(modelProvider));
            _languageProvider = languageProvider ?? throw new ArgumentNullException(nameof(languageProvider));
        }

        public bool IsModelLoaded => _modelProvider != null && !_disposed;

        public async Task<OcrResult> ProcessImageAsync(byte[] imageBytes, IPreprocessingPipeline preprocessingPipeline)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TesseractOcrService));
            }

            if (imageBytes == null || imageBytes.Length == 0)
            {
                return new OcrResult { Error = "No image data provided" };
            }

            Mat? inputImage = null;
            Mat? preprocessedImage = null;

            try
            {
                // Get model path and language from providers
                string modelPath = await _modelProvider.GetModelPathAsync();
                string language = _languageProvider.GetLanguage();

                // Convert byte array to Mat
                inputImage = Cv2.ImDecode(imageBytes, ImreadModes.Color);
                if (inputImage.Empty())
                {
                    return new OcrResult { Error = "Failed to decode image" };
                }

                // Preprocess the image
                preprocessedImage = preprocessingPipeline.Preprocess(inputImage);

                // Process with Tesseract
                using TesseractEngine engine = new TesseractEngine(modelPath, language, EngineMode.LstmOnly);
                engine.DefaultPageSegMode = PageSegMode.AutoOsd;

                // Convert Mat to byte array for Tesseract
                byte[] imageBytesForTesseract = preprocessedImage.ToBytes(".png");
                using Pix img = Pix.LoadFromMemory(imageBytesForTesseract);
                using Page page = engine.Process(img);

                // Extract text and bounding boxes
                OcrResult result = new OcrResult();
                using ResultIterator iterator = page.GetIterator();

                iterator.Begin();
                do
                {
                    if (iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out Tesseract.Rect rect))
                    {
                        string text = iterator.GetText(PageIteratorLevel.TextLine);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            result.DetectedTexts.Add(new TextDetection
                            {
                                Text = text.Trim(),
                                BoundingBox = new List<Models.Point>
                                {
                                    new Models.Point { X = rect.X1, Y = rect.Y1 },
                                    new Models.Point { X = rect.X2, Y = rect.Y1 },
                                    new Models.Point { X = rect.X2, Y = rect.Y2 },
                                    new Models.Point { X = rect.X1, Y = rect.Y2 }
                                }
                            });
                        }
                    }
                }
                while (iterator.Next(PageIteratorLevel.TextLine));

                return result;
            }
            catch (Exception ex)
            {
                return new OcrResult { Error = $"OCR processing failed: {ex.Message}" };
            }
            finally
            {
                // Clean up Mat objects
                inputImage?.Dispose();
                preprocessedImage?.Dispose();
            }
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
                _modelProvider?.Dispose();
                _disposed = true;
            }
        }
    }
}