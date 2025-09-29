using NUnit.Framework;
using SWLOR.Shared.Events.Events.NWNX;

namespace SWLOR.Test.Shared.Events.Events.NWNX
{
    [TestFixture]
    public class OnLevelUpAutomaticBeforeTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var eventInstance = new OnLevelUpAutomaticBefore();

            // Assert
            Assert.That(eventInstance, Is.Not.Null);
            Assert.That(eventInstance.Script, Is.EqualTo("lvl_upauto_bef"));
        }

        [Test]
        public void EventId_ShouldBeUnique()
        {
            // Arrange
            var event1 = new OnLevelUpAutomaticBefore();
            var event2 = new OnLevelUpAutomaticBefore();

            // Assert
            Assert.That(event1.EventId, Is.Not.EqualTo(event2.EventId));
        }

        [Test]
        public void Timestamp_ShouldBeSet()
        {
            // Arrange
            var eventInstance = new OnLevelUpAutomaticBefore();

            // Assert
            Assert.That(eventInstance.Timestamp, Is.GreaterThan(System.DateTime.MinValue));
        }
    }
}
