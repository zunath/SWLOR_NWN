using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Discord;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class DeferredInteractionTests
{
    [TestCase(839, true)]
    [TestCase(840, false)]
    [TestCase(900, false)]
    public void AdmissionReservesTimeForAReplyAndRejectsExpiredJobs(int ageSeconds, bool expected)
    {
        var clock = new ManualClock();
        var gateway = Gateway(clock);
        var createdAt = clock.GetUtcNow();
        clock.Advance(TimeSpan.FromSeconds(ageSeconds));
        Assert.That(gateway.TryQueueInteractionJob(createdAt,
            _ => throw new AssertionException("Admission must not execute a job."),
            _ => throw new AssertionException("Admission must not execute a rejection.")), Is.EqualTo(expected));
        Assert.That(DeferredInteractionPolicy.CanReply(createdAt, clock), Is.EqualTo(ageSeconds < 900));
    }

    [TestCase(840)]
    [TestCase(900)]
    [TestCase(960)]
    public async Task InteractionsWaitingBehindFourExportsNeverInvokeMutationAfterLatestStart(int ageSeconds)
    {
        var clock = new ManualClock();
        var gateway = Gateway(clock);
        var createdAt = clock.GetUtcNow();
        var startedCount = 0;
        var exportsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseExports = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var rejected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stopping = new CancellationTokenSource();
        for (var i = 0; i < 4; i++)
            Assert.That(gateway.TryQueueJob(async ct =>
            {
                if (Interlocked.Increment(ref startedCount) == 4) exportsStarted.SetResult();
                await releaseExports.Task.WaitAsync(ct);
            }), Is.True);
        var consumers = Enumerable.Range(0, 4).Select(_ => gateway.ProcessAsync(stopping.Token)).ToArray();
        var mutations = 0;
        try
        {
            await exportsStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(gateway.TryQueueInteractionJob(createdAt,
                _ => { Interlocked.Increment(ref mutations); return Task.CompletedTask; },
                _ => { rejected.SetResult(); return Task.CompletedTask; }), Is.True);
            clock.Advance(TimeSpan.FromSeconds(ageSeconds));
            releaseExports.SetResult();
            await rejected.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(mutations, Is.Zero, "Stale controls must not mutate tickets even when admitted while fresh.");
        }
        finally { await StopAsync(stopping, consumers); }
    }

    [Test]
    public void ShutdownAtExecutionBoundaryNeverInvokesTheJobOrRejection()
    {
        var clock = new ManualClock();
        using var stopping = new CancellationTokenSource();
        stopping.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(() => DeferredInteractionPolicy.ExecuteAsync(clock.GetUtcNow(), clock,
            _ => throw new AssertionException("A cancelled consumer must not mutate state."),
            _ => throw new AssertionException("A cancelled consumer must not attempt a reply."), stopping.Token));
    }

    [Test]
    public async Task ExportStartedBeforeBoundaryFinishesAfterTokenExpiryWithOnlyHostCancellation()
    {
        var clock = new ManualClock();
        var gateway = Gateway(clock);
        var createdAt = clock.GetUtcNow();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stopping = new CancellationTokenSource();
        clock.Advance(DeferredInteractionPolicy.LatestStartAge - TimeSpan.FromSeconds(1));
        Assert.That(gateway.TryQueueInteractionJob(createdAt, async ct =>
        {
            Assert.That(ct, Is.EqualTo(stopping.Token));
            for (var page = 0; page < 10; page++)
            {
                clock.Advance(TimeSpan.FromMinutes(3));
                await Task.Yield();
                ct.ThrowIfCancellationRequested();
            }
            completed.SetResult();
        }, _ => throw new AssertionException("A started export must remain able to save its archive."), allowExtendedExecution: true), Is.True);
        var consumer = gateway.ProcessAsync(stopping.Token);
        try
        {
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(DeferredInteractionPolicy.CanReply(createdAt, clock), Is.False);
        }
        finally { await StopAsync(stopping, [consumer]); }
    }

    [Test]
    public void ControlJobWaitingForATicketLockIsCancelledAtItsAgeBoundary()
    {
        var clock = new ManualClock();
        var createdAt = clock.GetUtcNow();
        clock.Advance(DeferredInteractionPolicy.LatestStartAge - TimeSpan.FromSeconds(1));
        var invoked = false;
        var mutations = 0;
        var execution = DeferredInteractionPolicy.ExecuteAsync(createdAt, clock, async ct =>
        {
            invoked = true;
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            mutations++;
        }, _ => throw new AssertionException("The job was fresh when it started."), CancellationToken.None);
        Assert.That(invoked, Is.True);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.ThrowsAsync<TaskCanceledException>(async () => await execution.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.That(mutations, Is.Zero);
        Assert.That(DeferredInteractionPolicy.CanReply(createdAt, clock), Is.True);
    }

    private static DiscordGateway Gateway(TimeProvider clock) => new(null!, new BotConfiguration(), null!, null!, null!, null!,
        clock, null!, null!, NullLogger<DiscordGateway>.Instance);

    private static async Task StopAsync(CancellationTokenSource stopping, Task[] consumers)
    {
        await stopping.CancelAsync();
        try { await Task.WhenAll(consumers); } catch (OperationCanceledException) { }
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => now;
        private readonly List<ManualTimer> timers = [];
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
            private bool disposed;
            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (disposed) return false;
                due = dueTime == Timeout.InfiniteTimeSpan ? null : clock.now + dueTime;
                return true;
            }
            public void FireIfDue()
            {
                if (disposed || due is not { } at || clock.now < at) return;
                due = null;
                callback(state);
            }
            public void Dispose() { disposed = true; clock.timers.Remove(this); }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
