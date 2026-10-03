using SWLOR.DiscordBot.Configuration;
using System.Text.Json;

namespace SWLOR.DiscordBot.Core;

public sealed record CommunityMember(ulong UserId, string DisplayName, IReadOnlyCollection<ulong> RoleIds, bool IsBot = false, bool IsWebhook = false);
public sealed record CommunityRole(ulong RoleId, int Position, bool IsManaged = false);
public sealed record CommunityEmbedField(string Name, string Value, bool Inline = false);
public sealed record CommunityEmbed(string? Title, string? Description, string? Url, uint Color, IReadOnlyList<CommunityEmbedField> Fields);
public sealed record CommunityMessage(string Content, IReadOnlyList<CommunityEmbed> Embeds, TimeSpan? DeleteAfter = null, string? DeliveryKey = null);

public interface ICommunityDiscord
{
    string ServerName { get; }
    Task<CommunityMember?> GetMemberAsync(ulong userId, CancellationToken ct);
    Task<CommunityRole?> GetRoleAsync(ulong roleId, CancellationToken ct);
    Task AddRoleAsync(ulong userId, ulong roleId, CancellationToken ct);
    Task RemoveRoleAsync(ulong userId, ulong roleId, CancellationToken ct);
    Task<ulong?> SendAsync(ulong channelId, CommunityMessage message, CancellationToken ct);
    Task<ulong?> SendDirectMessageAsync(ulong userId, CommunityMessage message, CancellationToken ct);
    Task DeleteMessageAsync(ulong channelId, ulong messageId, CancellationToken ct);
}

public sealed class CommunityService(BotConfiguration configuration, ITicketStore store, ICommunityDiscord discord, TimeProvider? timeProvider = null)
{
    private TimeProvider Clock => timeProvider ?? TimeProvider.System;

    public async Task WelcomeAsync(ulong userId, DateTimeOffset joinedAt, CancellationToken ct)
    {
        var welcome = configuration.Welcome;
        if (!welcome.Enabled || userId == 0) return;
        var member = await discord.GetMemberAsync(userId, ct);
        if (member is null || member.IsBot || member.IsWebhook) return;
        var key = $"welcome:{userId}:{joinedAt.UtcTicks}";
        await using var session = await store.LockAsync(ct);
        var message = TemplateRenderer.EnforceMessageLimits(new CommunityMessage(
            TemplateRenderer.RenderWelcome(welcome.Template, userId, discord.ServerName, welcome.ChannelMentions),
            [], DeliveryKey: key));
        var delivery = await session.GetOrCreateDeliveryAsync(key, JsonSerializer.Serialize(message), ct);
        if (delivery.Completed) return;
        var persistedMessage = JsonSerializer.Deserialize<CommunityMessage>(delivery.Intent)
                               ?? throw new InvalidDataException("The persisted welcome delivery is invalid.");
        var sentMessageId = welcome.DirectMessage
            ? await discord.SendDirectMessageAsync(userId, persistedMessage, ct)
            : welcome.ChannelId != 0 ? await discord.SendAsync(welcome.ChannelId, persistedMessage, ct) : null;
        if (sentMessageId is null) throw new InvalidOperationException("The welcome message was not delivered.");
        await session.CompleteDeliveryAsync(key, ct);
    }

