namespace SWLOR.DiscordBot.Core;

public sealed record PendingResponseDeletion(ulong ChannelId, ulong MessageId, DateTimeOffset DueAt,
    int Attempts = 0, string? LastError = null);
