namespace SWLOR.DiscordBot.Core;

public interface ITicketStore
{
    Task InitializeAsync(CancellationToken ct);
    Task PruneCompletedDeliveriesAsync(DateTimeOffset now, CancellationToken ct) => Task.CompletedTask;
    async Task PersistCommunityDeliveryAsync(string key, string intent, CancellationToken ct)
    {
        await using var session = await LockCommunityAsync(ct);
        await session.GetOrCreateDeliveryAsync(key, intent, ct);
    }
    // The database lock spans Discord mutations so two processes cannot mutate a ticket concurrently.
    Task<ITicketSession> LockAsync(CancellationToken ct);
    // Community posts serialize independently of ticket exports and channel mutations.
    Task<ITicketSession> LockCommunityAsync(CancellationToken ct) => LockAsync(ct);
}
