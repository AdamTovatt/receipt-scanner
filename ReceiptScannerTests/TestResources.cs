using EasyReasy;

namespace ReceiptScannerTests
{
    public static class TestResources
    {
        [ResourceCollection(typeof(EmbeddedResourceProvider))]
        public static class TestFiles
        {
            public static readonly Resource TestReceipt01 = new Resource("TestFiles/TestReceipt01.jpeg");
            public static readonly Resource TestReceipt01Cropped = new Resource("TestFiles/TestReceipt01_Cropped.jpg");
            public static readonly Resource SimpleImage = new Resource("TestFiles/SimpleImage.png");
            public static readonly Resource CroppedVat = new Resource("TestFiles/CroppedVat.png");
        }
    }
}