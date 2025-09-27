using NUnit.Framework;
using SWLOR.Shared.Events.Events.Creature;

namespace SWLOR.Test.Shared.Events.Events.Creature
{
    [TestFixture]
    public class OnCreatureHeartbeatAfterTests
    {
        [Test]
        public void Constructor_SetsTimestamp()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var eventData = new OnCreatureHeartbeatAfter();
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.That(eventData.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(eventData.Timestamp, Is.LessThanOrEqualTo(afterCreation));
        }

        [Test]
        public void Constructor_SetsEventId()
        {
            // Act
            var eventData = new OnCreatureHeartbeatAfter();

            // Assert
            Assert.That(eventData.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Constructor_EachInstanceHasUniqueId()
        {
            // Act
            var eventData1 = new OnCreatureHeartbeatAfter();
            var eventData2 = new OnCreatureHeartbeatAfter();

            // Assert
            Assert.That(eventData1.EventId, Is.Not.EqualTo(eventData2.EventId));
        }

        [Test]
        public void Script_ReturnsCorrectScriptName()
        {
            // Act
            var eventData = new OnCreatureHeartbeatAfter();

            // Assert
            Assert.That(eventData.Script, Is.EqualTo("crea_hb_aft"));
        }
    }
}
