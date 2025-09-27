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

        [Test]
        public void Properties_ShouldAcceptComplexObjects()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var complexObject = new { Name = "Test", Value = 123, Items = new[] { "A", "B", "C" } };

            // Act
            field.Name = "Complex Field";
            field.Value = complexObject;

            // Assert
            Assert.That(field.Name, Is.EqualTo("Complex Field"));
            Assert.That(field.Value, Is.EqualTo(complexObject));
        }

        [Test]
        public void Properties_ShouldAcceptCollections()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var list = new List<string> { "Item1", "Item2", "Item3" };
            var array = new int[] { 1, 2, 3, 4, 5 };

            // Act
            field.Name = "List Field";
            field.Value = list;

            // Assert
            Assert.That(field.Value, Is.EqualTo(list));

            // Act
            field.Name = "Array Field";
            field.Value = array;

            // Assert
            Assert.That(field.Value, Is.EqualTo(array));
        }

        [Test]
        public void Properties_ShouldAcceptDateTimeValues()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var dateTime = DateTime.UtcNow;

            // Act
            field.Name = "DateTime Field";
            field.Value = dateTime;

            // Assert
            Assert.That(field.Value, Is.EqualTo(dateTime));
        }

        [Test]
        public void Properties_ShouldAcceptGuidValues()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var guid = Guid.NewGuid();

            // Act
            field.Name = "Guid Field";
            field.Value = guid;

            // Assert
            Assert.That(field.Value, Is.EqualTo(guid));
        }

        [Test]
        public void Properties_ShouldAcceptDecimalValues()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var decimalValue = 123.456m;

            // Act
            field.Name = "Decimal Field";
            field.Value = decimalValue;

            // Assert
            Assert.That(field.Value, Is.EqualTo(decimalValue));
        }

        [Test]
        public void Properties_ShouldAcceptNullValues()
        {
            // Arrange
            var field = new DiscordNotificationField();

            // Act
            field.Name = null;
            field.Value = null;

            // Assert
            Assert.That(field.Name, Is.Null);
            Assert.That(field.Value, Is.Null);
        }

        [Test]
        public void Properties_ShouldAcceptUnicodeStrings()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var unicodeString = "测试-🚀-特殊字符";

            // Act
            field.Name = unicodeString;
            field.Value = unicodeString;

            // Assert
            Assert.That(field.Name, Is.EqualTo(unicodeString));
            Assert.That(field.Value, Is.EqualTo(unicodeString));
        }

        [Test]
        public void Properties_ShouldAcceptEmptyCollections()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var emptyList = new List<string>();
            var emptyArray = new int[0];

            // Act
            field.Name = "Empty List";
            field.Value = emptyList;

            // Assert
            Assert.That(field.Value, Is.EqualTo(emptyList));

            // Act
            field.Name = "Empty Array";
            field.Value = emptyArray;

            // Assert
            Assert.That(field.Value, Is.EqualTo(emptyArray));
        }

        [Test]
        public void Properties_ShouldAcceptNestedObjects()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var nestedObject = new
            {
                Level1 = new
                {
                    Level2 = new
                    {
                        Value = "Deep Value",
                        Number = 42
                    }
                }
            };

            // Act
            field.Name = "Nested Object";
            field.Value = nestedObject;

            // Assert
            Assert.That(field.Value, Is.EqualTo(nestedObject));
        }

        [Test]
        public void IsInline_ShouldBeSettable()
        {
            // Arrange
            var field = new DiscordNotificationField();

            // Act
            field.IsInline = true;

            // Assert
            Assert.That(field.IsInline, Is.True);

            // Act
            field.IsInline = false;

            // Assert
            Assert.That(field.IsInline, Is.False);
        }
    }
}
