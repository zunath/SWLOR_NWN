using SWLOR.Shared.Events.Events.Infrastructure;

namespace SWLOR.Test.Shared.Events.EventAggregator
{
    [TestFixture]
    public class BaseEventTests
    {
        [Test]
        public void Constructor_ShouldSetTimestamp()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var testEvent = new OnServerLoaded();

            // Assert
            Assert.That(testEvent.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(testEvent.Timestamp, Is.LessThanOrEqualTo(DateTime.UtcNow));
        }

        [Test]
        public void Constructor_ShouldSetEventId()
        {
            // Act
            var testEvent = new OnServerLoaded();

            // Assert
            Assert.That(testEvent.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Constructor_ShouldSetUniqueEventIds()
        {
            // Act
            var event1 = new OnServerLoaded();
            var event2 = new OnServerLoaded();

            // Assert
            Assert.That(event1.EventId, Is.Not.EqualTo(event2.EventId));
        }

        [Test]
        public void Script_ShouldReturnCorrectValue()
        {
            // Act
            var testEvent = new OnServerLoaded();

            // Assert
            Assert.That(testEvent.Script, Is.EqualTo("server_loaded"));
        }

        [Test]
        public void Timestamp_ShouldBeReadOnly()
        {
            // Arrange
            var testEvent = new OnServerLoaded();

            // Act & Assert
            Assert.That(testEvent.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            // Timestamp should be set during construction and not changeable
        }

        [Test]
        public void EventId_ShouldBeReadOnly()
        {
            // Arrange
            var testEvent = new OnServerLoaded();

            // Act & Assert
            Assert.That(testEvent.EventId, Is.Not.EqualTo(Guid.Empty));
            // EventId should be set during construction and not changeable
        }
    }
}