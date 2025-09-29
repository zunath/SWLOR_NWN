using SWLOR.Shared.Events.Events.NWNX;
using NUnit.Framework;

namespace SWLOR.Test.Shared.Events.Events.NWNX
{
    [TestFixture]
    public class OnLevelDownBeforeTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var eventInstance = new OnLevelDownBefore();

            // Assert
            Assert.That(eventInstance, Is.Not.Null);
        }

        [Test]
        public void Script_ShouldReturnCorrectValue()
        {
            // Act
            var eventInstance = new OnLevelDownBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo("lvl_down_bef"));
        }

        [Test]
        public void EventId_ShouldBeUnique()
        {
            // Act
            var event1 = new OnLevelDownBefore();
            var event2 = new OnLevelDownBefore();

            // Assert
            Assert.That(event1.EventId, Is.Not.EqualTo(event2.EventId));
        }

        [Test]
        public void Timestamp_ShouldBeSet()
        {
            // Act
            var eventInstance = new OnLevelDownBefore();

            // Assert
            Assert.That(eventInstance.Timestamp, Is.GreaterThan(DateTime.MinValue));
        }
    }
}
