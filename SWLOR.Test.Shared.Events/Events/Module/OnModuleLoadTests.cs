using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Test.Shared.Events.Events.Module
{
    [TestFixture]
    public class OnModuleLoadTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var eventInstance = new OnModuleLoad();

            // Assert
            Assert.That(eventInstance, Is.Not.Null);
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Script_ShouldReturnCorrectValue()
        {
            // Act
            var eventInstance = new OnModuleLoad();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo("mod_load"));
        }
    }
}