using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

[NonParallelizable]
public class SchedulerTests
{
    [Test]
    public void ZeroDelayWorkScheduledByACallbackWaitsForAnotherFrame()
    {
        var calls = 0;
        IDisposable pending = null;
        Action callback = null;
        callback = () =>
        {
            calls++;
            // Bound the reproduction even when run against the old scheduler.
            if (calls < 5)
                pending = Scheduler.Schedule(callback, TimeSpan.Zero);
        };
        pending = Scheduler.Schedule(callback, TimeSpan.Zero);
        try
        {
            Scheduler.Process();
            calls.Should().Be(1);
            Scheduler.Process();
            calls.Should().Be(2);
        }
        finally
        {
            pending.Dispose();
        }
    }

    [Test]
    public void CallbackCanCancelAnotherDueCallbackWithoutLosingOtherWork()
    {
        Scheduler.Process(); // Start the scheduler clock before waiting for positive delays.
        var calls = new List<string>();
        IDisposable cancelled = null;
        using var first = Scheduler.Schedule(() =>
        {
            calls.Add("first");
            cancelled.Dispose();
        }, TimeSpan.Zero);
        cancelled = Scheduler.Schedule(() => calls.Add("cancelled"), TimeSpan.FromTicks(1));
        using var last = Scheduler.Schedule(() => calls.Add("last"), TimeSpan.FromTicks(2));
        try
        {
            // Wait until all three are due without depending on task delays.
            System.Threading.Thread.Sleep(1);
            Scheduler.Process();
            calls.Should().Equal("first", "last");
        }
        finally
        {
            cancelled.Dispose();
        }
    }

    [Test]
    public void RepeatingCallbackCanCancelItself()
    {
        Scheduler.Process();
        var calls = 0;
        IDisposable repeating = null;
        repeating = Scheduler.ScheduleRepeating(() =>
        {
            calls++;
            repeating.Dispose();
        }, TimeSpan.FromTicks(1));
        try
        {
            System.Threading.Thread.Sleep(1);
            Scheduler.Process();
            Scheduler.Process();
            calls.Should().Be(1);
        }
        finally
        {
            repeating.Dispose();
        }
    }
}
