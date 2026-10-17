namespace SWLOR.DiscordBot.Core;

public sealed record CommunityRole(ulong RoleId, int Position, bool IsManaged = false);
