using NUnit.Framework;
using SWLOR.Shared.Events.Events.NWNX;

namespace SWLOR.Test.Shared.Events.Events.NWNX
{
    [TestFixture]
    public class OnPartyKickHenchmanAfterTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var eventInstance = new OnPartyKickHenchmanAfter();

            // Assert
            Assert.That(eventInstance, Is.Not.Null);
            Assert.That(eventInstance.Script, Is.EqualTo("pty_kickhen_aft"));
        }

        [Test]
        public void EventId_ShouldBeUnique()
        {
            // Arrange
            var event1 = new OnPartyKickHenchmanAfter();
            var event2 = new OnPartyKickHenchmanAfter();

            // Assert
            Assert.That(event1.EventId, Is.Not.EqualTo(event2.EventId));
        }

        [Test]
        public void Timestamp_ShouldBeSet()
        {
            // Arrange
            var eventInstance = new OnPartyKickHenchmanAfter();

            // Assert
            Assert.That(eventInstance.Timestamp, Is.GreaterThan(System.DateTime.MinValue));
        }
    }
}
