using EasyReasy.EnvironmentVariables;

namespace ReceiptScanner.Tests.Configuration
{
    [EnvironmentVariableNameContainer]
    public static class TestEnvironmentVariables
    {
        [EnvironmentVariableName(5)]
        public static string ByteShelfBackendUrl = "BYTE_SHELF_URL";
        [EnvironmentVariableName(5)]
        public static string ByteShelfApiKey = "BYTE_SHELF_API_KEY";
    }
}