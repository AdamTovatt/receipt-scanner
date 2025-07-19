using EasyReasy;
using EasyReasy.ByteShelfProvider;
using EasyReasy.EnvironmentVariables;
using ReceiptScanner.Tests.Configuration;
using System.Reflection;

namespace ReceiptScanner.Tests.Tests
{
    [TestClass]
    public class ResourceTests
    {
        [TestMethod]
        public async Task VerifyAllResourcesAreMappedCorrectly()
        {
            // Arrange
            if (!File.Exists("TestVariables.txt"))
            {
                File.WriteAllText("TestVariables.txt", "VARIABLE1=value1\nVARIABLE2=value2");
            }

            EnvironmentVariables.LoadFromFile("TestVariables.txt");
            EnvironmentVariables.ValidateVariableNamesIn(typeof(TestEnvironmentVariables));

            string apiKey = EnvironmentVariables.GetVariable(TestEnvironmentVariables.ByteShelfApiKey);
            string url = EnvironmentVariables.GetVariable(TestEnvironmentVariables.ByteShelfBackendUrl);

            FileSystemCache fileSystemCache = new FileSystemCache("CachedResources");
            PredefinedResourceProvider predefinedByteShelfProvider = ByteShelfResourceProvider.CreatePredefined(typeof(Resources.Models), url, apiKey, cache: fileSystemCache);

            Assembly mainProjectAssembly = Assembly.GetAssembly(typeof(Program)) ?? throw new Exception($"Could not find the assembly of {nameof(Program)}");

            // Act & Assert
            // This will throw an exception if any resources are not mapped correctly
            ResourceManager resourceManager = await ResourceManager.CreateInstanceAsync(mainProjectAssembly, predefinedByteShelfProvider);

            // Verify that all resource collections are discovered
            Assert.IsTrue(resourceManager.GetResourcesForCollection(typeof(Resources.Models)).Count > 0, "Models resource collection should contain resources");
            Assert.IsTrue(resourceManager.GetResourcesForCollection(typeof(Resources.Frontend)).Count > 0, "Frontend resource collection should contain resources");
            Assert.IsTrue(resourceManager.GetResourcesForCollection(typeof(Resources.CornerDetection)).Count > 0, "CornerDetection resource collection should contain resources");
        }
    }
}