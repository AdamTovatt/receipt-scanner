using EasyReasy;
using EasyReasy.ByteShelfProvider;

namespace ReceiptScanner
{
    /// <summary>
    /// Project-specific resource definitions for the ReceiptScanner application.
    /// </summary>
    public static class Resources
    {
        /// <summary>
        /// Machine learning models used by the receipt scanner.
        /// </summary>
        [ResourceCollection(typeof(ByteShelfResourceProvider))]
        public static class Models
        {
            /// <summary>
            /// File used by Tesseract to detect english text.
            /// </summary>
            public static readonly Resource TesseractEnglishModel = new Resource("Tesseract/tessdata/eng.traineddata");

            /// <summary>
            /// File used by Tesseract to detect swedish text.
            /// </summary>
            public static readonly Resource TesseractSwedishModel = new Resource("Tesseract/tessdata/swe.traineddata");

            /// <summary>
            /// File used by Tesseract to be able to detect text in images that are not originally oriented correctly.
            /// </summary>
            public static readonly Resource TesseractOrientationModel = new Resource("Tesseract/tessdata/osd.traineddata");
        }

        /// <summary>
        /// Frontend resources for the web interface.
        /// </summary>
        [ResourceCollection(typeof(EmbeddedResourceProvider))]
        public static class Frontend
        {
            /// <summary>
            /// The main HTML frontend for the receipt scanner application.
            /// </summary>
            public static readonly Resource ReceiptScannerFrontend = new Resource("Frontend/ReceiptScannerFrontend.html");
        }

        /// <summary>
        /// Machine learning models for corner detection used in document alignment.
        /// </summary>
        [ResourceCollection(typeof(EmbeddedResourceProvider))]
        public static class CornerDetection
        {
            /// <summary>
            /// FastViT SA24 model for high-accuracy corner detection with BIFPN architecture.
            /// </summary>
            public static readonly Resource FastVitSa24HEBifpn256Fp32 = new Resource("CornerDetection/fastvit_sa24_h_e_bifpn_256_fp32.onnx");

            /// <summary>
            /// LCNet 100 model for efficient corner detection with BIFPN architecture.
            /// </summary>
            public static readonly Resource Lcnet100HEBifpn256Fp32 = new Resource("CornerDetection/lcnet100_h_e_bifpn_256_fp32.onnx");

            /// <summary>
            /// LCNet 050 model for lightweight corner detection with multi-decoder architecture.
            /// </summary>
            public static readonly Resource Lcnet050PMultiDecoderL3D64256Fp32 = new Resource("CornerDetection/lcnet050_p_multi_decoder_l3_d64_256_fp32.onnx");

            /// <summary>
            /// FastViT T8 model for balanced corner detection with BIFPN architecture.
            /// </summary>
            public static readonly Resource FastVitT8HEBifpn256Fp32 = new Resource("CornerDetection/fastvit_t8_h_e_bifpn_256_fp32.onnx");
        }
    }
}