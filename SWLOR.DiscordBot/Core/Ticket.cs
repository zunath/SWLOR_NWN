namespace SWLOR.DiscordBot.Core;

public sealed record Ticket(Guid Id, string PanelId, ulong RequesterId, ulong? ChannelId,
    TicketState State, long Number, DateTimeOffset CreatedAt, DateTimeOffset? ClosedAt = null,
    DateTimeOffset? DeleteAfter = null, bool Hold = false, string? ArchivePath = null,
    DateTimeOffset? ArchiveExpiresAt = null, string? LastError = null, bool ArchiveComplete = false, string? ArchiveSnapshotPath = null);
