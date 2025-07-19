using OpenCvSharp;

namespace ReceiptScanner.Preprocessing
{
    public class PreprocessingPipeline : IPreprocessingPipeline
    {
        private readonly List<IImagePreprocessor> _preprocessors = new List<IImagePreprocessor>();

        public void AddPreprocessor(IImagePreprocessor preprocessor)
        {
            if (preprocessor == null)
                throw new ArgumentNullException(nameof(preprocessor));

            _preprocessors.Add(preprocessor);
        }

        public Mat Preprocess(Mat input)
        {
            if (input == null || input.Empty())
                throw new ArgumentException("Input image cannot be null or empty", nameof(input));

            if (_preprocessors.Count == 0)
                return input.Clone(); // Return a copy if no preprocessing

            Mat currentImage = input.Clone();

            try
            {
                // Process through each preprocessor in sequence
                foreach (IImagePreprocessor preprocessor in _preprocessors)
                {
                    Mat processedImage = preprocessor.Preprocess(currentImage);

                    // Dispose the previous image and use the new one
                    currentImage.Dispose();
                    currentImage = processedImage;
                }

                return currentImage;
            }
            catch
            {
                // Ensure we clean up on error
                currentImage?.Dispose();
                throw;
            }
        }
    }
}