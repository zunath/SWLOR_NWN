using NUnit.Framework;
using SWLOR.Shared.Events.Events.NWNX;

namespace SWLOR.Test.Shared.Events.Events.NWNX
{
    [TestFixture]
    public class AdditionalNWNXEventsTests
    {
        [Test]
        public void OnBartenderEndBefore_Constructor_SetsTimestamp()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var eventData = new OnBartenderEndBefore();
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.That(eventData.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(eventData.Timestamp, Is.LessThanOrEqualTo(afterCreation));
        }

        [Test]
        public void OnBartenderEndBefore_Constructor_SetsEventId()
        {
            // Act
            var eventData = new OnBartenderEndBefore();

            // Assert
            Assert.That(eventData.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnBartenderEndBefore_Constructor_EachInstanceHasUniqueId()
        {
            // Act
            var eventData1 = new OnBartenderEndBefore();
            var eventData2 = new OnBartenderEndBefore();

            // Assert
            Assert.That(eventData1.EventId, Is.Not.EqualTo(eventData2.EventId));
        }

        [Test]
        public void OnBartenderEndBefore_Script_ReturnsCorrectScriptName()
        {
            // Act
            var eventData = new OnBartenderEndBefore();

            // Assert
            Assert.That(eventData.Script, Is.EqualTo("bart_end_bef"));
        }

        [Test]
        public void OnBartenderStartBefore_Constructor_SetsTimestamp()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var eventData = new OnBartenderStartBefore();
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.That(eventData.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(eventData.Timestamp, Is.LessThanOrEqualTo(afterCreation));
        }

        [Test]
        public void OnBartenderStartBefore_Constructor_SetsEventId()
        {
            // Act
            var eventData = new OnBartenderStartBefore();

            // Assert
            Assert.That(eventData.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnBartenderStartBefore_Constructor_EachInstanceHasUniqueId()
        {
            // Act
            var eventData1 = new OnBartenderStartBefore();
            var eventData2 = new OnBartenderStartBefore();

            // Assert
            Assert.That(eventData1.EventId, Is.Not.EqualTo(eventData2.EventId));
        }

        [Test]
        public void OnBartenderStartBefore_Script_ReturnsCorrectScriptName()
        {
            // Act
            var eventData = new OnBartenderStartBefore();

            // Assert
            Assert.That(eventData.Script, Is.EqualTo("bart_start_bef"));
        }

        [Test]
        public void OnBroadcastAttackOfOpportunityAfter_Constructor_SetsTimestamp()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var eventData = new OnBroadcastAttackOfOpportunityAfter();
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.That(eventData.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(eventData.Timestamp, Is.LessThanOrEqualTo(afterCreation));
        }

        [Test]
        public void OnBroadcastAttackOfOpportunityAfter_Constructor_SetsEventId()
        {
            // Act
            var eventData = new OnBroadcastAttackOfOpportunityAfter();

            // Assert
            Assert.That(eventData.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnBroadcastAttackOfOpportunityAfter_Constructor_EachInstanceHasUniqueId()
        {
            // Act
            var eventData1 = new OnBroadcastAttackOfOpportunityAfter();
            var eventData2 = new OnBroadcastAttackOfOpportunityAfter();

            // Assert
            Assert.That(eventData1.EventId, Is.Not.EqualTo(eventData2.EventId));
        }

        [Test]
        public void OnBroadcastAttackOfOpportunityAfter_Script_ReturnsCorrectScriptName()
        {
            // Act
            var eventData = new OnBroadcastAttackOfOpportunityAfter();

            // Assert
            Assert.That(eventData.Script, Is.EqualTo("brdcast_aoo_aft"));
        }

        [Test]
        public void OnBroadcastAttackOfOpportunityBefore_Constructor_SetsTimestamp()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var eventData = new OnBroadcastAttackOfOpportunityBefore();
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.That(eventData.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(eventData.Timestamp, Is.LessThanOrEqualTo(afterCreation));
        }

        [Test]
        public void OnBroadcastAttackOfOpportunityBefore_Constructor_SetsEventId()
        {
            // Act
            var eventData = new OnBroadcastAttackOfOpportunityBefore();

            // Assert
            Assert.That(eventData.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnBroadcastAttackOfOpportunityBefore_Constructor_EachInstanceHasUniqueId()
        {
            // Act
            var eventData1 = new OnBroadcastAttackOfOpportunityBefore();
            var eventData2 = new OnBroadcastAttackOfOpportunityBefore();

            // Assert
            Assert.That(eventData1.EventId, Is.Not.EqualTo(eventData2.EventId));
        }

        [Test]
        public void OnBroadcastAttackOfOpportunityBefore_Script_ReturnsCorrectScriptName()
        {
            // Act
            var eventData = new OnBroadcastAttackOfOpportunityBefore();

            // Assert
            Assert.That(eventData.Script, Is.EqualTo("brdcast_aoo_bef"));
        }

        [Test]
        public void OnBroadcastCastSpellAfter_Constructor_SetsTimestamp()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var eventData = new OnBroadcastCastSpellAfter();
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.That(eventData.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(eventData.Timestamp, Is.LessThanOrEqualTo(afterCreation));
        }

        [Test]
        public void OnBroadcastCastSpellAfter_Constructor_SetsEventId()
        {
            // Act
            var eventData = new OnBroadcastCastSpellAfter();

            // Assert
            Assert.That(eventData.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnBroadcastCastSpellAfter_Constructor_EachInstanceHasUniqueId()
        {
            // Act
            var eventData1 = new OnBroadcastCastSpellAfter();
            var eventData2 = new OnBroadcastCastSpellAfter();

            // Assert
            Assert.That(eventData1.EventId, Is.Not.EqualTo(eventData2.EventId));
        }

        [Test]
        public void OnBroadcastCastSpellAfter_Script_ReturnsCorrectScriptName()
        {
            // Act
            var eventData = new OnBroadcastCastSpellAfter();

            // Assert
            Assert.That(eventData.Script, Is.EqualTo("cast_spell_aft"));
        }

        [Test]
        public void OnBroadcastCastSpellBefore_Constructor_SetsTimestamp()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var eventData = new OnBroadcastCastSpellBefore();
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.That(eventData.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(eventData.Timestamp, Is.LessThanOrEqualTo(afterCreation));
        }

        [Test]
        public void OnBroadcastCastSpellBefore_Constructor_SetsEventId()
        {
            // Act
            var eventData = new OnBroadcastCastSpellBefore();

            // Assert
            Assert.That(eventData.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnBroadcastCastSpellBefore_Constructor_EachInstanceHasUniqueId()
        {
            // Act
            var eventData1 = new OnBroadcastCastSpellBefore();
            var eventData2 = new OnBroadcastCastSpellBefore();

            // Assert
            Assert.That(eventData1.EventId, Is.Not.EqualTo(eventData2.EventId));
        }

        [Test]
        public void OnBroadcastCastSpellBefore_Script_ReturnsCorrectScriptName()
        {
            // Act
            var eventData = new OnBroadcastCastSpellBefore();

            // Assert
            Assert.That(eventData.Script, Is.EqualTo("cast_spell_bef"));
        }
    }
}
