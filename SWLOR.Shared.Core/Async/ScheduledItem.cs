namespace SWLOR.Shared.Core.Async
{
    internal class ScheduledItem : IDisposable
    {
        private readonly Action _task;
        private readonly Action<ScheduledItem> _unscheduleCallback;

        public double ExecutionTime { get; private set; }

        public readonly bool Repeating;
        public readonly double Schedule;

        public ScheduledItem(Action task, double executionTime, Action<ScheduledItem> unscheduleCallback)
        {
            _task = task;
            ExecutionTime = executionTime;
            _unscheduleCallback = unscheduleCallback;
            Repeating = false;
        }

        public ScheduledItem(Action task, double executionTime, double schedule, Action<ScheduledItem> unscheduleCallback)
        {
            _task = task;
            ExecutionTime = executionTime;
            Schedule = schedule;
            _unscheduleCallback = unscheduleCallback;
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
            _unscheduleCallback?.Invoke(this);
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