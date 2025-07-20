using Microsoft.AspNetCore.Mvc.Testing;
using EasyReasy.OcrClient.Models;
using System.Reflection;
using ReceiptScanner;

namespace EasyReasy.OcrClient.Tests
{
    [TestClass]
    public class OcrClientIntegrationTests
    {
        private WebApplicationFactory<ReceiptScanner.Program> _factory = null!;
        private HttpClient _httpClient = null!;
        private OcrClient _client = null!;
        private static ResourceManager _testResourceManager = null!;

        [ClassInitialize]
        public static async Task BeforeAll(TestContext testContext)
        {
            // Set up test environment variables
            if (!File.Exists("TestVariables.txt"))
            {
                File.WriteAllText("TestVariables.txt", "BYTE_SHELF_URL=https://test.example.com\nBYTE_SHELF_API_KEY=test-api-key");
            }

            EnvironmentVariables.EnvironmentVariables.LoadFromFile("TestVariables.txt");
            EnvironmentVariables.EnvironmentVariables.ValidateVariableNamesIn(typeof(ReceiptScanner.EnvironmentVariable));

            // Create ResourceManager with EmbeddedResourceProvider for test resources
            EmbeddedResourceProvider embeddedProvider = new EmbeddedResourceProvider(Assembly.GetExecutingAssembly());
            PredefinedResourceProvider predefinedProvider = embeddedProvider.AsPredefinedFor(typeof(TestResources.TestFiles));
            _testResourceManager = await ResourceManager.CreateInstanceAsync(predefinedProvider);
        }

        [TestInitialize]
        public void Setup()
        {
            _factory = new WebApplicationFactory<ReceiptScanner.Program>();
            _httpClient = _factory.CreateClient();

            // Get API key from environment variables for testing
            string apiKey = Environment.GetEnvironmentVariable("API_KEY") ?? "test-api-key-123";
            _client = new OcrClient(_httpClient, apiKey);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client?.Dispose();
            _httpClient?.Dispose();
            _factory?.Dispose();
        }

        [TestMethod]
        public async Task Ping_ShouldReturnTrue_WhenApiIsRunning()
        {
            // Act
            bool isHealthy = await _client.PingAsync();

            // Assert
            Assert.IsTrue(isHealthy, "API should be running and responding to ping");
        }

        [TestMethod]
        public async Task GetAvailablePreprocessors_ShouldRequireValidApiKey()
        {
            // Arrange - Create client with invalid API key for this test
            using HttpClient httpClientWithInvalidApiKey = _factory.CreateClient();
            using OcrClient clientWithInvalidApiKey = new OcrClient(httpClientWithInvalidApiKey, "invalid-api-key");

            // Act & Assert - Should fail with invalid API key
            try
            {
                List<string> preprocessors = await clientWithInvalidApiKey.GetAvailablePreprocessorsAsync();
                Assert.Fail("Should have thrown an exception due to invalid API key");
            }
            catch (HttpRequestException ex)
            {
                Assert.IsTrue(ex.Message.Contains("401") || ex.Message.Contains("Invalid API key") || ex.Message.Contains("API key is required"), "Should return 401 Unauthorized");
            }
        }

        [TestMethod]
        public async Task ScanReceipt_ShouldRequireValidApiKey()
        {
            // Arrange
            byte[] imageBytes = await _testResourceManager.ReadAsBytesAsync(TestResources.TestFiles.TestReceipt01);

            // Create client with invalid API key for this test
            using HttpClient httpClientWithInvalidApiKey = _factory.CreateClient();
            using OcrClient clientWithInvalidApiKey = new OcrClient(httpClientWithInvalidApiKey, "invalid-api-key");

            // Act & Assert - Should fail with invalid API key
            try
            {
                OcrResult result = await clientWithInvalidApiKey.ScanReceiptAsync(imageBytes);
                Assert.Fail("Should have thrown an exception due to invalid API key");
            }
            catch (HttpRequestException ex)
            {
                Assert.IsTrue(ex.Message.Contains("401") || ex.Message.Contains("Invalid API key") || ex.Message.Contains("API key is required"), "Should return 401 Unauthorized");
            }
        }

