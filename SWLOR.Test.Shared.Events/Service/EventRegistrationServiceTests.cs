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
            // Initialize the mock NWScript service
            InitializeMockNWScript();
            
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

        [Test]
        [Ignore("NWN.Core initialization required")]
        public void RegisterEvents_ShouldCallEventHandlerDiscovery()
        {
            // Act
            _eventRegistrationService.RegisterEvents();

            // Assert
            _mockEventHandlerDiscovery.Received(1).DiscoverAndRegisterHandlers();
        }

        [Test]
        [Ignore("NWN.Core initialization required")]
        public void RegisterEvents_ShouldPublishOnHookNativeOverridesEvent()
        {
            // Act
            _eventRegistrationService.RegisterEvents();

            // Assert
            _mockEventAggregator.Received(1).Publish(Arg.Is<OnHookNativeOverrides>(e => e != null), Arg.Any<uint>());
        }

        [Test]
        [Ignore("NWN.Core initialization required")]
        public void RegisterEvents_ShouldPublishOnHookEventsEvent()
        {
            // Act
            _eventRegistrationService.RegisterEvents();

            // Assert
            _mockEventAggregator.Received(1).Publish(Arg.Is<OnHookEvents>(e => e != null), Arg.Any<uint>());
        }

        [Test]
        [Ignore("NWN.Core initialization required")]
        public void RegisterEvents_ShouldScheduleServerHeartbeat()
        {
            // Act
            _eventRegistrationService.RegisterEvents();

            // Assert
            _mockScheduler.Received(1).ScheduleRepeating(
                Arg.Any<Action>(), 
                Arg.Is<TimeSpan>(ts => ts.TotalSeconds == 6));
        }

        [Test]
        [Ignore("NWN.Core initialization required")]
        public void RegisterEvents_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _eventRegistrationService.RegisterEvents());
        }

        [Test]
        [Ignore("NWN.Core initialization required")]
        public void RegisterEvents_ShouldCallAllRequiredMethods()
        {
            // Act
            _eventRegistrationService.RegisterEvents();

            // Assert
            _mockEventHandlerDiscovery.Received(1).DiscoverAndRegisterHandlers();
            _mockEventAggregator.Received(1).Publish(Arg.Is<OnHookNativeOverrides>(e => e != null), Arg.Any<uint>());
            _mockEventAggregator.Received(1).Publish(Arg.Is<OnHookEvents>(e => e != null), Arg.Any<uint>());
            _mockScheduler.Received(1).ScheduleRepeating(Arg.Any<Action>(), Arg.Any<TimeSpan>());
        }

        [Test]
        [Ignore("NWN.Core initialization required")]
        public void RegisterEvents_ShouldHandleExceptionsGracefully()
        {
            // Arrange
            _mockEventHandlerDiscovery.When(x => x.DiscoverAndRegisterHandlers())
                .Do(x => throw new Exception("Test exception"));

            // Act & Assert
            Assert.DoesNotThrow(() => _eventRegistrationService.RegisterEvents());
        }

        [Test]
        [Ignore("NWN.Core initialization required")]
        public void RegisterEvents_ShouldContinueAfterEventHandlerDiscoveryException()
        {
            // Arrange
            _mockEventHandlerDiscovery.When(x => x.DiscoverAndRegisterHandlers())
                .Do(x => throw new Exception("Test exception"));

            // Act
            _eventRegistrationService.RegisterEvents();

            // Assert
            // Should still call other methods even if discovery fails
            _mockEventAggregator.Received(1).Publish(Arg.Is<OnHookNativeOverrides>(e => e != null), Arg.Any<uint>());
            _mockEventAggregator.Received(1).Publish(Arg.Is<OnHookEvents>(e => e != null), Arg.Any<uint>());
        }

        [Test]
        [Ignore("NWN.Core initialization required")]
        public void RegisterEvents_ShouldUseCorrectTimeSpanForHeartbeat()
        {
            // Act
            _eventRegistrationService.RegisterEvents();

            // Assert
            _mockScheduler.Received(1).ScheduleRepeating(
                Arg.Any<Action>(), 
                Arg.Is<TimeSpan>(ts => ts == TimeSpan.FromSeconds(6)));
        }

        [Test]
        [Ignore("NWN.Core initialization required")]
        public void RegisterEvents_ShouldPublishEventsInCorrectOrder()
        {
            // Act
            _eventRegistrationService.RegisterEvents();

            // Assert
            Received.InOrder(() =>
            {
                _mockEventHandlerDiscovery.DiscoverAndRegisterHandlers();
                _mockEventAggregator.Publish(Arg.Is<OnHookNativeOverrides>(e => e != null), Arg.Any<uint>());
                _mockEventAggregator.Publish(Arg.Is<OnHookEvents>(e => e != null), Arg.Any<uint>());
            });
        }

        [Test]
        [Ignore("NWN.Core initialization required")]
        public void RegisterEvents_ShouldScheduleHeartbeatAfterEventPublishing()
        {
            // Act
            _eventRegistrationService.RegisterEvents();

            // Assert
            Received.InOrder(() =>
            {
                _mockEventAggregator.Publish(Arg.Is<OnHookEvents>(e => e != null), Arg.Any<uint>());
                _mockScheduler.ScheduleRepeating(Arg.Any<Action>(), Arg.Any<TimeSpan>());
            });
        }
    }
}
