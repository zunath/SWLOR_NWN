using SWLOR.Shared.Abstractions.Extensions;

namespace SWLOR.Test.Shared.Core.Extension
{
    [TestFixture]
    public class ExceptionExtensionsTests
    {
        [Test]
        public void ToMessageAndCompleteStacktrace_WithSimpleException_ShouldReturnFormattedString()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception message");

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("Exception type: System.InvalidOperationException"));
            Assert.That(result, Does.Contain("Message       : Test exception message"));
            Assert.That(result, Does.Contain("Stacktrace:"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithInnerException_ShouldReturnBothExceptions()
        {
            // Arrange
            var innerException = new ArgumentException("Inner exception message");
            var outerException = new InvalidOperationException("Outer exception message", innerException);

            // Act
            var result = outerException.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("Exception type: System.InvalidOperationException"));
            Assert.That(result, Does.Contain("Message       : Outer exception message"));
            Assert.That(result, Does.Contain("Exception type: System.ArgumentException"));
            Assert.That(result, Does.Contain("Message       : Inner exception message"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithNullMessage_ShouldHandleNullMessage()
        {
            // Arrange
            var exception = new Exception(null);

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("Exception type: System.Exception"));
            Assert.That(result, Does.Contain("Message       : "));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithMultipleInnerExceptions_ShouldReturnAllExceptions()
        {
            // Arrange
            var innermostException = new ArgumentNullException("Innermost exception");
            var middleException = new InvalidOperationException("Middle exception", innermostException);
            var outerException = new ApplicationException("Outer exception", middleException);

            // Act
            var result = outerException.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("Exception type: System.ApplicationException"));
            Assert.That(result, Does.Contain("Message       : Outer exception"));
            Assert.That(result, Does.Contain("Exception type: System.InvalidOperationException"));
            Assert.That(result, Does.Contain("Message       : Middle exception"));
            Assert.That(result, Does.Contain("Exception type: System.ArgumentNullException"));
            Assert.That(result, Does.Contain("Value cannot be null. (Parameter 'Innermost exception')"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithCustomException_ShouldReturnCustomExceptionType()
        {
            // Arrange
            var customException = new CustomTestException("Custom exception message");

            // Act
            var result = customException.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("Exception type: SWLOR.Test.Shared.Core.Extension.ExceptionExtensionsTests+CustomTestException"));
            Assert.That(result, Does.Contain("Message       : Custom exception message"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithEmptyMessage_ShouldReturnEmptyMessage()
        {
            // Arrange
            var exception = new Exception("");

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("Exception type: System.Exception"));
            Assert.That(result, Does.Contain("Message       : "));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithNullStackTrace_ShouldHandleNullStackTrace()
        {
            // Arrange
            var exception = new Exception("Test message");

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("Stacktrace:"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithVeryLongMessage_ShouldReturnFullMessage()
        {
            // Arrange
            var longMessage = new string('A', 1000);
            var exception = new Exception(longMessage);

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain(longMessage));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithSpecialCharacters_ShouldHandleSpecialCharacters()
        {
            // Arrange
            var specialMessage = "Exception with special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?`~";
            var exception = new Exception(specialMessage);

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain(specialMessage));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithUnicodeCharacters_ShouldHandleUnicode()
        {
            // Arrange
            var unicodeMessage = "Exception with unicode: 世界 🌍 🚀";
            var exception = new Exception(unicodeMessage);

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain(unicodeMessage));
        }

        private class CustomTestException : Exception
        {
            public CustomTestException(string message) : base(message)
            {
            }
        }
    }
}
