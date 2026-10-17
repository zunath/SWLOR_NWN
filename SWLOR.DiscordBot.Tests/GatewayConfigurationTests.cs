using Discord;
using NUnit.Framework;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Discord;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class GatewayConfigurationTests
{
    [Test]
    public void TranscriptCommandOffersExplicitOptionalSavedSnapshotDownload()
    {
        var transcript = DiscordGateway.TranscriptCommandOptions().Build();
        Assert.That(transcript.Name, Is.EqualTo("transcript"));
        Assert.That(transcript.Type, Is.EqualTo(ApplicationCommandOptionType.SubCommand));
        var saved = transcript.Options.Single();
        Assert.That(saved.Name, Is.EqualTo("saved"));
        Assert.That(saved.Type, Is.EqualTo(ApplicationCommandOptionType.Boolean));
        Assert.That(saved.Description, Is.EqualTo("Download the latest saved transcript"));
        Assert.That(saved.IsRequired, Is.False, "Fresh capture remains the default.");
    }
    [Test]
    public async Task GatewayQueuedJobsUseCallerCancellationWithoutAWholeOperationDeadline()
    {
        var gateway = new DiscordGateway(null!, new BotConfiguration(), null!, null!, null!, null!,
            TimeProvider.System, null!, null!, Microsoft.Extensions.Logging.Abstractions.NullLogger<DiscordGateway>.Instance);
        using var stopping = new CancellationTokenSource();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.That(gateway.TryQueueJob(async token =>
        {
            Assert.That(token, Is.EqualTo(stopping.Token), "Healthy scans must not inherit an additional whole-operation timer.");
            await Task.Yield();
            token.ThrowIfCancellationRequested();
            completed.SetResult();
        }), Is.True);
        var processor = gateway.ProcessAsync(stopping.Token);
        try { await completed.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
        finally
        {
            stopping.Cancel();
            try { await processor; } catch (OperationCanceledException) { }
        }
        Assert.That(processor.IsCompleted, Is.True);
    }
    [TestCase(1UL, false, false, true, true)]
    [TestCase(2UL, false, false, true, false)]
    [TestCase(1UL, true, false, true, false)]
    [TestCase(1UL, false, true, true, false)]
    [TestCase(1UL, false, false, false, false)]
    public void WelcomePersistenceFiltersConfiguredGuildHumanJoinPayloads(ulong guildId, bool bot, bool webhook,
        bool hasJoinedAt, bool expected)
    {
        var config = new BotConfiguration { GuildId = 1, Welcome = new WelcomeOptions { Enabled = true } };
        Assert.That(DiscordGateway.ShouldPersistWelcome(config, guildId, bot, webhook,
            hasJoinedAt ? DateTimeOffset.UnixEpoch : null), Is.EqualTo(expected));
        config.Welcome.Enabled = false;
        Assert.That(DiscordGateway.ShouldPersistWelcome(config, guildId, bot, webhook, DateTimeOffset.UnixEpoch), Is.False);
    }
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void PrefixCommandsCannotEnterReadOnlyOrHiddenChannels(bool view, bool send)
    {
        var config = new BotConfiguration
        {
            Factions = new FactionOptions { Enabled = true },
            Answers = [new QuickAnswerOptions { Name = "help", Responses = ["ok"] }]
        };
        var permissions = new ChannelPermissions(viewChannel: view, sendMessages: send);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, permissions, 10, "?rank Jedi"), Is.False);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, permissions, 10, "?help"), Is.False);
    }

    [TestCase("  ?help")]
    [TestCase("\t\r\n?help details")]
    [TestCase("\u2003?help")]
    [TestCase("?help")]
    public void CommunityDispatchPermissionGateAcceptsNormalizedPrefixesWithoutRelaxingChannelAccess(string content)
    {
        var config = new BotConfiguration
        {
            Answers = [new QuickAnswerOptions { Name = "help", Responses = ["answer"], AllowedChannelIds = [10] }]
        };
        var writable = new ChannelPermissions(viewChannel: true, sendMessages: true);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, writable, 10, content), Is.True);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, writable, 11, content), Is.False);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config,
            new ChannelPermissions(viewChannel: true), 10, content), Is.False);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, writable, 10, "  ordinary text ?help"), Is.False);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, writable, 10, "  ?missing"), Is.False);
    }

    [Test]
    public void RuntimePermissionChangesBlockFactionMutationAndCommandDeletion()
    {
        var config = new BotConfiguration { Factions = new FactionOptions { Enabled = true, DeleteCommand = true } };
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config,
            new ChannelPermissions(viewChannel: true, sendMessages: true, manageMessages: true), 10, "?rank Jedi"), Is.True);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config,
            new ChannelPermissions(viewChannel: true, sendMessages: true), 10, "?rank Jedi"), Is.False);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config,
            new ChannelPermissions(viewChannel: true, manageMessages: true), 10, "?rank Jedi"), Is.False);
    }

    [Test]
    public void PrefixGateChecksOnlyTheSelectedAnswersScopeAndCapabilities()
    {
        var config = new BotConfiguration
        {
            Answers =
            [
                new QuickAnswerOptions { Name = "plain", DeleteResponseAfter = TimeSpan.FromSeconds(5) },
                new QuickAnswerOptions { Name = "embed", Embeds = [new AnswerEmbed { Title = "Help" }] },
                new QuickAnswerOptions { Name = "delete", DeleteCommand = true },
                new QuickAnswerOptions { Name = "scoped", AllowedChannelIds = [11] },
                new QuickAnswerOptions { Name = "disabled", Enabled = false }
            ]
        };
        var ordinary = new ChannelPermissions(viewChannel: true, sendMessages: true);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, ordinary, 10, "?plain any arguments"), Is.True);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, ordinary, 10, "?embed"), Is.False);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, ordinary, 10, "?delete"), Is.False);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, ordinary, 10, "?scoped"), Is.False);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, ordinary, 11, "?scoped"), Is.True);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, ordinary, 10, "?disabled"), Is.False);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, ordinary, 10, "?missing"), Is.False);
        var privileged = new ChannelPermissions(viewChannel: true, sendMessages: true, embedLinks: true, manageMessages: true);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, privileged, 10, "?EMBED"), Is.True);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, privileged, 10, "?delete"), Is.True);
    }

    [Test]
    public void ResponseOnlyFactionDeletionNeedsNoModerationPermissionAndDisabledFactionRankCanBeAnAnswer()
    {
        var config = new BotConfiguration
        {
            Factions = new FactionOptions { Enabled = true, DeleteResponse = true },
            Answers = [new QuickAnswerOptions { Name = "rank" }]
        };
        var ordinary = new ChannelPermissions(viewChannel: true, sendMessages: true);
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, ordinary, 10, "?rank Jedi"), Is.True);
        config.Factions.Enabled = false;
        Assert.That(DiscordGateway.CanExecuteCommunityCommand(config, ordinary, 10, "?rank arguments"), Is.True);
    }

    [TestCase(true, "Close this ticket? The channel will become read-only for the requester.")]
    [TestCase(false, "Close this ticket? The channel will be hidden from the requester.")]
    public void CloseConfirmationDescribesConfiguredRequesterVisibility(bool requesterCanRead, string expected)
    {
        Assert.That(DiscordGateway.CloseConfirmationText(requesterCanRead), Is.EqualTo(expected));
    }

    [Test]
    public void TicketOnlyDeploymentDoesNotSubscribeToMessageContentOrMessageEvents()
    {
        var config = new BotConfiguration { Tickets = new TicketOptions { Enabled = true } };
        Assert.That(Program.GatewayIntentsFor(config), Is.EqualTo(GatewayIntents.Guilds));
    }

    [Test]
    public void WelcomeOnlyDeploymentRequestsMembersWithoutMessageContent()
    {
        var config = new BotConfiguration { Welcome = new WelcomeOptions { Enabled = true } };
        Assert.That(Program.GatewayIntentsFor(config), Is.EqualTo(GatewayIntents.Guilds | GatewayIntents.GuildMembers));
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void PrefixFeaturesRequestGuildMessagesAndContent(bool factions, bool answers)
    {
        var config = new BotConfiguration
        {
            Factions = new FactionOptions { Enabled = factions },
            Answers = [new QuickAnswerOptions { Enabled = answers }]
        };
        Assert.That(Program.GatewayIntentsFor(config),
            Is.EqualTo(GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent));
    }

    [TestCase(ApplicationFlags.GatewayGuildMembers)]
    [TestCase(ApplicationFlags.GatewayGuildMembersLimited)]
    public void WelcomePreflightAcceptsVerifiedAndLimitedMembersCapability(ApplicationFlags flags)
    {
        var config = new BotConfiguration { Welcome = new WelcomeOptions { Enabled = true } };
        Assert.DoesNotThrow(() => Program.ValidateApplicationCapabilities(config, flags));
        Assert.That(() => Program.ValidateApplicationCapabilities(config, 0),
            Throws.TypeOf<DiscordValidationException>().With.Message.Contains("Enable Server Members Intent"));
    }

    [TestCase(true, false, ApplicationFlags.GatewayMessageContent)]
    [TestCase(true, false, ApplicationFlags.GatewayMessageContentLimited)]
    [TestCase(false, true, ApplicationFlags.GatewayMessageContent)]
    [TestCase(false, true, ApplicationFlags.GatewayMessageContentLimited)]
    public void PrefixPreflightAcceptsVerifiedAndLimitedContentCapability(bool factions, bool answers, ApplicationFlags flags)
    {
        var config = new BotConfiguration
        {
            Factions = new FactionOptions { Enabled = factions },
            Answers = [new QuickAnswerOptions { Enabled = answers }]
        };
        Assert.DoesNotThrow(() => Program.ValidateApplicationCapabilities(config, flags));
        Assert.That(() => Program.ValidateApplicationCapabilities(config, 0),
            Throws.TypeOf<DiscordValidationException>().With.Message.Contains("Enable Message Content Intent"));
    }

    [Test]
    public void CombinedFeaturesRequireBothPrivilegedCapabilities()
    {
        var config = new BotConfiguration
        {
            Welcome = new WelcomeOptions { Enabled = true },
            Factions = new FactionOptions { Enabled = true }
        };
        Assert.That(() => Program.ValidateApplicationCapabilities(config, ApplicationFlags.GatewayMessageContentLimited),
            Throws.TypeOf<DiscordValidationException>().With.Message.Contains("Server Members Intent"));
        Assert.That(() => Program.ValidateApplicationCapabilities(config, ApplicationFlags.GatewayGuildMembersLimited),
            Throws.TypeOf<DiscordValidationException>().With.Message.Contains("Message Content Intent"));
        Assert.DoesNotThrow(() => Program.ValidateApplicationCapabilities(config,
            ApplicationFlags.GatewayGuildMembersLimited | ApplicationFlags.GatewayMessageContentLimited));
    }

    [Test]
    public void TicketOnlyPreflightChecksRestContentAccessWithoutRequestingGatewayContent()
    {
        var config = new BotConfiguration { Tickets = new TicketOptions { Enabled = true } };
        Assert.That(Program.GatewayIntentsFor(config), Is.EqualTo(GatewayIntents.Guilds));
        Assert.That(() => Program.ValidateApplicationCapabilities(config, 0),
            Throws.TypeOf<DiscordValidationException>().With.Message.Contains("ticket transcripts"));
        Assert.DoesNotThrow(() => Program.ValidateApplicationCapabilities(config, ApplicationFlags.GatewayMessageContentLimited));
    }

    [Test]
    public async Task PreflightCompletesBeforeStartingGatewayAndDisabledFeaturesNeedNoPrivilegedCapabilities()
    {
        var config = new BotConfiguration { Answers = [new QuickAnswerOptions { Enabled = false }] };
        var events = new List<string>();
        await Program.StartGatewayAsync(config,
            () => { events.Add("application"); return Task.FromResult((ApplicationFlags)0); },
            () => { events.Add("gateway"); return Task.CompletedTask; });
        Assert.That(events, Is.EqualTo(new[] { "application", "gateway" }));
    }

    [Test]
    public void PreflightFailurePreventsGatewayIdentifyAndRetainsSafeActionableError()
    {
        var config = new BotConfiguration { Welcome = new WelcomeOptions { Enabled = true } };
        var started = false;
        var error = Assert.ThrowsAsync<DiscordValidationException>(() => Program.StartGatewayAsync(config,
            () => Task.FromResult((ApplicationFlags)0),
            () => { started = true; return Task.CompletedTask; }));
        Assert.That(started, Is.False);
        Assert.That(DiscordGateway.SafeError(error!), Does.Contain("Enable Server Members Intent"));
    }

    [Test]
    public void PreflightApplicationLookupFailurePreventsGatewayIdentifyAndRemainsSanitized()
    {
        var started = false;
        var failure = new System.Net.Http.HttpRequestException("sensitive credentials");
        var error = Assert.ThrowsAsync<System.Net.Http.HttpRequestException>(() => Program.StartGatewayAsync(new BotConfiguration(),
            () => Task.FromException<ApplicationFlags>(failure),
            () => { started = true; return Task.CompletedTask; }));
        Assert.That(started, Is.False);
        Assert.That(error, Is.SameAs(failure));
        Assert.That(DiscordGateway.SafeError(error!), Is.EqualTo(nameof(System.Net.Http.HttpRequestException)));
    }

    [Test]
    public async Task DisablingIntakeRemovesOnlyThePanelSlashCommandAndRetainsManagement()
    {
        var remaining = new Dictionary<int, (string Name, ApplicationCommandType Type)>
        {
            [1] = ("ticket", ApplicationCommandType.Slash),
            [2] = ("ticket-panel", ApplicationCommandType.Slash),
            [3] = ("help", ApplicationCommandType.Slash),
            [4] = ("ticket", ApplicationCommandType.User),
            [5] = ("ticket-panel", ApplicationCommandType.Message)
        };
        async Task Synchronize()
        {
            var commands = remaining.Select(entry => (entry.Value.Name, entry.Value.Type,
                (Func<Task>)(() => { remaining.Remove(entry.Key); return Task.CompletedTask; }))).ToArray();
            await TicketCommandRegistration.RemoveDisabledIntakeAsync(commands, CancellationToken.None);
        }

        await Synchronize();
        await Synchronize();

        Assert.That(remaining.Keys, Is.EquivalentTo(new[] { 1, 3, 4, 5 }));
    }

    [Test]
    public void SafeErrorsRetainLocallyAuthoredValidationDetailsWithoutExposingForeignExceptions()
    {
        var validation = new DiscordValidationException("Configured text channel 123 is unavailable.");
        Assert.That(DiscordGateway.SafeError(validation), Is.EqualTo(validation.Message));
        Assert.That(DiscordGateway.SafeError(new InvalidOperationException("sensitive request payload or credentials")),
            Is.EqualTo(nameof(InvalidOperationException)));
        Assert.That(DiscordGateway.SafeError(new System.Net.Http.HttpRequestException("sensitive signed URL")),
            Is.EqualTo(nameof(System.Net.Http.HttpRequestException)));
    }

    [Test]
    public void CommandRemovalFailureDoesNotReportSuccessfulStartup()
    {
        var commands = new[] { ("ticket-panel", ApplicationCommandType.Slash,
            (Func<Task>)(() => Task.FromException(new InvalidOperationException("Discord rejected deletion")))) };
        Assert.ThrowsAsync<InvalidOperationException>(() => TicketCommandRegistration.RemoveDisabledIntakeAsync(commands, CancellationToken.None));
    }
}
