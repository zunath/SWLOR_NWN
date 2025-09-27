using NUnit.Framework;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Test.Shared.Events.Events.Area
{
    [TestFixture]
    public class AreaEventsTests
    {
        [Test]
        public void OnAreaEnter_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnAreaEnter();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnAreaEnter));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnAreaExit_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnAreaExit();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnAreaExit));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnAreaHeartbeat_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnAreaHeartbeat();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnAreaHeartbeat));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void OnAreaUserDefined_ShouldHaveCorrectScript()
        {
            // Arrange & Act
            var eventInstance = new OnAreaUserDefined();

            // Assert
            Assert.That(eventInstance.Script, Is.EqualTo(ScriptName.OnAreaUserDefined));
            Assert.That(eventInstance.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(eventInstance.EventId, Is.Not.EqualTo(Guid.Empty));
        }
    }
}

