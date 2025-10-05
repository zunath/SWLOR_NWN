using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.NWN.API.Contracts;
using NSubstitute;

namespace SWLOR.Test.Shared.Events.EventAggregator
{
    [TestFixture]
    public class ConditionalEventSubscriptionTests : TestBase
    {
        private SWLOR.Shared.Events.EventAggregator.EventAggregator _eventAggregator;
        private ILogger _mockLogger;
        private IScriptExecutionProvider _mockExecutionProvider;
        private IDisposable _subscription;
        private Func<OnHookEvents, bool> _handler;

        [SetUp]
        public void SetUp()
        {
            // Initialize the mock NWScript service
            InitializeMockNWScript();
            
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
            
            _handler = Substitute.For<Func<OnHookEvents, bool>>();
            _subscription = _eventAggregator.SubscribeConditional(_handler);
        }

        [TearDown]
        public void TearDown()
        {
            _subscription?.Dispose();
        }

        [Test]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Act
            var subscription = new ConditionalEventSubscription<OnHookEvents>(_eventAggregator, _handler);

            // Assert
            Assert.That(subscription, Is.Not.Null);
        }

        [Test]
        public void Dispose_ShouldRemoveHandlerFromAggregator()
        {
            // Arrange
            var eventData = new OnHookEvents();
            
            // Act
            _subscription.Dispose();

            // Assert
            // Publish an event and verify the handler is not called
            _eventAggregator.Publish(eventData, 123);
            _handler.DidNotReceive().Invoke(Arg.Any<OnHookEvents>());
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => 
            {
                _subscription.Dispose();
                _subscription.Dispose();
            });
        }

        [Test]
        public void Handler_ShouldBeCalledWhenEventIsPublished()
        {
            // Arrange
            var eventData = new OnHookEvents();
            _handler.Invoke(Arg.Any<OnHookEvents>()).Returns(true);
            
            // Act
            _eventAggregator.Publish(eventData, 123);

            // Assert
            _handler.Received(1).Invoke(eventData);
        }

        [Test]
        public void Handler_ShouldReturnValue()
        {
            // Arrange
            var eventData = new OnHookEvents();
            _handler.Invoke(Arg.Any<OnHookEvents>()).Returns(false);
            
            // Act
            _eventAggregator.Publish(eventData, 123);

            // Assert
            _handler.Received(1).Invoke(eventData);
        }
    }
}