namespace SWLOR.DiscordBot.Core;

public sealed record CommunityEmbed(string? Title, string? Description, string? Url, uint Color, IReadOnlyList<CommunityEmbedField> Fields);
