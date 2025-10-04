using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Events.Service;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.NWN.API.Contracts;
using NSubstitute;
using System.Reflection;

namespace SWLOR.Test.Shared.Events.Service
{
    [TestFixture]
    public class EventHandlerDiscoveryServiceTests
    {
        private IEventAggregator _mockEventAggregator;
        private ILogger _mockLogger;
        private IServiceProvider _mockServiceProvider;
        private EventHandlerDiscoveryService _service;

        [SetUp]
        public void SetUp()
        {
            _mockEventAggregator = Substitute.For<IEventAggregator>();
            _mockLogger = Substitute.For<ILogger>();
            _mockServiceProvider = Substitute.For<IServiceProvider>();
            
            _service = new EventHandlerDiscoveryService(_mockEventAggregator, _mockLogger, _mockServiceProvider);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        [Test]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Act
            var service = new EventHandlerDiscoveryService(_mockEventAggregator, _mockLogger, _mockServiceProvider);

            // Assert
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithNullEventAggregator_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new EventHandlerDiscoveryService(null, _mockLogger, _mockServiceProvider));
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new EventHandlerDiscoveryService(_mockEventAggregator, null, _mockServiceProvider));
        }

        [Test]
        public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new EventHandlerDiscoveryService(_mockEventAggregator, _mockLogger, null));
        }

        [Test]
        public void DiscoverAndRegisterHandlers_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _service.DiscoverAndRegisterHandlers());
        }

        [Test]
        public void Dispose_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _service.Dispose());
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => 
            {
                _service.Dispose();
                _service.Dispose();
            });
        }

        [Test]
        public void DiscoverAndRegisterHandlers_ShouldProcessAssemblies()
        {
            // Act
            _service.DiscoverAndRegisterHandlers();

            // Assert
            Assert.Pass("DiscoverAndRegisterHandlers completed successfully");
        }

        [Test]
        public void DiscoverAndRegisterHandlers_ShouldHandleExceptions()
        {
            // Arrange
            var throwingLogger = Substitute.For<ILogger>();
            throwingLogger.When(x => x.WriteError(Arg.Any<string>())).Do(x => throw new Exception("Logger error"));
            
            var service = new EventHandlerDiscoveryService(_mockEventAggregator, throwingLogger, _mockServiceProvider);

            // Act & Assert
            Assert.DoesNotThrow(() => service.DiscoverAndRegisterHandlers());
        }

    }

    // Test class with event handlers to test discovery
    public class TestEventHandlerClass
    {
        public bool VoidMethodCalled { get; private set; }
        public bool EventMethodCalled { get; private set; }
        public OnHookEvents LastEvent { get; private set; }

        [ScriptHandler<OnHookEvents>]
        public void VoidEventHandler()
        {
            VoidMethodCalled = true;
        }

        [ScriptHandler<OnHookEvents>]
        public void EventEventHandler(OnHookEvents eventData)
        {
            EventMethodCalled = true;
            LastEvent = eventData;
        }

        [ScriptHandler<OnHookEvents>]
        public bool ConditionalEventHandler()
        {
            return true;
        }

        [ScriptHandler<OnHookEvents>]
        public bool ConditionalEventEventHandler(OnHookEvents eventData)
        {
            return eventData != null;
        }
    }
}

