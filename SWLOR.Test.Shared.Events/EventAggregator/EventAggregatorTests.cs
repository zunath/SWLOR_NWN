using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.NWN.API.Contracts;
using NSubstitute;

namespace SWLOR.Test.Shared.Events.EventAggregator
{
    [TestFixture]
    public class EventAggregatorTests
    {
        private ILogger _mockLogger;
        private IScriptExecutionProvider _mockExecutionProvider;
        private SWLOR.Shared.Events.EventAggregator.EventAggregator _eventAggregator;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = Substitute.For<ILogger>();
            _mockExecutionProvider = Substitute.For<IScriptExecutionProvider>();
            
            // Configure the mock to actually execute the action when ExecuteInScriptContext is called
            _mockExecutionProvider
                .When(x => x.ExecuteInScriptContext(Arg.Any<Action>(), Arg.Any<uint>(), Arg.Any<int>()))
                .Do(callInfo =>
                {
                    var action = callInfo.Arg<Action>();
                    action?.Invoke();
                });
            
            _eventAggregator = new SWLOR.Shared.Events.EventAggregator.EventAggregator(_mockLogger, _mockExecutionProvider);
        }

        [Test]
        public void Publish_WithNoSubscribers_ShouldNotThrow()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            const uint target = 0x7F000000;

            // Act & Assert
            Assert.DoesNotThrow(() => _eventAggregator.Publish(testEvent, target));
        }

        [Test]
        public void Publish_WithSubscribers_ShouldCallAllHandlers()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            const uint target = 0x7F000000;
            var handler1Called = false;
            var handler2Called = false;

            var subscription1 = _eventAggregator.Subscribe<OnServerLoaded>(e => handler1Called = true);
            var subscription2 = _eventAggregator.Subscribe<OnServerLoaded>(e => handler2Called = true);

            // Act
            _eventAggregator.Publish(testEvent, target);

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
            const uint target = 0x7F000000;
            var exceptionMessage = "Test exception";

            _eventAggregator.Subscribe<OnServerLoaded>(e => throw new Exception(exceptionMessage));

            // Act
            _eventAggregator.Publish(testEvent, target);

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
            const uint target = 0x7F000000;
            var handler1Called = false;
            var handler2Called = false;

            // Act
            var subscription1 = _eventAggregator.Subscribe<OnServerLoaded>(e => handler1Called = true);
            var subscription2 = _eventAggregator.Subscribe<OnServerLoaded>(e => handler2Called = true);

            _eventAggregator.Publish(testEvent, target);

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
            const uint target = 0x7F000000;
            var handlerCalled = false;

            var subscription = _eventAggregator.Subscribe<OnServerLoaded>(e => handlerCalled = true);

            // Act
            _eventAggregator.Unsubscribe(subscription);
            _eventAggregator.Publish(testEvent, target);

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
            const uint target = 0x7F000000;
            var serverLoadedCalled = false;
            var eventsHookedCalled = false;

            var subscription1 = _eventAggregator.Subscribe<OnServerLoaded>(e => serverLoadedCalled = true);
            var subscription2 = _eventAggregator.Subscribe<OnEventsHooked>(e => eventsHookedCalled = true);

            // Act
            _eventAggregator.Publish(serverLoadedEvent, target);

            // Assert
            Assert.That(serverLoadedCalled, Is.True);
            Assert.That(eventsHookedCalled, Is.False);

            // Cleanup
            subscription1.Dispose();
            subscription2.Dispose();
        }
    }
}