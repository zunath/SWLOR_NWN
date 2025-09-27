using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Test.Shared.Events.Events.Creature
{
    [TestFixture]
    public class CreatureEventsTests
    {
        [Test]
        public void OnCreatureAggroEnter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureAggroEnter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureAggroEnter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureAggroExit_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureAggroExit();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureAggroExit));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureAttackAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureAttackAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureAttackAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureAttackBefore_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureAttackBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureAttackBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureBlockedAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureBlockedAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureBlockedAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureConversationAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureConversationAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureConversationAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureDamagedAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureDamagedAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureDamagedAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureDamagedBefore_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureDamagedBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureDamagedBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureDeathAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureDeathAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureDeathAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureDeathBefore_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureDeathBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureDeathBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureDisturbedAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureDisturbedAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureDisturbedAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureHeartbeatAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureHeartbeatAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureHeartbeatAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreaturePerceptionAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreaturePerceptionAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreaturePerceptionAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureRestedAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureRestedAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureRestedAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureRoundEndAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureRoundEndAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureRoundEndAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureSpawnAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureSpawnAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureSpawnAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureSpawnBefore_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureSpawnBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureSpawnBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureSpellCastAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureSpellCastAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureSpellCastAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCreatureUserDefinedAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCreatureUserDefinedAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCreatureUserDefinedAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }
    }
}

