namespace SWLOR.DiscordBot.Core;

public enum TicketState { Creating, Open, Closing, Closed, Reopening, Deleting, Deleted }
public sealed record Actor(ulong UserId, IReadOnlyCollection<ulong> RoleIds, bool IsGuildOwner = false);
public sealed record Ticket(Guid Id, string PanelId, ulong RequesterId, ulong? ChannelId,
    TicketState State, long Number, DateTimeOffset CreatedAt, DateTimeOffset? ClosedAt = null,
    DateTimeOffset? DeleteAfter = null, bool Hold = false, string? ArchivePath = null,
    DateTimeOffset? ArchiveExpiresAt = null, string? LastError = null, bool ArchiveComplete = false);
public sealed record TicketResult(bool Success, string Message, Ticket? Ticket = null);
public sealed record TranscriptAttachment(ulong Id, string FileName, string Url, long Size);
public sealed record TranscriptMessage(ulong Id, ulong AuthorId, string AuthorName, string Content,
    DateTimeOffset Timestamp, IReadOnlyList<TranscriptAttachment> Attachments, string? EmbedsJson = null);
public sealed record TranscriptSnapshot(IReadOnlyList<TranscriptMessage> Messages, ulong? LastMessageId);

public sealed record DeliveryState(string Intent, bool Completed);
public sealed record PendingDelivery(string Key, string Intent);

public interface ITicketSession : IAsyncDisposable
{
    Task<IReadOnlyList<Ticket>> GetTicketsAsync(CancellationToken ct);
    Task<Ticket?> GetTicketAsync(Guid id, CancellationToken ct);
    Task<Ticket?> FindByChannelAsync(ulong channelId, CancellationToken ct);
    Task<Ticket> ReserveAsync(string panelId, ulong requesterId, string interactionId, DateTimeOffset now, CancellationToken ct);
    Task<Ticket?> FindInteractionAsync(string interactionId, CancellationToken ct);
    Task SaveAsync(Ticket ticket, string action, ulong? actorId, CancellationToken ct);
    Task<bool> TryRecordDeliveryAsync(string key, CancellationToken ct);
    Task<DeliveryState> GetOrCreateDeliveryAsync(string key, string intent, CancellationToken ct);
    Task<IReadOnlyList<PendingDelivery>> GetPendingDeliveriesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PendingDelivery>>([]);
    Task CompleteDeliveryAsync(string key, CancellationToken ct);
    Task<DateTimeOffset?> GetCooldownAsync(string key, CancellationToken ct);
    Task SetCooldownAsync(string key, DateTimeOffset at, CancellationToken ct);
    Task<bool> TryAdvanceCommunityActionAsync(string scope, ulong messageId, CancellationToken ct);
}
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

public sealed record PendingResponseDeletion(ulong ChannelId, ulong MessageId, DateTimeOffset DueAt,
    int Attempts = 0, string? LastError = null);

public interface IResponseDeletionStore
{
    Task ScheduleDeletionAsync(ulong channelId, ulong messageId, DateTimeOffset dueAt, CancellationToken ct);
    Task<IReadOnlyList<PendingResponseDeletion>> GetDueDeletionsAsync(DateTimeOffset now, CancellationToken ct);
    Task CompleteDeletionAsync(ulong channelId, ulong messageId, CancellationToken ct);
    Task RetryDeletionAsync(PendingResponseDeletion deletion, DateTimeOffset dueAt, string error, CancellationToken ct);
}
public interface IDiscordTickets
{
    Task<Actor> ActorAsync(ulong userId, CancellationToken ct);
    Task<ulong?> FindManagedChannelAsync(Guid ticketId, CancellationToken ct);
    Task<ulong> CreateAsync(Ticket ticket, CancellationToken ct);
    Task OpenAsync(Ticket ticket, bool sendOpeningMessage, CancellationToken ct);
    Task CloseAsync(Ticket ticket, CancellationToken ct);
    Task RenameAsync(Ticket ticket, string name, CancellationToken ct);
    Task<bool> ExistsAsync(Ticket ticket, CancellationToken ct);
    Task FreezeAsync(Ticket ticket, CancellationToken ct);
    Task ReconcilePermissionsAsync(Ticket ticket, CancellationToken ct);
    Task<TranscriptSnapshot> ReadTranscriptAsync(Ticket ticket, CancellationToken ct);
    Task<TranscriptSnapshot> ReadTranscriptAsync(Ticket ticket, CancellationToken ct, Action progress) => ReadTranscriptAsync(ticket, ct);
    Task<ulong?> LastMessageIdAsync(Ticket ticket, CancellationToken ct);
    Task DeleteAsync(Ticket ticket, CancellationToken ct);
    Task LogAsync(string message, CancellationToken ct);
}
public interface ITranscriptArchive
{
    // Plan a managed directory without writing files, so ownership can commit before publication.
    string GetArchivePath(Ticket ticket);
    Task<string> ExportAsync(Ticket ticket, TranscriptSnapshot snapshot, CancellationToken ct);
    Task<string> ExportAsync(Ticket ticket, TranscriptSnapshot snapshot, CancellationToken ct, Action progress) => ExportAsync(ticket, snapshot, ct);
    Task DeleteAsync(string path, CancellationToken ct);
}
