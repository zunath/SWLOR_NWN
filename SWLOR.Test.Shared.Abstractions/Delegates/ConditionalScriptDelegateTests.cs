using SWLOR.Shared.Abstractions.Delegates;

namespace SWLOR.Test.Shared.Abstractions.Delegates
{
    [TestFixture]
    public class ConditionalScriptDelegateTests
    {
        [Test]
        public void Delegate_ShouldBeDefinedCorrectly()
        {
            // Arrange & Act
            var delegateType = typeof(ConditionalScriptDelegate);

            // Assert
            Assert.That(delegateType, Is.Not.Null);
            Assert.That(delegateType.IsClass, Is.True);
            Assert.That(delegateType.BaseType, Is.EqualTo(typeof(MulticastDelegate)));
        }

        [Test]
        public void Delegate_ShouldHaveCorrectSignature()
        {
            // Arrange
            var delegateType = typeof(ConditionalScriptDelegate);
            var invokeMethod = delegateType.GetMethod("Invoke");

            // Assert
            Assert.That(invokeMethod, Is.Not.Null);
            Assert.That(invokeMethod.ReturnType, Is.EqualTo(typeof(bool)));
            Assert.That(invokeMethod.GetParameters().Length, Is.EqualTo(0));
        }

        [Test]
        public void Delegate_ShouldBeInvokableWithLambdaExpression()
        {
            // Arrange
            ConditionalScriptDelegate delegate1 = () => true;
            ConditionalScriptDelegate delegate2 = () => false;

            // Act
            var result1 = delegate1.Invoke();
            var result2 = delegate2.Invoke();

            // Assert
            Assert.That(result1, Is.True);
            Assert.That(result2, Is.False);
        }

        [Test]
        public void Delegate_ShouldBeInvokableWithMethodReference()
        {
            // Arrange
            ConditionalScriptDelegate delegate1 = AlwaysTrue;
            ConditionalScriptDelegate delegate2 = AlwaysFalse;

            // Act
            var result1 = delegate1.Invoke();
            var result2 = delegate2.Invoke();

            // Assert
            Assert.That(result1, Is.True);
            Assert.That(result2, Is.False);
        }

        [Test]
        public void Delegate_ShouldBeInvokableWithAnonymousMethod()
        {
            // Arrange
            bool condition = true;
            ConditionalScriptDelegate delegate1 = delegate() { return condition; };
            ConditionalScriptDelegate delegate2 = delegate() { return !condition; };

            // Act
            var result1 = delegate1.Invoke();
            var result2 = delegate2.Invoke();

            // Assert
            Assert.That(result1, Is.True);
            Assert.That(result2, Is.False);
        }

        [Test]
        public void Delegate_ShouldSupportClosure()
        {
            // Arrange
            int counter = 0;
            ConditionalScriptDelegate delegate1 = () => ++counter > 2;
            ConditionalScriptDelegate delegate2 = () => counter == 0;

            // Act
            var result1First = delegate1.Invoke();
            var result1Second = delegate1.Invoke();
            var result1Third = delegate1.Invoke();
            var result2 = delegate2.Invoke();

            // Assert
            Assert.That(result1First, Is.False);
            Assert.That(result1Second, Is.False);
            Assert.That(result1Third, Is.True);
            Assert.That(result2, Is.False);
        }

        [Test]
        public void Delegate_ShouldSupportMultipleDelegates()
        {
            // Arrange
            var delegates = new List<ConditionalScriptDelegate>
            {
                () => true,
                () => false,
                () => true,
                () => false
            };

            // Act
            var results = delegates.Select(d => d.Invoke()).ToArray();

            // Assert
            Assert.That(results[0], Is.True);
            Assert.That(results[1], Is.False);
            Assert.That(results[2], Is.True);
            Assert.That(results[3], Is.False);
        }

        [Test]
        public void Delegate_ShouldSupportCombinationWithLogicalOperators()
        {
            // Arrange
            ConditionalScriptDelegate delegate1 = () => true;
            ConditionalScriptDelegate delegate2 = () => false;

            // Act
            var andResult = delegate1.Invoke() && delegate2.Invoke();
            var orResult = delegate1.Invoke() || delegate2.Invoke();

            // Assert
            Assert.That(andResult, Is.False);
            Assert.That(orResult, Is.True);
        }

        [Test]
        public void Delegate_ShouldSupportNullCheck()
        {
            // Arrange
            ConditionalScriptDelegate delegate1 = null;
            ConditionalScriptDelegate delegate2 = () => true;

            // Act & Assert
            Assert.That(delegate1, Is.Null);
            Assert.That(delegate2, Is.Not.Null);
        }

