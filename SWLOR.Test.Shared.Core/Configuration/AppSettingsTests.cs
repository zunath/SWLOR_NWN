using SWLOR.Shared.Core.Configuration;
using SWLOR.Shared.Abstractions.Enums;

namespace SWLOR.Test.Shared.Core.Configuration
{
    [TestFixture]
    public class AppSettingsTests
    {
        [TearDown]
        public void TearDown()
        {
            // Clean up environment variables
            Environment.SetEnvironmentVariable("SWLOR_APP_LOG_DIRECTORY", null);
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", null);
            Environment.SetEnvironmentVariable("SWLOR_BUG_WEBHOOK_URL", null);
            Environment.SetEnvironmentVariable("SWLOR_HOLONET_WEBHOOK_URL", null);
            Environment.SetEnvironmentVariable("SWLOR_DM_SHOUT_WEBHOOK_URL", null);
            Environment.SetEnvironmentVariable("SWLOR_ENVIRONMENT", null);
        }

        [Test]
        public void AppSettings_Constructor_ShouldSetPropertiesFromEnvironmentVariables()
        {
            // Arrange
            Environment.SetEnvironmentVariable("SWLOR_APP_LOG_DIRECTORY", "C:\\Logs");
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", "localhost");
            Environment.SetEnvironmentVariable("SWLOR_BUG_WEBHOOK_URL", "https://discord.com/api/webhooks/bug");
            Environment.SetEnvironmentVariable("SWLOR_HOLONET_WEBHOOK_URL", "https://discord.com/api/webhooks/holonet");
            Environment.SetEnvironmentVariable("SWLOR_DM_SHOUT_WEBHOOK_URL", "https://discord.com/api/webhooks/dm");
            Environment.SetEnvironmentVariable("SWLOR_ENVIRONMENT", "prod");

            // Act
            var settings = new AppSettings();

            // Assert
            Assert.That(settings.LogDirectory, Is.EqualTo("C:\\Logs"));
            Assert.That(settings.RedisIPAddress, Is.EqualTo("localhost"));
            Assert.That(settings.BugWebHookUrl, Is.EqualTo("https://discord.com/api/webhooks/bug"));
            Assert.That(settings.HolonetWebHookUrl, Is.EqualTo("https://discord.com/api/webhooks/holonet"));
            Assert.That(settings.DMShoutWebHookUrl, Is.EqualTo("https://discord.com/api/webhooks/dm"));
            Assert.That(settings.ServerEnvironment, Is.EqualTo(ServerEnvironmentType.Production));
        }

        [Test]
        public void AppSettings_Constructor_WithProductionEnvironment_ShouldSetProductionType()
        {
            // Arrange
            Environment.SetEnvironmentVariable("SWLOR_ENVIRONMENT", "production");

            // Act
            var settings = new AppSettings();

            // Assert
            Assert.That(settings.ServerEnvironment, Is.EqualTo(ServerEnvironmentType.Production));
        }

        [Test]
        public void AppSettings_Constructor_WithTestEnvironment_ShouldSetTestType()
        {
            // Arrange
            Environment.SetEnvironmentVariable("SWLOR_ENVIRONMENT", "test");

            // Act
            var settings = new AppSettings();

            // Assert
            Assert.That(settings.ServerEnvironment, Is.EqualTo(ServerEnvironmentType.Test));
        }

        [Test]
        public void AppSettings_Constructor_WithTestingEnvironment_ShouldSetTestType()
        {
            // Arrange
            Environment.SetEnvironmentVariable("SWLOR_ENVIRONMENT", "testing");

            // Act
            var settings = new AppSettings();

            // Assert
            Assert.That(settings.ServerEnvironment, Is.EqualTo(ServerEnvironmentType.Test));
        }