    public async Task ExecuteAsync(ulong userId, ulong channelId, ulong messageId, string content, CancellationToken ct)
    {
        if (userId == 0 || channelId == 0 || messageId == 0 || string.IsNullOrWhiteSpace(content)) return;
        var member = await discord.GetMemberAsync(userId, ct);
        if (member is null || member.IsBot || member.IsWebhook) return;
        var body = content.TrimStart();
        if (!body.StartsWith(configuration.Prefix, StringComparison.Ordinal)) return;
        var command = body[configuration.Prefix.Length..].TrimStart();
        if (command.Length == 0) return;

        var faction = FindFaction(command);
        if (faction is not null)
        {
            await HandleFactionAsync(faction, member, channelId, messageId, ct);
            return;
        }

        var tokenLength = 0;
        while (tokenLength < command.Length && !char.IsWhiteSpace(command[tokenLength])) tokenLength++;
        var commandName = command[..tokenLength];
        // Consume only the token separator; preserve the argument tail exactly for {args}.
        var rawArguments = tokenLength < command.Length ? command[(tokenLength + 1)..] : "";
        var answer = (configuration.Answers ?? []).FirstOrDefault(x => x.Enabled && x.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
        if (answer is null || answer.AllowedChannelIds.Length > 0 && !answer.AllowedChannelIds.Contains(channelId) ||
            answer.AllowedRoleIds.Length > 0 && !answer.AllowedRoleIds.Any(member.RoleIds.Contains)) return;

        var arguments = rawArguments.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var deliveryKey = $"answer:{channelId}:{messageId}";
        var cooldownKey = $"answer-cooldown:{answer.Name.ToLowerInvariant()}:{userId}";
        var texts = answer.Responses ?? [];
        var response = texts.Length == 0 ? "" : TemplateRenderer.RenderAnswer(texts[Random.Shared.Next(texts.Length)], userId, discord.ServerName, arguments, rawArguments);
        var embeds = (answer.Embeds ?? []).Select(embed => TemplateRenderer.RenderEmbed(embed, userId, discord.ServerName, arguments, rawArguments)).ToArray();
        var candidate = TemplateRenderer.EnforceMessageLimits(new CommunityMessage(response, embeds, answer.DeleteResponseAfter, deliveryKey));
        await using (var session = await store.LockAsync(ct))
        {
            var now = Clock.GetUtcNow();
            var lastDelivered = answer.Cooldown > TimeSpan.Zero ? await session.GetCooldownAsync(cooldownKey, ct) : null;
            if (answer.Cooldown > TimeSpan.Zero && lastDelivered is { } last && now < last + answer.Cooldown)
            {
                var suppressed = await session.GetOrCreateDeliveryAsync(deliveryKey, "suppressed", ct);
                if (suppressed.Intent == "suppressed")
                {
                    if (!suppressed.Completed) await session.CompleteDeliveryAsync(deliveryKey, ct);
                    return;
                }
            }
            var delivery = await session.GetOrCreateDeliveryAsync(deliveryKey, JsonSerializer.Serialize(candidate), ct);
            if (delivery.Intent == "suppressed") return;
            if (!delivery.Completed)
            {
                var persistedMessage = JsonSerializer.Deserialize<CommunityMessage>(delivery.Intent)
                                       ?? throw new InvalidDataException("The persisted quick answer delivery is invalid.");
                if (HasUsableContent(persistedMessage))
                {
                    if (await discord.SendAsync(channelId, persistedMessage, ct) is null)
                        throw new InvalidOperationException("The quick answer response was not delivered.");
                    if (answer.Cooldown > TimeSpan.Zero) await session.SetCooldownAsync(cooldownKey, Clock.GetUtcNow(), ct);
                }
                await session.CompleteDeliveryAsync(deliveryKey, ct);
            }
        }
        if (answer.DeleteCommand) await CompleteCommandDeletionAsync(channelId, messageId, $"{deliveryKey}:delete", ct);
    }

    private FactionRole? FindFaction(string command)
    {
        if (!configuration.Factions.Enabled) return null;
        var name = command.Trim();
        var separator = name.IndexOfAny([' ', '\t', '\r', '\n']);
        if (separator <= 0 || !name[..separator].Equals("rank", StringComparison.OrdinalIgnoreCase)) return null;
        name = name[(separator + 1)..].Trim();
        if (name.Length == 0) return null;
        return (configuration.Factions.Roles ?? []).FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task HandleFactionAsync(FactionRole faction, CommunityMember member, ulong channelId, ulong messageId, CancellationToken ct)
    {
        var factions = configuration.Factions;
        var roleInfo = await discord.GetRoleAsync(faction.RoleId, ct);
        if (roleInfo is null || roleInfo.IsManaged) return;
        // The adapter checks the bot's current permissions and hierarchy again before each role mutation.
        var deliveryKey = $"faction:{channelId}:{messageId}";
        await using (var session = await store.LockAsync(ct))
        {
            var currentMember = await discord.GetMemberAsync(member.UserId, ct);
            if (currentMember is null || currentMember.IsBot || currentMember.IsWebhook) return;
            var candidate = CreateFactionIntent(faction, currentMember, factions);
            var delivery = await session.GetOrCreateDeliveryAsync(deliveryKey, JsonSerializer.Serialize(candidate), ct);
            var intent = JsonSerializer.Deserialize<FactionIntent>(delivery.Intent)
                         ?? throw new InvalidDataException("The persisted faction delivery is invalid.");

            // Add/remove operations repeat safely after transient failures because the persisted intent fixes
            // the desired state for this message instead of re-evaluating the toggle against changed roles.
            if (!delivery.Completed)
            {
                foreach (var operation in intent.RoleOperations)
                {
                    var configuredRole = factions.Roles.FirstOrDefault(x => x.RoleId == operation.RoleId);
                    var info = await discord.GetRoleAsync(operation.RoleId, ct);
                    if (configuredRole is null || info is null || info.IsManaged) continue;
                    if (operation.Add) await discord.AddRoleAsync(currentMember.UserId, operation.RoleId, ct);
                    else await discord.RemoveRoleAsync(currentMember.UserId, operation.RoleId, ct);
                }
                var response = TemplateRenderer.EnforceMessageLimits(new CommunityMessage(
                    intent.Response,
                    [],
                    DeleteAfter: intent.ResponseDeleteAfter ?? (factions.DeleteResponse ? TimeSpan.FromSeconds(5) : null),
                    DeliveryKey: $"{deliveryKey}:response"));
                if (await discord.SendAsync(channelId, response, ct) is null)
                    throw new InvalidOperationException("The faction response was not delivered.");
                await session.CompleteDeliveryAsync(deliveryKey, ct);
            }
        }
        if (factions.DeleteCommand) await CompleteCommandDeletionAsync(channelId, messageId, $"{deliveryKey}:delete", ct);
    }

    private async Task CompleteCommandDeletionAsync(ulong channelId, ulong messageId, string deliveryKey, CancellationToken ct)
    {
        await using var session = await store.LockAsync(ct);
        var delivery = await session.GetOrCreateDeliveryAsync(deliveryKey, "delete-command", ct);
        if (delivery.Completed) return;
        await discord.DeleteMessageAsync(channelId, messageId, ct);
        await session.CompleteDeliveryAsync(deliveryKey, ct);
    }

    private static bool HasUsableContent(CommunityMessage message) =>
        !string.IsNullOrWhiteSpace(message.Content) ||
        message.Embeds.Any(embed =>
            !string.IsNullOrWhiteSpace(embed.Title) ||
            !string.IsNullOrWhiteSpace(embed.Description) ||
            embed.Fields.Any(field => !string.IsNullOrWhiteSpace(field.Name) && !string.IsNullOrWhiteSpace(field.Value)));

    private static FactionIntent CreateFactionIntent(FactionRole faction, CommunityMember member, FactionOptions options)
    {
        var roleIds = member.RoleIds.ToHashSet();
        var operations = new List<FactionRoleOperation>();
        string response;
        if (roleIds.Contains(faction.RoleId) && string.Equals(options.Behavior, "toggle", StringComparison.OrdinalIgnoreCase))
        {
            operations.Add(new FactionRoleOperation(faction.RoleId, Add: false));
            response = $"Removed the {faction.Name} role.";
        }
        else if (roleIds.Contains(faction.RoleId))
        {
            response = $"You already have the {faction.Name} role.";
        }
        else
        {
            if (options.Exclusive == true)
                operations.AddRange(options.Roles.Where(x => x.RoleId != faction.RoleId && roleIds.Contains(x.RoleId))
                    .Select(x => new FactionRoleOperation(x.RoleId, Add: false)));
            operations.Add(new FactionRoleOperation(faction.RoleId, Add: true));
            response = $"Added the {faction.Name} role.";
        }
        return new FactionIntent(operations, response, options.DeleteResponse ? TimeSpan.FromSeconds(5) : null);
    }

    private sealed record FactionRoleOperation(ulong RoleId, bool Add);
    private sealed record FactionIntent(IReadOnlyList<FactionRoleOperation> RoleOperations, string Response, TimeSpan? ResponseDeleteAfter = null);
}
