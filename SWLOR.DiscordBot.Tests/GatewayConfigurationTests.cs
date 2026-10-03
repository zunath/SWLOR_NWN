using Discord;
using NUnit.Framework;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Discord;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class GatewayConfigurationTests
{
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
    public async Task DisablingTicketsRemovesOnlyOwnedSlashCommandsAndIsRepeatable()
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
            await TicketCommandRegistration.RemoveDisabledAsync(commands, CancellationToken.None);
        }

        await Synchronize();
        await Synchronize();

        Assert.That(remaining.Keys, Is.EquivalentTo(new[] { 3, 4, 5 }));
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
        var commands = new[] { ("ticket", ApplicationCommandType.Slash,
            (Func<Task>)(() => Task.FromException(new InvalidOperationException("Discord rejected deletion")))) };
        Assert.ThrowsAsync<InvalidOperationException>(() => TicketCommandRegistration.RemoveDisabledAsync(commands, CancellationToken.None));
    }
}
