using NUnit.Framework;
using SWLOR.Shared.Abstractions.Models;

namespace SWLOR.Test.Shared.Abstractions.Models
{
    [TestFixture]
    public class DiscordNotificationFieldTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var field = new DiscordNotificationField();

            // Assert
            Assert.That(field.IsInline, Is.False);
            Assert.That(field.Name, Is.Null);
            Assert.That(field.Value, Is.Null);
        }

        [Test]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var testName = "Test Field";
            var testValue = "Test Value";
            var testIsInline = true;

            // Act
            field.Name = testName;
            field.Value = testValue;
            field.IsInline = testIsInline;

            // Assert
            Assert.That(field.Name, Is.EqualTo(testName));
            Assert.That(field.Value, Is.EqualTo(testValue));
            Assert.That(field.IsInline, Is.EqualTo(testIsInline));
        }

        [Test]
        public void Properties_ShouldAcceptDifferentValueTypes()
        {
            // Arrange
            var field = new DiscordNotificationField();

            // Act & Assert - String value
            field.Value = "String Value";
            Assert.That(field.Value, Is.EqualTo("String Value"));

            // Act & Assert - Integer value
            field.Value = 42;
            Assert.That(field.Value, Is.EqualTo(42));

            // Act & Assert - Boolean value
            field.Value = true;
            Assert.That(field.Value, Is.EqualTo(true));

            // Act & Assert - Double value
            field.Value = 3.14;
            Assert.That(field.Value, Is.EqualTo(3.14));

            // Act & Assert - Null value
            field.Value = null;
            Assert.That(field.Value, Is.Null);
        }

        [Test]
        public void Properties_ShouldAcceptEmptyString()
        {
            // Arrange
            var field = new DiscordNotificationField();

            // Act
            field.Name = "";
            field.Value = "";

            // Assert
            Assert.That(field.Name, Is.EqualTo(""));
            Assert.That(field.Value, Is.EqualTo(""));
        }

        [Test]
        public void Properties_ShouldAcceptLongString()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var longString = new string('A', 1000);

            // Act
            field.Name = longString;
            field.Value = longString;

            // Assert
            Assert.That(field.Name, Is.EqualTo(longString));
            Assert.That(field.Value, Is.EqualTo(longString));
        }
    }
}
