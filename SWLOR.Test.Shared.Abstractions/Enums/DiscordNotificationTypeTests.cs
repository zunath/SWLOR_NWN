using SWLOR.Shared.Abstractions.Enums;

namespace SWLOR.Test.Shared.Abstractions.Enums
{
    [TestFixture]
    public class DiscordNotificationTypeTests
    {
        [Test]
        public void EnumValues_ShouldBeCorrect()
        {
            // Assert
            Assert.That((int)DiscordNotificationType.Invalid, Is.EqualTo(0));
            Assert.That((int)DiscordNotificationType.DMShout, Is.EqualTo(1));
            Assert.That((int)DiscordNotificationType.Bug, Is.EqualTo(2));
            Assert.That((int)DiscordNotificationType.Holonet, Is.EqualTo(3));
        }

        [Test]
        public void EnumValues_ShouldBeUnique()
        {
            // Arrange
            var values = Enum.GetValues<DiscordNotificationType>().Cast<int>().ToArray();

            // Assert
            Assert.That(values, Is.Unique);
        }

        [Test]
        public void EnumValues_ShouldBeSequential()
        {
            // Arrange
            var values = Enum.GetValues<DiscordNotificationType>().Cast<int>().OrderBy(x => x).ToArray();

            // Assert
            for (int i = 0; i < values.Length - 1; i++)
            {
                Assert.That(values[i + 1], Is.EqualTo(values[i] + 1), 
                    $"Values should be sequential, but found {values[i]} followed by {values[i + 1]}");
            }
        }

        [Test]
        public void EnumToString_ShouldReturnCorrectName()
        {
            // Assert
            Assert.That(DiscordNotificationType.Invalid.ToString(), Is.EqualTo("Invalid"));
            Assert.That(DiscordNotificationType.DMShout.ToString(), Is.EqualTo("DMShout"));
            Assert.That(DiscordNotificationType.Bug.ToString(), Is.EqualTo("Bug"));
            Assert.That(DiscordNotificationType.Holonet.ToString(), Is.EqualTo("Holonet"));
        }

        [Test]
        public void EnumParse_ShouldWorkCorrectly()
        {
            // Assert
            Assert.That(Enum.Parse<DiscordNotificationType>("Invalid"), Is.EqualTo(DiscordNotificationType.Invalid));
            Assert.That(Enum.Parse<DiscordNotificationType>("DMShout"), Is.EqualTo(DiscordNotificationType.DMShout));
            Assert.That(Enum.Parse<DiscordNotificationType>("Bug"), Is.EqualTo(DiscordNotificationType.Bug));
            Assert.That(Enum.Parse<DiscordNotificationType>("Holonet"), Is.EqualTo(DiscordNotificationType.Holonet));
        }
    }
}
