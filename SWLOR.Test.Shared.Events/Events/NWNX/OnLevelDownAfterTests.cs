using SWLOR.Shared.Events.Events.NWNX;
using NUnit.Framework;

namespace SWLOR.Test.Shared.Events.Events.NWNX
{
    [TestFixture]
    public class OnLevelDownAfterTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var eventInstance = new OnLevelDownAfter();

            // Assert
            Assert.That(eventInstance, Is.Not.Null);
        }

        [Test]
        public void Script_ShouldReturnCorrectValue()
        {
            // Act
            var eventInstance = new OnLevelDownAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo("lvl_down_aft"));
        }

        [Test]
        public void EventId_ShouldBeUnique()
        {
            // Act
            var event1 = new OnLevelDownAfter();
            var event2 = new OnLevelDownAfter();

            // Assert
            Assert.That(event1.EventId, Is.Not.EqualTo(event2.EventId));
        }

        [Test]
        public void Timestamp_ShouldBeSet()
        {
            // Act
            var eventInstance = new OnLevelDownAfter();

            // Assert
            Assert.That(eventInstance.Timestamp, Is.GreaterThan(DateTime.MinValue));
        }
    }
}