        [Test]
        public void Delegate_ShouldSupportAssignment()
        {
            // Arrange
            ConditionalScriptDelegate delegate1 = () => true;
            ConditionalScriptDelegate delegate2 = delegate1;

            // Act
            var result1 = delegate1.Invoke();
            var result2 = delegate2.Invoke();

            // Assert
            Assert.That(result1, Is.True);
            Assert.That(result2, Is.True);
            Assert.That(delegate1, Is.EqualTo(delegate2));
        }

        [Test]
        public void Delegate_ShouldSupportCombination()
        {
            // Arrange
            ConditionalScriptDelegate delegate1 = () => true;
            ConditionalScriptDelegate delegate2 = () => false;
            ConditionalScriptDelegate combinedDelegate = () => delegate1.Invoke() && delegate2.Invoke();

            // Act
            var result = combinedDelegate.Invoke();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void Delegate_ShouldSupportConditionalLogic()
        {
            // Arrange
            int value = 5;
            ConditionalScriptDelegate isGreaterThanThree = () => value > 3;
            ConditionalScriptDelegate isLessThanTen = () => value < 10;
            ConditionalScriptDelegate isEven = () => value % 2 == 0;

            // Act
            var greaterThanThree = isGreaterThanThree.Invoke();
            var lessThanTen = isLessThanTen.Invoke();
            var even = isEven.Invoke();

            // Assert
            Assert.That(greaterThanThree, Is.True);
            Assert.That(lessThanTen, Is.True);
            Assert.That(even, Is.False);
        }

        [Test]
        public void Delegate_ShouldSupportExceptionHandling()
        {
            // Arrange
            ConditionalScriptDelegate throwingDelegate = () => throw new InvalidOperationException("Test exception");
            ConditionalScriptDelegate safeDelegate = () => 
            {
                try
                {
                    return throwingDelegate.Invoke();
                }
                catch
                {
                    return false;
                }
            };

            // Act
            var result = safeDelegate.Invoke();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void Delegate_ShouldSupportAsyncPattern()
        {
            // Arrange
            bool asyncResult = false;
            ConditionalScriptDelegate asyncDelegate = () => asyncResult;

            // Act
            var result = asyncDelegate.Invoke();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void Delegate_ShouldSupportComplexLogic()
        {
            // Arrange
            int[] numbers = { 1, 2, 3, 4, 5 };
            ConditionalScriptDelegate hasEvenNumbers = () => numbers.Any(n => n % 2 == 0);
            ConditionalScriptDelegate hasOddNumbers = () => numbers.Any(n => n % 2 == 1);
            ConditionalScriptDelegate allPositive = () => numbers.All(n => n > 0);

            // Act
            var evenResult = hasEvenNumbers.Invoke();
            var oddResult = hasOddNumbers.Invoke();
            var positiveResult = allPositive.Invoke();

            // Assert
            Assert.That(evenResult, Is.True);
            Assert.That(oddResult, Is.True);
            Assert.That(positiveResult, Is.True);
        }

        [Test]
        public void Delegate_ShouldSupportMethodGroupConversion()
        {
            // Arrange
            var testObject = new TestObject();
            ConditionalScriptDelegate delegate1 = testObject.IsValid;
            ConditionalScriptDelegate delegate2 = testObject.IsInvalid;

            // Act
            var result1 = delegate1.Invoke();
            var result2 = delegate2.Invoke();

            // Assert
            Assert.That(result1, Is.True);
            Assert.That(result2, Is.False);
        }

        [Test]
        public void Delegate_ShouldSupportStaticMethodReference()
        {
            // Arrange
            ConditionalScriptDelegate delegate1 = TestObject.StaticIsValid;
            ConditionalScriptDelegate delegate2 = TestObject.StaticIsInvalid;

            // Act
            var result1 = delegate1.Invoke();
            var result2 = delegate2.Invoke();

            // Assert
            Assert.That(result1, Is.True);
            Assert.That(result2, Is.False);
        }

        [Test]
        public void Delegate_ShouldSupportPropertyAccess()
        {
            // Arrange
            var testObject = new TestObject();
            ConditionalScriptDelegate delegate1 = () => testObject.IsValidProperty;
            ConditionalScriptDelegate delegate2 = () => testObject.IsInvalidProperty;

            // Act
            var result1 = delegate1.Invoke();
            var result2 = delegate2.Invoke();

            // Assert
            Assert.That(result1, Is.True);
            Assert.That(result2, Is.False);
        }

        // Helper methods
        private static bool AlwaysTrue() => true;
        private static bool AlwaysFalse() => false;

        // Helper class
        private class TestObject
        {
            public bool IsValid() => true;
            public bool IsInvalid() => false;
            public bool IsValidProperty => true;
            public bool IsInvalidProperty => false;
            
            public static bool StaticIsValid() => true;
            public static bool StaticIsInvalid() => false;
        }
    }
}