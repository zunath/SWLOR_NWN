using SWLOR.Shared.Events.Events.Space;

namespace SWLOR.Test.Shared.Events.Events.Space
{
    [TestFixture]
    public class OnSpaceEnterTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var eventInstance = new OnSpaceEnter();

            // Assert
            Assert.That(eventInstance, Is.Not.Null);
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Script_ShouldReturnCorrectValue()
        {
            // Act
            var eventInstance = new OnSpaceEnter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo("space_enter"));
        }
    }
}
