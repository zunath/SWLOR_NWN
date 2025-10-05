using SWLOR.Shared.Events.Events.NWNX;

namespace SWLOR.Test.Shared.Events.Events.NWNX
{
    [TestFixture]
    public class OnLevelUpBeforeTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var eventInstance = new OnLevelUpBefore();

            // Assert
            Assert.That(eventInstance, Is.Not.Null);
        }

        [Test]
        public void Script_ShouldReturnCorrectValue()
        {
            // Act
            var eventInstance = new OnLevelUpBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo("lvl_up_bef"));
        }

        [Test]
        public void EventId_ShouldBeUnique()
        {
            // Act
            var event1 = new OnLevelUpBefore();
            var event2 = new OnLevelUpBefore();

            // Assert
            Assert.That(event1.EventId, Is.Not.EqualTo(event2.EventId));
        }

        [Test]
        public void Timestamp_ShouldBeSet()
        {
            // Act
            var eventInstance = new OnLevelUpBefore();

            // Assert
            Assert.That(eventInstance.Timestamp, Is.GreaterThan(DateTime.MinValue));
        }
    }
}
