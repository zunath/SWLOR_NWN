namespace SWLOR.DiscordBot.Core;

public interface ICommunityDiscord
{
    string ServerName { get; }
    Task<CommunityMember?> GetMemberAsync(ulong userId, CancellationToken ct);
    Task<CommunityRole?> GetRoleAsync(ulong roleId, CancellationToken ct);
    Task ValidateCommunityChannelAsync(ulong channelId, bool deleteSource, bool requireEmbeds, CancellationToken ct) =>
        throw new NotSupportedException("Fresh community channel permission validation is unavailable.");
    Task AddRoleAsync(ulong userId, ulong roleId, CancellationToken ct);
    Task RemoveRoleAsync(ulong userId, ulong roleId, CancellationToken ct);
    Task<ulong?> SendAsync(ulong channelId, CommunityMessage message, CancellationToken ct);
    Task<ulong?> SendDirectMessageAsync(ulong userId, CommunityMessage message, CancellationToken ct);
    Task DeleteMessageAsync(ulong channelId, ulong messageId, CancellationToken ct);
}
