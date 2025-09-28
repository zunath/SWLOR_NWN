using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.NWN.API.Contracts;
using NSubstitute;

namespace SWLOR.Test.Shared.Events.EventAggregator
{
    [TestFixture]
    public class EventSubscriptionTests
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
        public void Subscribe_ShouldReturnDisposableSubscription()
        {
            // Arrange
            Action<OnServerLoaded> handler = e => { };

            // Act
            var subscription = _eventAggregator.Subscribe(handler);

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
        public void Dispose_ShouldRemoveHandler()
        {
            // Arrange
            var testEvent = new OnServerLoaded();
            const uint target = 0x7F000000;
            var handlerCalled = false;

            var subscription = _eventAggregator.Subscribe<OnServerLoaded>(e => handlerCalled = true);

            // Act
            subscription.Dispose();
            _eventAggregator.Publish(testEvent, target);

            // Assert
            Assert.That(handlerCalled, Is.False);
        }

        [Test]
        public void Dispose_WhenAlreadyDisposed_ShouldNotThrow()
        {
            // Arrange
            Action<OnServerLoaded> handler = e => { };

            var subscription = _eventAggregator.Subscribe(handler);

            // Act & Assert
            subscription.Dispose();
            Assert.DoesNotThrow(() => subscription.Dispose());
        }

        [Test]
        public void MultipleDisposeCalls_ShouldNotThrow()
        {
            // Arrange
            Action<OnServerLoaded> handler = e => { };

            var subscription = _eventAggregator.Subscribe(handler);

            // Act & Assert
            subscription.Dispose();
            subscription.Dispose();
            subscription.Dispose();
            Assert.DoesNotThrow(() => subscription.Dispose());
        }
    }
}