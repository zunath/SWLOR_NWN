using System.Diagnostics;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Extension;

namespace SWLOR.Shared.Core.Async
{
    public class Scheduler : IScheduler
    {
        private double Time { get; set; }
        private double DeltaTime { get; set; }

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly List<ScheduledItem> _scheduledItems = new List<ScheduledItem>(1024);
        private readonly IComparer<ScheduledItem> _comparer = new ScheduledItem.SortedByExecutionTime();

        public IDisposable Schedule(Action task, TimeSpan delay)
        {
            if (delay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay), $"{nameof(delay)} cannot be < zero.");
            }

            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var item = new ScheduledItem(task, Time + delay.TotalSeconds, Unschedule);
            _scheduledItems.InsertOrdered(item, _comparer);
            return item;
        }

        public IDisposable ScheduleRepeating(Action task, TimeSpan schedule, TimeSpan delay = default)
        {
            if (schedule <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay), $"{nameof(delay)} cannot be <= zero.");
            }

            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var item = new ScheduledItem(task, Time + delay.TotalSeconds + schedule.TotalSeconds, schedule.TotalSeconds, Unschedule);
            _scheduledItems.InsertOrdered(item, _comparer);
            return item;
        }

        internal void Unschedule(ScheduledItem scheduledItem)
        {
            _scheduledItems.Remove(scheduledItem);
        }

        public void Process()
        {
            ProcessTime();
            ProcessScheduledItems();
        }
        private void ProcessTime()
        {
            DeltaTime = _stopwatch.Elapsed.TotalSeconds;
            Time += DeltaTime;
            _stopwatch.Restart();
        }

        private void ProcessScheduledItems()
        {
            int i;
            for (i = 0; i < _scheduledItems.Count; i++)
            {
                var item = _scheduledItems[i];
                if (Time < item.ExecutionTime)
                {
                    break;
                }

                item.Execute();
                if (!item.Repeating)
                {
                    continue;
                }

                item.Reschedule(Time + item.Schedule);
                _scheduledItems.RemoveAt(i);
                _scheduledItems.InsertOrdered(item, _comparer);
                i--;
            }

            if (i > 0)
            {
                _scheduledItems.RemoveRange(0, i);
            }
        }
    }
}
