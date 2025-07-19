using OpenCvSharp;

namespace ReceiptScanner.Services.CornerDetection
{
    /// <summary>
    /// Service for preprocessing images before corner detection model inference.
    /// </summary>
    public class CornerDetectionPreprocessingService
    {
        private readonly Size _modelInputSize = new Size(256, 256);

        /// <summary>
        /// Preprocesses an image for corner detection model inference.
        /// </summary>
        /// <param name="image">The input image to preprocess.</param>
        /// <param name="doCenterCrop">Whether to apply center cropping to make the image square.</param>
        /// <returns>The preprocessing result containing the tensor and metadata.</returns>
        /// <exception cref="ArgumentException">Thrown when the input image is null, empty, or has incorrect number of channels.</exception>
        public PreprocessingResult PreprocessImage(Mat image, bool doCenterCrop = false)
        {
            if (image == null || image.Empty())
                throw new ArgumentException("Input image cannot be null or empty", nameof(image));

            if (image.Channels() != 3)
                throw new ArgumentException("Input image must have 3 channels (RGB)", nameof(image));

            Mat processedImage = image.Clone();
            Point centerCropAlign = new Point(0, 0);

            // Apply center crop if requested
            if (doCenterCrop)
            {
                processedImage = CenterCrop(processedImage);
                int h = image.Height;
                int w = image.Width;
                
                if (h > w)
                    centerCropAlign = new Point(0, (h - w) / 2);
                else
                    centerCropAlign = new Point((w - h) / 2, 0);
            }

            Size originalSize = new Size(processedImage.Width, processedImage.Height);

            // Resize to model input size
            Mat resizedImage = new Mat();
            Cv2.Resize(processedImage, resizedImage, _modelInputSize);

            // Convert to tensor format (HWC -> CHW) and normalize to [0,1]
            Mat tensorImage = ConvertToTensorFormat(resizedImage);

            // Cleanup intermediate images
            processedImage.Dispose();
            resizedImage.Dispose();

            return new PreprocessingResult
            {
                InputTensor = tensorImage,
                OriginalSize = originalSize,
                ModelInputSize = _modelInputSize,
                CenterCropAlign = centerCropAlign
            };
        }

        private Mat CenterCrop(Mat image)
        {
            int h = image.Height;
            int w = image.Width;
            int size = Math.Min(h, w);
            
            int x = (w - size) / 2;
            int y = (h - size) / 2;
            
            Rect cropRect = new Rect(x, y, size, size);
            return new Mat(image, cropRect);
        }

        private Mat ConvertToTensorFormat(Mat image)
        {
            // Convert BGR to RGB and normalize to [0,1]
            Mat rgbImage = new Mat();
            Cv2.CvtColor(image, rgbImage, ColorConversionCodes.BGR2RGB);
            
            // Convert to float32 and normalize
            Mat floatImage = new Mat();
            rgbImage.ConvertTo(floatImage, MatType.CV_32F, 1.0 / 255.0);
            
            // Create the tensor in the correct format for ONNX (1, 3, 256, 256)
            int height = 256;
            int width = 256;
            int channels = 3;
            
            // Create a 1D array to hold the tensor data
            float[] tensorData = new float[1 * channels * height * width];
            
            // Extract data from the image and rearrange to CHW format
            for (int c = 0; c < channels; c++)
            {
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        // Get the pixel value from the original image
                        Vec3f pixel = floatImage.Get<Vec3f>(h, w);
                        float value = c == 0 ? pixel.Item0 : (c == 1 ? pixel.Item1 : pixel.Item2);
                        
                        // Calculate the index in the CHW format
                        int index = c * height * width + h * width + w;
                        tensorData[index] = value;
                    }
                }
            }
            
            // Create the final tensor Mat
            Mat batchedTensor = new Mat(1, tensorData.Length, MatType.CV_32F);
            batchedTensor.SetArray(tensorData);
            
            rgbImage.Dispose();
            floatImage.Dispose();
            
            return batchedTensor;
        }
    }

    /// <summary>
    /// Result of image preprocessing for corner detection.
    /// </summary>
    public class PreprocessingResult
    {
        /// <summary>
        /// Gets or sets the preprocessed input tensor for model inference.
        /// </summary>
        public Mat InputTensor { get; set; } = null!;
        
        /// <summary>
        /// Gets or sets the original size of the input image.
        /// </summary>
        public Size OriginalSize { get; set; }
        
        /// <summary>
        /// Gets or sets the model input size used for resizing.
        /// </summary>
        public Size ModelInputSize { get; set; }
        
        /// <summary>
        /// Gets or sets the alignment offset when center cropping was applied.
        /// </summary>
        public Point CenterCropAlign { get; set; }
    }
}