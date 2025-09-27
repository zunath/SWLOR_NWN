using NSubstitute;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Test.Shared.Core.Service
{
    [TestFixture]
    public class LogServiceTests
    {
        private IAppSettings _mockAppSettings;
        private LogService _logService;

        [SetUp]
        public void SetUp()
        {
            _mockAppSettings = Substitute.For<IAppSettings>();
            _mockAppSettings.LogDirectory.Returns("logs/");
            _mockAppSettings.ServerEnvironment.Returns(ServerEnvironmentType.All);
            
            _logService = new LogService(_mockAppSettings);
        }

        [Test]
        public void Write_WithValidLogGroup_ShouldNotThrow()
        {
            // Arrange
            const string details = "Test log message";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.Write<ServerLogGroup>(details));
        }

        [Test]
        public void Write_WithPrintToConsoleTrue_ShouldNotThrow()
        {
            // Arrange
            const string details = "Test log message with console output";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.Write<ServerLogGroup>(details, true));
        }

        [Test]
        public void Write_WithDifferentLogGroups_ShouldNotThrow()
        {
            // Arrange
            const string details = "Test log message";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.Write<AttackLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<ChatLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<ConnectionLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<CraftingLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<DeathLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<DMAuthorizationLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<DMLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<ErrorLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<IncubationLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<MigrationLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<PerkRefundLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<PlayerMarketLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<PropertyLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<SpaceLogGroup>(details));
            Assert.DoesNotThrow(() => _logService.Write<StoreCleanupLogGroup>(details));
        }

        [Test]
        public void Write_WithEmptyDetails_ShouldNotThrow()
        {
            // Arrange
            const string details = "";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.Write<ServerLogGroup>(details));
        }

        [Test]
        public void Write_WithNullDetails_ShouldNotThrow()
        {
            // Arrange
            string details = null;

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.Write<ServerLogGroup>(details));
        }

        [Test]
        public void Write_WithLongDetails_ShouldNotThrow()
        {
            // Arrange
            var details = new string('A', 1000);

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.Write<ServerLogGroup>(details));
        }

        [Test]
        public void Write_WithSpecialCharacters_ShouldNotThrow()
        {
            // Arrange
            const string details = "Test message with special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?`~";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.Write<ServerLogGroup>(details));
        }

        [Test]
        public void Write_WithUnicodeCharacters_ShouldNotThrow()
        {
            // Arrange
            const string details = "Test message with unicode: 世界 🌍 🚀";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.Write<ServerLogGroup>(details));
        }

        [Test]
        public void Write_WithNewlines_ShouldNotThrow()
        {
            // Arrange
            const string details = "Test message\nwith newlines\r\nand carriage returns";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.Write<ServerLogGroup>(details));
        }

        [Test]
        public void WriteError_WithValidDetails_ShouldNotThrow()
        {
            // Arrange
            const string details = "Test error message";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.WriteError(details));
        }

        [Test]
        public void WriteError_WithEmptyDetails_ShouldNotThrow()
        {
            // Arrange
            const string details = "";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.WriteError(details));
        }

        [Test]
        public void WriteError_WithNullDetails_ShouldNotThrow()
        {
            // Arrange
            string details = null;

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.WriteError(details));
        }

        [Test]
        public void WriteError_WithLongDetails_ShouldNotThrow()
        {
            // Arrange
            var details = new string('A', 1000);

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.WriteError(details));
        }

        [Test]
        public void WriteError_WithSpecialCharacters_ShouldNotThrow()
        {
            // Arrange
            const string details = "Error message with special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?`~";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.WriteError(details));
        }

        [Test]
        public void WriteError_WithUnicodeCharacters_ShouldNotThrow()
        {
            // Arrange
            const string details = "Error message with unicode: 世界 🌍 🚀";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.WriteError(details));
        }

        [Test]
        public void OnApplicationShutdown_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _logService.OnApplicationShutdown());
        }

        [Test]
        public void OnApplicationShutdown_WithMultipleCalls_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _logService.OnApplicationShutdown());
            Assert.DoesNotThrow(() => _logService.OnApplicationShutdown());
            Assert.DoesNotThrow(() => _logService.OnApplicationShutdown());
        }

        [Test]
        public void Write_WithDifferentEnvironments_ShouldHandleCorrectly()
        {
            // Arrange
            _mockAppSettings.ServerEnvironment.Returns(ServerEnvironmentType.Development);
            const string details = "Test message for development environment";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.Write<ServerLogGroup>(details));
        }

        [Test]
        public void Write_WithProductionEnvironment_ShouldHandleCorrectly()
        {
            // Arrange
            _mockAppSettings.ServerEnvironment.Returns(ServerEnvironmentType.Production);
            const string details = "Test message for production environment";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.Write<ServerLogGroup>(details));
        }

        [Test]
        public void Write_WithTestEnvironment_ShouldHandleCorrectly()
        {
            // Arrange
            _mockAppSettings.ServerEnvironment.Returns(ServerEnvironmentType.Test);
            const string details = "Test message for test environment";

            // Act & Assert
            Assert.DoesNotThrow(() => _logService.Write<ServerLogGroup>(details));
        }

        [Test]
        public void Write_WithMultipleLogs_ShouldNotThrow()
        {
            // Arrange
            const string details = "Test log message";

            // Act & Assert
            for (int i = 0; i < 10; i++)
            {
                Assert.DoesNotThrow(() => _logService.Write<ServerLogGroup>($"{details} {i}"));
            }
        }

        [Test]
        public void Write_WithConcurrentLogs_ShouldNotThrow()
        {
            // Arrange
            const string details = "Test concurrent log message";
            var tasks = new List<Task>();

            // Act & Assert
            for (int i = 0; i < 5; i++)
            {
                var task = Task.Run(() => _logService.Write<ServerLogGroup>($"{details} {i}"));
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}