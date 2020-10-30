using System;
using System.Collections.Generic;
using System.Diagnostics;
using SWLOR.Game.Server.Core.Extensions;

namespace SWLOR.Game.Server.Core
{
    public static class Scheduler
    {
        private static double Time { get; set; }
        private static double DeltaTime { get; set; }

        private static readonly Stopwatch stopwatch = new Stopwatch();
        private static readonly List<ScheduledItem> scheduledItems = new List<ScheduledItem>(1024);
        private static readonly IComparer<ScheduledItem> comparer = new ScheduledItem.SortedByExecutionTime();

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
            scheduledItems.InsertOrdered(item, comparer);
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
            scheduledItems.InsertOrdered(item, comparer);
            return item;
        }

        internal static void Unschedule(ScheduledItem scheduledItem)
        {
            scheduledItems.Remove(scheduledItem);
        }

        public static void Process()
        {
            ProcessTime();
            ProcessScheduledItems();
        }
        private static void ProcessTime()
        {
            DeltaTime = stopwatch.Elapsed.TotalSeconds;
            Time += DeltaTime;
            stopwatch.Restart();
        }

        private static void ProcessScheduledItems()
        {
            int i;
            for (i = 0; i < scheduledItems.Count; i++)
            {
                var item = scheduledItems[i];
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
                scheduledItems.RemoveAt(i);
                scheduledItems.InsertOrdered(item, comparer);
                i--;
            }

            if (i > 0)
            {
                scheduledItems.RemoveRange(0, i);
            }
        }
    }
}
