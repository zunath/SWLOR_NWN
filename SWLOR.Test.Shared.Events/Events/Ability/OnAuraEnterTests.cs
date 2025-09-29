using SWLOR.Shared.Events.Events.Ability;

namespace SWLOR.Test.Shared.Events.Events.Ability
{
    [TestFixture]
    public class OnAuraEnterTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var eventInstance = new OnAuraEnter();

            // Assert
            Assert.That(eventInstance, Is.Not.Null);
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Script_ShouldReturnCorrectValue()
        {
            // Act
            var eventInstance = new OnAuraEnter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo("aura_enter"));
        }

        [Test]
        public void Timestamp_ShouldBeSetToCurrentTime()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var eventInstance = new OnAuraEnter();

            // Assert
            var afterCreation = DateTime.UtcNow;
            Assert.That(eventInstance.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(eventInstance.Timestamp, Is.LessThanOrEqualTo(afterCreation));
        }

        [Test]
        public void EventId_ShouldBeUnique()
        {
            // Act
            var event1 = new OnAuraEnter();
            var event2 = new OnAuraEnter();

            // Assert
            Assert.That(event1.EventId, Is.Not.EqualTo(event2.EventId));
        }
    }
}
