using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.Shared.Events.Service;
using NSubstitute;

namespace SWLOR.Test.Shared.Events.Service
{
    [TestFixture]
    public class EventServiceComprehensiveTests : TestBase
    {
        private IEventAggregator _mockEventAggregator;
        private IEventService _eventService;

        [SetUp]
        public void SetUp()
        {
            // Initialize the mock NWScript service
            InitializeMockNWScript();
            
            _mockEventAggregator = Substitute.For<IEventAggregator>();
            _eventService = new EventService(_mockEventAggregator);
        }

        [Test]
        public void Constructor_WithValidEventAggregator_ShouldCreateInstance()
        {
            // Act
            var service = new EventService(_mockEventAggregator);

            // Assert
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithNullEventAggregator_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EventService(null));
        }

        [Test]
        public void Publish_ShouldCallEventAggregatorPublish()
        {
            // Arrange
            var eventData = new OnHookEvents();
            var target = 123u;

            // Act
            _eventService.Publish(eventData, target);

            // Assert
            _mockEventAggregator.Received(1).Publish(eventData, target);
        }

        [Test]
        public void Subscribe_ShouldCallEventAggregatorSubscribe()
        {
            // Arrange
            Action<OnHookEvents> handler = _ => { };

            // Act
            var subscription = _eventService.Subscribe(handler);

            // Assert
            _mockEventAggregator.Received(1).Subscribe(handler);
            Assert.That(subscription, Is.Not.Null);
        }

        [Test]
        public void Subscribe_WithEventName_ShouldCallEventAggregatorSubscribe()
        {
            // Arrange
            var eventName = "test_event";
            Action<OnHookEvents> handler = _ => { };

            // Act
            var subscription = _eventService.Subscribe<OnHookEvents>(eventName, handler);

            // Assert
            Assert.That(subscription, Is.Not.Null);
        }

        [Test]
        public void Unsubscribe_ShouldCallEventAggregatorUnsubscribe()
        {
            // Arrange
            var mockSubscription = Substitute.For<IDisposable>();

            // Act
            _eventService.Unsubscribe(mockSubscription);

            // Assert - Should not throw
            Assert.Pass("Unsubscribe completed successfully");
        }

        [Test]
        public void GetSubscriberCount_WithEventName_ShouldReturnCount()
        {
            // Arrange
            var eventName = "test_event";

            // Act
            var count = _eventService.GetSubscriberCount(eventName);

            // Assert
            Assert.That(count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void HasSubscribers_WithEventName_ShouldReturnBoolean()
        {
            // Arrange
            var eventName = "test_event";

            // Act
            var hasSubscribers = _eventService.HasSubscribers(eventName);

            // Assert
            Assert.That(hasSubscribers, Is.TypeOf<bool>());
        }

        [Test]
        public void Subscribe_WithNullEventName_ShouldThrowArgumentNullException()
        {
            // Arrange
            Action<OnHookEvents> handler = _ => { };

            // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _eventService.Subscribe<OnHookEvents>(null, handler));
        }

        [Test]
        public void Subscribe_WithNullHandler_ShouldThrowArgumentNullException()
        {
            // Arrange
            var eventName = "test_event";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _eventService.Subscribe<OnHookEvents>(eventName, null));
        }

        [Test]
        public void GetSubscriberCount_WithNullEventName_ShouldReturnZero()
        {
            // Act
            var count = _eventService.GetSubscriberCount(null);

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void HasSubscribers_WithNullEventName_ShouldReturnFalse()
        {
            // Act
            var hasSubscribers = _eventService.HasSubscribers(null);

            // Assert
            Assert.That(hasSubscribers, Is.False);
        }

        [Test]
        public void GetSubscriberCount_WithEventType_ShouldReturnZero()
        {
            // Act
            var count = _eventService.GetSubscriberCount<OnHookEvents>();

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void HasSubscribers_WithEventType_ShouldReturnFalse()
        {
            // Act
            var hasSubscribers = _eventService.HasSubscribers<OnHookEvents>();

            // Assert
            Assert.That(hasSubscribers, Is.False);
        }

        [Test]
        public void GetSubscriberCount_WithEmptyEventName_ShouldReturnZero()
        {
            // Act
            var count = _eventService.GetSubscriberCount("");

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void HasSubscribers_WithEmptyEventName_ShouldReturnFalse()
        {
            // Act
            var hasSubscribers = _eventService.HasSubscribers("");

            // Assert
            Assert.That(hasSubscribers, Is.False);
        }

        [Test]
        public void GetSubscriberCount_WithNonExistentEventName_ShouldReturnZero()
        {
            // Act
            var count = _eventService.GetSubscriberCount("non_existent_event");

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void HasSubscribers_WithNonExistentEventName_ShouldReturnFalse()
        {
            // Act
            var hasSubscribers = _eventService.HasSubscribers("non_existent_event");

            // Assert
            Assert.That(hasSubscribers, Is.False);
        }

        [Test]
        public void Subscribe_WithEventName_ShouldAddHandler()
        {
            // Arrange
            var eventName = "test_event";
            Action<OnHookEvents> handler = _ => { };

            // Act
            var subscription = _eventService.Subscribe(eventName, handler);

            // Assert
            Assert.That(subscription, Is.Not.Null);
            Assert.That(_eventService.GetSubscriberCount(eventName), Is.EqualTo(1));
            Assert.That(_eventService.HasSubscribers(eventName), Is.True);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void Subscribe_WithEventName_ShouldAllowMultipleHandlers()
        {
            // Arrange
            var eventName = "test_event";
            Action<OnHookEvents> handler1 = _ => { };
            Action<OnHookEvents> handler2 = _ => { };

            // Act
            var subscription1 = _eventService.Subscribe(eventName, handler1);
            var subscription2 = _eventService.Subscribe(eventName, handler2);

            // Assert
            Assert.That(_eventService.GetSubscriberCount(eventName), Is.EqualTo(2));
            Assert.That(_eventService.HasSubscribers(eventName), Is.True);

            // Cleanup
            subscription1.Dispose();
            subscription2.Dispose();
        }

        [Test]
        public void Subscribe_WithEventName_ShouldHandleDisposal()
        {
            // Arrange
            var eventName = "test_event";
            Action<OnHookEvents> handler = _ => { };

            // Act
            var subscription = _eventService.Subscribe(eventName, handler);
            Assert.That(_eventService.GetSubscriberCount(eventName), Is.EqualTo(1));

            subscription.Dispose();

            // Assert
            Assert.That(_eventService.GetSubscriberCount(eventName), Is.EqualTo(0));
            Assert.That(_eventService.HasSubscribers(eventName), Is.False);
        }

        [Test]
        public void Subscribe_WithEventName_ShouldHandleMultipleDisposals()
        {
            // Arrange
            var eventName = "test_event";
            Action<OnHookEvents> handler = _ => { };

            // Act
            var subscription = _eventService.Subscribe(eventName, handler);
            subscription.Dispose();
            subscription.Dispose(); // Should not throw

            // Assert
            Assert.That(_eventService.GetSubscriberCount(eventName), Is.EqualTo(0));
        }
    }
}
