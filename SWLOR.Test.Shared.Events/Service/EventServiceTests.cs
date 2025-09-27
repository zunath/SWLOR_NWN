using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Service;
using SWLOR.Shared.Events.Events.Infrastructure;
using NSubstitute;

namespace SWLOR.Test.Shared.Events.Service
{
    [TestFixture]
    public class EventServiceTests
    {
        private EventService _eventService;
        private IEventAggregator _mockEventAggregator;

        [SetUp]
        public void SetUp()
        {
            _mockEventAggregator = Substitute.For<IEventAggregator>();
            _eventService = new EventService(_mockEventAggregator);
        }

        [Test]
        public void Constructor_WithNullAggregator_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EventService(null));
        }

        [Test]
        public void Publish_ShouldCallAggregatorPublish()
        {
            // Arrange
            var testEvent = new OnServerLoaded();

            // Act
            _eventService.Publish(testEvent);

            // Assert
            _mockEventAggregator.Received(1).Publish(testEvent);
        }

        [Test]
        public void Subscribe_ShouldCallAggregatorSubscribe()
        {
            // Arrange
            Action<OnServerLoaded> handler = e => { };
            var mockSubscription = Substitute.For<IDisposable>();
            _mockEventAggregator.Subscribe(handler).Returns(mockSubscription);

            // Act
            var result = _eventService.Subscribe(handler);

            // Assert
            _mockEventAggregator.Received(1).Subscribe(handler);
            Assert.That(result, Is.EqualTo(mockSubscription));
        }

        [Test]
        public void Subscribe_WithNamedEvent_ShouldRegisterHandler()
        {
            // Arrange
            var eventName = "TestEvent";
            Action<OnServerLoaded> handler = e => { };

            // Act
            var subscription = _eventService.Subscribe(eventName, handler);

            // Assert
            Assert.That(subscription, Is.Not.Null);
            Assert.That(_eventService.GetSubscriberCount(eventName), Is.EqualTo(1));
        }

        [Test]
        public void Subscribe_WithNullEventName_ShouldThrowArgumentException()
        {
            // Arrange
            Action<OnServerLoaded> handler = e => { };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _eventService.Subscribe(null, handler));
        }

        [Test]
        public void Subscribe_WithEmptyEventName_ShouldThrowArgumentException()
        {
            // Arrange
            Action<OnServerLoaded> handler = e => { };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _eventService.Subscribe("", handler));
        }

        [Test]
        public void Subscribe_WithNullHandler_ShouldThrowArgumentNullException()
        {
            // Arrange
            var eventName = "TestEvent";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _eventService.Subscribe<OnServerLoaded>(eventName, null));
        }

        [Test]
        public void Unsubscribe_ShouldCallAggregatorUnsubscribe()
        {
            // Arrange
            var mockSubscription = Substitute.For<IDisposable>();

            // Act
            _eventService.Unsubscribe(mockSubscription);

            // Assert
            _mockEventAggregator.Received(1).Unsubscribe(mockSubscription);
        }

        [Test]
        public void GetSubscriberCount_ForTypedEvent_ShouldReturnZero()
        {
            // Act
            var count = _eventService.GetSubscriberCount<OnServerLoaded>();

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void GetSubscriberCount_ForNamedEvent_ShouldReturnCorrectCount()
        {
            // Arrange
            var eventName = "TestEvent";
            Action<OnServerLoaded> handler1 = e => { };
            Action<OnServerLoaded> handler2 = e => { };

            var subscription1 = _eventService.Subscribe(eventName, handler1);
            var subscription2 = _eventService.Subscribe(eventName, handler2);

            // Act
            var count = _eventService.GetSubscriberCount(eventName);

            // Assert
            Assert.That(count, Is.EqualTo(2));

            // Cleanup
            subscription1.Dispose();
            subscription2.Dispose();
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
        public void GetSubscriberCount_WithEmptyEventName_ShouldReturnZero()
        {
            // Act
            var count = _eventService.GetSubscriberCount("");

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void HasSubscribers_ForTypedEvent_ShouldReturnFalse()
        {
            // Act
            var hasSubscribers = _eventService.HasSubscribers<OnServerLoaded>();

            // Assert
            Assert.That(hasSubscribers, Is.False);
        }

        [Test]
        public void HasSubscribers_ForNamedEvent_ShouldReturnCorrectValue()
        {
            // Arrange
            var eventName = "TestEvent";
            Action<OnServerLoaded> handler = e => { };

            var subscription = _eventService.Subscribe(eventName, handler);

            // Act
            var hasSubscribers = _eventService.HasSubscribers(eventName);

            // Assert
            Assert.That(hasSubscribers, Is.True);

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void HasSubscribers_WithNoSubscribers_ShouldReturnFalse()
        {
            // Arrange
            var eventName = "TestEvent";

            // Act
            var hasSubscribers = _eventService.HasSubscribers(eventName);

            // Assert
            Assert.That(hasSubscribers, Is.False);
        }

        [Test]
        public void Dispose_ShouldRemoveHandler()
        {
            // Arrange
            var eventName = "TestEvent";
            Action<OnServerLoaded> handler = e => { };

            var subscription = _eventService.Subscribe(eventName, handler);

            // Act
            subscription.Dispose();

            // Assert
            Assert.That(_eventService.GetSubscriberCount(eventName), Is.EqualTo(0));
        }

        [Test]
        public void Dispose_WithMultipleHandlers_ShouldRemoveOnlyOne()
        {
            // Arrange
            var eventName = "TestEvent";
            Action<OnServerLoaded> handler1 = e => { };
            Action<OnServerLoaded> handler2 = e => { };

            var subscription1 = _eventService.Subscribe(eventName, handler1);
            var subscription2 = _eventService.Subscribe(eventName, handler2);

            // Act
            subscription1.Dispose();

            // Assert
            Assert.That(_eventService.GetSubscriberCount(eventName), Is.EqualTo(1));

            // Cleanup
            subscription2.Dispose();
        }

        [Test]
        public void Subscribe_WithWhitespaceEventName_ShouldNotThrow()
        {
            // Act & Assert - whitespace is not considered empty by IsNullOrEmpty
            Assert.DoesNotThrow(() => _eventService.Subscribe<OnServerLoaded>("   ", e => { }));
        }

        [Test]
        public void Subscribe_WithNullNamedHandler_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _eventService.Subscribe<OnServerLoaded>("TestEvent", null!));
        }

        [Test]
        public void HasSubscribers_WithEmptyEventName_ShouldReturnFalse()
        {
            // Act
            var result = _eventService.HasSubscribers(string.Empty);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void HasSubscribers_WithNullEventName_ShouldReturnFalse()
        {
            // Act
            var result = _eventService.HasSubscribers(null!);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void Subscribe_WithTypedEvent_ShouldRegisterHandler()
        {
            // Arrange
            Action<OnServerLoaded> handler = e => { };
            var mockSubscription = Substitute.For<IDisposable>();
            _mockEventAggregator.Subscribe(handler).Returns(mockSubscription);

            // Act
            var subscription = _eventService.Subscribe(handler);

            // Assert
            Assert.That(subscription, Is.Not.Null);
            _mockEventAggregator.Received(1).Subscribe(handler);
        }

        [Test]
        public void Publish_WithTypedEvent_ShouldCallAggregatorPublish()
        {
            // Arrange
            var testEvent = new OnServerLoaded();

            // Act
            _eventService.Publish(testEvent);

            // Assert
            _mockEventAggregator.Received(1).Publish(testEvent);
        }

    }
}