namespace EasyReasy.OcrClient.Tests
{
    public static class TestResources
    {
        [ResourceCollection(typeof(EmbeddedResourceProvider))]
        public static class TestFiles
        {
            public static readonly Resource TestReceipt01 = new Resource("TestFiles/TestReceipt01.jpeg");
        }
    }
}