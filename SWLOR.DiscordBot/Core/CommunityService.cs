using System.Net;
using Discord;
using Discord.Net;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Discord;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SWLOR.DiscordBot.Core;

public sealed class CommunityService(BotConfiguration configuration, ITicketStore store, ICommunityDiscord discord,
    IResponseDeletionStore deletions, TimeProvider? timeProvider = null, ILogger<CommunityService>? logger = null)
{
    private TimeProvider Clock => timeProvider ?? TimeProvider.System;
    internal static readonly TimeSpan RecoveryInactivityTimeout = TimeSpan.FromMinutes(4);

    private CommunityDeliveryIntent CreateWelcomeIntent(ulong userId, string key)
    {
        var welcome = configuration.Welcome;
        var message = TemplateRenderer.EnforceMessageLimits(new CommunityMessage(
            TemplateRenderer.RenderWelcome(welcome.Template, userId, discord.ServerName, welcome.ChannelMentions),
            [], DeliveryKey: key));
        return new CommunityDeliveryIntent(1, "welcome", message, UserId: userId,
            ChannelId: welcome.DirectMessage ? null : welcome.ChannelId, DirectMessage: welcome.DirectMessage);
    }

    public async Task PersistWelcomeAsync(ulong userId, DateTimeOffset joinedAt, CancellationToken ct)
    {
        if (!configuration.Welcome.Enabled || userId == 0) return;
        var key = $"welcome:{userId}:{joinedAt.UtcTicks}";
        await store.PersistCommunityDeliveryAsync(key, JsonSerializer.Serialize(CreateWelcomeIntent(userId, key)), ct);
    }
    public async Task WelcomeAsync(ulong userId, DateTimeOffset joinedAt, CancellationToken ct)
    {
        var welcome = configuration.Welcome;
        if (!welcome.Enabled || userId == 0) return;
        var member = await discord.GetMemberAsync(userId, ct);
        if (member is null || member.IsBot || member.IsWebhook) return;
        var key = $"welcome:{userId}:{joinedAt.UtcTicks}";
        await using var session = await store.LockCommunityAsync(ct);
        var intent = CreateWelcomeIntent(userId, key);

        var delivery = await session.GetOrCreateDeliveryAsync(key, JsonSerializer.Serialize(intent), ct);
        if (delivery.Completed) return;
        if (!TryReadCurrentIntent(delivery.Intent, out var persistedIntent))
        {
            logger?.LogWarning("Skipped legacy welcome delivery {DeliveryKey}: its original destination was not persisted.", key);
            await session.CompleteDeliveryAsync(key, ct);
            return;
        }
        var persistedMessage = persistedIntent.Message
                               ?? throw new InvalidDataException("The persisted welcome message is missing.");
        if (!configuration.Welcome.Enabled || persistedIntent.UserId is not { } welcomeUserId ||
            !await IsCurrentMemberAsync(welcomeUserId, ct))
        {
            await session.CompleteDeliveryAsync(key, ct);
            return;
        }
        if (!persistedIntent.DirectMessage && persistedIntent.ChannelId is { } welcomeChannel)
            await discord.ValidateCommunityChannelAsync(welcomeChannel, false, persistedMessage.Embeds.Count > 0, ct);
        if (persistedIntent.DirectMessage)
            await SendWelcomeDirectMessageAsync(welcomeUserId, persistedMessage, ct);
        else
        {
            var sentMessageId = persistedIntent.ChannelId is { } destination && destination != 0
                ? await discord.SendAsync(destination, persistedMessage, ct) : null;
            if (sentMessageId is null) throw new InvalidOperationException("The welcome message was not delivered.");
        }
        await session.CompleteDeliveryAsync(key, ct);
    }

    private async Task SendWelcomeDirectMessageAsync(ulong userId, CommunityMessage message, CancellationToken ct)
    {
        try
        {
            if (await discord.SendDirectMessageAsync(userId, message, ct) is null)
                throw new InvalidOperationException("The welcome direct message was not delivered.");
        }
        catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden &&
                                       ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            ct.ThrowIfCancellationRequested();
            // Only the recipient refusal (50007) is terminal; other permission/network failures remain retryable.
            logger?.LogInformation("Skipped welcome direct message for member {MemberId}: the recipient does not accept messages.", userId);
        }
    }

    public async Task<int> RecoverPendingDeliveriesAsync(CancellationToken ct)
    {
        IReadOnlyList<PendingDelivery> pending;
        await using (var session = await store.LockCommunityAsync(ct))
            pending = await session.GetPendingDeliveriesAsync(ct);

        var recovered = 0;
        foreach (var item in pending.Take(20))
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                using var intentTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var intentCt = intentTimeout.Token;
                using var watchdog = Clock.CreateTimer(_ =>
                {
                    try { intentTimeout.Cancel(); } catch (ObjectDisposedException) { }
                }, null, RecoveryInactivityTimeout, Timeout.InfiniteTimeSpan);
                void Progress()
                {
                    intentCt.ThrowIfCancellationRequested();
                    watchdog.Change(RecoveryInactivityTimeout, Timeout.InfiniteTimeSpan);
                }
                bool deleteSource;
                ulong? deletionChannel = null;
                ulong? deletionMessage = null;
                await using (var session = await WithRecoveryProgressAsync(store.LockCommunityAsync(intentCt), Progress))
                {
                    var current = await WithRecoveryProgressAsync(session.GetOrCreateDeliveryAsync(item.Key, item.Intent, intentCt), Progress);
                    if (current.Completed) continue;
                    if (current.Intent == "suppressed")
                    {
                        await WithRecoveryProgressAsync(session.CompleteDeliveryAsync(item.Key, intentCt), Progress);
                        recovered++;
                        continue;
                    }
                    if (!TryReadCurrentIntent(current.Intent, out var intent) || intent.Message is null ||
                        intent.UserId is not { } userId || intent.ChannelId is 0 || intent.SourceMessageId is 0 ||
                        (intent.Kind is "answer" or "faction") &&
                        (intent.ChannelId is not > 0 || intent.SourceMessageId is not > 0))
                    {
                        logger?.LogWarning("Skipped legacy or invalid community delivery {DeliveryKey}: routing or actor context is unavailable.", item.Key);
                        await WithRecoveryProgressAsync(session.CompleteDeliveryAsync(item.Key, intentCt), Progress);
                        recovered++;
                        continue;
                    }
                    if (intent.Kind == "answer" && !HasUsableContent(intent.Message))
                    {
                        await WithRecoveryProgressAsync(session.CompleteDeliveryAsync(item.Key, intentCt), Progress);
                        recovered++;
                        continue;
                    }

                    deleteSource = false;
                    if (!await WithRecoveryProgressAsync(IsCurrentMemberAsync(userId, intentCt), Progress))
                    {
                        logger?.LogInformation("Completed pending community delivery {DeliveryKey} without sending because its actor is no longer a guild member.", item.Key);
                        await WithRecoveryProgressAsync(session.CompleteDeliveryAsync(item.Key, intentCt), Progress);
                        recovered++;
                        continue;
                    }

                    var scope = intent.Kind == "faction" ? $"faction:{userId}" : null;
                    if (scope is not null && !await WithRecoveryProgressAsync(session.TryAdvanceCommunityActionAsync(scope, intent.SourceMessageId!.Value, intentCt), Progress))
                    {
                        await WithRecoveryProgressAsync(SuppressDeliveryAsync(session, item.Key, intent, intentCt), Progress);
                        recovered++;
                        continue;
                    }

                    switch (intent.Kind)
                    {
                        case "welcome":
                            if (!configuration.Welcome.Enabled ||
                                (intent.DirectMessage ? !intent.UserId.HasValue : intent.ChannelId is not > 0))
                            {
                                await WithRecoveryProgressAsync(session.CompleteDeliveryAsync(item.Key, intentCt), Progress);
                                recovered++;
                                continue;
                            }
                            if (intent.DirectMessage)
                            {
                                await WithRecoveryProgressAsync(SendWelcomeDirectMessageAsync(userId, intent.Message, intentCt), Progress);
                            }
                            else if (intent.ChannelId is { } welcomeChannel)
                            {
                                await WithRecoveryProgressAsync(discord.ValidateCommunityChannelAsync(welcomeChannel, false,
                                    intent.Message.Embeds.Count > 0, intentCt), Progress);
                                if (await WithRecoveryProgressAsync(discord.SendAsync(welcomeChannel, intent.Message, intentCt), Progress) is null)
                                    throw new InvalidOperationException("The pending welcome message was not delivered.");
                            }
                            break;

                        case "answer":
                            var answer = (configuration.Answers ?? []).FirstOrDefault(x => x.Enabled &&
                                x.Name.Equals(intent.AnswerName, StringComparison.OrdinalIgnoreCase));
                            var member = await WithRecoveryProgressAsync(discord.GetMemberAsync(userId, intentCt), Progress);
                            if (answer is null || member is null || member.IsBot || member.IsWebhook ||
                                answer.AllowedChannelIds.Length > 0 && !answer.AllowedChannelIds.Contains(intent.ChannelId!.Value) ||
                                answer.AllowedRoleIds.Length > 0 && !answer.AllowedRoleIds.Any(member.RoleIds.Contains))
                            {
                                await WithRecoveryProgressAsync(session.CompleteDeliveryAsync(item.Key, intentCt), Progress);
                                recovered++;
                                continue;
                            }
                            var cooldownDuration = EffectiveAnswerCooldown(answer, intent);
                            if (cooldownDuration > TimeSpan.Zero && intent.CooldownKey is { } answerScope &&
                                !await WithRecoveryProgressAsync(session.TryAdvanceCommunityActionAsync(answerScope, intent.SourceMessageId!.Value, intentCt), Progress))
                            {
                                await WithRecoveryProgressAsync(SuppressDeliveryAsync(session, item.Key, intent, intentCt), Progress);
                                recovered++;
                                continue;
                            }
                            var lastDelivered = cooldownDuration > TimeSpan.Zero && intent.CooldownKey is { } recoveryCooldownKey
                                ? await WithRecoveryProgressAsync(session.GetCooldownAsync(recoveryCooldownKey, intentCt), Progress) : null;
                            if (lastDelivered is { } last && Clock.GetUtcNow() < last + cooldownDuration)
                            {
                                await WithRecoveryProgressAsync(SuppressDeliveryAsync(session, item.Key, intent, intentCt), Progress);
                                recovered++;
                                continue;
                            }
                            await WithRecoveryProgressAsync(discord.ValidateCommunityChannelAsync(intent.ChannelId!.Value,
                                intent.DeleteSource, intent.Message.Embeds.Count > 0, intentCt), Progress);
                            if (await WithRecoveryProgressAsync(discord.SendAsync(intent.ChannelId!.Value, intent.Message, intentCt), Progress) is null)
                                throw new InvalidOperationException("The pending quick answer response was not delivered.");
                            if (cooldownDuration > TimeSpan.Zero && intent.CooldownKey is { } cooldownKey)
                                await WithRecoveryProgressAsync(session.SetCooldownAsync(cooldownKey, Clock.GetUtcNow(), intentCt), Progress);
                            break;

                        case "faction":
                            var factions = configuration.Factions;
                            var faction = (factions.Roles ?? []).FirstOrDefault(x => x.RoleId == intent.FactionRoleId &&
                                x.Name.Equals(intent.FactionName, StringComparison.OrdinalIgnoreCase));
                            if (!factions.Enabled || faction is null || intent.RoleOperations is null)
                            {
                                await WithRecoveryProgressAsync(session.CompleteDeliveryAsync(item.Key, intentCt), Progress);
                                recovered++;
                                continue;
                            }
                            await WithRecoveryProgressAsync(discord.ValidateCommunityChannelAsync(intent.ChannelId!.Value,
                                intent.DeleteSource, intent.Message.Embeds.Count > 0, intentCt), Progress);
                            foreach (var operation in intent.RoleOperations)
                            {
                                var configuredRole = (factions.Roles ?? []).FirstOrDefault(x => x.RoleId == operation.RoleId);
                                var roleInfo = await WithRecoveryProgressAsync(discord.GetRoleAsync(operation.RoleId, intentCt), Progress);
                                if (configuredRole is null || roleInfo is null || roleInfo.IsManaged) continue;
                                await WithRecoveryProgressAsync(discord.ValidateCommunityChannelAsync(intent.ChannelId!.Value,
                                    intent.DeleteSource, intent.Message.Embeds.Count > 0, intentCt), Progress);
                                if (operation.Add) await WithRecoveryProgressAsync(discord.AddRoleAsync(userId, operation.RoleId, intentCt), Progress);
                                else await WithRecoveryProgressAsync(discord.RemoveRoleAsync(userId, operation.RoleId, intentCt), Progress);
                            }
                            if (await WithRecoveryProgressAsync(discord.SendAsync(intent.ChannelId!.Value, intent.Message, intentCt), Progress) is null)
                                throw new InvalidOperationException("The pending faction response was not delivered.");
                            break;

                        default:
                            logger?.LogWarning("Skipped pending community delivery {DeliveryKey}: unknown delivery kind.", item.Key);
                            await WithRecoveryProgressAsync(session.CompleteDeliveryAsync(item.Key, intentCt), Progress);
                            recovered++;
                            continue;
                    }

                    if (intent.DeleteSource && intent.ChannelId is { } sourceChannel && intent.SourceMessageId is { } sourceMessage)
                    {
                        await WithRecoveryProgressAsync(deletions.ScheduleDeletionAsync(sourceChannel, sourceMessage, Clock.GetUtcNow(), intentCt), Progress);
                        deleteSource = true;
                        deletionChannel = sourceChannel;
                        deletionMessage = sourceMessage;
                    }
                    await WithRecoveryProgressAsync(session.CompleteDeliveryAsync(item.Key, intentCt), Progress);
                    recovered++;
                }

                if (deleteSource && deletionChannel is { } channelToDelete && deletionMessage is { } messageToDelete)
                    await WithRecoveryProgressAsync(CompleteCommandDeletionAsync(channelToDelete, messageToDelete, intentCt), Progress);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger?.LogWarning("Community delivery recovery step failed for {DeliveryKey}: {ErrorKind}.",
                    item.Key, DiscordGateway.SafeError(ex));
            }
        }
        return recovered;
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
        var candidateMessage = TemplateRenderer.EnforceMessageLimits(new CommunityMessage(response, embeds, answer.DeleteResponseAfter, deliveryKey));
        var candidate = new CommunityDeliveryIntent(1, "answer", candidateMessage, UserId: userId,
            ChannelId: channelId, SourceMessageId: messageId, DeleteSource: answer.DeleteCommand,
            AnswerName: answer.Name, CooldownKey: cooldownKey, Cooldown: answer.Cooldown);
        var deleteSourceCommand = false;
        await using (var session = await store.LockCommunityAsync(ct))
        {
            var now = Clock.GetUtcNow();
            var lastDelivered = answer.Cooldown > TimeSpan.Zero ? await session.GetCooldownAsync(cooldownKey, ct) : null;
            if (answer.Cooldown > TimeSpan.Zero && lastDelivered is { } last && now < last + answer.Cooldown)
            {
                var suppressed = await session.GetOrCreateDeliveryAsync(deliveryKey, "suppressed", ct);
                if (!suppressed.Completed)
                {
                    TryReadCurrentIntent(suppressed.Intent, out var pendingIntent);
                    await SuppressDeliveryAsync(session, deliveryKey, pendingIntent, ct);
                }
                return;
            }
            var delivery = await session.GetOrCreateDeliveryAsync(deliveryKey, JsonSerializer.Serialize(candidate), ct);
            if (delivery.Intent == "suppressed") return;
            if (!TryReadCurrentIntent(delivery.Intent, out var persistedIntent))
            {
                if (!delivery.Completed)
                {
                    logger?.LogWarning("Skipped legacy quick answer delivery {DeliveryKey}: its actor and deletion context were not persisted.", deliveryKey);
                    await session.CompleteDeliveryAsync(deliveryKey, ct);
                }
                return;
            }
            var persistedMessage = persistedIntent.Message
                                   ?? throw new InvalidDataException("The persisted quick answer message is missing.");
            deleteSourceCommand = persistedIntent.DeleteSource;
            if (!HasUsableContent(persistedMessage))
            {
                if (!delivery.Completed) await session.CompleteDeliveryAsync(deliveryKey, ct);
                return;
            }
            if (!delivery.Completed)
            {
                var cooldownDuration = EffectiveAnswerCooldown(answer, persistedIntent);
                if (cooldownDuration > TimeSpan.Zero && persistedIntent.CooldownKey is { } scope &&
                    !await session.TryAdvanceCommunityActionAsync(scope, persistedIntent.SourceMessageId!.Value, ct))
                {
                    await SuppressDeliveryAsync(session, deliveryKey, persistedIntent, ct);
                    return;
                }
                var lastPersistedDelivery = cooldownDuration > TimeSpan.Zero && persistedIntent.CooldownKey is { } retryCooldownKey
                    ? await session.GetCooldownAsync(retryCooldownKey, ct) : null;
                if (lastPersistedDelivery is { } deliveredAt && now < deliveredAt + cooldownDuration)
                {
                    await SuppressDeliveryAsync(session, deliveryKey, persistedIntent, ct);
                    return;
                }
                if (persistedIntent.ChannelId is { } answerChannel)
                    await discord.ValidateCommunityChannelAsync(answerChannel, persistedIntent.DeleteSource, persistedMessage.Embeds.Count > 0, ct);
                if (persistedIntent.ChannelId is not { } destination || destination == 0 ||
                    await discord.SendAsync(destination, persistedMessage, ct) is null)
                    throw new InvalidOperationException("The quick answer response was not delivered.");
                if (cooldownDuration > TimeSpan.Zero && persistedIntent.CooldownKey is { } persistedCooldownKey)
                    await session.SetCooldownAsync(persistedCooldownKey, Clock.GetUtcNow(), ct);
                // Commit cleanup ownership before acknowledging delivery so a shutdown after this point
                // does not depend on Discord replaying the source Gateway event.
                if (persistedIntent.DeleteSource && persistedIntent.ChannelId is { } sourceChannel && persistedIntent.SourceMessageId is { } sourceMessage)
                    await deletions.ScheduleDeletionAsync(sourceChannel, sourceMessage, Clock.GetUtcNow(), ct);
                await session.CompleteDeliveryAsync(deliveryKey, ct);
            }
        }
        if (deleteSourceCommand) await CompleteCommandDeletionAsync(channelId, messageId, ct);
    }

    private static TimeSpan EffectiveAnswerCooldown(QuickAnswerOptions answer, CommunityDeliveryIntent intent) =>
        answer.Cooldown > (intent.Cooldown ?? TimeSpan.Zero) ? answer.Cooldown : intent.Cooldown ?? TimeSpan.Zero;

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
        var deleteSourceCommand = false;
        await using (var session = await store.LockCommunityAsync(ct))
        {
            var currentMember = await discord.GetMemberAsync(member.UserId, ct);
            if (currentMember is null || currentMember.IsBot || currentMember.IsWebhook) return;
            var candidate = CreateFactionIntent(faction, currentMember, factions);
            var response = TemplateRenderer.EnforceMessageLimits(new CommunityMessage(
                candidate.Response, [], DeleteAfter: candidate.ResponseDeleteAfter ?? (factions.DeleteResponse ? TimeSpan.FromSeconds(5) : null),
                DeliveryKey: $"{deliveryKey}:response"));
            var persistedCandidate = new CommunityDeliveryIntent(1, "faction", response,
                UserId: currentMember.UserId, ChannelId: channelId, SourceMessageId: messageId,
                DeleteSource: factions.DeleteCommand, FactionName: faction.Name, FactionRoleId: faction.RoleId,
                RoleOperations: candidate.RoleOperations);
            var delivery = await session.GetOrCreateDeliveryAsync(deliveryKey, JsonSerializer.Serialize(persistedCandidate), ct);
            if (!TryReadCurrentIntent(delivery.Intent, out var intent))
            {
                if (!delivery.Completed)
                {
                    logger?.LogWarning("Skipped legacy faction delivery {DeliveryKey}: its actor and response route were not persisted.", deliveryKey);
                    await session.CompleteDeliveryAsync(deliveryKey, ct);
                }
                return;
            }
            if (intent.Message is null || intent.RoleOperations is null)
                throw new InvalidDataException("The persisted faction delivery is invalid.");
            deleteSourceCommand = intent.DeleteSource;

            // Add/remove operations repeat safely after transient failures because the persisted intent fixes
            // the desired state for this message instead of re-evaluating the toggle against changed roles.
            if (!delivery.Completed)
            {
                if (!await session.TryAdvanceCommunityActionAsync($"faction:{intent.UserId!.Value}", intent.SourceMessageId!.Value, ct))
                {
                    await SuppressDeliveryAsync(session, deliveryKey, intent, ct);
                    return;
                }
                await discord.ValidateCommunityChannelAsync(intent.ChannelId!.Value,
                    intent.DeleteSource, intent.Message.Embeds.Count > 0, ct);
                foreach (var operation in intent.RoleOperations)
                {
                    var configuredRole = (factions.Roles ?? []).FirstOrDefault(x => x.RoleId == operation.RoleId);
                    var info = await discord.GetRoleAsync(operation.RoleId, ct);
                    if (configuredRole is null || info is null || info.IsManaged) continue;
                    await discord.ValidateCommunityChannelAsync(intent.ChannelId!.Value,
                        intent.DeleteSource, intent.Message.Embeds.Count > 0, ct);
                    if (operation.Add) await discord.AddRoleAsync(intent.UserId!.Value, operation.RoleId, ct);
                    else await discord.RemoveRoleAsync(intent.UserId!.Value, operation.RoleId, ct);
                }
                if (intent.ChannelId is not { } responseChannel || responseChannel == 0 ||
                    await discord.SendAsync(responseChannel, intent.Message, ct) is null)
                    throw new InvalidOperationException("The faction response was not delivered.");
                if (intent.DeleteSource && intent.ChannelId is { } sourceChannel && intent.SourceMessageId is { } sourceMessage)
                    await deletions.ScheduleDeletionAsync(sourceChannel, sourceMessage, Clock.GetUtcNow(), ct);
                await session.CompleteDeliveryAsync(deliveryKey, ct);
            }
        }
        if (deleteSourceCommand) await CompleteCommandDeletionAsync(channelId, messageId, ct);
    }

    private async Task SuppressDeliveryAsync(ITicketSession session, string key, CommunityDeliveryIntent? intent, CancellationToken ct)
    {
        if (intent is { DeleteSource: true, ChannelId: > 0, SourceMessageId: > 0 })
            await deletions.ScheduleDeletionAsync(intent.ChannelId.Value, intent.SourceMessageId.Value, Clock.GetUtcNow(), ct);
        await session.CompleteDeliveryAsync(key, ct);
    }

    private static async Task<T> WithRecoveryProgressAsync<T>(Task<T> operation, Action progress)
    {
        var result = await operation;
        progress();
        return result;
    }

    private static async Task WithRecoveryProgressAsync(Task operation, Action progress)
    {
        await operation;
        progress();
    }

    private async Task CompleteCommandDeletionAsync(ulong channelId, ulong messageId, CancellationToken ct)
    {
        // Preserve immediate cleanup. Any interruption leaves the durable queue record for the worker,
        // independently of the completed response and the Gateway's bounded event retries.
        await discord.DeleteMessageAsync(channelId, messageId, ct);
        await deletions.CompleteDeletionAsync(channelId, messageId, ct);
    }

    private async Task<bool> IsCurrentMemberAsync(ulong userId, CancellationToken ct)
    {
        var member = await discord.GetMemberAsync(userId, ct);
        return member is { IsBot: false, IsWebhook: false };
    }

    private static bool TryReadCurrentIntent(string serialized, out CommunityDeliveryIntent intent)
    {
        intent = null!;
        try
        {
            var parsed = JsonSerializer.Deserialize<CommunityDeliveryIntent>(serialized);
            if (parsed is not { Version: 1 } || parsed.Kind is not ("welcome" or "answer" or "faction")) return false;
            intent = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
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

    private sealed record CommunityDeliveryIntent(int Version, string Kind, CommunityMessage? Message,
        ulong? ChannelId = null, ulong? UserId = null, ulong? SourceMessageId = null,
        bool DeleteSource = false, bool DirectMessage = false, string? AnswerName = null,
        string? CooldownKey = null, TimeSpan? Cooldown = null, string? FactionName = null,
        ulong? FactionRoleId = null, IReadOnlyList<FactionRoleOperation>? RoleOperations = null);
    private sealed record FactionRoleOperation(ulong RoleId, bool Add);
    private sealed record FactionIntent(IReadOnlyList<FactionRoleOperation> RoleOperations, string Response, TimeSpan? ResponseDeleteAfter = null);
}
