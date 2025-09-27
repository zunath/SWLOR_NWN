using SWLOR.Shared.Core.Data;

namespace SWLOR.Test.Shared.Core.Data
{
    [TestFixture]
    public class DatabaseTokenHelperTests
    {
        private DB _db;

        [SetUp]
        public void SetUp()
        {
            _db = new DB(null, null);
        }

        [Test]
        public void EscapeTokens_WithAtSymbol_ShouldEscapeAtSymbol()
        {
            // Arrange
            const string input = "test@example.com";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("test\\@example.com"));
        }

        [Test]
        public void EscapeTokens_WithExclamationMark_ShouldEscapeExclamationMark()
        {
            // Arrange
            const string input = "Hello! World!";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("Hello\\! World\\!"));
        }

        [Test]
        public void EscapeTokens_WithBraces_ShouldEscapeBraces()
        {
            // Arrange
            const string input = "test{value}test";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("test\\{value\\}test"));
        }

        [Test]
        public void EscapeTokens_WithParentheses_ShouldEscapeParentheses()
        {
            // Arrange
            const string input = "test(value)test";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("test\\(value\\)test"));
        }

        [Test]
        public void EscapeTokens_WithPipe_ShouldEscapePipe()
        {
            // Arrange
            const string input = "test|value|test";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("test\\|value\\|test"));
        }

        [Test]
        public void EscapeTokens_WithHyphen_ShouldEscapeHyphen()
        {
            // Arrange
            const string input = "test-value-test";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("test\\-value\\-test"));
        }

        [Test]
        public void EscapeTokens_WithEquals_ShouldEscapeEquals()
        {
            // Arrange
            const string input = "test=value=test";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("test\\=value\\=test"));
        }

        [Test]
        public void EscapeTokens_WithGreaterThan_ShouldEscapeGreaterThan()
        {
            // Arrange
            const string input = "test>value>test";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("test\\>value\\>test"));
        }

        [Test]
        public void EscapeTokens_WithSingleQuote_ShouldEscapeSingleQuote()
        {
            // Arrange
            const string input = "test'value'test";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("test\\'value\\'test"));
        }

        [Test]
        public void EscapeTokens_WithDoubleQuote_ShouldEscapeDoubleQuote()
        {
            // Arrange
            const string input = "test\"value\"test";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("test\\\"value\\\"test"));
        }

        [Test]
        public void EscapeTokens_WithAllSpecialCharacters_ShouldEscapeAllCharacters()
        {
            // Arrange
            const string input = "@!{}()|-='\"";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("\\@\\!\\{\\}\\(\\)\\|\\-\\=\\'\\\""));
        }

        [Test]
        public void EscapeTokens_WithNoSpecialCharacters_ShouldReturnOriginalString()
        {
            // Arrange
            const string input = "normal text without special characters";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo(input));
        }

        [Test]
        public void EscapeTokens_WithEmptyString_ShouldReturnEmptyString()
        {
            // Arrange
            const string input = "";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void EscapeTokens_WithNullString_ShouldReturnNull()
        {
            // Arrange
            string input = null;

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void EscapeTokens_WithMixedContent_ShouldEscapeOnlySpecialCharacters()
        {
            // Arrange
            const string input = "Hello @world! This is a {test} with (parentheses) and |pipes|";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("Hello \\@world\\! This is a \\{test\\} with \\(parentheses\\) and \\|pipes\\|"));
        }

        [Test]
        public void EscapeTokens_WithRepeatedSpecialCharacters_ShouldEscapeAllInstances()
        {
            // Arrange
            const string input = "@@@!!!{{{}}}";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo(@"\@\@\@\!\!\!\{\{\{\}\}\}"));
        }

        [Test]
        public void EscapeTokens_WithUnicodeCharacters_ShouldEscapeSpecialCharactersAndPreserveUnicode()
        {
            // Arrange
            const string input = "Hello 世界 @test!";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("Hello 世界 \\@test\\!"));
        }

        [Test]
        public void EscapeTokens_WithWhitespace_ShouldPreserveWhitespace()
        {
            // Arrange
            const string input = "  test  @  value  ";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("  test  \\@  value  "));
        }

        [Test]
        public void EscapeTokens_WithNumbers_ShouldPreserveNumbers()
        {
            // Arrange
            const string input = "123@456!789";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo("123\\@456\\!789"));
        }

        [Test]
        public void EscapeTokens_WithLetters_ShouldPreserveLetters()
        {
            // Arrange
            const string input = "abcdefghijklmnopqrstuvwxyz";

            // Act
            var result = _db.EscapeTokens(input);

            // Assert
            Assert.That(result, Is.EqualTo(input));
        }
    }
}
