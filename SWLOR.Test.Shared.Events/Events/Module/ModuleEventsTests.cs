using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Test.Shared.Events.Events.Module
{
    [TestFixture]
    public class ModuleEventsTests
    {
        [Test]
        public void OnModuleLoad_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleLoad();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleLoad));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleEnter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleEnter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleEnter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleExit_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleExit();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleExit));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleDeath_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleDeath();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleDeath));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleDying_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleDying();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleDying));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleRespawn_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleRespawn();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleRespawn));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleAcquire_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleAcquire();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleAcquire));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleUnacquire_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleUnacquire();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleUnacquire));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleHeartbeat_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleHeartbeat();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleHeartbeat));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleChat_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleChat();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleChat));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleActivate_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleActivate();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleActivate));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleEquip_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleEquip();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleEquip));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleUnequip_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleUnequip();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleUnequip));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleRest_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleRest();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleRest));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleLevelUp_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleLevelUp();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleLevelUp));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnModuleUserDefined_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnModuleUserDefined();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnModuleUserDefined));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }
    }
}

