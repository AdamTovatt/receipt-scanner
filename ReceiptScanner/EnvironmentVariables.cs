using EasyReasy.EnvironmentVariables;

namespace ReceiptScanner
{
    [EnvironmentVariableNameContainer]
    public static class EnvironmentVariables
    {
        [EnvironmentVariableName(minLength: 10)]
        public static readonly string ByteShelfUrl = "BYTE_SHELF_URL";
        [EnvironmentVariableName(minLength: 10)]
        public static readonly string ByteShelfApiKey = "BYTE_SHELF_API_KEY";
    }
}
