using System.Net;
using System.Text.Json;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Hosting;

namespace SWLOR.DiscordBot.Discord;

public sealed class DiscordOperations(DiscordSocketClient client, BotConfiguration configuration, ResponseDeletionQueue responseDeletions, DiscordCommunityPoster poster, ITicketStore store) : IDiscordTickets, ICommunityDiscord
{
    public string ServerName => client.GetGuild(configuration.GuildId)?.Name ?? "SWLOR";
    private static string Topic(Guid id) => $"swlor-ticket:{id:D}";
    internal static RequestOptions Options(CancellationToken ct) => new()
    {
        CancelToken = ct, Timeout = 30000,
        RetryMode = RetryMode.RetryRatelimit | RetryMode.Retry502,
        AuditLogReason = "SWLOR support bot"
    };

    internal static RequestOptions PostingOptions(CancellationToken ct)
    {
        var options = Options(ct);
        options.RetryMode = RetryMode.RetryRatelimit;
        return options;
    }

    internal async Task<RestGuild> GuildAsync(CancellationToken ct) =>
        (RestGuild)(await ((IDiscordClient)client.Rest).GetGuildAsync(configuration.GuildId, options: Options(ct))
            ?? throw new InvalidOperationException("The configured guild is unavailable."));

    public async Task<Actor> ActorAsync(ulong userId, CancellationToken ct)
    {
        var guild = await GuildAsync(ct);
        var member = await guild.GetUserAsync(userId, Options(ct))
            ?? throw new InvalidOperationException("You must be a current member of the configured guild.");
        return new Actor(member.Id, member.RoleIds, guild.OwnerId == member.Id);
    }

    internal async Task<RestTextChannel> TextChannelAsync(ulong id, CancellationToken ct)
    {
        var channel = await client.Rest.GetChannelAsync(id, Options(ct));
        return channel is RestTextChannel text && text.GuildId == configuration.GuildId && text.ChannelType == ChannelType.Text
            ? text : throw new InvalidOperationException("The configured channel is not a text channel in this guild.");
    }

    private async Task<RestTextChannel?> ManagedChannelAsync(Ticket ticket, CancellationToken ct)
    {
        if (!ticket.ChannelId.HasValue) return null;
        try
        {
            var channel = await client.Rest.GetChannelAsync(ticket.ChannelId.Value, Options(ct));
            if (channel is null) return null;
            if (channel is not RestTextChannel text || text.GuildId != configuration.GuildId || text.ChannelType != ChannelType.Text || text.Topic != Topic(ticket.Id))
                throw new InvalidOperationException("The ticket channel no longer has its managed identity; no mutation was performed.");
            return text;
        }
        catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { return null; }
    }

    private async Task<RestTextChannel> RequireManagedAsync(Ticket ticket, CancellationToken ct) =>
        await ManagedChannelAsync(ticket, ct) ?? throw new InvalidOperationException("The ticket channel no longer exists.");

    public async Task<ulong?> FindManagedChannelAsync(Guid ticketId, CancellationToken ct)
    {
        var channels = await (await GuildAsync(ct)).GetTextChannelsAsync(Options(ct));
        var matches = channels.Where(x => x.ChannelType == ChannelType.Text && x.Topic == Topic(ticketId)).ToArray();
        if (matches.Length > 1) throw new InvalidOperationException("Multiple channels claim the same ticket identity; staff must review them.");
        return matches.SingleOrDefault()?.Id;
    }

    private async Task<RestCategoryChannel> SelectCategoryAsync(Ticket ticket, CancellationToken ct)
    {
        var guild = await GuildAsync(ct);
        var channels = await guild.GetChannelsAsync(Options(ct));
        var panel = configuration.Tickets.Panels.Single(x => x.Id == ticket.PanelId);
        foreach (var id in panel.OpenCategoryIds)
        {
            var category = channels.OfType<RestCategoryChannel>().SingleOrDefault(x => x.Id == id)
                ?? throw new InvalidOperationException("A configured open category is unavailable.");
            if (channels.OfType<INestedChannel>().Count(x => x.CategoryId == id && x.Id != ticket.ChannelId) < 50) return category;
        }
        throw new InvalidOperationException("All configured ticket categories are full.");
    }

