namespace SWLOR.DiscordBot.Core;

public enum TicketState { Creating, Open, Closing, Closed, Reopening, Deleting, Deleted }
public sealed record Actor(ulong UserId, IReadOnlyCollection<ulong> RoleIds, bool IsGuildOwner = false);
public sealed record Ticket(Guid Id, string PanelId, ulong RequesterId, ulong? ChannelId,
    TicketState State, long Number, DateTimeOffset CreatedAt, DateTimeOffset? ClosedAt = null,
    DateTimeOffset? DeleteAfter = null, bool Hold = false, string? ArchivePath = null,
    DateTimeOffset? ArchiveExpiresAt = null, string? LastError = null);
public sealed record TicketResult(bool Success, string Message, Ticket? Ticket = null);
public sealed record TranscriptAttachment(ulong Id, string FileName, string Url, long Size);
public sealed record TranscriptMessage(ulong Id, ulong AuthorId, string AuthorName, string Content,
    DateTimeOffset Timestamp, IReadOnlyList<TranscriptAttachment> Attachments, string? EmbedsJson = null);
public sealed record TranscriptSnapshot(IReadOnlyList<TranscriptMessage> Messages, ulong? LastMessageId);

public sealed record DeliveryState(string Intent, bool Completed);

public interface ITicketSession : IAsyncDisposable
{
    Task<IReadOnlyList<Ticket>> GetTicketsAsync(CancellationToken ct);
    Task<Ticket> ReserveAsync(string panelId, ulong requesterId, string interactionId, DateTimeOffset now, CancellationToken ct);
    Task<Ticket?> FindInteractionAsync(string interactionId, CancellationToken ct);
    Task SaveAsync(Ticket ticket, string action, ulong? actorId, CancellationToken ct);
    Task<bool> TryRecordDeliveryAsync(string key, CancellationToken ct);
    Task<DeliveryState> GetOrCreateDeliveryAsync(string key, string intent, CancellationToken ct);
    Task CompleteDeliveryAsync(string key, CancellationToken ct);
    Task<DateTimeOffset?> GetCooldownAsync(string key, CancellationToken ct);
    Task SetCooldownAsync(string key, DateTimeOffset at, CancellationToken ct);
}
public interface ITicketStore
{
    Task InitializeAsync(CancellationToken ct);
    // The database lock spans Discord mutations so two processes cannot mutate a ticket concurrently.
    Task<ITicketSession> LockAsync(CancellationToken ct);
}
public interface IDiscordTickets
{
    Task<ulong?> FindManagedChannelAsync(Guid ticketId, CancellationToken ct);
    Task<ulong> CreateAsync(Ticket ticket, CancellationToken ct);
    Task OpenAsync(Ticket ticket, bool sendOpeningMessage, CancellationToken ct);
    Task CloseAsync(Ticket ticket, CancellationToken ct);
    Task RenameAsync(Ticket ticket, string name, CancellationToken ct);
    Task<bool> ExistsAsync(Ticket ticket, CancellationToken ct);
    Task FreezeAsync(Ticket ticket, CancellationToken ct);
    Task<TranscriptSnapshot> ReadTranscriptAsync(Ticket ticket, CancellationToken ct);
    Task<ulong?> LastMessageIdAsync(Ticket ticket, CancellationToken ct);
    Task DeleteAsync(Ticket ticket, CancellationToken ct);
    Task LogAsync(string message, CancellationToken ct);
}
public interface ITranscriptArchive
{
    Task<string> ExportAsync(Ticket ticket, TranscriptSnapshot snapshot, CancellationToken ct);
    Task DeleteAsync(string path, CancellationToken ct);
}
