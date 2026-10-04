using SWLOR.DiscordBot.Hosting;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;

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
    public void Parse_NormalizesWelcomeMentionKeysBeforeValidationAndRendering()
    {
        var configuration = ConfigurationLoader.Parse("""
            {
                "GuildId": 1,
                "Welcome": {
                    "Enabled": true,
                    "ChannelId": 2,
                    "Template": "Visit {#RULES}.",
                    "ChannelMentions": { "rules": 3 }
                }
            }
            """);

        Assert.That(configuration.Welcome.ChannelMentions.Comparer, Is.EqualTo(StringComparer.OrdinalIgnoreCase));
        Assert.That(ConfigurationValidator.Validate(configuration), Is.Empty);
        Assert.That(TemplateRenderer.RenderWelcome(
            configuration.Welcome.Template,
            7,
            "SWLOR",
            configuration.Welcome.ChannelMentions), Is.EqualTo("Visit <#3>."));
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

    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, true)]
    [TestCase(true, true, true)]
    [TestCase(false, false, false)]
    public void Factions_RejectTicketBypassRolesRegardlessOfExemptionScope(bool member, bool panel, bool guild)
    {
        var configuration = new BotConfiguration
        {
            GuildId = 1,
            Tickets = new TicketOptions
            {
                Enabled = true,
                Panels = [new TicketPanelOptions { ChannelId = 2, OpenCategoryIds = [3], PanelMessage = "Open ticket" }],
                ClosedCategoryId = 4, LogChannelId = 5, SupportRoleIds = [6], BypassRoleIds = [10],
                BypassMemberLimit = member, BypassPanelLimit = panel, BypassGuildLimit = guild,
                ArchiveDirectory = Path.GetTempPath()
            },
            Factions = new FactionOptions
            {
                Enabled = true, Exclusive = true, Behavior = "join",
                Roles = [new FactionRole { Name = "Jedi", RoleId = 10 }]
            }
        };
        var errors = ConfigurationValidator.Validate(configuration);
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0], Does.Contain("ticket bypass role"));
        configuration.Factions.Roles[0].RoleId = 11;
        Assert.That(ConfigurationValidator.Validate(configuration), Is.Empty);
        configuration.Factions.Roles[0].RoleId = 6;
        Assert.That(ConfigurationValidator.Validate(configuration), Has.Some.Contains("ticket support role"));
    }

    [Test]
    public void Factions_DisabledTicketsDoNotProtectUnusedSupportOrBypassRoles()
    {
        var configuration = new BotConfiguration
        {
            GuildId = 1, AdministratorRoleIds = [12],
            Tickets = new TicketOptions { Enabled = false, SupportRoleIds = [10], BypassRoleIds = [11] },
            Factions = new FactionOptions
            {
                Enabled = true, Exclusive = true, Behavior = "join",
                Roles = [new FactionRole { Name = "Jedi", RoleId = 10 }, new FactionRole { Name = "Sith", RoleId = 11 }]
            }
        };
        Assert.That(ConfigurationValidator.Validate(configuration), Is.Empty);
        configuration.Factions.Roles[0].RoleId = 12;
        Assert.That(ConfigurationValidator.Validate(configuration), Has.Some.Contains("administrator"));
    }

    [Test]
    public void Answers_MayMatchFactionDisplayNamesButRankRemainsReserved()
    {
        var configuration = ConfigurationLoader.Parse("""
            {
                "GuildId":1,
                "Factions":{"Enabled":true,"Exclusive":true,"Behavior":"join","Roles":[{"Name":"Jedi","RoleId":10}]},
                "Answers":[{"Name":"jedi","Responses":["Use ?rank Jedi to join."]}]
            }
            """);
        Assert.That(ConfigurationValidator.Validate(configuration), Is.Empty);
        configuration.Answers[0].Name = "RANK";
        Assert.That(ConfigurationValidator.Validate(configuration), Has.Some.Contains("reserved for faction role selection"));
        configuration.Factions.Enabled = false;
        Assert.That(ConfigurationValidator.Validate(configuration), Is.Empty);
    }

    [Test]
    public void Validate_RejectsUnknownJsonProperties()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => ConfigurationLoader.Parse("""{"GuildId":1,"TypoField":true}"""));
    }

    [TestCase(TicketState.Creating)]
    [TestCase(TicketState.Open)]
    [TestCase(TicketState.Closing)]
    [TestCase(TicketState.Closed)]
    [TestCase(TicketState.Reopening)]
    [TestCase(TicketState.Deleting)]
    public void PersistedChannelsRequireRetainedTicketSettingsEvenWhenNewTicketingIsDisabled(TicketState state)
    {
        var config = new BotConfiguration { GuildId = 1, Tickets = new TicketOptions { Enabled = false, ArchiveDirectory = "" } };
        Assert.That(ConfigurationValidator.Validate(config), Is.Empty, "the config-only check cannot know persisted state");
        var errors = ConfigurationValidator.ValidatePersistedTickets(config, [StoredTicket(state)]);
        Assert.That(errors, Has.Some.Contains("tickets.panels"));
        Assert.That(errors, Has.Some.Contains("tickets.supportRoleIds"));
        Assert.That(errors, Has.Some.Contains("tickets.archiveDirectory"));
    }

    [Test]
    public void DisabledTicketingWithNoRetainedRecordsStillAllowsInitialConfiguration()
    {
        var config = new BotConfiguration { GuildId = 1, Tickets = new TicketOptions { Enabled = false, Panels = null!, SupportRoleIds = null!, ArchiveDirectory = "" } };
        Assert.That(ConfigurationValidator.ValidatePersistedTickets(config, []), Is.Empty);
        Assert.That(ConfigurationValidator.ValidatePersistedTickets(config, [StoredTicket(TicketState.Deleted)]), Is.Empty);
    }

    [TestCase(TicketState.Creating)]
    [TestCase(TicketState.Reopening)]
    public void PendingChannelOperationsRequireTheirOriginalPanel(TicketState state)
    {
        var config = RetainedTicketConfiguration();
        Assert.That(ConfigurationValidator.ValidatePersistedTickets(config, [StoredTicket(state)]), Is.Empty);
        config.Tickets.Panels[0].Id = "replacement";
        Assert.That(ConfigurationValidator.ValidatePersistedTickets(config, [StoredTicket(state)]), Has.Some.Contains("retain panel 'support'"));
    }

    [Test]
    public void DeletedArchiveRetentionRequiresOnlyTheOriginalArchiveDirectory()
    {
        var config = new BotConfiguration { GuildId = 1, Tickets = new TicketOptions { Enabled = false, ArchiveDirectory = Path.GetTempPath() } };
        var deleted = StoredTicket(TicketState.Deleted);
        deleted = deleted with
        {
            ArchivePath = Path.Combine(config.Tickets.ArchiveDirectory, deleted.Id.ToString("N")),
            ArchiveExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Assert.That(ConfigurationValidator.ValidatePersistedTickets(config, [deleted]), Is.Empty);
        config.Tickets.ArchiveDirectory = "";
        var errors = ConfigurationValidator.ValidatePersistedTickets(config, [deleted]);
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors.Single(), Does.Contain("tickets.archiveDirectory"));
    }

    [Test]
    public void ChangingAnArchiveRootFailsBeforeRetentionCanAbandonTheOldDirectory()
    {
        var config = RetainedTicketConfiguration();
        var ticket = StoredTicket(TicketState.Deleted);
        ticket = ticket with { ArchivePath = Path.Combine(config.Tickets.ArchiveDirectory, ticket.Id.ToString("N")) };
        config.Tickets.ArchiveDirectory = Path.Combine(Path.GetTempPath(), "replacement-archive-root");
        Assert.That(ConfigurationValidator.Validate(config), Is.Empty);
        Assert.That(ConfigurationValidator.ValidatePersistedTickets(config, [ticket]), Has.Some.Contains("retain ownership of the persisted archive"));
    }

    [Test]
    public void HeldChannelsStillRequireRetainedPolicyAndCannotExposeSupportRolesAsSelfSelectableFactions()
    {
        var config = RetainedTicketConfiguration();
        config.Factions = new FactionOptions
        {
            Enabled = true, Exclusive = false, Behavior = "join", Roles = [new FactionRole { Name = "Support", RoleId = 6 }]
        };
        Assert.That(ConfigurationValidator.Validate(config), Is.Empty);
        Assert.That(ConfigurationValidator.ValidatePersistedTickets(config, [StoredTicket(TicketState.Closed) with { Hold = true }]),
            Has.Some.Contains("retained for persisted maintenance"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task WorkerStartupValidationReadsDurableRecordsBeforeDiscordInitialization(bool existing)
    {
        var config = new BotConfiguration { GuildId = 1 };
        var store = new StoredTicketFixture(existing ? [StoredTicket(TicketState.Creating)] : []);
        var errors = await BotWorker.ValidatePersistedConfigurationAsync(config, store, default);
        Assert.That(store.Read, Is.True);
        Assert.That(store.Disposed, Is.True);
        Assert.That(errors.Count > 0, Is.EqualTo(existing));
    }

    [TestCase(0L, false)]
    [TestCase(-1L, false)]
    [TestCase(10737418241L, false)]
    [TestCase(1L, true)]
    [TestCase(10737418240L, true)]
    public void TicketAttachmentBudgetMustBePositiveAndFinite(long bytes, bool valid)
    {
        var config = RetainedTicketConfiguration();
        config.Tickets.Enabled = true;
        config.Tickets.MaxTicketAttachmentBytes = bytes;
        var errors = ConfigurationValidator.Validate(config);
        Assert.That(errors.Any(error => error.Contains("maxTicketAttachmentBytes", StringComparison.Ordinal)), Is.EqualTo(!valid));
    }

    [Test]
    public void OmittedTicketAttachmentBudgetHasCompatibleFiniteDefaultAndExplicitValueParses()
    {
        var config = ConfigurationLoader.Parse("""{"GuildId":1,"Tickets":{"Enabled":false}}""");
        Assert.That(config.Tickets.MaxTicketAttachmentBytes, Is.EqualTo(1073741824L));
        config = ConfigurationLoader.Parse("""{"GuildId":1,"Tickets":{"Enabled":false,"MaxTicketAttachmentBytes":1234}}""");
        Assert.That(config.Tickets.MaxTicketAttachmentBytes, Is.EqualTo(1234L));
    }

    private static Ticket StoredTicket(TicketState state) => new(Guid.NewGuid(), "support", 7, 20, state, 1, DateTimeOffset.UtcNow);

    private static BotConfiguration RetainedTicketConfiguration() => new()
    {
        GuildId = 1,
        Tickets = new TicketOptions
        {
            Enabled = false,
            Panels = [new TicketPanelOptions { Id = "support", ChannelId = 2, OpenCategoryIds = [3], PanelMessage = "Open ticket" }],
            ClosedCategoryId = 4, LogChannelId = 5, SupportRoleIds = [6], ArchiveDirectory = Path.GetTempPath()
        }
    };

    private sealed class StoredTicketFixture(IReadOnlyList<Ticket> tickets) : ITicketStore, ITicketSession
    {
        internal bool Read;
        internal bool Disposed;
        public Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<ITicketSession> LockAsync(CancellationToken ct) => Task.FromResult<ITicketSession>(this);
        public Task<IReadOnlyList<Ticket>> GetTicketsAsync(CancellationToken ct) { Read = true; return Task.FromResult(tickets); }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
        public Task<Ticket> ReserveAsync(string panelId, ulong requesterId, string interactionId, DateTimeOffset now, CancellationToken ct) => throw new NotSupportedException();
        public Task<Ticket?> FindInteractionAsync(string interactionId, CancellationToken ct) => throw new NotSupportedException();
        public Task SaveAsync(Ticket ticket, string action, ulong? actorId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> TryRecordDeliveryAsync(string key, CancellationToken ct) => throw new NotSupportedException();
        public Task<DeliveryState> GetOrCreateDeliveryAsync(string key, string intent, CancellationToken ct) => throw new NotSupportedException();
        public Task CompleteDeliveryAsync(string key, CancellationToken ct) => throw new NotSupportedException();
        public Task<DateTimeOffset?> GetCooldownAsync(string key, CancellationToken ct) => throw new NotSupportedException();
        public Task SetCooldownAsync(string key, DateTimeOffset at, CancellationToken ct) => throw new NotSupportedException();
    }
}
