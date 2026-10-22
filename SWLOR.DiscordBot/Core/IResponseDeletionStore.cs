namespace SWLOR.DiscordBot.Core;

public interface IResponseDeletionStore
{
    Task ScheduleDeletionAsync(ulong channelId, ulong messageId, DateTimeOffset dueAt, CancellationToken ct);
    Task<IReadOnlyList<PendingResponseDeletion>> GetDueDeletionsAsync(DateTimeOffset now, CancellationToken ct);
    Task CompleteDeletionAsync(ulong channelId, ulong messageId, CancellationToken ct);
    Task RetryDeletionAsync(PendingResponseDeletion deletion, DateTimeOffset dueAt, string error, CancellationToken ct);
}
