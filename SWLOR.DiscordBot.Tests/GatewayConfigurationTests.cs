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
