namespace EasyReasy.EnvironmentVariables.Tests
{
    [TestClass]
    public class EnvironmentVariableTests
    {
        private const string TestVariableName = "TEST_ENV_VARIABLE";

        [TestCleanup]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable(TestVariableName, null);
            Environment.SetEnvironmentVariable("TEST_VAR_1", null);
            Environment.SetEnvironmentVariable("TEST_VAR_2", null);
            Environment.SetEnvironmentVariable("TEST_VAR_5", null);
            Environment.SetEnvironmentVariable("TEST_VAR_6", null);
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithValidVariable_ReturnsValue()
        {
            // Arrange
            string expectedValue = "test-value";
            Environment.SetEnvironmentVariable(TestVariableName, expectedValue);

            // Act
            string result = EnvironmentVariable.GetStringValue(TestVariableName);

            // Assert
            Assert.AreEqual(expectedValue, result);
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithMissingVariable_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => EnvironmentVariable.GetStringValue("NON_EXISTENT_VARIABLE"));
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithEmptyVariable_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable(TestVariableName, "");

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => EnvironmentVariable.GetStringValue(TestVariableName));
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithWhitespaceVariable_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable(TestVariableName, "   ");

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => EnvironmentVariable.GetStringValue(TestVariableName));
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithMinLength_ValidLength_ReturnsValue()
        {
            // Arrange
            string expectedValue = "test-value";
            Environment.SetEnvironmentVariable(TestVariableName, expectedValue);

            // Act
            string result = EnvironmentVariable.GetStringValue(TestVariableName, 5);

            // Assert
            Assert.AreEqual(expectedValue, result);
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithMinLength_TooShort_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable(TestVariableName, "short");

            // Act & Assert
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => EnvironmentVariable.GetStringValue(TestVariableName, 10));
            Assert.IsTrue(exception.Message.Contains("minimum required length is 10"));
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithMinLength_ExactLength_ReturnsValue()
        {
            // Arrange
            string expectedValue = "exact";
            Environment.SetEnvironmentVariable(TestVariableName, expectedValue);

            // Act
            string result = EnvironmentVariable.GetStringValue(TestVariableName, 5);

            // Assert
            Assert.AreEqual(expectedValue, result);
        }

        [TestMethod]
        public void ValidateVariableNamesIn_WithValidConfiguration_DoesNotThrow()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TEST_VAR_1", "valid-value");
            Environment.SetEnvironmentVariable("TEST_VAR_2", "another-valid-value");

            // Act & Assert
            EnvironmentVariable.ValidateVariableNamesIn(typeof(TestConfiguration));
        }

        [TestMethod]
        public void ValidateVariableNamesIn_WithMissingVariable_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TEST_VAR_1", "valid-value");
            // TEST_VAR_2 is not set

            // Act & Assert
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => EnvironmentVariable.ValidateVariableNamesIn(typeof(TestConfiguration)));
            Assert.IsTrue(exception.Message.Contains("TEST_VAR_2"));
            Assert.IsTrue(exception.Message.Contains("is not set or is empty"));
        }

        [TestMethod]
        public void ValidateVariableNamesIn_WithEmptyVariable_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TEST_VAR_1", "valid-value");
            Environment.SetEnvironmentVariable("TEST_VAR_2", "");

            // Act & Assert
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => EnvironmentVariable.ValidateVariableNamesIn(typeof(TestConfiguration)));
            Assert.IsTrue(exception.Message.Contains("TEST_VAR_2"));
            Assert.IsTrue(exception.Message.Contains("is not set or is empty"));
        }

        [TestMethod]
        public void ValidateVariableNamesIn_WithWhitespaceVariable_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TEST_VAR_1", "valid-value");
            Environment.SetEnvironmentVariable("TEST_VAR_2", "   ");

            // Act & Assert
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => EnvironmentVariable.ValidateVariableNamesIn(typeof(TestConfiguration)));
            Assert.IsTrue(exception.Message.Contains("TEST_VAR_2"));
            Assert.IsTrue(exception.Message.Contains("is not set or is empty"));
        }

        [TestMethod]
        public void ValidateVariableNamesIn_WithMinLengthRequirement_ValidLength_DoesNotThrow()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TEST_VAR_5", "valid-long-value");
            Environment.SetEnvironmentVariable("TEST_VAR_6", "another-long-value");

            // Act & Assert
            EnvironmentVariable.ValidateVariableNamesIn(typeof(TestConfigurationWithMinLength));
        }

        [TestMethod]
        public void ValidateVariableNamesIn_WithMinLengthRequirement_TooShort_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TEST_VAR_5", "short");
            Environment.SetEnvironmentVariable("TEST_VAR_6", "another-long-value");

            // Act & Assert
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => EnvironmentVariable.ValidateVariableNamesIn(typeof(TestConfigurationWithMinLength)));
            Assert.IsTrue(exception.Message.Contains("TEST_VAR_5"));
            Assert.IsTrue(exception.Message.Contains("minimum required length is 10"));
        }

        [TestMethod]
        public void ValidateVariableNamesIn_WithMinLengthRequirement_ExactLength_DoesNotThrow()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TEST_VAR_5", "exactlyten");
            Environment.SetEnvironmentVariable("TEST_VAR_6", "another-long-value");

            // Act & Assert
            EnvironmentVariable.ValidateVariableNamesIn(typeof(TestConfigurationWithMinLength));
        }

        [TestMethod]
        public void ValidateVariableNamesIn_WithMultipleConfigurations_AllValid_DoesNotThrow()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TEST_VAR_1", "valid-value");
            Environment.SetEnvironmentVariable("TEST_VAR_2", "another-valid-value");
            Environment.SetEnvironmentVariable("TEST_VAR_5", "valid-long-value");
            Environment.SetEnvironmentVariable("TEST_VAR_6", "another-long-value");

            // Act & Assert
            EnvironmentVariable.ValidateVariableNamesIn(typeof(TestConfiguration), typeof(TestConfigurationWithMinLength));
        }

        [TestMethod]
        public void ValidateVariableNamesIn_WithMultipleConfigurations_OneInvalid_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TEST_VAR_1", "valid-value");
            Environment.SetEnvironmentVariable("TEST_VAR_2", "another-valid-value");
            Environment.SetEnvironmentVariable("TEST_VAR_5", "valid-long-value");
            // TEST_VAR_6 is not set

            // Act & Assert
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => EnvironmentVariable.ValidateVariableNamesIn(typeof(TestConfiguration), typeof(TestConfigurationWithMinLength)));
            Assert.IsTrue(exception.Message.Contains("TEST_VAR_6"));
            Assert.IsTrue(exception.Message.Contains("is not set or is empty"));
        }

        [TestMethod]
        public void ValidateVariableNamesIn_WithMultipleConfigurations_MultipleInvalid_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TEST_VAR_1", "valid-value");
            // TEST_VAR_2 is not set
            Environment.SetEnvironmentVariable("TEST_VAR_5", "short"); // Too short
            // TEST_VAR_6 is not set

            // Act & Assert
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => EnvironmentVariable.ValidateVariableNamesIn(typeof(TestConfiguration), typeof(TestConfigurationWithMinLength)));
            Assert.IsTrue(exception.Message.Contains("TEST_VAR_2"));
            Assert.IsTrue(exception.Message.Contains("TEST_VAR_5"));
            Assert.IsTrue(exception.Message.Contains("TEST_VAR_6"));
        }

        [TestMethod]
        public void ValidateVariableNamesIn_WithTypeNotMarkedWithContainerAttribute_ThrowsArgumentException()
        {
            // Act & Assert
            ArgumentException exception = Assert.ThrowsException<ArgumentException>(() => EnvironmentVariable.ValidateVariableNamesIn(typeof(TestConfigurationWithoutAttribute)));
            Assert.IsTrue(exception.Message.Contains("is not marked with EnvironmentVariableNameContainerAttribute"));
        }

        [TestMethod]
        public void ValidateVariableNamesIn_WithEmptyConfiguration_DoesNotThrow()
        {
            // Act & Assert
            EnvironmentVariable.ValidateVariableNamesIn(typeof(TestEmptyConfiguration));
        }

        [TestMethod]
        public void ValidateVariableNamesIn_WithConfigurationWithoutAttributeFields_DoesNotThrow()
        {
            // Act & Assert
            EnvironmentVariable.ValidateVariableNamesIn(typeof(TestConfigurationWithoutAttributeFields));
        }
    }

    [EnvironmentVariableNameContainer]
    public static class TestConfiguration
    {
        [EnvironmentVariableName]
        public static readonly string Variable1 = "TEST_VAR_1";

        [EnvironmentVariableName]
        public static readonly string Variable2 = "TEST_VAR_2";
    }

    [EnvironmentVariableNameContainer]
    public static class TestConfigurationWithMinLength
    {
        [EnvironmentVariableName(10)]
        public static readonly string Variable5 = "TEST_VAR_5";

        [EnvironmentVariableName(10)]
        public static readonly string Variable6 = "TEST_VAR_6";
    }

    [EnvironmentVariableNameContainer]
    public static class TestEmptyConfiguration
    {
        // No fields with EnvironmentVariableNameAttribute
    }

    [EnvironmentVariableNameContainer]
    public static class TestConfigurationWithoutAttributeFields
    {
        public static readonly string VariableWithoutAttribute = "TEST_VAR_WITHOUT_ATTRIBUTE";
    }

    public static class TestConfigurationWithoutAttribute
    {
        [EnvironmentVariableName]
        public static readonly string Variable1 = "TEST_VAR_1";
    }
}