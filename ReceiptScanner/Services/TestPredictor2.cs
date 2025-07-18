using Tesseract;

namespace ReceiptScanner.Services
{
    public static class TestPredictor2
    {
        public static async Task<string> PredictAsync(byte[] imageBytes, string modelDirectory, string language)
        {
            await Task.CompletedTask;

            // Initialize Tesseract engine with English language support
            using (TesseractEngine engine = new TesseractEngine(modelDirectory, language, EngineMode.Default))
            {
                // Load image from file
                using (Pix img = Pix.LoadFromMemory(imageBytes))
                {
                    // Process image with Tesseract to extract text
                    using (Page page = engine.Process(img))
                    {
                        string text = page.GetText();
                        return text; // Print extracted text to console
                    }
                }
            }
        }
    }
}
