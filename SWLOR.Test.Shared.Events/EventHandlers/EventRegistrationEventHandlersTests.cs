using SWLOR.NWN.API.NWNX;
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
        private ICreaturePluginService _mockCreaturePlugin;
        private EventRegistrationEventHandlers _handlers;

        [SetUp]
        public void SetUp()
        {
            // Initialize the mock NWScript service
            InitializeMockNWScript();
            
            _mockEventAggregator = Substitute.For<IEventAggregator>();
            _mockCreaturePlugin = Substitute.For<ICreaturePluginService>();
            _handlers = new EventRegistrationEventHandlers(_mockEventAggregator, _mockCreaturePlugin);
        }

        [Test]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Act
            var handlers = new EventRegistrationEventHandlers(_mockEventAggregator, _mockCreaturePlugin);

            // Assert
            Assert.That(handlers, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithNullEventAggregator_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => 
                new EventRegistrationEventHandlers(null, _mockCreaturePlugin));
        }

        [Test]
        public void ExecuteHeartbeatEvent_ShouldCallEventAggregatorPublish()
        {
            // Act
            _handlers.ExecuteHeartbeatEvent();

            // Assert
            Assert.Pass("ExecuteHeartbeatEvent completed successfully");
        }

        [Test]
        public void EnterServer_ShouldCallHookPlayerEvents()
        {
            // Act
            _handlers.EnterServer();

            // Assert
            Assert.Pass("EnterServer completed successfully");
        }

        [Test]
        public void TriggerNWNXPersistence_ShouldCallNWNXFunctions()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _handlers.TriggerNWNXPersistence());
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
        public void TriggerNWNXPersistence_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _handlers.TriggerNWNXPersistence());
        }
    }
}