        [Test]
        public void AppSettings_Constructor_WithInvalidEnvironment_ShouldSetDevelopmentType()
        {
            // Arrange
            Environment.SetEnvironmentVariable("SWLOR_ENVIRONMENT", "invalid");

            // Act
            var settings = new AppSettings();

            // Assert
            Assert.That(settings.ServerEnvironment, Is.EqualTo(ServerEnvironmentType.Development));
        }

        [Test]
        public void AppSettings_Constructor_WithNullEnvironment_ShouldSetDevelopmentType()
        {
            // Arrange
            Environment.SetEnvironmentVariable("SWLOR_ENVIRONMENT", null);

            // Act
            var settings = new AppSettings();

            // Assert
            Assert.That(settings.ServerEnvironment, Is.EqualTo(ServerEnvironmentType.Development));
        }

        [Test]
        public void AppSettings_Constructor_WithEmptyEnvironment_ShouldSetDevelopmentType()
        {
            // Arrange
            Environment.SetEnvironmentVariable("SWLOR_ENVIRONMENT", "");

            // Act
            var settings = new AppSettings();

            // Assert
            Assert.That(settings.ServerEnvironment, Is.EqualTo(ServerEnvironmentType.Development));
        }

        [Test]
        public void AppSettings_Constructor_WithWhitespaceEnvironment_ShouldSetDevelopmentType()
        {
            // Arrange
            Environment.SetEnvironmentVariable("SWLOR_ENVIRONMENT", "   ");

            // Act
            var settings = new AppSettings();

            // Assert
            Assert.That(settings.ServerEnvironment, Is.EqualTo(ServerEnvironmentType.Development));
        }

        [Test]
        public void AppSettings_Constructor_WithCaseInsensitiveEnvironment_ShouldSetCorrectType()
        {
            // Arrange
            Environment.SetEnvironmentVariable("SWLOR_ENVIRONMENT", "PROD");

            // Act
            var settings = new AppSettings();

            // Assert
            Assert.That(settings.ServerEnvironment, Is.EqualTo(ServerEnvironmentType.Production));
        }

        [Test]
        public void AppSettings_Constructor_WithNullEnvironmentVariables_ShouldSetNullProperties()
        {
            // Arrange
            Environment.SetEnvironmentVariable("SWLOR_APP_LOG_DIRECTORY", null);
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", null);
            Environment.SetEnvironmentVariable("SWLOR_BUG_WEBHOOK_URL", null);
            Environment.SetEnvironmentVariable("SWLOR_HOLONET_WEBHOOK_URL", null);
            Environment.SetEnvironmentVariable("SWLOR_DM_SHOUT_WEBHOOK_URL", null);

            // Act
            var settings = new AppSettings();

            // Assert
            Assert.That(settings.LogDirectory, Is.Null);
            Assert.That(settings.RedisIPAddress, Is.Null);
            Assert.That(settings.BugWebHookUrl, Is.Null);
            Assert.That(settings.HolonetWebHookUrl, Is.Null);
            Assert.That(settings.DMShoutWebHookUrl, Is.Null);
        }

        [Test]
        public void AppSettings_Constructor_WithEmptyEnvironmentVariables_ShouldSetEmptyProperties()
        {
            // Arrange
            Environment.SetEnvironmentVariable("SWLOR_APP_LOG_DIRECTORY", "");
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", "");
            Environment.SetEnvironmentVariable("SWLOR_BUG_WEBHOOK_URL", "");
            Environment.SetEnvironmentVariable("SWLOR_HOLONET_WEBHOOK_URL", "");
            Environment.SetEnvironmentVariable("SWLOR_DM_SHOUT_WEBHOOK_URL", "");

            // Act
            var settings = new AppSettings();

            // Assert
            Assert.That(settings.LogDirectory, Is.Null);
            Assert.That(settings.RedisIPAddress, Is.Null);
            Assert.That(settings.BugWebHookUrl, Is.Null);
            Assert.That(settings.HolonetWebHookUrl, Is.Null);
            Assert.That(settings.DMShoutWebHookUrl, Is.Null);
        }
    }
}
