namespace SWLOR.DiscordBot.Core;

public interface IDiscordTickets
{
    Task<Actor> ActorAsync(ulong userId, CancellationToken ct);
    Task<ulong?> FindManagedChannelAsync(Guid ticketId, CancellationToken ct);
    Task<ulong> CreateAsync(Ticket ticket, CancellationToken ct);
    Task OpenAsync(Ticket ticket, bool sendOpeningMessage, CancellationToken ct);
    Task OpenAsync(Ticket ticket, bool sendOpeningMessage, CancellationToken ct, Action progress) =>
        OpenAsync(ticket, sendOpeningMessage, ct);
    Task OpenAsync(Ticket ticket, bool sendOpeningMessage, CancellationToken ct, Action progress,
        Func<CancellationToken, Task> beforeOpeningSend) => sendOpeningMessage
            ? Task.FromException(new NotSupportedException("The ticket adapter must honor the durable opening-message send callback."))
            : OpenAsync(ticket, false, ct, progress);
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
