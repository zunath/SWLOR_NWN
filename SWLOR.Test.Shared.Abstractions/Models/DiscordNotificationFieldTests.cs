using SWLOR.Shared.Abstractions.Models;

namespace SWLOR.Test.Shared.Abstractions.Models
{
    [TestFixture]
    public class DiscordNotificationFieldTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var field = new DiscordNotificationField();

            // Assert
            Assert.That(field, Is.Not.Null);
            Assert.That(field.IsInline, Is.False);
            Assert.That(field.Name, Is.Null);
            Assert.That(field.Value, Is.Null);
        }

        [Test]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var field = new DiscordNotificationField();

            // Act
            field.IsInline = true;
            field.Name = "Test Name";
            field.Value = "Test Value";

            // Assert
            Assert.That(field.IsInline, Is.True);
            Assert.That(field.Name, Is.EqualTo("Test Name"));
            Assert.That(field.Value, Is.EqualTo("Test Value"));
        }

        [Test]
        public void Properties_ShouldBeSettableWithDifferentTypes()
        {
            // Arrange
            var field = new DiscordNotificationField();

            // Act
            field.IsInline = true;
            field.Name = "Number Field";
            field.Value = 42;

            // Assert
            Assert.That(field.IsInline, Is.True);
            Assert.That(field.Name, Is.EqualTo("Number Field"));
            Assert.That(field.Value, Is.EqualTo(42));
        }

        [Test]
        public void Properties_ShouldBeSettableWithComplexObjects()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var complexObject = new { Id = 1, Name = "Test", Values = new[] { 1, 2, 3 } };

            // Act
            field.IsInline = false;
            field.Name = "Complex Field";
            field.Value = complexObject;

            // Assert
            Assert.That(field.IsInline, Is.False);
            Assert.That(field.Name, Is.EqualTo("Complex Field"));
            Assert.That(field.Value, Is.EqualTo(complexObject));
        }

        [Test]
        public void Properties_ShouldBeSettableWithNullValues()
        {
            // Arrange
            var field = new DiscordNotificationField();

            // Act
            field.IsInline = false;
            field.Name = null;
            field.Value = null;

            // Assert
            Assert.That(field.IsInline, Is.False);
            Assert.That(field.Name, Is.Null);
            Assert.That(field.Value, Is.Null);
        }

        [Test]
        public void Properties_ShouldBeSettableWithEmptyStrings()
        {
            // Arrange
            var field = new DiscordNotificationField();

            // Act
            field.IsInline = true;
            field.Name = "";
            field.Value = "";

            // Assert
            Assert.That(field.IsInline, Is.True);
            Assert.That(field.Name, Is.EqualTo(""));
            Assert.That(field.Value, Is.EqualTo(""));
        }

        [Test]
        public void Properties_ShouldBeSettableWithWhitespaceStrings()
        {
            // Arrange
            var field = new DiscordNotificationField();

            // Act
            field.IsInline = false;
            field.Name = "   ";
            field.Value = "\t\n";

            // Assert
            Assert.That(field.IsInline, Is.False);
            Assert.That(field.Name, Is.EqualTo("   "));
            Assert.That(field.Value, Is.EqualTo("\t\n"));
        }

        [Test]
        public void Properties_ShouldBeSettableWithSpecialCharacters()
        {
            // Arrange
            var field = new DiscordNotificationField();

            // Act
            field.IsInline = true;
            field.Name = "Special chars: !@#$%^&*()";
            field.Value = "Unicode: 测试 🚀 ñáéíóú";

            // Assert
            Assert.That(field.IsInline, Is.True);
            Assert.That(field.Name, Is.EqualTo("Special chars: !@#$%^&*()"));
            Assert.That(field.Value, Is.EqualTo("Unicode: 测试 🚀 ñáéíóú"));
        }

        [Test]
        public void Properties_ShouldBeSettableWithVeryLongStrings()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var longString = new string('A', 10000);

            // Act
            field.IsInline = false;
            field.Name = longString;
            field.Value = longString;

            // Assert
            Assert.That(field.IsInline, Is.False);
            Assert.That(field.Name, Is.EqualTo(longString));
            Assert.That(field.Value, Is.EqualTo(longString));
        }

        [Test]
        public void Properties_ShouldBeSettableWithDifferentValueTypes()
        {
            // Arrange
            var field = new DiscordNotificationField();

            // Act & Assert
            field.Value = true;
            Assert.That(field.Value, Is.EqualTo(true));

            field.Value = 42;
            Assert.That(field.Value, Is.EqualTo(42));

            field.Value = 3.14;
            Assert.That(field.Value, Is.EqualTo(3.14));

            field.Value = DateTime.Now;
            Assert.That(field.Value, Is.InstanceOf<DateTime>());

            field.Value = new List<int> { 1, 2, 3 };
            Assert.That(field.Value, Is.InstanceOf<List<int>>());
        }

        [Test]
        public void Properties_ShouldBeSettableWithCollections()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var list = new List<string> { "item1", "item2", "item3" };
            var array = new int[] { 1, 2, 3, 4, 5 };
            var dictionary = new Dictionary<string, object> { { "key1", "value1" }, { "key2", 42 } };

            // Act & Assert
            field.Value = list;
            Assert.That(field.Value, Is.EqualTo(list));

            field.Value = array;
            Assert.That(field.Value, Is.EqualTo(array));

            field.Value = dictionary;
            Assert.That(field.Value, Is.EqualTo(dictionary));
        }

        [Test]
        public void Properties_ShouldBeSettableWithNestedObjects()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var nestedObject = new
            {
                Level1 = new
                {
                    Level2 = new
                    {
                        Level3 = "Deep Value"
                    }
                }
            };

            // Act
            field.IsInline = true;
            field.Name = "Nested Object";
            field.Value = nestedObject;

            // Assert
            Assert.That(field.IsInline, Is.True);
            Assert.That(field.Name, Is.EqualTo("Nested Object"));
            Assert.That(field.Value, Is.EqualTo(nestedObject));
        }

        [Test]
        public void Properties_ShouldBeSettableWithEnumValues()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var enumValue = System.DayOfWeek.Monday;

            // Act
            field.IsInline = false;
            field.Name = "Day of Week";
            field.Value = enumValue;

            // Assert
            Assert.That(field.IsInline, Is.False);
            Assert.That(field.Name, Is.EqualTo("Day of Week"));
            Assert.That(field.Value, Is.EqualTo(enumValue));
        }

        [Test]
        public void Properties_ShouldBeSettableWithNullableTypes()
        {
            // Arrange
            var field = new DiscordNotificationField();

            // Act & Assert
            field.Value = (int?)42;
            Assert.That(field.Value, Is.EqualTo(42));

            field.Value = (int?)null;
            Assert.That(field.Value, Is.Null);

            field.Value = (bool?)true;
            Assert.That(field.Value, Is.EqualTo(true));

            field.Value = (bool?)null;
            Assert.That(field.Value, Is.Null);
        }

        [Test]
        public void Properties_ShouldBeSettableWithGuidValues()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var guid = Guid.NewGuid();

            // Act
            field.IsInline = true;
            field.Name = "GUID Field";
            field.Value = guid;

            // Assert
            Assert.That(field.IsInline, Is.True);
            Assert.That(field.Name, Is.EqualTo("GUID Field"));
            Assert.That(field.Value, Is.EqualTo(guid));
        }

        [Test]
        public void Properties_ShouldBeSettableWithTimeSpanValues()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var timeSpan = TimeSpan.FromHours(2.5);

            // Act
            field.IsInline = false;
            field.Name = "Duration";
            field.Value = timeSpan;

            // Assert
            Assert.That(field.IsInline, Is.False);
            Assert.That(field.Name, Is.EqualTo("Duration"));
            Assert.That(field.Value, Is.EqualTo(timeSpan));
        }

        [Test]
        public void Properties_ShouldBeSettableWithDecimalValues()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var decimalValue = 123.456m;

            // Act
            field.IsInline = true;
            field.Name = "Price";
            field.Value = decimalValue;

            // Assert
            Assert.That(field.IsInline, Is.True);
            Assert.That(field.Name, Is.EqualTo("Price"));
            Assert.That(field.Value, Is.EqualTo(decimalValue));
        }

        [Test]
        public void Properties_ShouldBeSettableWithCharValues()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var charValue = 'A';

            // Act
            field.IsInline = false;
            field.Name = "Character";
            field.Value = charValue;

            // Assert
            Assert.That(field.IsInline, Is.False);
            Assert.That(field.Name, Is.EqualTo("Character"));
            Assert.That(field.Value, Is.EqualTo(charValue));
        }

        [Test]
        public void Properties_ShouldBeSettableWithByteArray()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var byteArray = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            field.IsInline = true;
            field.Name = "Binary Data";
            field.Value = byteArray;

            // Assert
            Assert.That(field.IsInline, Is.True);
            Assert.That(field.Name, Is.EqualTo("Binary Data"));
            Assert.That(field.Value, Is.EqualTo(byteArray));
        }

        [Test]
        public void Properties_ShouldBeSettableWithMultipleChanges()
        {
            // Arrange
            var field = new DiscordNotificationField();

            // Act & Assert
            field.IsInline = true;
            field.Name = "First Name";
            field.Value = "First Value";
            Assert.That(field.IsInline, Is.True);
            Assert.That(field.Name, Is.EqualTo("First Name"));
            Assert.That(field.Value, Is.EqualTo("First Value"));

            field.IsInline = false;
            field.Name = "Second Name";
            field.Value = 42;
            Assert.That(field.IsInline, Is.False);
            Assert.That(field.Name, Is.EqualTo("Second Name"));
            Assert.That(field.Value, Is.EqualTo(42));

            field.IsInline = true;
            field.Name = null;
            field.Value = null;
            Assert.That(field.IsInline, Is.True);
            Assert.That(field.Name, Is.Null);
            Assert.That(field.Value, Is.Null);
        }

        [Test]
        public void Properties_ShouldBeSettableWithConcurrentAccess()
        {
            // Arrange
            var field = new DiscordNotificationField();
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() =>
                {
                    field.IsInline = index % 2 == 0;
                    field.Name = $"Name{index}";
                    field.Value = index;
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.That(field, Is.Not.Null);
            Assert.That(field.Name, Is.Not.Null);
            Assert.That(field.Value, Is.Not.Null);
        }
    }
}