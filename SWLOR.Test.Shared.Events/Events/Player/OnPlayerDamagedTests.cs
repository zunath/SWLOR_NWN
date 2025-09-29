using SWLOR.Shared.Events.Events.Player;

namespace SWLOR.Test.Shared.Events.Events.Player
{
    [TestFixture]
    public class OnPlayerDamagedTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var eventInstance = new OnPlayerDamaged();

            // Assert
            Assert.That(eventInstance, Is.Not.Null);
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Script_ShouldReturnCorrectValue()
        {
            // Act
            var eventInstance = new OnPlayerDamaged();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo("pc_damaged"));
        }
    }
}