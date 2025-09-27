using NUnit.Framework;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Test.Shared.Events.Events.NWNX
{
    [TestFixture]
    public class CommonNWNXEventsTests
    {
        [Test]
        public void OnAssociateAddAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnAssociateAddAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnAssociateAddAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnAssociateAddBefore_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnAssociateAddBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnAssociateAddBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnAssociateRemoveAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnAssociateRemoveAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnAssociateRemoveAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnAssociateRemoveBefore_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnAssociateRemoveBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnAssociateRemoveBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCastSpellAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCastSpellAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCastSpellAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCastSpellBefore_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCastSpellBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCastSpellBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnClientConnectAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnClientConnectAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnClientConnectAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnClientConnectBefore_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnClientConnectBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnClientConnectBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnClientDisconnectAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnClientDisconnectAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnClientDisconnectAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnClientDisconnectBefore_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnClientDisconnectBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnClientDisconnectBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCombatModeOff_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCombatModeOff();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCombatModeOff));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnCombatModeOn_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnCombatModeOn();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnCombatModeOn));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnExamineObjectAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnExamineObjectAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnExamineObjectAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnExamineObjectBefore_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnExamineObjectBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnExamineObjectBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnUseFeatAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnUseFeatAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnUseFeatAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnUseFeatBefore_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnUseFeatBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnUseFeatBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnUseItemAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnUseItemAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnUseItemAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        // OnUseItemBefore doesn't exist in the codebase, skipping this test

        [Test]
        public void OnUseSkillAfter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnUseSkillAfter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnUseSkillAfter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnUseSkillBefore_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnUseSkillBefore();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnUseSkillBefore));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }
    }
}
