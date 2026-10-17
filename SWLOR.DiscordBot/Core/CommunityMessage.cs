namespace SWLOR.DiscordBot.Core;

public sealed record CommunityMessage(string Content, IReadOnlyList<CommunityEmbed> Embeds, TimeSpan? DeleteAfter = null, string? DeliveryKey = null);
