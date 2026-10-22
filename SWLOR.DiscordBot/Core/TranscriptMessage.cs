namespace SWLOR.DiscordBot.Core;

public sealed record TranscriptMessage(ulong Id, ulong AuthorId, string AuthorName, string Content,
    DateTimeOffset Timestamp, IReadOnlyList<TranscriptAttachment> Attachments, string? EmbedsJson = null, string? MetadataJson = null);
