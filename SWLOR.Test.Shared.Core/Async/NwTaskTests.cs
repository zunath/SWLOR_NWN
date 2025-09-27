using SWLOR.Shared.Core.Async;
using SWLOR.Test.Shared.Core.Async.TestHelpers;

namespace SWLOR.Test.Shared.Core.Async
{
    [TestFixture]
    public class NwTaskTests
    {
        [Test]
        public void MainThreadSynchronizationContext_ShouldNotBeNull()
        {
            // Act
            var context = NwTask.MainThreadSynchronizationContext;

            // Assert
            Assert.That(context, Is.Not.Null);
        }

        [Test]
        public void SwitchToMainThread_ShouldReturnAwaitable()
        {
            // Act
            var awaitable = NwTask.SwitchToMainThread();

            // Assert
            Assert.That(awaitable, Is.Not.Null);
        }

        [Test]
        public async Task Delay_WithValidTimeSpan_ShouldComplete()
        {
            // Arrange
            var delay = TimeSpan.FromMilliseconds(10);

            // Act & Assert
            var startTime = DateTime.Now;
            await TestNwTask.Delay(delay);
            var elapsed = DateTime.Now - startTime;
            
            Assert.That(elapsed.TotalMilliseconds, Is.GreaterThanOrEqualTo(9));
        }

        [Test]
        public async Task Delay_WithZeroTimeSpan_ShouldCompleteImmediately()
        {
            // Arrange
            var delay = TimeSpan.Zero;

            // Act & Assert
            var startTime = DateTime.Now;
            await TestNwTask.Delay(delay);
            var elapsed = DateTime.Now - startTime;
            
            Assert.That(elapsed.TotalMilliseconds, Is.LessThan(100));
        }

        [Test]
        public async Task Delay_WithCancellationToken_ShouldComplete()
        {
            // Arrange
            var delay = TimeSpan.FromMilliseconds(10);
            var cancellationToken = new CancellationToken();

            // Act & Assert
            var startTime = DateTime.Now;
            await TestNwTask.Delay(delay, cancellationToken);
            var elapsed = DateTime.Now - startTime;
            
            Assert.That(elapsed.TotalMilliseconds, Is.GreaterThanOrEqualTo(9));
        }

        [Test]
        public async Task NextFrame_ShouldComplete()
        {
            // Act & Assert
            await TestNwTask.NextFrame();
            // If we get here without throwing an exception, the test passes
        }

        [Test]
        public async Task DelayFrame_WithZeroFrames_ShouldCompleteImmediately()
        {
            // Arrange
            const int frames = 0;

            // Act & Assert
            var startTime = DateTime.Now;
            await TestNwTask.DelayFrame(frames);
            var elapsed = DateTime.Now - startTime;
            
            Assert.That(elapsed.TotalMilliseconds, Is.LessThan(100));
        }

        [Test]
        public async Task DelayFrame_WithPositiveFrames_ShouldComplete()
        {
            // Arrange
            const int frames = 1;

            // Act & Assert
            await TestNwTask.DelayFrame(frames);
            // If we get here without throwing an exception, the test passes
        }

        [Test]
        public async Task DelayFrame_WithCancellationToken_ShouldComplete()
        {
            // Arrange
            const int frames = 1;
            var cancellationToken = new CancellationToken();

            // Act & Assert
            await TestNwTask.DelayFrame(frames, cancellationToken);
            // If we get here without throwing an exception, the test passes
        }

        [Test]
        public async Task Run_WithValidFunction_ShouldComplete()
        {
            // Arrange
            var functionExecuted = false;
            Func<Task> function = async () => 
            {
                await Task.Delay(1);
                functionExecuted = true;
            };

            // Act
            await TestNwTask.Run(function);

            // Assert
            Assert.That(functionExecuted, Is.True);
        }

        [Test]
        public async Task Run_WithValidFunctionReturningValue_ShouldReturnValue()
        {
            // Arrange
            const string expectedValue = "test value";
            Func<Task<string>> function = async () => 
            {
                await Task.Delay(1);
                return expectedValue;
            };

            // Act
            var result = await TestNwTask.Run(function);

            // Assert
            Assert.That(result, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task WaitUntil_WithTrueCondition_ShouldCompleteImmediately()
        {
            // Arrange
            var condition = true;

            // Act & Assert
            var startTime = DateTime.Now;
            await TestNwTask.WaitUntil(() => condition);
            var elapsed = DateTime.Now - startTime;
            
            Assert.That(elapsed.TotalMilliseconds, Is.LessThan(100));
        }

        [Test]
        public async Task WaitUntil_WithFalseCondition_ShouldCompleteAfterTimeout()
        {
            // Arrange
            var condition = false;
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(50)).Token;

            // Act & Assert
            var startTime = DateTime.Now;
            await TestNwTask.WaitUntil(() => condition, cancellationToken);
            var elapsed = DateTime.Now - startTime;
            
            Assert.That(elapsed.TotalMilliseconds, Is.GreaterThanOrEqualTo(45));
        }

        [Test]
        public async Task WaitUntilValueChanged_WithChangingValue_ShouldComplete()
        {
            // Arrange
            var value = 0;
            var valueSource = () => value;

            // Act
            var task = TestNwTask.WaitUntilValueChanged(valueSource);
            value = 1; // Change the value
            await task;

            // Assert
            Assert.That(value, Is.EqualTo(1));
        }

        [Test]
        public async Task WhenAll_WithMultipleTasks_ShouldComplete()
        {
            // Arrange
            var tasks = new[]
            {
                TestNwTask.Delay(TimeSpan.FromMilliseconds(10)),
                TestNwTask.Delay(TimeSpan.FromMilliseconds(20)),
                TestNwTask.Delay(TimeSpan.FromMilliseconds(30))
            };

            // Act & Assert
            await TestNwTask.WhenAll(tasks);
            // If we get here without throwing an exception, the test passes
        }

        [Test]
        public async Task WhenAll_WithTaskEnumerable_ShouldComplete()
        {
            // Arrange
            var tasks = new List<Task>
            {
                TestNwTask.Delay(TimeSpan.FromMilliseconds(10)),
                TestNwTask.Delay(TimeSpan.FromMilliseconds(20)),
                TestNwTask.Delay(TimeSpan.FromMilliseconds(30))
            };

            // Act & Assert
            await TestNwTask.WhenAll(tasks);
            // If we get here without throwing an exception, the test passes
        }

        [Test]
        public async Task WhenAll_WithGenericTasks_ShouldReturnResults()
        {
            // Arrange
            var tasks = new[]
            {
                Task.FromResult("result1"),
                Task.FromResult("result2"),
                Task.FromResult("result3")
            };

            // Act
            var results = await TestNwTask.WhenAll(tasks);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Length, Is.EqualTo(3));
            Assert.That(results[0], Is.EqualTo("result1"));
            Assert.That(results[1], Is.EqualTo("result2"));
            Assert.That(results[2], Is.EqualTo("result3"));
        }

