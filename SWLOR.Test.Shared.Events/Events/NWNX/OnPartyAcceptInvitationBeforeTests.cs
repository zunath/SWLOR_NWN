using NUnit.Framework;
using SWLOR.Shared.Events.Events.NWNX;

namespace SWLOR.Test.Shared.Events.Events.NWNX
{
    [TestFixture]
    public class OnPartyAcceptInvitationBeforeTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var eventInstance = new OnPartyAcceptInvitationBefore();

            // Assert
            Assert.That(eventInstance, Is.Not.Null);
            Assert.That(eventInstance.Script, Is.EqualTo("pty_accept_bef"));
        }

        [Test]
        public void EventId_ShouldBeUnique()
        {
            // Arrange
            var event1 = new OnPartyAcceptInvitationBefore();
            var event2 = new OnPartyAcceptInvitationBefore();

            // Assert
            Assert.That(event1.EventId, Is.Not.EqualTo(event2.EventId));
        }

        [Test]
        public void Timestamp_ShouldBeSet()
        {
            // Arrange
            var eventInstance = new OnPartyAcceptInvitationBefore();

            // Assert
            Assert.That(eventInstance.Timestamp, Is.GreaterThan(System.DateTime.MinValue));
        }
    }
}
