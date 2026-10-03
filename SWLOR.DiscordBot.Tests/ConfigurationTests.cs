using SWLOR.DiscordBot.Configuration;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class ConfigurationTests
{
    [Test]
    public void Parse_IsCaseInsensitiveAndAcceptsQuotedDiscordIds()
    {
        var configuration = ConfigurationLoader.Parse("""
            { "GuildID": "123456789012345678", "Prefix": "!", "AdministratorRoleIds": ["234567890123456789"] }
            """);

        Assert.That(configuration.GuildId, Is.EqualTo(123456789012345678UL));
        Assert.That(configuration.AdministratorRoleIds, Is.EqualTo(new[] { 234567890123456789UL }));
        Assert.That(ConfigurationValidator.Validate(configuration), Is.Empty);
    }

    [Test]
    public void Validate_RejectsUnknownMacrosDuplicateCommandsAndStaffFactionOverlap()
    {
        var configuration = new BotConfiguration
        {
            GuildId = 1,
            AdministratorRoleIds = [10],
            Welcome = new WelcomeOptions { Enabled = true, ChannelId = 2, Template = "Hi {player}" },
            Factions = new FactionOptions
            {
                Enabled = true,
                Exclusive = true,
                Behavior = "toggle",
                Roles = [new FactionRole { Name = "Outer Rim Colonists", RoleId = 10 }]
            },
            Answers = [
                new QuickAnswerOptions { Name = "help", Responses = ["ok"] },
                new QuickAnswerOptions { Name = "HELP", Responses = ["also ok"] }
            ]
        };

        var errors = ConfigurationValidator.Validate(configuration);
        Assert.That(errors, Has.Some.Contains("unknown macro '{player}'"));
        Assert.That(errors, Has.Some.Contains("Duplicate quick answer command 'HELP'"));
        Assert.That(errors, Has.Some.Contains("overlaps an administrator or ticket support role"));
    }

    [Test]
    public void Validate_AllowsDisabledFeaturesWithoutTheirFeatureSpecificSettings()
    {
        var errors = ConfigurationValidator.Validate(new BotConfiguration { GuildId = 1 });
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_RejectsInvalidTicketRetentionAndUnknownAnswerMacros()
    {
        var configuration = new BotConfiguration
        {
            GuildId = 1,
            Tickets = new TicketOptions
            {
                Enabled = true,
                Panels = [new TicketPanelOptions { Id = "support", ChannelId = 2 }],
                ClosedCategoryId = 3,
                LogChannelId = 4,
                CleanupDelay = TimeSpan.FromDays(10),
                ArchiveRetentionDays = 7
            },
            Answers = [new QuickAnswerOptions { Name = "faq", Responses = ["{mystery}"] }]
        };

        var errors = ConfigurationValidator.Validate(configuration);
        Assert.That(errors, Has.Some.Contains("archiveRetentionDays"));
        Assert.That(errors, Has.Some.Contains("unknown macro '{mystery}'"));
    }

    [Test]
    public void Validate_RejectsNullSectionsAndUnmappedWelcomeChannels()
    {
        var nullSections = new BotConfiguration { GuildId = 1, Tickets = null!, Welcome = null!, Factions = null!, Answers = null! };
        var errors = ConfigurationValidator.Validate(nullSections);
        Assert.That(errors, Has.Some.Contains("tickets must not be null"));
        Assert.That(errors, Has.Some.Contains("welcome must not be null"));
        Assert.That(errors, Has.Some.Contains("factions must not be null"));
        Assert.That(errors, Has.Some.Contains("answers must not be null"));

        var welcome = new BotConfiguration { GuildId = 1, Welcome = new WelcomeOptions { Enabled = true, ChannelId = 2, Template = "Read {#rules}" } };
        Assert.That(ConfigurationValidator.Validate(welcome), Has.Some.Contains("without a valid channelMentions mapping"));
    }

    [Test]
    public void Validate_RequiresTicketSupportAndOpenCategoriesAndExplicitBypassScopes()
    {
        var config = new BotConfiguration
        {
            GuildId = 1,
            Tickets = new TicketOptions
            {
                Enabled = true,
                Panels = [new TicketPanelOptions { Id = "support", ChannelId = 2, OpenCategoryIds = [], PanelMessage = "Open a ticket" }],
                SupportRoleIds = [],
                BypassRoleIds = [3],
                ClosedCategoryId = 4,
                LogChannelId = 5
            }
        };

        var errors = ConfigurationValidator.Validate(config);
        Assert.That(errors, Has.Some.Contains("supportRoleIds must contain at least one role"));
        Assert.That(errors, Has.Some.Contains("openCategoryIds must contain at least one category"));
        Assert.That(errors, Has.Some.Contains("must all be explicitly set"));
    }

    [Test]
    public void Validate_RejectsReservedRankCommandEmptyEmbedsUnsafeUrlsAndOversizedResponses()
    {
        var config = new BotConfiguration
        {
            GuildId = 1,
            Factions = new FactionOptions { Enabled = true, Behavior = "toggle", Exclusive = false, Roles = [new FactionRole { Name = "Faction - Jedi Order", RoleId = 20 }] },
            Answers =
            [
                new QuickAnswerOptions { Name = "rank", Responses = ["Choose a faction"] },
                new QuickAnswerOptions
                {
                    Name = "help",
                    Responses = [new string('x', 2001)],
                    Embeds = [new AnswerEmbed { Url = "javascript:alert(1)" }, new AnswerEmbed { Title = "Safe title" }]
                }
            ]
        };

        var errors = ConfigurationValidator.Validate(config);
        Assert.That(errors, Has.Some.Contains("reserved for faction role selection"));
        Assert.That(errors, Has.Some.Contains("2000 character message limit"));
        Assert.That(errors, Has.Some.Contains("safe absolute HTTP or HTTPS URL"));
        Assert.That(errors, Has.Some.Contains("must contain a title, description, or field"));
    }

    [TestCase("""{"GuildId":1,"Answers":[{"Name":"help","Responses":["ok"],"Embeds":null}]}""", "answers[0].embeds must not be null.")]
    [TestCase("""{"GuildId":1,"Welcome":{"Enabled":true,"DirectMessage":true,"Template":"Welcome {user}","ChannelMentions":null}}""", "welcome.channelMentions must not be null.")]
    public async Task Validate_RejectsEnabledNullCollectionsBeforeCredentialsOrDiscordStartup(string json, string expectedError)
    {
        var configuration = ConfigurationLoader.Parse(json);
        Assert.That(ConfigurationValidator.Validate(configuration), Does.Contain(expectedError));
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path, json);
            Assert.That(ConfigurationValidator.Validate(ConfigurationLoader.Load(path)), Does.Contain(expectedError));
            Assert.That(await Program.Main(["--config", path, "--validate"]), Is.EqualTo(2));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [TestCase(" help")]
    [TestCase("help ")]
    [TestCase(" help ")]
    [TestCase("\thelp")]
    [TestCase("help\n")]
    public async Task Validate_RejectsSurroundingWhitespaceInStoredEnabledAnswerNames(string name)
    {
        var source = new BotConfiguration { GuildId = 1, Answers = [new QuickAnswerOptions { Name = name, Responses = ["ok"] }] };
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        var configuration = ConfigurationLoader.Parse(json);
        Assert.That(configuration.Answers[0].Name, Is.EqualTo(name));
        Assert.That(ConfigurationValidator.Validate(configuration), Has.Some.Contains("answers[0].name"));
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path, json);
            Assert.That(await Program.Main(["--config", path, "--validate"]), Is.EqualTo(2));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
        configuration.Answers[0].Name = "help";
        Assert.That(ConfigurationValidator.Validate(configuration), Is.Empty);
    }

    [Test]
    public void Validate_AllowsDisabledNullCollectionsAndInvalidCommandNames()
    {
        var configuration = ConfigurationLoader.Parse("""
            {"GuildId":1,"Welcome":{"Enabled":false,"ChannelMentions":null},"Answers":[{"Enabled":false,"Name":" help ","Embeds":null}]}
            """);
        Assert.That(ConfigurationValidator.Validate(configuration), Is.Empty);
    }

    [Test]
    public void Validate_RejectsUnknownJsonProperties()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => ConfigurationLoader.Parse("""{"GuildId":1,"TypoField":true}"""));
    }
}
