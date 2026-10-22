using System.Threading.Channels;
using NUnit.Framework;
using SWLOR.DiscordBot.Hosting;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class TicketMaintenanceSchedulerTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task ReadinessResumesMaintenanceWithoutWaitingForThirtyDayInterval(bool alreadyReady)
    {
        var scheduler = new TicketMaintenanceScheduler();
        var clock = new ManualClock();
        using var cancellation = new CancellationTokenSource();
        var ready = alreadyReady;
        var observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sweeps = Channel.CreateUnbounded<int>();
        var calls = 0;
        var run = scheduler.RunAsync(TimeSpan.FromDays(30), clock, () =>
        {
            var current = ready;
            observed.TrySetResult();
            return current;
        }, _ => { sweeps.Writer.TryWrite(++calls); return Task.CompletedTask; }, cancellation.Token);
        try
        {
            await observed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (!alreadyReady)
            {
                Assert.That(calls, Is.Zero);
                ready = true;
                scheduler.RequestSweep();
            }
            Assert.That(await sweeps.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo(1));
            Assert.That(clock.GetUtcNow(), Is.EqualTo(DateTimeOffset.UnixEpoch));

            observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ready = false;
            clock.Advance(TimeSpan.FromDays(30));
            await observed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(calls, Is.EqualTo(1));
            // A fresh ready transition must resume another pass even if the scheduled pass was skipped offline.
            ready = true;
            scheduler.RequestSweep();
            Assert.That(await sweeps.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo(2));
        }
        finally
        {
            cancellation.Cancel();
            Assert.CatchAsync<OperationCanceledException>(async () => await run.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        Assert.That(clock.ActiveTimers, Is.Zero);
    }

    [Test]
    public async Task ReadySweepDoesNotReplacePeriodicCleanup()
    {
        var scheduler = new TicketMaintenanceScheduler();
        var clock = new ManualClock();
        using var cancellation = new CancellationTokenSource();
        var sweeps = Channel.CreateUnbounded<int>();
        var calls = 0;
        var run = scheduler.RunAsync(TimeSpan.FromDays(30), clock, () => true,
            _ => { sweeps.Writer.TryWrite(++calls); return Task.CompletedTask; }, cancellation.Token);
        try
        {
            Assert.That(await sweeps.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo(1));
            scheduler.RequestSweep();
            Assert.That(await sweeps.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo(2));
            clock.Advance(TimeSpan.FromDays(30));
            Assert.That(await sweeps.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo(3));
            clock.Advance(TimeSpan.FromDays(30));
            Assert.That(await sweeps.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo(4));
        }
        finally
        {
            cancellation.Cancel();
            Assert.CatchAsync<OperationCanceledException>(async () => await run.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        Assert.That(clock.ActiveTimers, Is.Zero);
    }

    [Test]
    public async Task ReadySignalsCoalesceWhileSweepingAndNeverOverlap()
    {
        var scheduler = new TicketMaintenanceScheduler();
        var clock = new ManualClock();
        using var cancellation = new CancellationTokenSource();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = 0;
        var maximumActive = 0;
        var calls = 0;
        var run = scheduler.RunAsync(TimeSpan.FromDays(30), clock, () => true, async ct =>
        {
            maximumActive = Math.Max(maximumActive, ++active);
            var call = ++calls;
            if (call == 1)
            {
                firstStarted.TrySetResult();
                await release.Task.WaitAsync(ct);
            }
            active--;
            if (call == 2) secondFinished.TrySetResult();
        }, cancellation.Token);
        try
        {
            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            for (var i = 0; i < 100; i++) scheduler.RequestSweep();
            clock.Advance(TimeSpan.FromDays(30));
            Assert.That(calls, Is.EqualTo(1));
            release.TrySetResult();
            await secondFinished.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(maximumActive, Is.EqualTo(1));
        }
        finally
        {
            cancellation.Cancel();
            Assert.CatchAsync<OperationCanceledException>(async () => await run.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        Assert.That(clock.ActiveTimers, Is.Zero);
    }

    [Test]
    public async Task CancellationWhileOfflineDisposesTimerAndStopsQueuedSweeps()
    {
        var scheduler = new TicketMaintenanceScheduler();
        var clock = new ManualClock();
        using var cancellation = new CancellationTokenSource();
        var observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var run = scheduler.RunAsync(TimeSpan.FromDays(30), clock,
            () => { observed.TrySetResult(); return false; },
            _ => { calls++; return Task.CompletedTask; }, cancellation.Token);
        await observed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        Assert.CatchAsync<OperationCanceledException>(async () => await run.WaitAsync(TimeSpan.FromSeconds(5)));
        scheduler.RequestSweep();
        clock.Advance(TimeSpan.FromDays(60));
        Assert.That(calls, Is.Zero);
        Assert.That(clock.ActiveTimers, Is.Zero);
    }

    private sealed class ManualClock : TimeProvider
    {
        private readonly List<ManualTimer> timers = [];
        private DateTimeOffset now = DateTimeOffset.UnixEpoch;
        public int ActiveTimers => timers.Count;
        public override DateTimeOffset GetUtcNow() => now;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            timers.Add(timer);
            timer.Change(dueTime, period);
            return timer;
        }
        public void Advance(TimeSpan amount)
        {
            now += amount;
            foreach (var timer in timers.ToArray()) timer.FireIfDue();
        }
        private sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state) : ITimer
        {
            private DateTimeOffset? due;
            private TimeSpan period;
            private bool disposed;
            public bool Change(TimeSpan dueTime, TimeSpan repeat)
            {
                if (disposed) return false;
                due = dueTime == Timeout.InfiniteTimeSpan ? null : clock.now + dueTime;
                period = repeat;
                return true;
            }
            public void FireIfDue()
            {
                if (disposed || due is not { } at || clock.now < at) return;
                due = period == Timeout.InfiniteTimeSpan ? null : clock.now + period;
                callback(state);
            }
            public void Dispose() { disposed = true; clock.timers.Remove(this); }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
