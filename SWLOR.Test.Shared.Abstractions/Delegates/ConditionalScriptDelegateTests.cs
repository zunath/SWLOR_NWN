using SWLOR.Shared.Abstractions.Delegates;

namespace SWLOR.Test.Shared.Abstractions.Delegates
{
    [TestFixture]
    public class ConditionalScriptDelegateTests
    {
        [Test]
        public void Delegate_ShouldBeCallable()
        {
            // Arrange
            ConditionalScriptDelegate delegate1 = () => true;
            ConditionalScriptDelegate delegate2 = () => false;

            // Act
            var result1 = delegate1();
            var result2 = delegate2();

            // Assert
            Assert.That(result1, Is.True);
            Assert.That(result2, Is.False);
        }

        [Test]
        public void Delegate_ShouldReturnBoolean()
        {
            // Arrange
            ConditionalScriptDelegate delegate1 = () => true;
            ConditionalScriptDelegate delegate2 = () => false;

            // Act
            var result1 = delegate1();
            var result2 = delegate2();

            // Assert
            Assert.That(result1, Is.InstanceOf<bool>());
            Assert.That(result2, Is.InstanceOf<bool>());
        }

        [Test]
        public void Delegate_ShouldSupportComplexLogic()
        {
            // Arrange
            var counter = 0;
            ConditionalScriptDelegate delegate1 = () => counter++ > 0;
            ConditionalScriptDelegate delegate2 = () => counter % 2 == 0;

            // Act
            var result1 = delegate1(); // counter becomes 1, returns false (0 > 0)
            var result2 = delegate2(); // counter is 1, returns false (1 % 2 == 0)
            var result3 = delegate1(); // counter becomes 2, returns true (1 > 0)

            // Assert
            Assert.That(result1, Is.False);
            Assert.That(result2, Is.False);
            Assert.That(result3, Is.True);
        }

        [Test]
        public void Delegate_ShouldSupportMethodReferences()
        {
            // Arrange
            ConditionalScriptDelegate delegate1 = TestMethod1;
            ConditionalScriptDelegate delegate2 = TestMethod2;

            // Act
            var result1 = delegate1();
            var result2 = delegate2();

            // Assert
            Assert.That(result1, Is.True);
            Assert.That(result2, Is.False);
        }

        [Test]
        public void Delegate_ShouldSupportLambdaExpressions()
        {
            // Arrange
            var value = 42;
            ConditionalScriptDelegate delegate1 = () => value > 40;
            ConditionalScriptDelegate delegate2 = () => value < 30;

            // Act
            var result1 = delegate1();
            var result2 = delegate2();

            // Assert
            Assert.That(result1, Is.True);
            Assert.That(result2, Is.False);
        }

        [Test]
        public void Delegate_ShouldSupportNullChecks()
        {
            // Arrange
            string testString = null;
            ConditionalScriptDelegate delegate1 = () => testString != null;
            ConditionalScriptDelegate delegate2 = () => testString == null;

            // Act
            var result1 = delegate1();
            var result2 = delegate2();

            // Assert
            Assert.That(result1, Is.False);
            Assert.That(result2, Is.True);
        }

        [Test]
        public void Delegate_ShouldSupportExceptionHandling()
        {
            // Arrange
            ConditionalScriptDelegate delegate1 = () => 
            {
                try
                {
                    throw new InvalidOperationException("Test exception");
                }
                catch
                {
                    return false;
                }
            };

            // Act
            var result = delegate1();

            // Assert
            Assert.That(result, Is.False);
        }

        // Helper methods for testing
        private static bool TestMethod1() => true;
        private static bool TestMethod2() => false;
    }
}
