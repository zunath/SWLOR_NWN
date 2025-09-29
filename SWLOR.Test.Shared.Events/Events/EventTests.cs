using SWLOR.Shared.Events.Events.Ability;
using SWLOR.Shared.Events.Events.Character;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.Events.Events.Space;
using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Test.Shared.Events.Events
{
    [TestFixture]
    public class EventTests
    {
        [Test]
        public void AllEventClasses_ShouldHaveValidScriptProperty()
        {
            // Test various event classes to ensure they have valid Script properties
            var events = new List<object>
            {
                new OnAuraEnter(),
                new OnAuraExit(),
                new OnBurstOfSpeedApply(),
                new OnBurstOfSpeedRemoved(),
                new OnCharacterRebuild(),
                new OnExitRebuild(),
                new OnExitSpending(),
                new OnBuyStatRebuild(),
                new OnHookEvents(),
                new OnHookNativeOverrides(),
                new OnServerLoaded(),
                new OnModuleLoad(),
                new OnModuleEnter(),
                new OnModuleExit(),
                new OnModuleDeath(),
                new OnModuleDying(),
                new OnModuleRespawn(),
                new OnModuleAcquire(),
                new OnModuleUnacquire(),
                new OnPlayerDamaged(),
                new OnPlayerHeartbeat(),
                new OnPlayerPerception(),
                new OnPlayerSpellCastAt(),
                new OnSpaceEnter(),
                new OnSpaceExit(),
                new OnUseShipComputer(),
                new OnSpaceTarget()
            };

            // Act & Assert
            foreach (var eventObj in events)
            {
                var scriptProperty = eventObj.GetType().GetProperty("Script");
                Assert.That(scriptProperty, Is.Not.Null, $"Event {eventObj.GetType().Name} should have Script property");
                
                var scriptValue = scriptProperty.GetValue(eventObj) as string;
                Assert.That(scriptValue, Is.Not.Null, $"Event {eventObj.GetType().Name} Script should not be null");
                Assert.That(scriptValue, Is.Not.Empty, $"Event {eventObj.GetType().Name} Script should not be empty");
            }
        }

        [Test]
        public void AllEventClasses_ShouldHaveValidTimestamp()
        {
            // Test various event classes to ensure they have valid Timestamp
            var events = new List<object>
            {
                new OnAuraEnter(),
                new OnAuraExit(),
                new OnBurstOfSpeedApply(),
                new OnCharacterRebuild(),
                new OnHookEvents(),
                new OnModuleLoad(),
                new OnPlayerDamaged(),
                new OnSpaceEnter()
            };

            // Act & Assert
            foreach (var eventObj in events)
            {
                var timestampProperty = eventObj.GetType().GetProperty("Timestamp");
                Assert.That(timestampProperty, Is.Not.Null, $"Event {eventObj.GetType().Name} should have Timestamp property");
                
                var timestampValue = (DateTime)timestampProperty.GetValue(eventObj);
                Assert.That(timestampValue, Is.Not.EqualTo(DateTime.MinValue), $"Event {eventObj.GetType().Name} Timestamp should be set");
            }
        }

        [Test]
        public void AllEventClasses_ShouldHaveValidEventId()
        {
            // Test various event classes to ensure they have valid EventId
            var events = new List<object>
            {
                new OnAuraEnter(),
                new OnAuraExit(),
                new OnBurstOfSpeedApply(),
                new OnCharacterRebuild(),
                new OnHookEvents(),
                new OnModuleLoad(),
                new OnPlayerDamaged(),
                new OnSpaceEnter()
            };

            // Act & Assert
            foreach (var eventObj in events)
            {
                var eventIdProperty = eventObj.GetType().GetProperty("EventId");
                Assert.That(eventIdProperty, Is.Not.Null, $"Event {eventObj.GetType().Name} should have EventId property");
                
                var eventIdValue = (Guid)eventIdProperty.GetValue(eventObj);
                Assert.That(eventIdValue, Is.Not.EqualTo(Guid.Empty), $"Event {eventObj.GetType().Name} EventId should be set");
            }
        }

        [Test]
        public void AllEventClasses_ShouldHaveUniqueEventIds()
        {
            // Test that multiple instances of the same event class have unique EventIds
            var event1 = new OnAuraEnter();
            var event2 = new OnAuraEnter();
            var event3 = new OnAuraEnter();

            // Act & Assert
            Assert.That(event1.EventId, Is.Not.EqualTo(event2.EventId));
            Assert.That(event1.EventId, Is.Not.EqualTo(event3.EventId));
            Assert.That(event2.EventId, Is.Not.EqualTo(event3.EventId));
        }

        [Test]
        public void AllEventClasses_ShouldImplementIEvent()
        {
            // Test that all event classes implement IEvent interface
            var eventTypes = new List<Type>
            {
                typeof(OnAuraEnter),
                typeof(OnAuraExit),
                typeof(OnBurstOfSpeedApply),
                typeof(OnCharacterRebuild),
                typeof(OnHookEvents),
                typeof(OnModuleLoad),
                typeof(OnPlayerDamaged),
                typeof(OnSpaceEnter)
            };

            // Act & Assert
            foreach (var eventType in eventTypes)
            {
                Assert.That(typeof(IEvent).IsAssignableFrom(eventType), Is.True, 
                    $"Event {eventType.Name} should implement IEvent interface");
            }
        }
    }
}
