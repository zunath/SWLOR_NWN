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
            _mockLogger.Received(1).WriteError(Arg.Is<string>(msg => 
                msg.Contains($"Error in event handler for {typeof(OnServerLoaded).Name}:") &&
                msg.Contains("Exception type: System.Exception") &&
                msg.Contains($"Message       : {exceptionMessage}") &&
                msg.Contains("Stacktrace:")));
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
            var eventsHookedEvent = new OnHookEvents();
            const uint target = 0x7F000000;
            var serverLoadedCalled = false;
            var eventsHookedCalled = false;

            var subscription1 = _eventAggregator.Subscribe<OnServerLoaded>(e => serverLoadedCalled = true);
            var subscription2 = _eventAggregator.Subscribe<OnHookEvents>(e => eventsHookedCalled = true);

            // Act
            _eventAggregator.Publish(serverLoadedEvent, target);

            // Assert
            Assert.That(serverLoadedCalled, Is.True);
            Assert.That(eventsHookedCalled, Is.False);

            // Cleanup
            subscription1.Dispose();
            subscription2.Dispose();
        }

        [Test]
        public void SubscribeConditional_ShouldReturnDisposableSubscription()
        {
            // Arrange & Act
            var subscription = _eventAggregator.SubscribeConditional<OnServerLoaded>(e => true);

            // Assert
            Assert.That(subscription, Is.Not.Null);
            Assert.That(subscription, Is.InstanceOf<IDisposable>());

            // Cleanup
            subscription.Dispose();
        }

        [Test]
        public void Publish_WithConditionalHandlers_ShouldCallAllConditionalHandlers()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            const uint target = 0x7F000000;
            var handler1Called = false;
            var handler2Called = false;

            var subscription1 = _eventAggregator.SubscribeConditional<OnServerLoaded>(e => 
            {
                handler1Called = true;
                return true;
            });
            var subscription2 = _eventAggregator.SubscribeConditional<OnServerLoaded>(e => 
            {
                handler2Called = true;
                return false;
            });

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
        public void Publish_WithConditionalHandlerException_ShouldLogError()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            const uint target = 0x7F000000;
            var exceptionMessage = "Test conditional exception";

            _eventAggregator.SubscribeConditional<OnServerLoaded>(e => throw new Exception(exceptionMessage));

            // Act
            _eventAggregator.Publish(testEvent, target);

            // Assert
            _mockLogger.Received(1).WriteError(Arg.Is<string>(msg => 
                msg.Contains($"Error in conditional event handler for {typeof(OnServerLoaded).Name}:") &&
                msg.Contains("Exception type: System.Exception") &&
                msg.Contains($"Message       : {exceptionMessage}") &&
                msg.Contains("Stacktrace:")));

            // Cleanup - we can't dispose since the subscription wasn't returned due to exception
        }

        [Test]
        public void Publish_WithMixedHandlers_ShouldCallBothRegularAndConditionalHandlers()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            const uint target = 0x7F000000;
            var regularHandlerCalled = false;
            var conditionalHandlerCalled = false;

            var regularSubscription = _eventAggregator.Subscribe<OnServerLoaded>(e => regularHandlerCalled = true);
            var conditionalSubscription = _eventAggregator.SubscribeConditional<OnServerLoaded>(e => 
            {
                conditionalHandlerCalled = true;
                return true;
            });

            // Act
            _eventAggregator.Publish(testEvent, target);

            // Assert
            Assert.That(regularHandlerCalled, Is.True);
            Assert.That(conditionalHandlerCalled, Is.True);

            // Cleanup
            regularSubscription.Dispose();
            conditionalSubscription.Dispose();
        }

        [Test]
        public void Publish_WithNoHandlers_ShouldNotThrow()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            const uint target = 0x7F000000;

            // Act & Assert
            Assert.DoesNotThrow(() => _eventAggregator.Publish(testEvent, target));
        }

        [Test]
        public void Publish_WithEmptyHandlerLists_ShouldNotThrow()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            const uint target = 0x7F000000;

            // Subscribe and immediately unsubscribe to create empty lists
            var subscription = _eventAggregator.Subscribe<OnServerLoaded>(e => { });
            subscription.Dispose();

            // Act & Assert
            Assert.DoesNotThrow(() => _eventAggregator.Publish(testEvent, target));
        }

        [Test]
        public void Publish_WithExecutionProviderException_ShouldLogError()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            const uint target = 0x7F000000;
            var exceptionMessage = "Execution provider exception";

            _mockExecutionProvider
                .When(x => x.ExecuteInScriptContext(Arg.Any<Action>(), Arg.Any<uint>(), Arg.Any<int>()))
                .Do(callInfo => throw new Exception(exceptionMessage));

            _eventAggregator.Subscribe<OnServerLoaded>(e => { });

            // Act
            _eventAggregator.Publish(testEvent, target);

            // Assert
            _mockLogger.Received(1).WriteError(Arg.Is<string>(msg => 
                msg.Contains($"Error in event handler for {typeof(OnServerLoaded).Name}:") &&
                msg.Contains("Exception type: System.Exception") &&
                msg.Contains($"Message       : {exceptionMessage}") &&
                msg.Contains("Stacktrace:")));
        }

        [Test]
        public void Publish_WithConditionalExecutionProviderException_ShouldLogError()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            const uint target = 0x7F000000;
            var exceptionMessage = "Conditional execution provider exception";

            _mockExecutionProvider
                .When(x => x.ExecuteInScriptContext(Arg.Any<Action>(), Arg.Any<uint>(), Arg.Any<int>()))
                .Do(callInfo => throw new Exception(exceptionMessage));

            _eventAggregator.SubscribeConditional<OnServerLoaded>(e => true);

            // Act
            _eventAggregator.Publish(testEvent, target);

            // Assert
            _mockLogger.Received(1).WriteError(Arg.Is<string>(msg => 
                msg.Contains($"Error in conditional event handler for {typeof(OnServerLoaded).Name}:") &&
                msg.Contains("Exception type: System.Exception") &&
                msg.Contains($"Message       : {exceptionMessage}") &&
                msg.Contains("Stacktrace:")));
        }
    }
}