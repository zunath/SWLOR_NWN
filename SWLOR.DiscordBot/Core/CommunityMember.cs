namespace SWLOR.DiscordBot.Core;

public sealed record CommunityMember(ulong UserId, string DisplayName, IReadOnlyCollection<ulong> RoleIds, bool IsBot = false, bool IsWebhook = false);
