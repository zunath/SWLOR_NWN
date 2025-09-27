using SWLOR.Shared.Core.Async.Awaiters;

namespace SWLOR.Test.Shared.Core.Async.Awaiters
{
    [TestFixture]
    public class IAwaiterTests
    {
        [Test]
        public void IAwaiter_IsCompleted_ShouldReturnBoolean()
        {
            // Arrange
            var awaiter = new TestAwaiter(true);

            // Act
            var isCompleted = awaiter.IsCompleted;

            // Assert
            Assert.That(isCompleted, Is.True);
        }

        [Test]
        public void IAwaiter_GetResult_ShouldNotThrow()
        {
            // Arrange
            var awaiter = new TestAwaiter(true);

            // Act & Assert
            Assert.DoesNotThrow(() => awaiter.GetResult());
        }

        [Test]
        public void IAwaiter_OnCompleted_ShouldInvokeContinuation()
        {
            // Arrange
            var awaiter = new TestAwaiter(false);
            var continuationExecuted = false;
            Action continuation = () => continuationExecuted = true;

            // Act
            awaiter.OnCompleted(continuation);

            // Assert
            Assert.That(continuationExecuted, Is.True);
        }

        [Test]
        public void IAwaiter_OnCompleted_WithNullContinuation_ShouldNotThrow()
        {
            // Arrange
            var awaiter = new TestAwaiter(false);

            // Act & Assert
            Assert.DoesNotThrow(() => awaiter.OnCompleted(null!));
        }

        private class TestAwaiter : IAwaiter
        {
            private readonly bool _isCompleted;

            public TestAwaiter(bool isCompleted)
            {
                _isCompleted = isCompleted;
            }

            public bool IsCompleted => _isCompleted;

            public void GetResult()
            {
                // Test implementation
            }

            public void OnCompleted(Action continuation)
            {
                continuation?.Invoke();
            }
        }
    }
}
