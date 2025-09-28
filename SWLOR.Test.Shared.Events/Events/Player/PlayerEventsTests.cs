using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Space;

namespace SWLOR.Test.Shared.Events.Events.Player
{
    [TestFixture]
    public class PlayerEventsTests
    {
        [Test]
        public void OnPlayerAttacked_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerAttacked();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerAttacked));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerBlocked_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerBlocked();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerBlocked));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerDamaged_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerDamaged();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerDamaged));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerDeath_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerDeath();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerDeath));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerDisturbed_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerDisturbed();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerDisturbed));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerHeartbeat_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerHeartbeat();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerHeartbeat));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerPerception_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerPerception();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerPerception));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerRested_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerRested();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerRested));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerRoundEnd_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerRoundEnd();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerRoundEnd));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerSpawn_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerSpawn();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerSpawn));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerSpellCastAt_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerSpellCastAt();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerSpellCastAt));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerUserDefined_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerUserDefined();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerUserDefined));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerFPAdjusted_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerFPAdjusted();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerFPAdjusted));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerStaminaAdjusted_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerStaminaAdjusted();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerStaminaAdjusted));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerShieldAdjusted_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerShieldAdjusted();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerShieldAdjusted));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerHullAdjusted_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerHullAdjusted();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerHullAdjusted));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerCapAdjusted_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerCapAdjusted();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerCapAdjusted));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerTargetUpdated_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerTargetUpdated();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerTargetUpdated));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnPlayerCacheData_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnPlayerCacheData();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnPlayerCacheData));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }
    }
}

