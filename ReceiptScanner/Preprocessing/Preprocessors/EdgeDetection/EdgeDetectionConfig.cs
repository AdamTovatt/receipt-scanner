namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public class EdgeDetectionConfig
    {
        public double CannyThreshold1 { get; set; } = 50;
        public double CannyThreshold2 { get; set; } = 150;
        public double Epsilon { get; set; } = 0.02;
        public double MinAreaRatio { get; set; } = 0.1;
        public int GaussianBlurKernelSize { get; set; } = 5;
        public double GaussianBlurSigma { get; set; } = 0;
        public double MinContourArea { get; set; } = 1000;
    }
} 