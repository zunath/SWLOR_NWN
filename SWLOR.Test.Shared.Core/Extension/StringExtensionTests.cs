using SWLOR.Shared.Core.Extension;

namespace SWLOR.Test.Shared.Core.Extension
{
    [TestFixture]
    public class StringExtensionTests
    {
        [Test]
        public void Truncate_WithNullString_ShouldReturnNull()
        {
            // Arrange
            string value = null;
            const int maxLength = 10;

            // Act
            var result = value.Truncate(maxLength);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Truncate_WithEmptyString_ShouldReturnEmptyString()
        {
            // Arrange
            const string value = "";
            const int maxLength = 10;

            // Act
            var result = value.Truncate(maxLength);

            // Assert
            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void Truncate_WithStringShorterThanMaxLength_ShouldReturnOriginalString()
        {
            // Arrange
            const string value = "hello";
            const int maxLength = 10;

            // Act
            var result = value.Truncate(maxLength);

            // Assert
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void Truncate_WithStringEqualToMaxLength_ShouldReturnOriginalString()
        {
            // Arrange
            const string value = "hello";
            const int maxLength = 5;

            // Act
            var result = value.Truncate(maxLength);

            // Assert
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void Truncate_WithStringLongerThanMaxLength_ShouldReturnTruncatedString()
        {
            // Arrange
            const string value = "hello world";
            const int maxLength = 5;

            // Act
            var result = value.Truncate(maxLength);

            // Assert
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void Truncate_WithMaxLengthZero_ShouldReturnEmptyString()
        {
            // Arrange
            const string value = "hello world";
            const int maxLength = 0;

            // Act
            var result = value.Truncate(maxLength);

            // Assert
            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void Truncate_WithMaxLengthOne_ShouldReturnFirstCharacter()
        {
            // Arrange
            const string value = "hello world";
            const int maxLength = 1;

            // Act
            var result = value.Truncate(maxLength);

            // Assert
            Assert.That(result, Is.EqualTo("h"));
        }

        [Test]
        public void Truncate_WithVeryLongString_ShouldReturnTruncatedString()
        {
            // Arrange
            const string value = "This is a very long string that should be truncated when we apply a small max length";
            const int maxLength = 20;

            // Act
            var result = value.Truncate(maxLength);

            // Assert
            Assert.That(result, Is.EqualTo("This is a very long "));
            Assert.That(result.Length, Is.EqualTo(maxLength));
        }

        [Test]
        public void Truncate_WithUnicodeString_ShouldHandleUnicodeCorrectly()
        {
            // Arrange
            const string value = "Hello 世界 🌍";
            const int maxLength = 8;

            // Act
            var result = value.Truncate(maxLength);

            // Assert
            Assert.That(result, Is.EqualTo("Hello 世界"));
        }

        [Test]
        public void Truncate_WithWhitespaceString_ShouldReturnTruncatedWhitespace()
        {
            // Arrange
            const string value = "   ";
            const int maxLength = 2;

            // Act
            var result = value.Truncate(maxLength);

            // Assert
            Assert.That(result, Is.EqualTo("  "));
        }
    }
}
