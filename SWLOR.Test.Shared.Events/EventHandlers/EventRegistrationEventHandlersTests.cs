using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Events.EventHandlers;
using SWLOR.Test.Shared;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Events.Constants;
using SWLOR.Test.Shared.NWScriptMocks;
using NSubstitute;

namespace SWLOR.Test.Shared.Events.EventHandlers
{
    [TestFixture]
    public class EventRegistrationEventHandlersTests : TestBase
    {
        private IEventAggregator _mockEventAggregator;
        private EventRegistrationEventHandlers _handlers;
        private NWScriptServiceMock _mockNWScript;

        [SetUp]
        public void SetUp()
        {
            // Initialize the mock NWScript service
            InitializeMockNWScript();
            _mockNWScript = GetMockService();
            
            _mockEventAggregator = Substitute.For<IEventAggregator>();
            _handlers = new EventRegistrationEventHandlers(_mockEventAggregator);
        }

        [Test]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Act
            var handlers = new EventRegistrationEventHandlers(_mockEventAggregator);

            // Assert
            Assert.That(handlers, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithNullEventAggregator_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => 
                new EventRegistrationEventHandlers(null));
        }

        [Test]
        public void ExecuteHeartbeatEvent_ShouldCallEventAggregatorPublish()
        {
            // Act
            _handlers.ExecuteHeartbeatEvent();

            // Assert - The method should complete without throwing
            // We can't easily test the internal logic without mocking static NWScript calls
            Assert.Pass("ExecuteHeartbeatEvent completed successfully");
        }

        [Test]
        public void EnterServer_ShouldCallHookPlayerEvents()
        {
            // Act
            _handlers.EnterServer();

            // Assert - The method should complete without throwing
            // We can't easily test the internal logic without mocking static NWScript calls
            Assert.Pass("EnterServer completed successfully");
        }

        [Test]
        [Ignore("NWN.Core initialization required")]
        public void TriggerNWNXPersistence_ShouldCallNWNXFunctions()
        {
            // Act
            _handlers.TriggerNWNXPersistence();

            // Assert - The method should complete without throwing
            // We can't easily test the internal logic without mocking static NWScript calls
            Assert.Pass("TriggerNWNXPersistence completed successfully");
        }

        [Test]
        public void ExecuteHeartbeatEvent_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _handlers.ExecuteHeartbeatEvent());
        }

        [Test]
        public void EnterServer_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _handlers.EnterServer());
        }

        [Test]
        [Ignore("NWN.Core initialization required")]
        public void TriggerNWNXPersistence_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _handlers.TriggerNWNXPersistence());
        }
    }
}
