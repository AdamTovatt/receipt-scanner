using OpenCvSharp;

namespace ReceiptScanner.Services.CornerDetection
{
    public class CornerDetectionPreprocessingService
    {
        private readonly Size _modelInputSize = new Size(256, 256);

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

            // Convert HWC to CHW format
            Mat tensorImage = new Mat();
            Cv2.Transpose(floatImage, tensorImage);

            // Add batch dimension (1, C, H, W) - reshape manually
            int totalElements = (int)tensorImage.Total();
            Mat batchedTensor = new Mat(1, totalElements, MatType.CV_32F);

            // Copy data to the new shape
            float[] tensorData = new float[totalElements];
            tensorImage.GetArray(out tensorData);
            batchedTensor.SetArray(tensorData);

            rgbImage.Dispose();
            floatImage.Dispose();
            tensorImage.Dispose();

            return batchedTensor;
        }
    }

    public class PreprocessingResult
    {
        public Mat InputTensor { get; set; } = null!;
        public Size OriginalSize { get; set; }
        public Size ModelInputSize { get; set; }
        public Point CenterCropAlign { get; set; }
    }
}