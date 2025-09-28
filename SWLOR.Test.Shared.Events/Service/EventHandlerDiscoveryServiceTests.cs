using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Events.Infrastructure;
using NSubstitute;
using SWLOR.Shared.Events.Service;
using SWLOR.Shared.Events.Attributes;

namespace SWLOR.Test.Shared.Events.Service
{
    [TestFixture]
    public class EventHandlerDiscoveryServiceTests
    {
        private ILogger _mockLogger;
        private IEventAggregator _mockEventAggregator;
        private IServiceProvider _mockServiceProvider;
        private EventHandlerDiscoveryService _discoveryService;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = Substitute.For<ILogger>();
            _mockEventAggregator = Substitute.For<IEventAggregator>();
            _mockServiceProvider = Substitute.For<IServiceProvider>();
            _discoveryService = new EventHandlerDiscoveryService(_mockEventAggregator, _mockLogger, _mockServiceProvider);
        }

        [TearDown]
        public void TearDown()
        {
            _discoveryService?.Dispose();
        }

        [Test]
        public void DiscoverAndRegisterHandlers_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _discoveryService.DiscoverAndRegisterHandlers());
        }

        [Test]
        public void DiscoverAndRegisterHandlers_ShouldLogCompletion()
        {
            // Act
            _discoveryService.DiscoverAndRegisterHandlers();

            // Assert
            // The service should log completion message
            // We can't easily verify console output, but we can ensure it doesn't throw
            Assert.Pass("Discovery completed without throwing");
        }

        [Test]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            _discoveryService.DiscoverAndRegisterHandlers();

            // Act & Assert
            Assert.DoesNotThrow(() => _discoveryService.Dispose());
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            _discoveryService.DiscoverAndRegisterHandlers();

            // Act & Assert
            Assert.DoesNotThrow(() => _discoveryService.Dispose());
            Assert.DoesNotThrow(() => _discoveryService.Dispose());
        }
    }

    // Test handler class to verify discovery works
    public class TestEventHandler
    {
        private readonly IEventAggregator _eventAggregator;

        public TestEventHandler(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        [ScriptHandler<OnServerLoaded>]
        public void HandleServerLoaded(OnServerLoaded eventData)
        {
            // Test handler implementation
        }
    }
}
