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
            /// The main PGNet model for text detection and recognition.
            /// </summary>
            public static readonly Resource PgNetModel = new Resource("PgNet/pgnet.onnx");
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
        /// Dictionary files used for text processing.
        /// </summary>
        [ResourceCollection(typeof(EmbeddedResourceProvider))]
        public static class Dictionaries
        {
            /// <summary>
            /// IC15 dictionary for text recognition.
            /// </summary>
            public static readonly Resource IC15Dict = new Resource("ic15_dict.txt");
        }
    }
}