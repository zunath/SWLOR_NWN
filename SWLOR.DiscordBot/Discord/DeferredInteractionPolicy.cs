namespace SWLOR.DiscordBot.Discord;

internal static class DeferredInteractionPolicy
{
    internal static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);
    // Leave time to explain a queue rejection while the original response can still be edited.
    internal static readonly TimeSpan LatestStartAge = TokenLifetime - TimeSpan.FromMinutes(1);

    internal static bool CanStart(DateTimeOffset createdAt, TimeProvider clock) =>
        clock.GetUtcNow() < createdAt + LatestStartAge;

    internal static bool CanReply(DateTimeOffset createdAt, TimeProvider clock) =>
        clock.GetUtcNow() < createdAt + TokenLifetime;

    internal static async Task ExecuteAsync(DateTimeOffset createdAt, TimeProvider clock,
        Func<CancellationToken, Task> job, Func<CancellationToken, Task> expired, CancellationToken ct, bool allowExtendedExecution = false)
    {
        ct.ThrowIfCancellationRequested();
        if (!CanStart(createdAt, clock)) { await expired(ct); return; }
        // Progressing exports must still finish and save snapshots after their response token expires.
        if (allowExtendedExecution) { await job(ct); return; }
        var remaining = createdAt + LatestStartAge - clock.GetUtcNow();
        if (remaining <= TimeSpan.Zero) { await expired(ct); return; }
        using var deadline = new CancellationTokenSource(remaining, clock);
        using var execution = CancellationTokenSource.CreateLinkedTokenSource(ct, deadline.Token);
        execution.Token.ThrowIfCancellationRequested();
        await job(execution.Token);
    }
}
