using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.Shared.Events.Service;
using SWLOR.Test.Shared;
using NSubstitute;

namespace SWLOR.Test.Shared.Events.Service
{
    [TestFixture]
    public class EventRegistrationServiceTests : TestBase
    {
        private IScheduler _mockScheduler;
        private IEventAggregator _mockEventAggregator;
        private IEventHandlerDiscoveryService _mockEventHandlerDiscovery;
        private EventRegistrationService _eventRegistrationService;

        [SetUp]
        public void SetUp()
        {
            _mockScheduler = Substitute.For<IScheduler>();
            _mockEventAggregator = Substitute.For<IEventAggregator>();
            _mockEventHandlerDiscovery = Substitute.For<IEventHandlerDiscoveryService>();
            _eventRegistrationService = new EventRegistrationService(
                _mockScheduler, 
                _mockEventAggregator, 
                _mockEventHandlerDiscovery);
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
                _mockEventHandlerDiscovery);

            // Assert
            Assert.That(service, Is.Not.Null);
        }

    }
}
