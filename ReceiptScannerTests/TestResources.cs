namespace ReceiptScannerTests
{
    public readonly struct TestResource
    {
        public string Path { get; }

        private TestResource(string path)
        {
            Path = path;
        }

        public static TestResource Create(string resourcePath)
        {
            return new TestResource(resourcePath);
        }

        public string GetFileName()
        {
            return System.IO.Path.GetFileName(Path);
        }

        public override string ToString() => Path;

        public static implicit operator string(TestResource resourcePath) => resourcePath.Path;

        public static class TestFiles
        {
            public static readonly TestResource TestReceipt01 = new TestResource("TestFiles/TestReceipt01.jpeg");
        }
    }
} 