        [TestMethod]
        public async Task ScanReceiptWithPreprocessors_ShouldRequireValidApiKey()
        {
            // Arrange
            byte[] imageBytes = await _testResourceManager.ReadAsBytesAsync(TestResources.TestFiles.TestReceipt01);
            List<string> preprocessors = new List<string> { "Threshold", "NoOp" };

            // Create client with invalid API key for this test
            using HttpClient httpClientWithInvalidApiKey = _factory.CreateClient();
            using OcrClient clientWithInvalidApiKey = new OcrClient(httpClientWithInvalidApiKey, "invalid-api-key");

            // Act & Assert - Should fail with invalid API key
            try
            {
                OcrResult result = await clientWithInvalidApiKey.ScanReceiptAsync(imageBytes, preprocessors);
                Assert.Fail("Should have thrown an exception due to invalid API key");
            }
            catch (HttpRequestException ex)
            {
                Assert.IsTrue(ex.Message.Contains("401") || ex.Message.Contains("Invalid API key") || ex.Message.Contains("API key is required"), "Should return 401 Unauthorized");
            }
        }

        [TestMethod]
        public async Task ScanReceiptWithInvalidPreprocessor_ShouldRequireValidApiKey()
        {
            // Arrange
            byte[] imageBytes = await _testResourceManager.ReadAsBytesAsync(TestResources.TestFiles.TestReceipt01);
            List<string> preprocessors = new List<string> { "InvalidPreprocessor" };

            // Create client with invalid API key for this test
            using HttpClient httpClientWithInvalidApiKey = _factory.CreateClient();
            using OcrClient clientWithInvalidApiKey = new OcrClient(httpClientWithInvalidApiKey, "invalid-api-key");

            // Act & Assert - Should fail with invalid API key
            try
            {
                OcrResult result = await clientWithInvalidApiKey.ScanReceiptAsync(imageBytes, preprocessors);
                Assert.Fail("Should have thrown an exception due to invalid API key");
            }
            catch (HttpRequestException ex)
            {
                Assert.IsTrue(ex.Message.Contains("401") || ex.Message.Contains("Invalid API key") || ex.Message.Contains("API key is required"), "Should return 401 Unauthorized");
            }
        }

        [TestMethod]
        public async Task OcrClient_ShouldWorkWithValidApiKey_WhenApiKeyProvided()
        {
            using HttpClient httpClient = _factory.CreateClient();
            using OcrClient clientWithApiKey = new OcrClient(httpClient, EnvironmentVariables.EnvironmentVariables.GetVariable(EnvironmentVariable.ApiKey));

            // Act
            List<string> preprocessors = await clientWithApiKey.GetAvailablePreprocessorsAsync();

            // Assert
            Assert.IsNotNull(preprocessors);
            Assert.IsTrue(preprocessors.Count > 0, "Should have at least one preprocessor available");
            CollectionAssert.Contains(preprocessors, "Threshold", "Threshold preprocessor should be available");
            CollectionAssert.Contains(preprocessors, "NoOp", "NoOp preprocessor should be available");
        }

        [TestMethod]
        public async Task OcrClient_ShouldFailWithInvalidApiKey_WhenInvalidApiKeyProvided()
        {
            using HttpClient httpClient = _factory.CreateClient();
            using OcrClient clientWithInvalidApiKey = new OcrClient(httpClient, "invalid-key");

            // Act & Assert - Should fail with invalid API key
            try
            {
                List<string> preprocessors = await clientWithInvalidApiKey.GetAvailablePreprocessorsAsync();
                Assert.Fail("Should have thrown an exception due to invalid API key");
            }
            catch (HttpRequestException ex)
            {
                Assert.IsTrue(ex.Message.Contains("401") || ex.Message.Contains("Invalid API key") || ex.Message.Contains("API key is required"), "Should return 401 Unauthorized");
            }
        }
    }
}