using SWLOR.Shared.Core.Async.Awaiters;

namespace SWLOR.Test.Shared.Core.Async.Awaiters
{
    [TestFixture]
    public class IAwaitableTests
    {
        [Test]
        public void IAwaitable_GetAwaiter_ShouldReturnIAwaiter()
        {
            // Arrange
            var awaitable = new TestAwaitable();

            // Act
            var awaiter = awaitable.GetAwaiter();

            // Assert
            Assert.That(awaiter, Is.Not.Null);
            Assert.That(awaiter, Is.InstanceOf<IAwaiter>());
        }

        private class TestAwaitable : IAwaitable
        {
            public IAwaiter GetAwaiter()
            {
                return new TestAwaiter();
            }
        }

        private class TestAwaiter : IAwaiter
        {
            public bool IsCompleted => true;

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