    internal static Overwrite[] BuildOverwrites(ulong guildId, ulong botId, ulong requesterId, IReadOnlyCollection<ulong> supportRoles,
        IEnumerable<Overwrite> categoryOverwrites, bool requesterRead, bool requesterWrite, bool supportWrite)
    {
        var result = new Dictionary<(ulong, PermissionTarget), Overwrite>();
        void Put(ulong id, PermissionTarget target, OverwritePermissions permission) => result[(id, target)] = new(id, target, permission);
        var deny = new OverwritePermissions(viewChannel: PermValue.Deny, sendMessages: PermValue.Deny,
            addReactions: PermValue.Deny, createPublicThreads: PermValue.Deny, createPrivateThreads: PermValue.Deny,
            sendMessagesInThreads: PermValue.Deny);
        Put(guildId, PermissionTarget.Role, deny);
        // Explicitly replace inherited visibility grants before the channel can become visible.
        foreach (var overwrite in categoryOverwrites.Where(x => x.TargetType == PermissionTarget.Role && x.Permissions.ViewChannel == PermValue.Allow))
            Put(overwrite.TargetId, PermissionTarget.Role, deny);
        foreach (var role in supportRoles)
            Put(role, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Allow, readMessageHistory: PermValue.Allow,
                sendMessages: supportWrite ? PermValue.Allow : PermValue.Deny, attachFiles: supportWrite ? PermValue.Allow : PermValue.Deny,
                addReactions: supportWrite ? PermValue.Allow : PermValue.Deny, createPublicThreads: PermValue.Deny,
                createPrivateThreads: PermValue.Deny, sendMessagesInThreads: PermValue.Deny));
        Put(requesterId, PermissionTarget.User, new OverwritePermissions(viewChannel: requesterRead ? PermValue.Allow : PermValue.Deny,
            readMessageHistory: requesterRead ? PermValue.Allow : PermValue.Deny, sendMessages: requesterWrite ? PermValue.Allow : PermValue.Deny,
            attachFiles: requesterWrite ? PermValue.Allow : PermValue.Deny, addReactions: requesterWrite ? PermValue.Allow : PermValue.Deny,
            createPublicThreads: PermValue.Deny, createPrivateThreads: PermValue.Deny, sendMessagesInThreads: PermValue.Deny));
        Put(botId, PermissionTarget.User, new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow,
            readMessageHistory: PermValue.Allow, manageChannel: PermValue.Allow, manageMessages: PermValue.Allow,
            attachFiles: PermValue.Allow, embedLinks: PermValue.Allow));
        if (result.Count > 100) throw new InvalidOperationException("Ticket privacy requires more permission overwrites than Discord permits.");
        return result.Values.ToArray();
    }

    private async Task VerifyPrivacyAsync(Ticket ticket, bool requesterRead, bool requesterWrite, bool supportWrite, CancellationToken ct)
    {
        var channel = await RequireManagedAsync(ticket, ct);
        var roles = configuration.Tickets.SupportRoleIds.ToHashSet();
        var everyone = channel.PermissionOverwrites.SingleOrDefault(x => x.TargetType == PermissionTarget.Role && x.TargetId == configuration.GuildId);
        if (everyone.Permissions.ViewChannel != PermValue.Deny)
            throw new InvalidOperationException("Ticket privacy validation failed for the everyone role.");
        foreach (var item in channel.PermissionOverwrites)
        {
            var allowed = item.TargetType == PermissionTarget.Role ? roles.Contains(item.TargetId) :
                item.TargetId == client.CurrentUser.Id || (item.TargetId == ticket.RequesterId && requesterRead);
            if (!allowed && item.Permissions.ViewChannel == PermValue.Allow)
                throw new InvalidOperationException("Ticket privacy validation found an unexpected visibility grant.");
            if (item.TargetType == PermissionTarget.Role && roles.Contains(item.TargetId) && !supportWrite && item.Permissions.SendMessages != PermValue.Deny)
                throw new InvalidOperationException("Ticket freeze validation failed for support.");
        }
        var owner = channel.PermissionOverwrites.Single(x => x.TargetType == PermissionTarget.User && x.TargetId == ticket.RequesterId);
        if (owner.Permissions.ViewChannel != (requesterRead ? PermValue.Allow : PermValue.Deny) ||
            owner.Permissions.SendMessages != (requesterWrite ? PermValue.Allow : PermValue.Deny))
            throw new InvalidOperationException("Ticket requester permissions failed verification.");
        var guild = await GuildAsync(ct);
        var bot = await guild.GetUserAsync(client.CurrentUser.Id, Options(ct));
        if (bot is null || !bot.GetPermissions(channel).ViewChannel || !bot.GetPermissions(channel).ReadMessageHistory)
            throw new InvalidOperationException("The bot cannot read the ticket it manages.");
    }

    public async Task<ulong> CreateAsync(Ticket ticket, CancellationToken ct)
    {
        var category = await SelectCategoryAsync(ticket, ct);
        var guild = await GuildAsync(ct);
        var overwrites = BuildOverwrites(guild.Id, client.CurrentUser.Id, ticket.RequesterId, configuration.Tickets.SupportRoleIds,
            category.PermissionOverwrites, true, true, true);
        // Rate-limit retries are safe; timeout/502 retries could create a second channel after an ambiguous response.
        var createOptions = Options(ct);
        createOptions.RetryMode = RetryMode.RetryRatelimit;
        var channel = await guild.CreateTextChannelAsync($"ticket-{ticket.Number:D4}", properties =>
        {
            properties.CategoryId = category.Id;
            properties.Topic = Topic(ticket.Id);
            properties.PermissionOverwrites = overwrites;
        }, createOptions);
        await VerifyPrivacyAsync(ticket with { ChannelId = channel.Id }, true, true, true, ct);
        return channel.Id;
    }

    public async Task OpenAsync(Ticket ticket, bool sendOpeningMessage, CancellationToken ct)
    {
        var category = await SelectCategoryAsync(ticket, ct);
        var channel = await RequireManagedAsync(ticket, ct);
        var overwrites = BuildOverwrites(configuration.GuildId, client.CurrentUser.Id, ticket.RequesterId, configuration.Tickets.SupportRoleIds,
            category.PermissionOverwrites, true, true, true);
        await channel.ModifyAsync(x => { x.CategoryId = category.Id; x.PermissionOverwrites = overwrites; }, Options(ct));
        await VerifyPrivacyAsync(ticket, true, true, true, ct);
        if (sendOpeningMessage && !await HasOpeningMessageAsync(ticket, channel, ct))
        {
            var panel = configuration.Tickets.Panels.Single(x => x.Id == ticket.PanelId);
            var text = TemplateRenderer.RenderTicket(panel.OpeningMessage, ticket.RequesterId, ServerName);
            if (string.IsNullOrWhiteSpace(text)) text = $"Ticket #{ticket.Number} is open. Tell support how we can help.";
            await channel.SendMessageAsync(text, allowedMentions: AllowedMentions.None,
                components: new ComponentBuilder().WithButton("Close ticket", $"v1:close:{ticket.Id:D}", ButtonStyle.Danger).Build(), options: PostingOptions(ct));
        }
    }

    private async Task<bool> HasOpeningMessageAsync(Ticket ticket, RestTextChannel channel, CancellationToken ct)
    {
        ulong? before = null;
        while (true)
        {
            var messages = await (before.HasValue ? channel.GetMessagesAsync(before.Value, Direction.Before, 100, Options(ct)) :
                channel.GetMessagesAsync(100, Options(ct))).FlattenAsync();
            if (!messages.Any()) return false;
            if (messages.Any(message => message.Author.Id == client.CurrentUser.Id &&
                message.Components.OfType<ActionRowComponent>().SelectMany(row => row.Components).OfType<ButtonComponent>()
                    .Any(button => button.CustomId == $"v1:close:{ticket.Id:D}"))) return true;
            var oldest = messages.Min(x => x.Id);
            if (before.HasValue && oldest >= before.Value) throw new InvalidOperationException("Opening-message lookup did not advance.");
            before = oldest;
        }
    }
    internal static bool IsCategoryPlacementFailure(HttpException exception) =>
        exception.HttpCode == HttpStatusCode.BadRequest && exception.DiscordCode == DiscordErrorCode.InvalidFormBody &&
        exception.Errors.Count > 0 && exception.Errors.All(x => x.Path == "parent_id");

    internal static async Task CloseChannelPlacementAsync(ulong closedCategoryId, ulong? currentCategoryId,
        Func<Task<int>> closedCategoryChildren, Func<Task> restrictAccess, Func<ulong?, Task> moveChannel)
    {
        // Close access before relocation so a rejected category move cannot leave a writable ticket.
        await restrictAccess();
        if (currentCategoryId == closedCategoryId) return;
        if (await closedCategoryChildren() >= 50)
        {
            if (currentCategoryId.HasValue) await moveChannel(null);
            return;
        }
        try { await moveChannel(closedCategoryId); }
        catch (HttpException ex) when (IsCategoryPlacementFailure(ex))
        {
            // Another channel can take the last slot after the initial count. Other placement errors still fail.
            if (await closedCategoryChildren() < 50) throw;
            await moveChannel(null);
        }
    }

    public async Task CloseAsync(Ticket ticket, CancellationToken ct)
    {
        var channel = await RequireManagedAsync(ticket, ct);
        var guild = await GuildAsync(ct);
        var category = (await guild.GetCategoryChannelsAsync(Options(ct)))
            .Single(x => x.Id == configuration.Tickets.ClosedCategoryId);
        var overwrites = BuildOverwrites(configuration.GuildId, client.CurrentUser.Id, ticket.RequesterId, configuration.Tickets.SupportRoleIds,
            category.PermissionOverwrites.Concat(channel.PermissionOverwrites), configuration.Tickets.ClosedRequesterCanRead, false, true);
        await CloseChannelPlacementAsync(category.Id, channel.CategoryId,
            async () => (await guild.GetChannelsAsync(Options(ct))).OfType<INestedChannel>().Count(x => x.CategoryId == category.Id && x.Id != channel.Id),
            () => channel.ModifyAsync(x => x.PermissionOverwrites = overwrites, Options(ct)),
            destination => channel.ModifyAsync(x => { x.CategoryId = destination; x.PermissionOverwrites = overwrites; }, Options(ct)));
        await VerifyPrivacyAsync(ticket, configuration.Tickets.ClosedRequesterCanRead, false, true, ct);
    }

    public async Task FreezeAsync(Ticket ticket, CancellationToken ct)
    {
        var channel = await RequireManagedAsync(ticket, ct);
        var overwrites = BuildOverwrites(configuration.GuildId, client.CurrentUser.Id, ticket.RequesterId, configuration.Tickets.SupportRoleIds,
            channel.PermissionOverwrites, configuration.Tickets.ClosedRequesterCanRead, false, false);
        await channel.ModifyAsync(x => x.PermissionOverwrites = overwrites, Options(ct));
        await VerifyPrivacyAsync(ticket, configuration.Tickets.ClosedRequesterCanRead, false, false, ct);
    }

    public async Task RenameAsync(Ticket ticket, string name, CancellationToken ct) =>
        await (await RequireManagedAsync(ticket, ct)).ModifyAsync(x => x.Name = name, Options(ct));
    public async Task<bool> ExistsAsync(Ticket ticket, CancellationToken ct) => await ManagedChannelAsync(ticket, ct) is not null;
    public async Task DeleteAsync(Ticket ticket, CancellationToken ct)
    {
        var channel = await ManagedChannelAsync(ticket, ct);
        if (channel is not null) await channel.DeleteAsync(Options(ct));
    }
    public async Task<ulong?> LastMessageIdAsync(Ticket ticket, CancellationToken ct)
    {
        var channel = await RequireManagedAsync(ticket, ct);
        return (await channel.GetMessagesAsync(1, Options(ct)).FlattenAsync()).SingleOrDefault()?.Id;
    }
    public Task<TranscriptSnapshot> ReadTranscriptAsync(Ticket ticket, CancellationToken ct) =>
        ReadTranscriptAsync(ticket, ct, static () => { });

    public async Task<TranscriptSnapshot> ReadTranscriptAsync(Ticket ticket, CancellationToken ct, Action progress)
    {
        var channel = await RequireManagedAsync(ticket, ct);
        var messages = new List<TranscriptMessage>();
        ulong? before = null;
        while (true)
        {
            var page = await (before.HasValue ? channel.GetMessagesAsync(before.Value, Direction.Before, 100, Options(ct)) :
                channel.GetMessagesAsync(100, Options(ct))).FlattenAsync();
            ct.ThrowIfCancellationRequested();
            if (!page.Any()) { progress(); break; }
            foreach (var message in page)
                messages.Add(new(message.Id, message.Author.Id, message.Author.Username, message.Content, message.Timestamp,
                    message.Attachments.Select(x => new TranscriptAttachment(x.Id, x.Filename, x.Url, x.Size)).ToArray(),
                    JsonSerializer.Serialize(message.Embeds)));
            var oldest = page.Min(x => x.Id);
            if (before.HasValue && oldest >= before.Value) throw new InvalidOperationException("Discord transcript pagination did not advance.");
            before = oldest;
            progress();
        }
        messages.Sort((left, right) => left.Id.CompareTo(right.Id));
        return new(messages, messages.Count == 0 ? null : messages[^1].Id);
    }
    public async Task LogAsync(string message, CancellationToken ct) =>
        await (await TextChannelAsync(configuration.Tickets.LogChannelId, ct)).SendMessageAsync(message, allowedMentions: AllowedMentions.None, options: Options(ct));

    public async Task<CommunityMember?> GetMemberAsync(ulong userId, CancellationToken ct)
    {
        try
        {
            var member = await (await GuildAsync(ct)).GetUserAsync(userId, Options(ct));
            return member is null ? null : new(member.Id, member.DisplayName, member.RoleIds, member.IsBot, member.IsWebhook);
        }
        catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { return null; }
    }
    public async Task<CommunityRole?> GetRoleAsync(ulong roleId, CancellationToken ct)
    {
        var role = (await GuildAsync(ct)).Roles.SingleOrDefault(x => x.Id == roleId);
        return role is null ? null : new(role.Id, role.Position, role.IsManaged);
    }
    internal static bool HasStaffPermissions(GuildPermissions permissions)
    {
        // Only ordinary member capabilities are safe for self-selection. Unknown/new bits fail closed.
        const GuildPermission ordinaryPermissions =
            GuildPermission.CreateInstantInvite | GuildPermission.AddReactions | GuildPermission.ViewChannel |
            GuildPermission.SendMessages | GuildPermission.SendTTSMessages | GuildPermission.EmbedLinks |
            GuildPermission.AttachFiles | GuildPermission.ReadMessageHistory | GuildPermission.UseExternalEmojis |
            GuildPermission.Connect | GuildPermission.Speak | GuildPermission.UseVAD | GuildPermission.Stream |
            GuildPermission.ChangeNickname | GuildPermission.UseApplicationCommands | GuildPermission.RequestToSpeak |
            GuildPermission.CreatePublicThreads | GuildPermission.CreatePrivateThreads | GuildPermission.UseExternalStickers |
            GuildPermission.SendMessagesInThreads | GuildPermission.StartEmbeddedActivities | GuildPermission.UseSoundboard |
            GuildPermission.UseExternalSounds | GuildPermission.SendVoiceMessages | GuildPermission.SendPolls |
            GuildPermission.UseExternalApps;
        return (permissions.RawValue & ~(ulong)ordinaryPermissions) != 0;
    }

    internal static void ValidateFactionRoleOverwrites(ulong roleId,
        IEnumerable<(ulong ChannelId, Overwrite Overwrite)> overwrites)
    {
        foreach (var item in overwrites)
        {
            var overwrite = item.Overwrite;
            if (overwrite.TargetType == PermissionTarget.Role && overwrite.TargetId == roleId &&
                HasStaffPermissions(new GuildPermissions(overwrite.Permissions.AllowValue)))
                throw new DiscordValidationException($"Faction role {roleId} grants privileged permissions in channel or category {item.ChannelId}.");
        }
    }
    internal static void ValidateTranscriptCapability(ApplicationFlags flags)
    {
        if ((flags & (ApplicationFlags.GatewayMessageContent | ApplicationFlags.GatewayMessageContentLimited)) == 0)
            throw new DiscordValidationException("Enable Message Content Intent for the Discord application to preserve ticket transcripts.");
    }

    internal static void ValidateTextChannelPermissions(ulong id, ChannelPermissions permissions,
        bool deleteCommand = false, bool requireEmbeds = false)
    {
        // Deleting our own response does not require Manage Messages.
        if (!permissions.ViewChannel || !permissions.SendMessages)
            throw new DiscordValidationException($"The bot requires View Channel and Send Messages in text channel {id}.");
        if (requireEmbeds && !permissions.EmbedLinks)
            throw new DiscordValidationException($"The bot requires Embed Links in text channel {id}.");
        if (deleteCommand && !permissions.ManageMessages)
            throw new DiscordValidationException($"Command deletion requires Manage Messages in text channel {id}.");
    }

    internal static void ValidateCommunityChannels(BotConfiguration configuration,
        IReadOnlyCollection<(ulong Id, ChannelType Type, ChannelPermissions Permissions)> channels)
    {
        void RequireText(ulong id, bool deleteCommand = false, bool requireEmbeds = false)
        {
            var channel = channels.SingleOrDefault(x => x.Id == id && x.Type == ChannelType.Text);
            if (channel.Id == 0) throw new DiscordValidationException($"Configured text channel {id} is unavailable.");
            ValidateTextChannelPermissions(id, channel.Permissions, deleteCommand, requireEmbeds);
        }

        if (configuration.Welcome.Enabled)
        {
            foreach (var id in configuration.Welcome.ChannelMentions.Values.Distinct())
                if (channels.All(x => x.Id != id))
                    throw new DiscordValidationException($"Welcome channel mention {id} is unavailable in the configured guild.");
            if (!configuration.Welcome.DirectMessage) RequireText(configuration.Welcome.ChannelId);
        }
        // Unrestricted prefix commands can complete only where the bot can read and reply.
        var unrestrictedScope = channels.Where(x => x.Type == ChannelType.Text && x.Permissions.ViewChannel && x.Permissions.SendMessages).Select(x => x.Id).ToArray();
        if (configuration.Factions.Enabled && configuration.Factions.DeleteCommand)
            foreach (var id in unrestrictedScope) RequireText(id, deleteCommand: true);
        foreach (var answer in configuration.Answers.Where(x => x.Enabled))
        {
            var scope = answer.AllowedChannelIds.Length > 0 ? answer.AllowedChannelIds : unrestrictedScope;
            foreach (var id in scope) RequireText(id, answer.DeleteCommand, answer.Embeds.Length > 0);
        }
    }

    private async Task<RestGuildUser> RoleMemberAsync(ulong userId, ulong roleId, CancellationToken ct)
    {
        var guild = await GuildAsync(ct);
        var bot = await guild.GetUserAsync(client.CurrentUser.Id, Options(ct));
        var role = guild.Roles.SingleOrDefault(x => x.Id == roleId);
        if (bot is null || role is null || role.IsManaged || HasStaffPermissions(role.Permissions) || role.Id == guild.Id || role.Position >= bot.Hierarchy || !bot.GuildPermissions.ManageRoles)
            throw new InvalidOperationException("The configured faction role cannot be managed by this bot.");
        // Role overwrites can grant moderation capabilities independently of guild permissions.
        // Refresh all channels before every mutation, since staff can change grants after startup.
        var channels = await guild.GetChannelsAsync(Options(ct));
        ValidateFactionRoleOverwrites(roleId, channels.SelectMany(channel =>
            channel.PermissionOverwrites.Select(overwrite => (channel.Id, overwrite))));
        var member = await guild.GetUserAsync(userId, Options(ct)) ?? throw new InvalidOperationException("The member is unavailable.");
        if (member.Id == guild.OwnerId || member.Hierarchy >= bot.Hierarchy)
            throw new InvalidOperationException("The bot cannot manage roles for this member.");
        return member;
    }
    public async Task AddRoleAsync(ulong userId, ulong roleId, CancellationToken ct) => await (await RoleMemberAsync(userId, roleId, ct)).AddRoleAsync(roleId, Options(ct));
    public async Task RemoveRoleAsync(ulong userId, ulong roleId, CancellationToken ct) => await (await RoleMemberAsync(userId, roleId, ct)).RemoveRoleAsync(roleId, Options(ct));
    public async Task<ulong?> SendAsync(ulong channelId, CommunityMessage message, CancellationToken ct)
    {
        var channel = await TextChannelAsync(channelId, ct);
        var messageId = await poster.SendAsync(channel.Id, message, ct);
        if (message.DeleteAfter.HasValue) await responseDeletions.ScheduleAsync(channelId, messageId, message.DeleteAfter.Value, ct);
        return messageId;
    }
    public async Task<ulong?> SendDirectMessageAsync(ulong userId, CommunityMessage message, CancellationToken ct)
    {
        var member = await (await GuildAsync(ct)).GetUserAsync(userId, Options(ct)) ?? throw new InvalidOperationException("The member is unavailable.");
        var channel = await member.CreateDMChannelAsync(Options(ct));
        return await poster.SendAsync(channel.Id, message, ct);
    }
    public async Task DeleteMessageAsync(ulong channelId, ulong messageId, CancellationToken ct)
    {
        try { await (await TextChannelAsync(channelId, ct)).DeleteMessageAsync(messageId, Options(ct)); }
        catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { }
    }

    internal static bool RequiresTicketCapabilities(BotConfiguration configuration, IReadOnlyList<Ticket> persistedTickets) =>
        configuration.Tickets.Enabled || persistedTickets.Any(ticket => ticket.State != TicketState.Deleted);
    public async Task ValidateDiscordAsync(CancellationToken ct)
    {
        IReadOnlyList<Ticket> persistedTickets;
        await using (var session = await store.LockAsync(ct))
            persistedTickets = await session.GetTicketsAsync(ct);
        if (ConfigurationValidator.ValidatePersistedTickets(configuration, persistedTickets).Count > 0)
            throw new DiscordValidationException("Retained ticket maintenance configuration is invalid; restore its required panels, roles, and archive ownership before startup.");
        var requiresTicketCapabilities = RequiresTicketCapabilities(configuration, persistedTickets);
        var guild = await GuildAsync(ct);
        var bot = await guild.GetUserAsync(client.CurrentUser.Id, Options(ct)) ?? throw new DiscordValidationException("The bot is not a member of the configured guild.");
        foreach (var id in configuration.AdministratorRoleIds.Concat(requiresTicketCapabilities ? configuration.Tickets.SupportRoleIds.Concat(configuration.Tickets.BypassRoleIds) : [])
            .Concat(configuration.Answers.Where(x => x.Enabled).SelectMany(x => x.AllowedRoleIds)).Distinct())
            if (id == guild.Id || guild.Roles.All(x => x.Id != id)) throw new DiscordValidationException($"Configured access role {id} is unavailable or is the everyone role.");
        var channels = await guild.GetChannelsAsync(Options(ct));
        void RequireText(ulong id)
        {
            var channel = channels.OfType<RestTextChannel>().SingleOrDefault(x => x.Id == id && x.ChannelType == ChannelType.Text)
                ?? throw new DiscordValidationException($"Configured text channel {id} is unavailable.");
            ValidateTextChannelPermissions(id, bot.GetPermissions(channel));
        }
        if (requiresTicketCapabilities)
        {
            var application = await ((IDiscordClient)client.Rest).GetApplicationInfoAsync(Options(ct));
            ValidateTranscriptCapability(application.Flags);
            if (!bot.GuildPermissions.ManageChannels || !bot.GuildPermissions.ManageRoles)
                throw new DiscordValidationException("Tickets require Manage Channels and Manage Roles permissions.");
            foreach (var id in configuration.Tickets.Panels.SelectMany(x => x.OpenCategoryIds).Append(configuration.Tickets.ClosedCategoryId).Distinct())
            {
                var category = channels.OfType<RestCategoryChannel>().SingleOrDefault(x => x.Id == id)
                    ?? throw new DiscordValidationException($"Configured category {id} is unavailable.");
                var permissions = bot.GetPermissions(category);
                if (!permissions.ViewChannel || !permissions.ManageChannel || !permissions.ManageRoles || !permissions.ReadMessageHistory || !permissions.AttachFiles)
                    throw new DiscordValidationException($"The bot cannot manage ticket channels in category {id}.");
            }
            foreach (var panel in configuration.Tickets.Panels) RequireText(panel.ChannelId);
            RequireText(configuration.Tickets.LogChannelId);
        }
        ValidateCommunityChannels(configuration, channels.Select(x => (x.Id, x.ChannelType, bot.GetPermissions(x))).ToArray());
        if (configuration.Factions.Enabled)
        {
            foreach (var item in configuration.Factions.Roles)
            {
                var role = guild.Roles.SingleOrDefault(x => x.Id == item.RoleId);
                if (role is null || role.IsManaged || HasStaffPermissions(role.Permissions) || role.Id == guild.Id || role.Position >= bot.Hierarchy || !bot.GuildPermissions.ManageRoles)
                    throw new DiscordValidationException($"The configured faction role {item.RoleId} cannot be managed by the bot.");
                ValidateFactionRoleOverwrites(item.RoleId, channels.SelectMany(channel =>
                    channel.PermissionOverwrites.Select(overwrite => (channel.Id, overwrite))));
            }
        }
    }
}
