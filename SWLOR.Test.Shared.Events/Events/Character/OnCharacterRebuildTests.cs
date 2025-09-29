using SWLOR.Shared.Events.Events.Character;

namespace SWLOR.Test.Shared.Events.Events.Character
{
    [TestFixture]
    public class OnCharacterRebuildTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var eventInstance = new OnCharacterRebuild();

            // Assert
            Assert.That(eventInstance, Is.Not.Null);
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Script_ShouldReturnCorrectValue()
        {
            // Act
            var eventInstance = new OnCharacterRebuild();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo("char_rebuild"));
        }

        [Test]
        public void Timestamp_ShouldBeSetToCurrentTime()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var eventInstance = new OnCharacterRebuild();

            // Assert
            var afterCreation = DateTime.UtcNow;
            Assert.That(eventInstance.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(eventInstance.Timestamp, Is.LessThanOrEqualTo(afterCreation));
        }

        [Test]
        public void EventId_ShouldBeUnique()
        {
            // Act
            var event1 = new OnCharacterRebuild();
            var event2 = new OnCharacterRebuild();

            // Assert
            Assert.That(event1.EventId, Is.Not.EqualTo(event2.EventId));
        }
    }
}
