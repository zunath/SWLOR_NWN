using NUnit.Framework;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Test.Shared.Events.EventAggregator
{
    [TestFixture]
    public class BaseEventTests
    {
        [Test]
        public void Constructor_SetsTimestamp()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var testEvent = new TestEvent();
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.That(testEvent.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(testEvent.Timestamp, Is.LessThanOrEqualTo(afterCreation));
        }

        [Test]
        public void Constructor_SetsEventId()
        {
            // Act
            var testEvent = new TestEvent();

            // Assert
            Assert.That(testEvent.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Constructor_EachInstanceHasUniqueId()
        {
            // Act
            var testEvent1 = new TestEvent();
            var testEvent2 = new TestEvent();

            // Assert
            Assert.That(testEvent1.EventId, Is.Not.EqualTo(testEvent2.EventId));
        }

        [Test]
        public void Script_IsAbstractProperty()
        {
            // Act
            var testEvent = new TestEvent();

            // Assert
            Assert.That(testEvent.Script, Is.EqualTo("TestScript"));
        }
    }

    // Test event class
    public class TestEvent : BaseEvent
    {
        public override string Script => "TestScript";
    }
}