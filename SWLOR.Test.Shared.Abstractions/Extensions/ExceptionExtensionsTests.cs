using SWLOR.Shared.Abstractions.Extensions;

namespace SWLOR.Test.Shared.Abstractions.Extensions
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
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: System.InvalidOperationException"));
            Assert.That(result, Does.Contain("Message       : Test exception message"));
            Assert.That(result, Does.Contain("Stacktrace:"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithNullMessage_ShouldHandleNullMessage()
        {
            // Arrange
            var exception = new TestExceptionWithNullMessage();

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: SWLOR.Test.Shared.Abstractions.Extensions.ExceptionExtensionsTests+TestExceptionWithNullMessage"));
            Assert.That(result, Does.Contain("Message       : "));
            Assert.That(result, Does.Contain("Stacktrace:"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithInnerException_ShouldIncludeInnerException()
        {
            // Arrange
            var innerException = new ArgumentException("Inner exception message");
            var outerException = new InvalidOperationException("Outer exception message", innerException);

            // Act
            var result = outerException.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: System.InvalidOperationException"));
            Assert.That(result, Does.Contain("Message       : Outer exception message"));
            Assert.That(result, Does.Contain("Exception type: System.ArgumentException"));
            Assert.That(result, Does.Contain("Message       : Inner exception message"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithMultipleInnerExceptions_ShouldIncludeAllExceptions()
        {
            // Arrange
            var innermostException = new NotSupportedException("Innermost exception message");
            var middleException = new ArgumentException("Middle exception message", innermostException);
            var outerException = new InvalidOperationException("Outer exception message", middleException);

            // Act
            var result = outerException.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: System.InvalidOperationException"));
            Assert.That(result, Does.Contain("Message       : Outer exception message"));
            Assert.That(result, Does.Contain("Exception type: System.ArgumentException"));
            Assert.That(result, Does.Contain("Message       : Middle exception message"));
            Assert.That(result, Does.Contain("Exception type: System.NotSupportedException"));
            Assert.That(result, Does.Contain("Message       : Innermost exception message"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithNullInnerException_ShouldHandleNullInnerException()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception message", null);

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: System.InvalidOperationException"));
            Assert.That(result, Does.Contain("Message       : Test exception message"));
            Assert.That(result, Does.Not.Contain("Exception type: System.ArgumentException"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithEmptyMessage_ShouldHandleEmptyMessage()
        {
            // Arrange
            var exception = new InvalidOperationException("");

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: System.InvalidOperationException"));
            Assert.That(result, Does.Contain("Message       : "));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithWhitespaceMessage_ShouldHandleWhitespaceMessage()
        {
            // Arrange
            var exception = new InvalidOperationException("   ");

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: System.InvalidOperationException"));
            Assert.That(result, Does.Contain("Message       :    "));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithNullStackTrace_ShouldHandleNullStackTrace()
        {
            // Arrange
            var exception = new TestExceptionWithNullStackTrace("Test message");

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: SWLOR.Test.Shared.Abstractions.Extensions.ExceptionExtensionsTests+TestExceptionWithNullStackTrace"));
            Assert.That(result, Does.Contain("Message       : Test message"));
            Assert.That(result, Does.Contain("Stacktrace:"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithCustomException_ShouldIncludeCustomExceptionType()
        {
            // Arrange
            var exception = new CustomTestException("Custom exception message");

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: SWLOR.Test.Shared.Abstractions.Extensions.ExceptionExtensionsTests+CustomTestException"));
            Assert.That(result, Does.Contain("Message       : Custom exception message"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithVeryLongMessage_ShouldHandleLongMessage()
        {
            // Arrange
            var longMessage = new string('A', 10000);
            var exception = new InvalidOperationException(longMessage);

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: System.InvalidOperationException"));
            Assert.That(result, Does.Contain($"Message       : {longMessage}"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithSpecialCharactersInMessage_ShouldHandleSpecialCharacters()
        {
            // Arrange
            var specialMessage = "Test message with special chars: \n\r\t\"'\\";
            var exception = new InvalidOperationException(specialMessage);

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: System.InvalidOperationException"));
            Assert.That(result, Does.Contain($"Message       : {specialMessage}"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithUnicodeCharactersInMessage_ShouldHandleUnicodeCharacters()
        {
            // Arrange
            var unicodeMessage = "Test message with unicode: 测试 🚀 ñáéíóú";
            var exception = new InvalidOperationException(unicodeMessage);

            // Act
            var result = exception.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: System.InvalidOperationException"));
            Assert.That(result, Does.Contain($"Message       : {unicodeMessage}"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_ShouldIncludeSeparatorLines()
        {
            // Arrange
            var innerException = new ArgumentException("Inner exception");
            var outerException = new InvalidOperationException("Outer exception", innerException);

            // Act
            var result = outerException.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            
            // Should contain separator lines between exceptions
            var lines = result.Split('\n');
            var separatorLines = lines.Where(line => string.IsNullOrWhiteSpace(line)).ToArray();
            Assert.That(separatorLines.Length, Is.GreaterThan(0));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_ShouldFormatConsistently()
        {
            // Arrange
            var exception1 = new InvalidOperationException("Test message 1");
            var exception2 = new InvalidOperationException("Test message 2");

            // Act
            var result1 = exception1.ToMessageAndCompleteStacktrace();
            var result2 = exception2.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result1, Is.Not.Null);
            Assert.That(result2, Is.Not.Null);
            
            // Both should have the same format structure
            Assert.That(result1, Does.Contain("Exception type:"));
            Assert.That(result1, Does.Contain("Message       :"));
            Assert.That(result1, Does.Contain("Stacktrace:"));
            
            Assert.That(result2, Does.Contain("Exception type:"));
            Assert.That(result2, Does.Contain("Message       :"));
            Assert.That(result2, Does.Contain("Stacktrace:"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithAggregateException_ShouldHandleAggregateException()
        {
            // Arrange
            var innerException1 = new ArgumentException("Inner exception 1");
            var innerException2 = new InvalidOperationException("Inner exception 2");
            var aggregateException = new AggregateException("Aggregate exception message", innerException1, innerException2);

            // Act
            var result = aggregateException.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: System.AggregateException"));
            Assert.That(result, Does.Contain("Message       : Aggregate exception message"));
        }

        [Test]
        public void ToMessageAndCompleteStacktrace_WithCircularReference_ShouldHandleCircularReference()
        {
            // Arrange
            var exception1 = new TestExceptionWithCircularReference("Exception 1");
            var exception2 = new TestExceptionWithCircularReference("Exception 2");
            
            // Create circular reference
            exception1.InnerException = exception2;
            exception2.InnerException = exception1;

            // Act
            var result = exception1.ToMessageAndCompleteStacktrace();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("Exception type: SWLOR.Test.Shared.Abstractions.Extensions.ExceptionExtensionsTests+TestExceptionWithCircularReference"));
            Assert.That(result, Does.Contain("Message       : Exception 1"));
        }

        // Test helper classes
        private class TestExceptionWithNullMessage : Exception
        {
            public override string Message => null;
        }

        private class TestExceptionWithNullStackTrace : Exception
        {
            public TestExceptionWithNullStackTrace(string message) : base(message) { }
            
            public override string StackTrace => null;
        }

        private class CustomTestException : Exception
        {
            public CustomTestException(string message) : base(message) { }
        }

        private class TestExceptionWithCircularReference : Exception
        {
            public TestExceptionWithCircularReference(string message) : base(message) { }
            
            public new Exception InnerException { get; set; }
        }
    }
}
