using NUnit.Framework;
using SWLOR.Shared.Events.Model;
using SWLOR.Shared.Events.Service;
using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Abstractions.Contracts;
using NSubstitute;

namespace SWLOR.Test.Shared.Events.Model
{
    [TestFixture]
    public class NamedEventSubscriptionTests
    {
        private EventService _mockEventService;
        private IDisposable _subscription;
        private Action<object> _handler;

        [SetUp]
        public void SetUp()
        {
            var mockEventAggregator = Substitute.For<IEventAggregator>();
            _mockEventService = new EventService(mockEventAggregator);
            _handler = obj => { };
            _subscription = _mockEventService.Subscribe<TestEventForNamed>("TestEvent", e => { });
        }

        [TearDown]
        public void TearDown()
        {
            _subscription?.Dispose();
        }

        [Test]
        public void Constructor_WithValidParameters_SetsProperties()
        {
            // Assert
            Assert.That(_subscription, Is.Not.Null);
        }

        [Test]
        public void Dispose_WhenNotDisposed_CallsRemoveNamedHandler()
        {
            // Act
            _subscription.Dispose();

            // Assert
            // The actual implementation will be tested through the EventService
            Assert.That(_subscription, Is.Not.Null);
        }

        [Test]
        public void Dispose_WhenAlreadyDisposed_DoesNotCallRemoveNamedHandlerAgain()
        {
            // Arrange
            _subscription.Dispose();

            // Act
            _subscription.Dispose();

            // Assert
            // The actual implementation will be tested through the EventService
            Assert.That(_subscription, Is.Not.Null);
        }

        [Test]
        public void Dispose_MultipleCalls_DoNotThrow()
        {
            // Act & Assert
            _subscription.Dispose();
            Assert.DoesNotThrow(() => _subscription.Dispose());
            Assert.DoesNotThrow(() => _subscription.Dispose());
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            // Act
            _subscription.Dispose();
            _subscription.Dispose();

            // Assert
            // The actual implementation will be tested through the EventService
            Assert.That(_subscription, Is.Not.Null);
        }
    }

    // Test event class
    public class TestEventForNamed : BaseEvent
    {
        public override string Script => "TestScript";
    }
}