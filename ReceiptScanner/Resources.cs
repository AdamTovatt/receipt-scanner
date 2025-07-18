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
            public static readonly Resource TesseractEnglishModel = new Resource("Tesseract/tessdata/eng.traineddata");
            public static readonly Resource TesseractSwedishModel = new Resource("Tesseract/tessdata/swe.traineddata");
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


    }
}