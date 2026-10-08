using System.Collections.Concurrent;
using System.Threading.Channels;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Hosting;

namespace SWLOR.DiscordBot.Discord;

public sealed class DiscordGateway(DiscordSocketClient client, BotConfiguration configuration, DiscordOperations discord,
    TicketService tickets, ITicketStore store, CommunityService community, TimeProvider clock,
    IHostApplicationLifetime lifetime, ReadinessMarker marker, ILogger<DiscordGateway> logger)
{
    private readonly Channel<Func<CancellationToken, Task>> jobs = Channel.CreateBounded<Func<CancellationToken, Task>>(new BoundedChannelOptions(128)
    { FullMode = BoundedChannelFullMode.Wait, SingleWriter = false, SingleReader = false });
    private readonly ConcurrentDictionary<string, CloseConfirmation> confirmations = new();
    private CancellationToken stoppingToken;
    private readonly GatewaySessionState sessionState = new();
    public bool Ready => sessionState.IsReady(client.ConnectionState == ConnectionState.Connected);
    internal event Action? ReadinessEstablished
    {
        add => sessionState.ReadinessEstablished += value;
        remove => sessionState.ReadinessEstablished -= value;
    }
    private sealed record CloseConfirmation(ulong UserId, ulong ChannelId, Guid TicketId, DateTimeOffset ExpiresAt);

    public void Suspend() { sessionState.Disconnect(); marker.Clear(); }
    public void Attach(CancellationToken ct)
    {
        stoppingToken = ct;
        client.Ready += OnReadyAsync;
        client.LatencyUpdated += OnLatencyUpdatedAsync;
        client.Disconnected += OnDisconnectedAsync;
        client.SlashCommandExecuted += OnSlashAsync;
        client.ButtonExecuted += OnButtonAsync;
        client.MessageReceived += OnMessageAsync;
        client.UserJoined += OnJoinedAsync;
        client.Log += OnLogAsync;
    }
    public void Detach()
    {
        sessionState.Stop();
        client.Ready -= OnReadyAsync;
        client.LatencyUpdated -= OnLatencyUpdatedAsync;
        client.Disconnected -= OnDisconnectedAsync;
        client.SlashCommandExecuted -= OnSlashAsync;
        client.ButtonExecuted -= OnButtonAsync;
        client.MessageReceived -= OnMessageAsync;
        client.UserJoined -= OnJoinedAsync;
        client.Log -= OnLogAsync;
        jobs.Writer.TryComplete();
    }
    internal bool TryQueueJob(Func<CancellationToken, Task> job) => jobs.Writer.TryWrite(job);
    internal bool TryQueueInteractionJob(DateTimeOffset createdAt, Func<CancellationToken, Task> job,
        Func<CancellationToken, Task> expired, bool allowExtendedExecution = false) => DeferredInteractionPolicy.CanStart(createdAt, clock) &&
        TryQueueJob(ct => DeferredInteractionPolicy.ExecuteAsync(createdAt, clock, job, expired, ct, allowExtendedExecution));
    public async Task ProcessAsync(CancellationToken ct)
    {
        await foreach (var job in jobs.Reader.ReadAllAsync(ct))
        {
            // REST requests and ticket inactivity watchdogs bound stalled work without limiting healthy pagination.
            try { await job(ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError("Discord event failed: {ErrorKind}.", SafeError(ex)); }
        }
    }
    public static string SafeError(Exception ex) => ex switch
    {
        DiscordValidationException validation => validation.Message,
        HttpException http => $"Discord HTTP {(int)http.HttpCode}",
        _ => ex.GetType().Name
    };
    private Task OnLogAsync(LogMessage message)
    {
        // SDK messages can contain request bodies or exception details; retain only severity/source.
        if (message.Severity <= LogSeverity.Warning) logger.LogWarning("Discord SDK {Severity} from {Source}.", message.Severity, message.Source);
        return Task.CompletedTask;
    }
    private Task OnDisconnectedAsync(Exception _) { Suspend(); return Task.CompletedTask; }
    private Task OnReadyAsync()
    {
        if (stoppingToken.IsCancellationRequested) return Task.CompletedTask;
        marker.Clear();
        QueueValidation(sessionState.BeginValidation());
        return Task.CompletedTask;
    }
    private Task OnLatencyUpdatedAsync(int previous, int current)
    {
        if (stoppingToken.IsCancellationRequested) return Task.CompletedTask;
        // Heartbeat ACKs also arrive after a successful RESUMED dispatch, which does not raise Ready again.
        var connected = client.ConnectionState == ConnectionState.Connected && client.CurrentUser is not null &&
            client.GetGuild(configuration.GuildId) is { IsConnected: true };
        if (sessionState.ObserveHeartbeat(connected) is { } generation) QueueValidation(generation);
        return Task.CompletedTask;
    }
    private void QueueValidation(int generation)
    {
        if (!TryQueueJob(async ct =>
        {
            if (!sessionState.IsCurrent(generation)) return;
            try
            {
                await discord.ValidateDiscordAsync(ct);
                await tickets.ReconcileRetainedPermissionsAsync(ct);
                if (!sessionState.IsCurrent(generation)) return;
                await RegisterAsync(ct);
                if (sessionState.CompleteValidation(generation, client.ConnectionState == ConnectionState.Connected))
                    logger.LogInformation("Discord gateway is ready for guild {GuildId}.", configuration.GuildId);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { sessionState.AbandonValidation(generation); }
            catch (Exception ex)
            {
                // A disconnect can finish an old REST request after its gateway session has already changed.
                if (!sessionState.IsCurrent(generation) || client.ConnectionState != ConnectionState.Connected)
                { sessionState.AbandonValidation(generation); return; }
                logger.LogCritical("Discord startup validation failed: {ErrorKind}.", SafeError(ex));
                Environment.ExitCode = 1;
                lifetime.StopApplication();
            }
        })) { Environment.ExitCode = 1; lifetime.StopApplication(); }
    }
    internal static SlashCommandOptionBuilder TranscriptCommandOptions() =>
        new SlashCommandOptionBuilder().WithName("transcript").WithDescription("Export a transcript (support only)")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("saved", ApplicationCommandOptionType.Boolean, "Download the latest saved transcript", isRequired: false);
    private async Task RegisterAsync(CancellationToken ct)
    {
        if (!configuration.Tickets.Enabled)
        {
            var guild = await discord.GuildAsync(ct);
            var commands = await guild.GetApplicationCommandsAsync(options: DiscordOperations.Options(ct));
            await TicketCommandRegistration.RemoveDisabledIntakeAsync(commands.Select(command =>
                (command.Name, command.Type, (Func<Task>)(() => command.DeleteAsync(DiscordOperations.Options(ct))))), ct);
        }
        var ticket = new SlashCommandBuilder().WithName("ticket").WithDescription("Manage this support ticket");
        foreach (var (name, description) in new[] { ("close", "Close this ticket"), ("reopen", "Reopen this ticket"),
            ("hold", "Hold cleanup (support only)"), ("release", "Release cleanup hold (support only)") })
            ticket.AddOption(new SlashCommandOptionBuilder().WithName(name).WithDescription(description).WithType(ApplicationCommandOptionType.SubCommand));
        ticket.AddOption(TranscriptCommandOptions());
        ticket.AddOption(new SlashCommandOptionBuilder().WithName("rename").WithDescription("Rename this ticket (support only)")
            .WithType(ApplicationCommandOptionType.SubCommand).AddOption("name", ApplicationCommandOptionType.String, "New channel name", isRequired: true));
        var publish = new SlashCommandBuilder().WithName("ticket-panel").WithDescription("Publish a configured ticket panel (bot administrators only)")
            .AddOption(new SlashCommandOptionBuilder().WithName("publish").WithDescription("Publish an open-ticket button in its configured channel")
                .WithType(ApplicationCommandOptionType.SubCommand).AddOption("panel", ApplicationCommandOptionType.String, "Configured panel ID", isRequired: true));
        await client.Rest.CreateGuildCommand(ticket.Build(), configuration.GuildId, DiscordOperations.Options(ct));
        if (configuration.Tickets.Enabled)
            await client.Rest.CreateGuildCommand(publish.Build(), configuration.GuildId, DiscordOperations.Options(ct));
    }
    internal static bool CanExecuteCommunityCommand(BotConfiguration config, ChannelPermissions permissions, ulong channelId, string content)
    {
        if (!permissions.ViewChannel || !permissions.SendMessages) return false;
        var body = content.TrimStart();
        if (!body.StartsWith(config.Prefix, StringComparison.Ordinal)) return false;
        var command = body[config.Prefix.Length..].TrimStart();
        var length = 0;
        while (length < command.Length && !char.IsWhiteSpace(command[length])) length++;
        var name = command[..length];
        if (config.Factions.Enabled && name.Equals("rank", StringComparison.OrdinalIgnoreCase))
            return !config.Factions.DeleteCommand || permissions.ManageMessages;
        var answer = config.Answers.FirstOrDefault(x => x.Enabled && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return answer is not null && (answer.AllowedChannelIds.Length == 0 || answer.AllowedChannelIds.Contains(channelId)) &&
            (!answer.DeleteCommand || permissions.ManageMessages) && (answer.Embeds.Length == 0 || permissions.EmbedLinks);
    }

    internal static string CloseConfirmationText(bool requesterCanRead) => requesterCanRead
        ? "Close this ticket? The channel will become read-only for the requester."
        : "Close this ticket? The channel will be hidden from the requester.";

    private Task OnMessageAsync(SocketMessage message)
    {
        if (!Ready || message.Author.IsBot || message.Author.IsWebhook || message.Channel is not SocketTextChannel channel ||
            channel.ChannelType != ChannelType.Text || channel.Guild.Id != configuration.GuildId) return Task.CompletedTask;
        bool CanExecute() => channel.Guild.CurrentUser is { } bot &&
            CanExecuteCommunityCommand(configuration, bot.GetPermissions(channel), channel.Id, message.Content);
        if (!CanExecute()) return Task.CompletedTask;
        if (!jobs.Writer.TryWrite(ct => RetryCommunityAsync(token => CanExecute()
            ? community.ExecuteAsync(message.Author.Id, channel.Id, message.Id, message.Content, token) : Task.CompletedTask, ct)))
            logger.LogWarning("Community event queue is full.");
        return Task.CompletedTask;
    }
    internal static bool ShouldPersistWelcome(BotConfiguration configuration, ulong guildId,
        bool isBot, bool isWebhook, DateTimeOffset? joinedAt) =>
        configuration.Welcome.Enabled && guildId == configuration.GuildId && !isBot && !isWebhook && joinedAt.HasValue;

    internal static async Task<bool> PersistAndQueueWelcomeAsync(CommunityService community, ulong userId,
        DateTimeOffset joinedAt, Func<bool> isReady, Func<Func<CancellationToken, Task>, bool> tryQueue, CancellationToken ct)
    {
        await community.PersistWelcomeAsync(userId, joinedAt, ct);
        return isReady() && tryQueue(token => isReady()
            ? community.WelcomeAsync(userId, joinedAt, token) : Task.CompletedTask);
    }

    private async Task OnJoinedAsync(SocketGuildUser member)
    {
        if (!ShouldPersistWelcome(configuration, member.Guild.Id, member.IsBot, member.IsWebhook, member.JoinedAt)) return;
        try
        {
            using var persistence = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            persistence.CancelAfter(TimeSpan.FromSeconds(10));
            if (!await PersistAndQueueWelcomeAsync(community, member.Id, member.JoinedAt!.Value, () => Ready,
                job => jobs.Writer.TryWrite(ct => RetryCommunityAsync(job, ct)), persistence.Token))
                logger.LogInformation("Welcome intent for member {MemberId} is persisted for ready recovery.", member.Id);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning("Welcome intent persistence was interrupted by shutdown for member {MemberId}.", member.Id);
        }
        catch (Exception ex)
        {
            // Discord will not replay this one-shot event. Make inability to commit it an explicit worker failure.
            logger.LogCritical("Welcome intent persistence failed for member {MemberId}: {ErrorKind}.", member.Id, SafeError(ex));
            Suspend();
            Environment.ExitCode = 1;
            lifetime.StopApplication();
        }
    }
    private async Task RetryCommunityAsync(Func<CancellationToken, Task> job, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            try { await job(ct); return; }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (attempt < 2)
            {
                logger.LogWarning("Community delivery attempt {Attempt} failed: {ErrorKind}.", attempt + 1, SafeError(ex));
                await Task.Delay(TimeSpan.FromSeconds(5 * (attempt + 1)), clock, ct);
            }
        }
    }
    private async Task QueueInteractionAsync(SocketInteraction interaction, Func<CancellationToken, Task> job, bool allowExtendedExecution = false)
    {
        if (interaction is SocketMessageComponent component) await component.DeferLoadingAsync(ephemeral: true);
        else await interaction.DeferAsync(ephemeral: true);
        async Task RejectExpiredAsync(CancellationToken ct)
        {
            logger.LogWarning("Interaction {InteractionId} expired in the queue and was not executed.", interaction.Id);
            if (DeferredInteractionPolicy.CanReply(interaction.CreatedAt, clock))
                await ReplyAsync(interaction, "This operation waited too long and was not executed. Please try again.", ct);
        }
        if (!DeferredInteractionPolicy.CanStart(interaction.CreatedAt, clock))
        { await RejectExpiredAsync(stoppingToken); return; }
        if (!Ready || !TryQueueInteractionJob(interaction.CreatedAt, async ct =>
        {
            // REST requests and ticket inactivity watchdogs bound stalled work without limiting healthy pagination.
            try { await job(ct); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                logger.LogWarning("Interaction {InteractionId} exceeded its execution deadline.", interaction.Id);
                if (DeferredInteractionPolicy.CanReply(interaction.CreatedAt, clock))
                    await ReplyAsync(interaction, "The operation could not be completed in time. Please check the ticket before trying again.", stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Interaction {InteractionId} failed: {ErrorKind}.", interaction.Id, SafeError(ex));
                await ReplyAsync(interaction, "The operation could not be completed. Staff can check the bot status.", ct);
            }
        }, RejectExpiredAsync, allowExtendedExecution)) await ReplyAsync(interaction, "The bot is temporarily unavailable. Please try again shortly.", stoppingToken);
    }
    private Task OnSlashAsync(SocketSlashCommand command)
    {
        if (command.Data.Name is not ("ticket" or "ticket-panel")) return Task.CompletedTask;
        return QueueInteractionAsync(command, ct => HandleSlashAsync(command, ct),
            allowExtendedExecution: command.Data.Name == "ticket" && command.Data.Options.Single().Name == "transcript");
    }
    private Task OnButtonAsync(SocketMessageComponent component)
    {
        if (!component.Data.CustomId.StartsWith("v1:", StringComparison.Ordinal)) return Task.CompletedTask;
        return QueueInteractionAsync(component, ct => HandleButtonAsync(component, ct));
    }
    private static Task ReplyAsync(SocketInteraction interaction, string content, CancellationToken ct) =>
        interaction.ModifyOriginalResponseAsync(x => { x.Content = content; x.Components = new ComponentBuilder().Build(); x.AllowedMentions = AllowedMentions.None; }, DiscordOperations.Options(ct));
    private async Task<bool> GuildGuardAsync(SocketInteraction interaction, CancellationToken ct)
    {
        if (interaction.GuildId == configuration.GuildId) return true;
        await ReplyAsync(interaction, "Ticket actions are available only in the configured server.", ct);
        return false;
    }
    private async Task HandleSlashAsync(SocketSlashCommand command, CancellationToken ct)
    {
        if (!await GuildGuardAsync(command, ct)) return;
        var actor = await discord.ActorAsync(command.User.Id, ct);
        var option = command.Data.Options.Single();
        if (command.Data.Name == "ticket-panel")
        {
            if (!configuration.Tickets.Enabled)
            { await ReplyAsync(command, "New ticket intake is disabled.", ct); return; }
            if (!actor.IsGuildOwner && !configuration.AdministratorRoleIds.Any(actor.RoleIds.Contains))
            { await ReplyAsync(command, "Only configured bot administrators can publish a ticket panel.", ct); return; }
            var panelId = (string)option.Options.Single(x => x.Name == "panel").Value;
            var panel = configuration.Tickets.Panels.SingleOrDefault(x => x.Id == panelId);
            if (panel is null) { await ReplyAsync(command, "That panel is not configured.", ct); return; }
            var channel = await discord.TextChannelAsync(panel.ChannelId, ct);
            await channel.SendMessageAsync(TemplateRenderer.RenderTicket(panel.PanelMessage, actor.UserId, discord.ServerName), allowedMentions: AllowedMentions.None,
                components: new ComponentBuilder().WithButton(panel.Label, $"v1:open:{panel.Id}", ButtonStyle.Primary).Build(), options: DiscordOperations.PostingOptions(ct));
            await ReplyAsync(command, $"Published the {panel.Id} panel in <#{panel.ChannelId}>.", ct);
            return;
        }
        var channelId = command.Channel.Id;
        if (option.Name == "close" && configuration.Tickets.CloseConfirmation)
        { await ConfirmCloseAsync(command, actor, channelId, null, ct); return; }
        var result = option.Name switch
        {
            "close" => await tickets.CloseAsync(channelId, actor, ct),
            "reopen" => await tickets.ReopenAsync(channelId, actor, ct),
            "rename" => await tickets.RenameAsync(channelId, actor, (string)option.Options.Single(x => x.Name == "name").Value, ct),
            "transcript" => await tickets.ExportAsync(channelId, actor, ct, async (ticket, token) =>
            {
                await ReplyAsync(command, option.Options.Any(item => item.Name == "saved" && item.Value is true)
                    ? "Preparing the latest saved transcript; newer messages are not included."
                    : "Transcript archived; preparing download.", token);
                await TranscriptDelivery.SendAsync(Path.Combine(ticket.ArchiveSnapshotPath ?? ticket.ArchivePath!, "transcript.html"), command.AttachmentSizeLimit,
                    async (stream, name, text, uploadToken) =>
                    {
                        await command.FollowupWithFileAsync(stream, name, text: text,
                            ephemeral: true, allowedMentions: AllowedMentions.None, options: DiscordOperations.Options(uploadToken));
                    }, token, async authorizationToken =>
                    {
                        var currentActor = await discord.ActorAsync(actor.UserId, authorizationToken);
                        if (!tickets.CanSupport(currentActor))
                            throw new DiscordValidationException("Support access changed; transcript delivery was denied.");
                    });
            }, useSavedArchive: option.Options.Any(item => item.Name == "saved" && item.Value is true)),
            "hold" => await tickets.SetHoldAsync(channelId, actor, true, ct),
            "release" => await tickets.SetHoldAsync(channelId, actor, false, ct),
            _ => new TicketResult(false, "Unknown ticket action.")
        };
        await ReplyAsync(command, result.Message, ct);
    }

    private async Task<Ticket?> AuthorizedCloseTicketAsync(ulong channelId, Actor actor, Guid? ticketId, CancellationToken ct)
    {
        await using var session = await store.LockAsync(ct);
        var ticket = ticketId is { } id
            ? await session.GetTicketAsync(id, ct)
            : await session.FindByChannelAsync(channelId, ct);
        if (ticket?.ChannelId != channelId) ticket = null;
        return ticket is not null && (ticket.RequesterId == actor.UserId || tickets.CanSupport(actor)) ? ticket : null;
    }
    private async Task ConfirmCloseAsync(SocketInteraction interaction, Actor actor, ulong channelId, Guid? ticketId, CancellationToken ct)
    {
        var ticket = await AuthorizedCloseTicketAsync(channelId, actor, ticketId, ct);
        if (ticket is null) { await ReplyAsync(interaction, "You cannot close this channel as a managed ticket.", ct); return; }
        foreach (var entry in confirmations.Where(x => x.Value.ExpiresAt <= clock.GetUtcNow())) confirmations.TryRemove(entry.Key, out _);
        var nonce = Guid.NewGuid().ToString("N");
        confirmations[nonce] = new(actor.UserId, channelId, ticket.Id, clock.GetUtcNow() + TimeSpan.FromMinutes(5));
        await interaction.ModifyOriginalResponseAsync(x =>
        {
            x.Content = CloseConfirmationText(configuration.Tickets.ClosedRequesterCanRead);
            x.Components = new ComponentBuilder().WithButton("Confirm close", $"v1:confirm:{nonce}", ButtonStyle.Danger).Build();
            x.AllowedMentions = AllowedMentions.None;
        }, DiscordOperations.Options(ct));
    }
    private async Task HandleButtonAsync(SocketMessageComponent component, CancellationToken ct)
    {
        if (!await GuildGuardAsync(component, ct)) return;
        if (component.Message.Author.Id != client.CurrentUser.Id)
        { await ReplyAsync(component, "This is not a bot-issued ticket control.", ct); return; }
        var parts = component.Data.CustomId.Split(':');
        if (parts.Length != 3) { await ReplyAsync(component, "Invalid ticket control.", ct); return; }
        var actor = await discord.ActorAsync(component.User.Id, ct);
        if (parts[1] == "open")
        {
            if (!configuration.Tickets.Enabled)
            { await ReplyAsync(component, "New ticket intake is disabled.", ct); return; }
            var panel = configuration.Tickets.Panels.SingleOrDefault(x => x.Id == parts[2] && x.ChannelId == component.Channel.Id);
            if (panel is null) { await ReplyAsync(component, "This ticket panel is unavailable in this channel.", ct); return; }
            var result = await tickets.OpenAsync(panel.Id, actor, component.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), ct);
            await ReplyAsync(component, result.Success && result.Ticket?.ChannelId is { } id ? $"Your ticket is ready: <#{id}>." : result.Message, ct);
        }
        else if (parts[1] == "close" && Guid.TryParseExact(parts[2], "D", out var ticketId))
        {
            if (configuration.Tickets.CloseConfirmation) await ConfirmCloseAsync(component, actor, component.Channel.Id, ticketId, ct);
            else if (await AuthorizedCloseTicketAsync(component.Channel.Id, actor, ticketId, ct) is not null)
                await ReplyAsync(component, (await tickets.CloseAsync(component.Channel.Id, actor, ct)).Message, ct);
            else await ReplyAsync(component, "You cannot close this channel as a managed ticket.", ct);
        }
        else if (parts[1] == "confirm")
        {
            if (!confirmations.TryGetValue(parts[2], out var confirmation) || confirmation.UserId != actor.UserId ||
                confirmation.ChannelId != component.Channel.Id || confirmation.ExpiresAt <= clock.GetUtcNow())
            { await ReplyAsync(component, "This confirmation is expired or belongs to another member.", ct); return; }
            if (!confirmations.TryRemove(parts[2], out _) || await AuthorizedCloseTicketAsync(confirmation.ChannelId, actor, confirmation.TicketId, ct) is null)
            { await ReplyAsync(component, "This ticket can no longer be closed by you.", ct); return; }
            await ReplyAsync(component, (await tickets.CloseAsync(confirmation.ChannelId, actor, ct)).Message, ct);
        }
        else await ReplyAsync(component, "Invalid ticket control.", ct);
    }
}
