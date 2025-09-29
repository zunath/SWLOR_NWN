using SWLOR.Shared.Events.Events.Ability;

namespace SWLOR.Test.Shared.Events.Events.Ability
{
    [TestFixture]
    public class OnGrenadeGas1EnableTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var eventInstance = new OnGrenadeGas1Enable();

            // Assert
            Assert.That(eventInstance, Is.Not.Null);
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Script_ShouldReturnCorrectValue()
        {
            // Act
            var eventInstance = new OnGrenadeGas1Enable();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo("grenade_gas1_en"));
        }
    }
}
