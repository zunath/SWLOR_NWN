using NUnit.Framework;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Events.Infrastructure;
using NSubstitute;

namespace SWLOR.Test.Shared.Events.EventAggregator
{
    [TestFixture]
    public class EventAggregatorTests
    {
        private ILogger _mockLogger;
        private SWLOR.Shared.Events.EventAggregator.EventAggregator _eventAggregator;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = Substitute.For<ILogger>();
            _eventAggregator = new SWLOR.Shared.Events.EventAggregator.EventAggregator(_mockLogger);
        }

        [Test]
        public void Publish_WithNoSubscribers_ShouldNotThrow()
        {
            // Arrange
            var testEvent = new OnServerLoaded();

            // Act & Assert
            Assert.DoesNotThrow(() => _eventAggregator.Publish(testEvent));
        }

        [Test]
        public void Publish_WithSubscribers_ShouldCallAllHandlers()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            var handler1Called = false;
            var handler2Called = false;

            var subscription1 = _eventAggregator.Subscribe<OnServerLoaded>(e => handler1Called = true);
            var subscription2 = _eventAggregator.Subscribe<OnServerLoaded>(e => handler2Called = true);

            // Act
            _eventAggregator.Publish(testEvent);

            // Assert
            Assert.That(handler1Called, Is.True);
            Assert.That(handler2Called, Is.True);

            // Cleanup
            subscription1.Dispose();
            subscription2.Dispose();
        }

        [Test]
        public void Publish_WithHandlerException_ShouldLogError()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            var exceptionMessage = "Test exception";

            _eventAggregator.Subscribe<OnServerLoaded>(e => throw new Exception(exceptionMessage));

            // Act
            _eventAggregator.Publish(testEvent);

            // Assert
            _mockLogger.Received(1).WriteError($"Error in event handler for {typeof(OnServerLoaded).Name}: {exceptionMessage}");
        }

        [Test]
        public void Subscribe_ShouldReturnDisposableSubscription()
        {
            // Arrange & Act
            var subscription = _eventAggregator.Subscribe<OnServerLoaded>(e => { });

            // Assert
            Assert.That(subscription, Is.Not.Null);
            Assert.That(subscription, Is.InstanceOf<IDisposable>());

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void Subscribe_ShouldAllowMultipleSubscriptions()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            var handler1Called = false;
            var handler2Called = false;

            // Act
            var subscription1 = _eventAggregator.Subscribe<OnServerLoaded>(e => handler1Called = true);
            var subscription2 = _eventAggregator.Subscribe<OnServerLoaded>(e => handler2Called = true);

            _eventAggregator.Publish(testEvent);

            // Assert
            Assert.That(handler1Called, Is.True);
            Assert.That(handler2Called, Is.True);

            // Cleanup
            subscription1.Dispose();
            subscription2.Dispose();
        }

        [Test]
        public void Unsubscribe_ShouldRemoveHandler()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            var handlerCalled = false;

            var subscription = _eventAggregator.Subscribe<OnServerLoaded>(e => handlerCalled = true);

            // Act
            _eventAggregator.Unsubscribe(subscription);
            _eventAggregator.Publish(testEvent);

            // Assert
            Assert.That(handlerCalled, Is.False);
        }

        [Test]
        public void Unsubscribe_WithNullSubscription_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _eventAggregator.Unsubscribe(null));
        }

        [Test]
        public void Publish_WithDifferentEventTypes_ShouldOnlyCallCorrectHandlers()
        {
            // Arrange
            var serverLoadedEvent = new OnServerLoaded();
            var eventsHookedEvent = new OnEventsHooked();
            var serverLoadedCalled = false;
            var eventsHookedCalled = false;

            var subscription1 = _eventAggregator.Subscribe<OnServerLoaded>(e => serverLoadedCalled = true);
            var subscription2 = _eventAggregator.Subscribe<OnEventsHooked>(e => eventsHookedCalled = true);

            // Act
            _eventAggregator.Publish(serverLoadedEvent);

            // Assert
            Assert.That(serverLoadedCalled, Is.True);
            Assert.That(eventsHookedCalled, Is.False);

            // Cleanup
            subscription1.Dispose();
            subscription2.Dispose();
        }
    }
}