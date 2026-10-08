namespace SWLOR.DiscordBot.Core;

public interface ITicketSession : IAsyncDisposable
{
    Task<IReadOnlyList<Ticket>> GetTicketsAsync(CancellationToken ct);
    Task<Ticket?> GetTicketAsync(Guid id, CancellationToken ct);
    Task<Ticket?> FindByChannelAsync(ulong channelId, CancellationToken ct);
    Task<Ticket> ReserveAsync(string panelId, ulong requesterId, string interactionId, DateTimeOffset now, CancellationToken ct);
    Task<Ticket?> FindInteractionAsync(string interactionId, CancellationToken ct);
    Task SaveAsync(Ticket ticket, string action, ulong? actorId, CancellationToken ct);
    Task<bool> IsChannelCreationNotSentAsync(Guid ticketId, Guid attemptId, CancellationToken ct) =>
        Task.FromResult(false);
    Task<bool> IsOpeningMessageNotSentAsync(Guid ticketId, Guid attemptId, CancellationToken ct) =>
        Task.FromResult(false);
    Task<bool> TryRecordDeliveryAsync(string key, CancellationToken ct);
    Task<DeliveryState> GetOrCreateDeliveryAsync(string key, string intent, CancellationToken ct);
    Task<IReadOnlyList<PendingDelivery>> GetPendingDeliveriesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PendingDelivery>>([]);
    // Called under the community lock to commit fresh authorization or a role plan before Discord mutations.
    Task UpdateCommunityDeliveryIntentAsync(string key, string intent, CancellationToken ct) =>
        throw new NotSupportedException("Community intent preparation is unavailable.");
    Task CompleteDeliveryAsync(string key, CancellationToken ct);
    Task<DateTimeOffset?> GetCooldownAsync(string key, CancellationToken ct);
    Task SetCooldownAsync(string key, DateTimeOffset at, CancellationToken ct);
    Task<bool> TryAdvanceCommunityActionAsync(string scope, ulong messageId, CancellationToken ct);
}
