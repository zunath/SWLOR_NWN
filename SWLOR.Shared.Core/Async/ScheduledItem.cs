namespace SWLOR.Shared.Core.Async
{
    internal class ScheduledItem : IDisposable
    {
        private readonly Action _task;

        public double ExecutionTime { get; private set; }

        public readonly bool Repeating;
        public readonly double Schedule;

        public ScheduledItem(Action task, double executionTime)
        {
            _task = task;
            ExecutionTime = executionTime;
            Repeating = false;
        }

        public ScheduledItem(Action task, double executionTime, double schedule)
        {
            _task = task;
            ExecutionTime = executionTime;
            Schedule = schedule;
            Repeating = true;
        }

        public void Reschedule(double newTime)
        {
            ExecutionTime = newTime;
        }

        public void Execute()
        {
            _task();
        }

        public void Dispose()
        {
            Scheduler.Unschedule(this);
        }

        public sealed class SortedByExecutionTime : IComparer<ScheduledItem>
        {
            public int Compare(ScheduledItem x, ScheduledItem y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (ReferenceEquals(null, y))
                {
                    return 1;
                }

                if (ReferenceEquals(null, x))
                {
                    return -1;
                }

                return x.ExecutionTime.CompareTo(y.ExecutionTime);
            }
        }
    }
}