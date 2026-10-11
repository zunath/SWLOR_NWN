using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SWLOR.Game.Server.Core.Extensions;

namespace SWLOR.Game.Server.Core
{
    public static class Scheduler
    {
        private static double Time { get; set; }
        private static double DeltaTime { get; set; }

        private static readonly Stopwatch _stopwatch = new Stopwatch();
        private static readonly List<ScheduledItem> _scheduledItems = new List<ScheduledItem>(1024);
        private static readonly IComparer<ScheduledItem> _comparer = new ScheduledItem.SortedByExecutionTime();

        public static IDisposable Schedule(Action task, TimeSpan delay)
        {
            if (delay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay), $"{nameof(delay)} cannot be < zero.");
            }

            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var item = new ScheduledItem(task, Time + delay.TotalSeconds);
            _scheduledItems.InsertOrdered(item, _comparer);
            return item;
        }

        public static IDisposable ScheduleRepeating(Action task, TimeSpan schedule, TimeSpan delay = default)
        {
            if (schedule <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay), $"{nameof(delay)} cannot be <= zero.");
            }

            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var item = new ScheduledItem(task, Time + delay.TotalSeconds + schedule.TotalSeconds, schedule.TotalSeconds);
            _scheduledItems.InsertOrdered(item, _comparer);
            return item;
        }

        internal static void Unschedule(ScheduledItem scheduledItem)
        {
            _scheduledItems.Remove(scheduledItem);
        }

        public static void Process()
        {
            ProcessTime();
            ProcessScheduledItems();
        }
        private static void ProcessTime()
        {
            DeltaTime = _stopwatch.Elapsed.TotalSeconds;
            Time += DeltaTime;
            _stopwatch.Restart();
        }

        private static void ProcessScheduledItems()
        {
            // Callbacks can schedule or cancel work. Only run the work due at this frame's start.
            var dueItems = _scheduledItems.TakeWhile(item => item.ExecutionTime <= Time).ToArray();
            foreach (var item in dueItems)
            {
                if (item.IsCancelled)
                    continue;

                _scheduledItems.Remove(item);
                try
                {
                    item.Execute();
                }
                finally
                {
                    if (item.Repeating && !item.IsCancelled)
                    {
                        item.Reschedule(Time + item.Schedule);
                        _scheduledItems.InsertOrdered(item, _comparer);
                    }
                }
            }
        }
    }
}