        [Test]
        public async Task WhenAll_WithGenericTaskEnumerable_ShouldReturnResults()
        {
            // Arrange
            var tasks = new List<Task<string>>
            {
                Task.FromResult("result1"),
                Task.FromResult("result2"),
                Task.FromResult("result3")
            };

            // Act
            var results = await TestNwTask.WhenAll(tasks);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Length, Is.EqualTo(3));
            Assert.That(results[0], Is.EqualTo("result1"));
            Assert.That(results[1], Is.EqualTo("result2"));
            Assert.That(results[2], Is.EqualTo("result3"));
        }

        [Test]
        public async Task WhenAny_WithMultipleTasks_ShouldComplete()
        {
            // Arrange
            var tasks = new[]
            {
                TestNwTask.Delay(TimeSpan.FromMilliseconds(100)),
                TestNwTask.Delay(TimeSpan.FromMilliseconds(50)),
                TestNwTask.Delay(TimeSpan.FromMilliseconds(200))
            };

            // Act & Assert
            await TestNwTask.WhenAny(tasks);
            // If we get here without throwing an exception, the test passes
        }

        [Test]
        public async Task WhenAny_WithTaskEnumerable_ShouldComplete()
        {
            // Arrange
            var tasks = new List<Task>
            {
                TestNwTask.Delay(TimeSpan.FromMilliseconds(100)),
                TestNwTask.Delay(TimeSpan.FromMilliseconds(50)),
                TestNwTask.Delay(TimeSpan.FromMilliseconds(200))
            };

            // Act & Assert
            await TestNwTask.WhenAny(tasks);
            // If we get here without throwing an exception, the test passes
        }

        [Test]
        public async Task WhenAny_WithGenericTasks_ShouldReturnFirstCompleted()
        {
            // Arrange
            var tasks = new[]
            {
                Task.Delay(100).ContinueWith(_ => "slow"),
                Task.Delay(50).ContinueWith(_ => "fast"),
                Task.Delay(200).ContinueWith(_ => "slowest")
            };

            // Act
            var result = await TestNwTask.WhenAny(tasks);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Result, Is.EqualTo("fast"));
        }

        [Test]
        public async Task WhenAny_WithGenericTaskEnumerable_ShouldReturnFirstCompleted()
        {
            // Arrange
            var tasks = new List<Task<string>>
            {
                Task.Delay(100).ContinueWith(_ => "slow"),
                Task.Delay(50).ContinueWith(_ => "fast"),
                Task.Delay(200).ContinueWith(_ => "slowest")
            };

            // Act
            var result = await TestNwTask.WhenAny(tasks);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Result, Is.EqualTo("fast"));
        }

        [Test]
        public async Task Delay_WithCancelledToken_ShouldThrowOperationCancelledException()
        {
            // Arrange
            var delay = TimeSpan.FromSeconds(1);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)).Token;

            // Act & Assert
            Assert.ThrowsAsync<OperationCanceledException>(async () => await TestNwTask.Delay(delay, cancellationToken));
        }

        [Test]
        public async Task DelayFrame_WithCancelledToken_ShouldThrowOperationCancelledException()
        {
            // Arrange
            const int frames = 100;
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)).Token;

            // Act & Assert
            Assert.ThrowsAsync<OperationCanceledException>(async () => await TestNwTask.DelayFrame(frames, cancellationToken));
        }

        [Test]
        public async Task WaitUntil_WithCancelledToken_ShouldCompleteAfterTimeout()
        {
            // Arrange
            var condition = false;
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)).Token;

            // Act & Assert
            var startTime = DateTime.Now;
            await TestNwTask.WaitUntil(() => condition, cancellationToken);
            var elapsed = DateTime.Now - startTime;
            
            // Should complete after timeout without throwing an exception
            Assert.That(elapsed.TotalMilliseconds, Is.GreaterThanOrEqualTo(9));
        }

        [Test]
        public async Task WaitUntilValueChanged_WithCancelledToken_ShouldCompleteAfterTimeout()
        {
            // Arrange
            var value = 0;
            var valueSource = () => value;
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)).Token;

            // Act & Assert
            var startTime = DateTime.Now;
            await TestNwTask.WaitUntilValueChanged(valueSource, cancellationToken);
            var elapsed = DateTime.Now - startTime;
            
            // Should complete after timeout without throwing an exception
            Assert.That(elapsed.TotalMilliseconds, Is.GreaterThanOrEqualTo(9));
        }
    }
}
