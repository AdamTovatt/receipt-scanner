using EasyReasy;

namespace ReceiptScanner.Tests.Configuration
{
    public static class TestResources
    {
        [ResourceCollection(typeof(EmbeddedResourceProvider))]
        public static class TestFiles
        {
            public static readonly Resource TestReceipt01 = new Resource("TestFiles/TestReceipt01.jpeg");
            public static readonly Resource TestReceipt02 = new Resource("TestFiles/TestReceipt02.jpeg");
            public static readonly Resource TestReceipt03 = new Resource("TestFiles/TestReceipt03.jpeg");
            public static readonly Resource TestReceipt01Cropped = new Resource("TestFiles/TestReceipt01_Cropped.jpg");
            public static readonly Resource SimpleImage = new Resource("TestFiles/SimpleImage.png");
            public static readonly Resource CroppedVat = new Resource("TestFiles/CroppedVat.png");
        }
    }
}