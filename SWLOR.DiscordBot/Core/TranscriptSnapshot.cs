namespace SWLOR.DiscordBot.Core;

public sealed record TranscriptSnapshot(IReadOnlyList<TranscriptMessage> Messages, ulong? LastMessageId);
