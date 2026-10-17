namespace SWLOR.DiscordBot.Core;

public sealed record Actor(ulong UserId, IReadOnlyCollection<ulong> RoleIds, bool IsGuildOwner = false);
