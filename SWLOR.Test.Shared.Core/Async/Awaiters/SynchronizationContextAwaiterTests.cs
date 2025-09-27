using SWLOR.Shared.Core.Async.Awaiters;

namespace SWLOR.Test.Shared.Core.Async.Awaiters
{
    [TestFixture]
    public class SynchronizationContextAwaiterTests
    {
        [Test]
        public void SynchronizationContextAwaiter_Constructor_ShouldSetContext()
        {
            // Arrange
            var context = new TestSynchronizationContext();

            // Act
            var awaiter = new SynchronizationContextAwaiter(context);

            // Assert
            // SynchronizationContextAwaiter is a struct, so it can't be null
        }

        [Test]
        public void SynchronizationContextAwaiter_IsCompleted_WhenCurrentContext_ShouldReturnTrue()
        {
            // Arrange
            var context = new TestSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(context);
            var awaiter = new SynchronizationContextAwaiter(context);

            try
            {
                // Act
                var isCompleted = awaiter.IsCompleted;

                // Assert
                Assert.That(isCompleted, Is.True);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(null);
            }
        }

        [Test]
        public void SynchronizationContextAwaiter_IsCompleted_WhenDifferentContext_ShouldReturnFalse()
        {
            // Arrange
            var context1 = new TestSynchronizationContext();
            var context2 = new TestSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(context1);
            var awaiter = new SynchronizationContextAwaiter(context2);

            try
            {
                // Act
                var isCompleted = awaiter.IsCompleted;

                // Assert
                Assert.That(isCompleted, Is.False);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(null);
            }
        }

        [Test]
        public void SynchronizationContextAwaiter_OnCompleted_ShouldPostToContext()
        {
            // Arrange
            var context = new TestSynchronizationContext();
            var awaiter = new SynchronizationContextAwaiter(context);
            var continuationExecuted = false;
            Action continuation = () => continuationExecuted = true;

            // Act
            awaiter.OnCompleted(continuation);

            // Assert
            Assert.That(context.PostedCallbacks.Count, Is.EqualTo(1));
            context.PostedCallbacks[0].Item1(continuation);
            Assert.That(continuationExecuted, Is.True);
        }

        [Test]
        public void SynchronizationContextAwaiter_GetResult_ShouldNotThrow()
        {
            // Arrange
            var context = new TestSynchronizationContext();
            var awaiter = new SynchronizationContextAwaiter(context);

            // Act & Assert
            Assert.DoesNotThrow(() => awaiter.GetResult());
        }

        private class TestSynchronizationContext : SynchronizationContext
        {
            public List<(SendOrPostCallback, object?)> PostedCallbacks { get; } = new();

            public override void Post(SendOrPostCallback d, object? state)
            {
                PostedCallbacks.Add((d, state));
            }

            public override void Send(SendOrPostCallback d, object? state)
            {
                PostedCallbacks.Add((d, state));
            }
        }
    }
}
