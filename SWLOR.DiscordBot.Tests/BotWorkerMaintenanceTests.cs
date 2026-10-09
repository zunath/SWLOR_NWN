using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Hosting;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class BotWorkerMaintenanceTests
{
    [TestCase("Archive expiration")]
    [TestCase("Ticket maintenance")]
    public async Task ProgressingPassCanFinishLaterEntriesAfterTenMinutes(string operationName)
    {
        var clock = new ManualClock();
        using var stopping = new CancellationTokenSource();
        using var worker = Worker(clock);
        var entriesCompleted = new List<int>();
        var attempts = 0;
        await worker.RetryMaintenanceAsync(operationName, async ct =>
        {
            attempts++;
            Assert.That(ct, Is.EqualTo(stopping.Token), "Progressing work must inherit only shutdown cancellation.");
            for (var entry = 0; entry < 3; entry++)
            {
                for (var page = 0; page < 4; page++)
                {
                    clock.Advance(TimeSpan.FromMinutes(1));
                    await Task.Yield();
                    ct.ThrowIfCancellationRequested();
                }
                entriesCompleted.Add(entry);
            }
        }, stopping.Token);
        Assert.That(entriesCompleted, Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(attempts, Is.EqualTo(1));
        Assert.That(clock.GetUtcNow() - DateTimeOffset.UnixEpoch, Is.EqualTo(TimeSpan.FromMinutes(12)));
    }

    [Test]
    public void ShutdownCancelsActiveExpirationPassWithoutRetrying()
    {
        var clock = new ManualClock();
        using var worker = Worker(clock);
        using var stopping = new CancellationTokenSource();
        var attempts = 0;
        var pass = worker.RetryMaintenanceAsync("Archive expiration", async ct =>
        {
            attempts++;
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }, stopping.Token);
        stopping.Cancel();
        Assert.ThrowsAsync<TaskCanceledException>(async () => await pass.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.That(attempts, Is.EqualTo(1));
    }

    [Test]
    public async Task FailedExpirationPassRetainsBoundedRetriesAndShutdownToken()
    {
        var clock = new ManualClock();
        using var worker = Worker(clock);
        using var stopping = new CancellationTokenSource();
        var attempts = 0;
        var secondAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pass = worker.RetryMaintenanceAsync("Archive expiration", ct =>
        {
            Assert.That(ct, Is.EqualTo(stopping.Token));
            if (++attempts == 2) secondAttempt.SetResult();
            throw new IOException("Archive enumeration failed.");
        }, stopping.Token);
        Assert.That(attempts, Is.EqualTo(1));
        clock.Advance(TimeSpan.FromSeconds(5));
        await secondAttempt.Task.WaitAsync(TimeSpan.FromSeconds(2));
        // The continuation installs the second retry delay before yielding control.
        await WaitForTimerAsync(clock);
        clock.Advance(TimeSpan.FromSeconds(10));
        await pass.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.That(attempts, Is.EqualTo(3));
    }

    private static async Task WaitForTimerAsync(ManualClock clock)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (clock.ActiveTimers == 0) { timeout.Token.ThrowIfCancellationRequested(); await Task.Yield(); }
    }

    private static BotWorker Worker(TimeProvider clock) => new(new BotConfiguration(), new BotSecrets("", ""),
        null!, null!, null!, null!, null!, null!, null!, null!, clock, null!, NullLogger<BotWorker>.Instance);

    private sealed class ManualClock : TimeProvider
    {
        private readonly object sync = new();
        private readonly List<ManualTimer> timers = [];
        private DateTimeOffset now = DateTimeOffset.UnixEpoch;
        public int ActiveTimers { get { lock (sync) return timers.Count; } }
        public override DateTimeOffset GetUtcNow() { lock (sync) return now; }
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            lock (sync)
            {
                var timer = new ManualTimer(this, callback, state);
                timers.Add(timer);
                timer.Change(dueTime, period);
                return timer;
            }
        }
        public void Advance(TimeSpan amount)
        {
            ManualTimer[] active;
            lock (sync) { now += amount; active = timers.ToArray(); }
            foreach (var timer in active) timer.FireIfDue();
        }
        private sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state) : ITimer
        {
            private DateTimeOffset? due;
            private bool disposed;
            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (clock.sync)
                {
                    if (disposed) return false;
                    due = dueTime == Timeout.InfiniteTimeSpan ? null : clock.now + dueTime;
                    return true;
                }
            }
            public void FireIfDue()
            {
                lock (clock.sync)
                {
                    if (disposed || due is not { } at || clock.now < at) return;
                    due = null;
                }
                callback(state);
            }
            public void Dispose() { lock (clock.sync) { disposed = true; clock.timers.Remove(this); } }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
