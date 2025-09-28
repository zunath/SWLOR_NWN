using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Test.Shared.Events.Events.Infrastructure
{
    [TestFixture]
    public class OnHookEventsTests
    {
        [Test]
        public void Script_ShouldReturnCorrectScriptName()
        {
            // Arrange & Act
            var eventInstance = new OnHookEvents();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnHookEvents));
        }

        [Test]
        public void Constructor_ShouldInitializeBaseEventProperties()
        {
            // Arrange & Act
            var eventInstance = new OnHookEvents();

            // Assert
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Constructor_ShouldSetTimestampToUtcNow()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var eventInstance = new OnHookEvents();

            // Assert
            var afterCreation = DateTime.UtcNow;
            Assert.That(eventInstance.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(eventInstance.Timestamp, Is.LessThanOrEqualTo(afterCreation));
        }

        [Test]
        public void Constructor_ShouldGenerateUniqueEventIds()
        {
            // Arrange & Act
            var event1 = new OnHookEvents();
            var event2 = new OnHookEvents();

            // Assert
            Assert.That(event1.EventId, Is.Not.EqualTo(event2.EventId));
        }
    }
}

