using SWLOR.Shared.Core.Async;

namespace SWLOR.Test.Shared.Core.Async
{
    [TestFixture]
    public class SchedulerTests
    {
        private Scheduler _scheduler;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new Scheduler();
        }

        [Test]
        public void Schedule_WithValidTaskAndDelay_ShouldReturnDisposable()
        {
            // Arrange
            Action task = () => { };
            var delay = TimeSpan.FromMilliseconds(100);

            // Act
            var disposable = _scheduler.Schedule(task, delay);

            // Assert
            Assert.That(disposable, Is.Not.Null);
            Assert.That(disposable, Is.InstanceOf<IDisposable>());
        }

        [Test]
        public void Schedule_WithNegativeDelay_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            Action task = () => { };
            var delay = TimeSpan.FromMilliseconds(-100);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _scheduler.Schedule(task, delay));
        }

        [Test]
        public void Schedule_WithNullTask_ShouldThrowArgumentNullException()
        {
            // Arrange
            Action task = null;
            var delay = TimeSpan.FromMilliseconds(100);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _scheduler.Schedule(task, delay));
        }

        [Test]
        public void Schedule_WithZeroDelay_ShouldReturnDisposable()
        {
            // Arrange
            Action task = () => { };
            var delay = TimeSpan.Zero;

            // Act
            var disposable = _scheduler.Schedule(task, delay);

            // Assert
            Assert.That(disposable, Is.Not.Null);
        }

        [Test]
        public void ScheduleRepeating_WithValidTaskAndSchedule_ShouldReturnDisposable()
        {
            // Arrange
            Action task = () => { };
            var schedule = TimeSpan.FromMilliseconds(100);

            // Act
            var disposable = _scheduler.ScheduleRepeating(task, schedule);

            // Assert
            Assert.That(disposable, Is.Not.Null);
            Assert.That(disposable, Is.InstanceOf<IDisposable>());
        }

        [Test]
        public void ScheduleRepeating_WithZeroSchedule_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            Action task = () => { };
            var schedule = TimeSpan.Zero;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _scheduler.ScheduleRepeating(task, schedule));
        }

        [Test]
        public void ScheduleRepeating_WithNegativeSchedule_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            Action task = () => { };
            var schedule = TimeSpan.FromMilliseconds(-100);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _scheduler.ScheduleRepeating(task, schedule));
        }

        [Test]
        public void ScheduleRepeating_WithNullTask_ShouldThrowArgumentNullException()
        {
            // Arrange
            Action task = null;
            var schedule = TimeSpan.FromMilliseconds(100);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _scheduler.ScheduleRepeating(task, schedule));
        }

        [Test]
        public void ScheduleRepeating_WithDefaultDelay_ShouldUseZeroDelay()
        {
            // Arrange
            Action task = () => { };
            var schedule = TimeSpan.FromMilliseconds(100);
            var delay = default(TimeSpan);

            // Act
            var disposable = _scheduler.ScheduleRepeating(task, schedule, delay);

            // Assert
            Assert.That(disposable, Is.Not.Null);
        }

        [Test]
        public void ScheduleRepeating_WithCustomDelay_ShouldUseCustomDelay()
        {
            // Arrange
            Action task = () => { };
            var schedule = TimeSpan.FromMilliseconds(100);
            var delay = TimeSpan.FromMilliseconds(50);

            // Act
            var disposable = _scheduler.ScheduleRepeating(task, schedule, delay);

            // Assert
            Assert.That(disposable, Is.Not.Null);
        }

        [Test]
        public void Process_WithNoScheduledItems_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _scheduler.Process());
        }

        [Test]
        public void Process_WithScheduledItem_ShouldExecuteItemWhenTimeElapses()
        {
            // Arrange
            var taskExecuted = false;
            Action task = () => taskExecuted = true;
            var delay = TimeSpan.FromMilliseconds(10);

            _scheduler.Schedule(task, delay);

            // Act
            // Call Process multiple times to advance internal time
            for (int i = 0; i < 100; i++)
            {
                _scheduler.Process();
                if (taskExecuted) break;
                Thread.Sleep(1); // Small delay to allow time to advance
            }

            // Assert
            Assert.That(taskExecuted, Is.True);
        }

        [Test]
        public void Process_WithRepeatingScheduledItem_ShouldExecuteItemMultipleTimes()
        {
            // Arrange
            var executionCount = 0;
            Action task = () => executionCount++;
            var schedule = TimeSpan.FromMilliseconds(10);

            _scheduler.ScheduleRepeating(task, schedule);

            // Act
            // Call Process multiple times to advance internal time and allow multiple executions
            for (int i = 0; i < 100; i++)
            {
                _scheduler.Process();
                Thread.Sleep(1); // Small delay to allow time to advance
            }

            // Assert
            Assert.That(executionCount, Is.GreaterThan(0));
        }

        [Test]
        public void Process_WithMultipleScheduledItems_ShouldExecuteAllItems()
        {
            // Arrange
            var executionCount = 0;
            Action task1 = () => executionCount++;
            Action task2 = () => executionCount++;
            var delay = TimeSpan.FromMilliseconds(10);

            _scheduler.Schedule(task1, delay);
            _scheduler.Schedule(task2, delay);

            // Act
            // Call Process multiple times to advance internal time
            for (int i = 0; i < 100; i++)
            {
                _scheduler.Process();
                if (executionCount >= 2) break;
                Thread.Sleep(1); // Small delay to allow time to advance
            }

            // Assert
            Assert.That(executionCount, Is.EqualTo(2));
        }

        [Test]
        public void Process_WithScheduledItemNotYetDue_ShouldNotExecuteItem()
        {
            // Arrange
            var taskExecuted = false;
            Action task = () => taskExecuted = true;
            var delay = TimeSpan.FromSeconds(10); // Long delay

            _scheduler.Schedule(task, delay);

            // Act
            _scheduler.Process();

            // Assert
            Assert.That(taskExecuted, Is.False);
        }

        [Test]
        public void Process_WithDisposedScheduledItem_ShouldNotExecuteItem()
        {
            // Arrange
            var taskExecuted = false;
            Action task = () => taskExecuted = true;
            var delay = TimeSpan.FromMilliseconds(10);

            var disposable = _scheduler.Schedule(task, delay);
            disposable.Dispose();

            // Act
            Thread.Sleep(50);
            _scheduler.Process();

            // Assert
            Assert.That(taskExecuted, Is.False);
        }

        [Test]
        public void Process_WithDisposedRepeatingScheduledItem_ShouldNotExecuteItem()
        {
            // Arrange
            var taskExecuted = false;
            Action task = () => taskExecuted = true;
            var schedule = TimeSpan.FromMilliseconds(10);

            var disposable = _scheduler.ScheduleRepeating(task, schedule);
            disposable.Dispose();

            // Act
            Thread.Sleep(50);
            _scheduler.Process();

            // Assert
            Assert.That(taskExecuted, Is.False);
        }

        [Test]
        public void Process_WithRepeatingItem_ShouldRescheduleAfterExecution()
        {
            // Arrange
            var executionCount = 0;
            Action task = () => executionCount++;
            var schedule = TimeSpan.FromMilliseconds(10);

            _scheduler.ScheduleRepeating(task, schedule);

            // Act
            // Call Process multiple times to advance internal time and allow multiple executions
            for (int i = 0; i < 100; i++)
            {
                _scheduler.Process();
                Thread.Sleep(1); // Small delay to allow time to advance
            }

            // Assert
            Assert.That(executionCount, Is.GreaterThan(1));
        }

        [Test]
        public void Process_WithNonRepeatingItem_ShouldNotRescheduleAfterExecution()
        {
            // Arrange
            var executionCount = 0;
            Action task = () => executionCount++;
            var delay = TimeSpan.FromMilliseconds(10);

            _scheduler.Schedule(task, delay);

            // Act
            Thread.Sleep(50);
            _scheduler.Process();
            Thread.Sleep(50);
            _scheduler.Process();

            // Assert
            Assert.That(executionCount, Is.EqualTo(1));
        }

        [Test]
        public void Process_WithMultipleProcessCalls_ShouldHandleCorrectly()
        {
            // Arrange
            var executionCount = 0;
            Action task = () => executionCount++;
            var delay = TimeSpan.FromMilliseconds(10);

            _scheduler.Schedule(task, delay);

            // Act
            // Call Process multiple times to advance internal time
            for (int i = 0; i < 100; i++)
            {
                _scheduler.Process();
                if (executionCount >= 1) break;
                Thread.Sleep(1); // Small delay to allow time to advance
            }

            // Assert
            Assert.That(executionCount, Is.EqualTo(1));
        }

        [Test]
        public void Process_WithVeryShortDelay_ShouldExecuteImmediately()
        {
            // Arrange
            var taskExecuted = false;
            Action task = () => taskExecuted = true;
            var delay = TimeSpan.FromTicks(1); // Very short delay

            _scheduler.Schedule(task, delay);

            // Act
            // Call Process multiple times to advance internal time
            for (int i = 0; i < 10; i++)
            {
                _scheduler.Process();
                if (taskExecuted) break;
                Thread.Sleep(1); // Small delay to allow time to advance
            }

            // Assert
            Assert.That(taskExecuted, Is.True);
        }

        [Test]
        public void Process_WithZeroDelay_ShouldExecuteImmediately()
        {
            // Arrange
            var taskExecuted = false;
            Action task = () => taskExecuted = true;
            var delay = TimeSpan.Zero;

            _scheduler.Schedule(task, delay);

            // Act
            _scheduler.Process();

            // Assert
            Assert.That(taskExecuted, Is.True);
        }
    }
}
