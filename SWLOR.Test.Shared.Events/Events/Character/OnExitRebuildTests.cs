using SWLOR.Shared.Events.Events.Character;

namespace SWLOR.Test.Shared.Events.Events.Character
{
    [TestFixture]
    public class OnExitRebuildTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var eventInstance = new OnExitRebuild();

            // Assert
            Assert.That(eventInstance, Is.Not.Null);
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Script_ShouldReturnCorrectValue()
        {
            // Act
            var eventInstance = new OnExitRebuild();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo("exit_rebuild"));
        }
    }
}
