using NUnit.Framework;
using SWLOR.Shared.Core.Async;
using System;
using System.Threading;

namespace SWLOR.Test.Shared.Core.Async
{
    [TestFixture]
    public class MainThreadSynchronizationContextTests
    {
        private MainThreadSynchronizationContext _context;

        [SetUp]
        public void SetUp()
        {
            _context = new MainThreadSynchronizationContext();
        }

        [TearDown]
        public void TearDown()
        {
            _context = null!;
        }

        [Test]
        public void MainThreadSynchronizationContext_GetAwaiter_ShouldReturnSynchronizationContextAwaiter()
        {
            // Act
            var awaiter = _context.GetAwaiter();

            // Assert
            Assert.That(awaiter, Is.Not.Null);
            Assert.That(awaiter, Is.InstanceOf<SWLOR.Shared.Core.Async.Awaiters.SynchronizationContextAwaiter>());
        }

        [Test]
        public void MainThreadSynchronizationContext_Post_ShouldQueueCallback()
        {
            // Arrange
            var callbackExecuted = false;
            SendOrPostCallback callback = _ => callbackExecuted = true;
            var state = new object();

            // Act
            _context.Post(callback, state);

            // Assert
            Assert.That(callbackExecuted, Is.False); // Not executed yet
        }

        [Test]
        public void MainThreadSynchronizationContext_Send_ShouldQueueCallback()
        {
            // Arrange
            var callbackExecuted = false;
            SendOrPostCallback callback = _ => callbackExecuted = true;
            var state = new object();

            // Act
            _context.Send(callback, state);

            // Assert
            Assert.That(callbackExecuted, Is.False); // Not executed yet
        }

        [Test]
        public void MainThreadSynchronizationContext_Update_ShouldExecuteQueuedCallbacks()
        {
            // Arrange
            var callback1Executed = false;
            var callback2Executed = false;
            SendOrPostCallback callback1 = _ => callback1Executed = true;
            SendOrPostCallback callback2 = _ => callback2Executed = true;

            _context.Post(callback1, null);
            _context.Post(callback2, null);

            // Act
            _context.Update();

            // Assert
            Assert.That(callback1Executed, Is.True);
            Assert.That(callback2Executed, Is.True);
        }

        [Test]
        public void MainThreadSynchronizationContext_Update_WithException_ShouldNotCrash()
        {
            // Arrange
            var callbackExecuted = false;
            SendOrPostCallback callback1 = _ => throw new Exception("Test exception");
            SendOrPostCallback callback2 = _ => callbackExecuted = true;

            _context.Post(callback1, null);
            _context.Post(callback2, null);

            // Act & Assert
            Assert.DoesNotThrow(() => _context.Update());
            Assert.That(callbackExecuted, Is.True); // Second callback should still execute
        }

        [Test]
        public void MainThreadSynchronizationContext_Update_ShouldClearCurrentWorkAfterExecution()
        {
            // Arrange
            var callbackExecuted = false;
            SendOrPostCallback callback = _ => callbackExecuted = true;

            _context.Post(callback, null);
            _context.Update(); // First execution

            callbackExecuted = false;
            _context.Post(callback, null);

            // Act
            _context.Update(); // Second execution

            // Assert
            Assert.That(callbackExecuted, Is.True);
        }

        [Test]
        public void MainThreadSynchronizationContext_Update_WithNoQueuedTasks_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _context.Update());
        }

        [Test]
        public void MainThreadSynchronizationContext_Update_WithNullCallback_ShouldNotThrow()
        {
            // Arrange
            _context.Post(null, null);

            // Act & Assert
            Assert.DoesNotThrow(() => _context.Update());
        }
    }
}
