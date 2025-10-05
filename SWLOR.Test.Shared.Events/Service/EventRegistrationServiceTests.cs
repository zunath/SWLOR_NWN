using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Events.Service;
using NSubstitute;
using SWLOR.NWN.API.Contracts;

namespace SWLOR.Test.Shared.Events.Service
{
    [TestFixture]
    public class EventRegistrationServiceTests : TestBase
    {
        private IScheduler _mockScheduler;
        private IEventAggregator _mockEventAggregator;
        private IEventHandlerDiscoveryService _mockEventHandlerDiscovery;
        private IChatPluginService _mockChatPlugin;
        private IEventsPluginService _mockEventsPlugin;
        private EventRegistrationService _eventRegistrationService;

        [SetUp]
        public void SetUp()
        {
            _mockScheduler = Substitute.For<IScheduler>();
            _mockEventAggregator = Substitute.For<IEventAggregator>();
            _mockEventHandlerDiscovery = Substitute.For<IEventHandlerDiscoveryService>();
            _mockChatPlugin = Substitute.For<IChatPluginService>();
            _mockEventsPlugin = Substitute.For<IEventsPluginService>();
            _eventRegistrationService = new EventRegistrationService(
                _mockScheduler, 
                _mockEventAggregator, 
                _mockEventHandlerDiscovery,
                _mockChatPlugin,
                _mockEventsPlugin);
        }

        [TearDown]
        public void TearDown()
        {
            _mockEventHandlerDiscovery?.Dispose();
        }

        [Test]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Act
            var service = new EventRegistrationService(
                _mockScheduler, 
                _mockEventAggregator, 
                _mockEventHandlerDiscovery,
                _mockChatPlugin,
                _mockEventsPlugin);

            // Assert
            Assert.That(service, Is.Not.Null);
        }

    }
}
