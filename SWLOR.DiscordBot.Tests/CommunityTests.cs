using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class CommunityTests
{
    [Test]
    public async Task WelcomeAsync_RecordsDeliveryAndSuppressesDuplicateJoinEvents()
    {
        var config = new BotConfiguration
        {
            GuildId = 1,
            Welcome = new WelcomeOptions { Enabled = true, ChannelId = 100, Template = "Welcome {user} to {server}! Visit {#rules}.", ChannelMentions = new() { ["rules"] = 200 } }
        };
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { ServerName = "SWLOR", Member = new CommunityMember(7, "A Player", []) };
        var service = new CommunityService(config, store, discord);
        var joinedAt = DateTimeOffset.Parse("2026-10-03T12:00:00Z");

        await service.WelcomeAsync(7, joinedAt, CancellationToken.None);
        await service.WelcomeAsync(7, joinedAt, CancellationToken.None);

        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        Assert.That(discord.Sent[0].Message.Content, Is.EqualTo("Welcome <@7> to SWLOR! Visit <#200>."));
    }

    [Test]
    public async Task WelcomeAsync_RetriesFailedSendUsingThePersistedDeliveryIntent()
    {
        var config = new BotConfiguration { GuildId = 1, Welcome = new WelcomeOptions { Enabled = true, ChannelId = 100, Template = "Welcome {user}" } };
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "A Player", []), SendFailuresRemaining = 1 };
        var service = new CommunityService(config, new FakeTicketStore(), discord);
        var joinedAt = DateTimeOffset.Parse("2026-10-03T12:00:00Z");

        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.WelcomeAsync(7, joinedAt, CancellationToken.None));
        await service.WelcomeAsync(7, joinedAt, CancellationToken.None);

        Assert.That(discord.Sent, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_ParsesRankFactionPhraseAndDoesNotToggleBackOnDuplicateDelivery()
    {
        var config = new BotConfiguration
        {
            GuildId = 1,
            Prefix = "?",
            Factions = new FactionOptions
            {
                Enabled = true,
                Behavior = "join",
                Exclusive = true,
                Roles = [new FactionRole { Name = "Outer Rim Colonists", RoleId = 20 }, new FactionRole { Name = "Republic Navy", RoleId = 21 }]
            }
        };
        var discord = new FakeCommunityDiscord
        {
            Member = new CommunityMember(7, "A Player", [20]),
            Roles = [new CommunityRole(20, 1), new CommunityRole(21, 1)]
        };
        var service = new CommunityService(config, new FakeTicketStore(), discord);

        await service.ExecuteAsync(7, 100, 300, "?rank Republic Navy", CancellationToken.None);
        await service.ExecuteAsync(7, 100, 300, "?rank Republic Navy", CancellationToken.None);

        Assert.That(discord.RoleMutations, Is.EqualTo(new[] { "remove:7:20", "add:7:21" }));
        Assert.That(discord.Sent.Single().Message.Content, Is.EqualTo("Added the Republic Navy role."));
    }

    [Test]
    public async Task ExecuteAsync_RetriesFactionRolePlanWithoutReversingToggleAfterPartialFailure()
    {
        var config = new BotConfiguration
        {
            GuildId = 1,
            Prefix = "?",
            Factions = new FactionOptions
            {
                Enabled = true,
                Behavior = "join",
                Exclusive = true,
                Roles = [new FactionRole { Name = "Republic Navy", RoleId = 21 }, new FactionRole { Name = "Outer Rim Colonists", RoleId = 20 }]
            }
        };
        var discord = new FakeCommunityDiscord
        {
            Member = new CommunityMember(7, "A Player", [20]),
            Roles = [new CommunityRole(20, 1), new CommunityRole(21, 1)],
            FailAfterNextRemove = true
        };
        var service = new CommunityService(config, new FakeTicketStore(), discord);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.ExecuteAsync(7, 100, 301, "?rank Republic Navy", CancellationToken.None));
        await service.ExecuteAsync(7, 100, 301, "?rank Republic Navy", CancellationToken.None);

        Assert.That(discord.Member!.RoleIds, Does.Contain(21));
        Assert.That(discord.Member.RoleIds, Does.Not.Contain(20));
        Assert.That(discord.RoleMutations, Does.Not.Contain("remove:7:21"));
    }

    [Test]
    public async Task ExecuteAsync_SendsFactionResponseBeforeSchedulingItsDeletion()
    {
        var config = new BotConfiguration
        {
            GuildId = 1,
            Prefix = "?",
            Factions = new FactionOptions
            {
                Enabled = true,
                DeleteResponse = true,
                Roles = [new FactionRole { Name = "Republic Navy", RoleId = 21 }]
            }
        };
        var discord = new FakeCommunityDiscord
        {
            Member = new CommunityMember(7, "A Player", []),
            Roles = [new CommunityRole(21, 1)]
        };
        var service = new CommunityService(config, new FakeTicketStore(), discord);

        await service.ExecuteAsync(7, 100, 302, "?rank Republic Navy", CancellationToken.None);

        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        Assert.That(discord.Sent[0].Message.Content, Is.EqualTo("Added the Republic Navy role."));
        Assert.That(discord.Sent[0].Message.DeleteAfter, Is.EqualTo(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public async Task ExecuteAsync_EnforcesAnswerAllowListsAndCooldownAndRendersArguments()
    {
        var config = new BotConfiguration
        {
            GuildId = 1,
            Prefix = "?",
            Answers = [new QuickAnswerOptions
            {
                Name = "guide",
                Responses = ["Hi {user}: {1} / {args} on {server}"],
                AllowedRoleIds = [9],
                AllowedChannelIds = [100],
                Cooldown = TimeSpan.FromMinutes(1),
                DeleteCommand = true
            }]
        };
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { ServerName = "SWLOR", Member = new CommunityMember(7, "A Player", [9]) };
        var service = new CommunityService(config, store, discord);

        await service.ExecuteAsync(7, 999, 300, "?guide alpha beta", CancellationToken.None);
        Assert.That(discord.Sent, Is.Empty, "a channel outside the allow list must receive no answer");
        await service.ExecuteAsync(7, 100, 301, "?guide alpha beta", CancellationToken.None);
        await service.ExecuteAsync(7, 100, 302, "?guide other", CancellationToken.None);

        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        Assert.That(discord.Sent[0].Message.Content, Is.EqualTo("Hi <@7>: alpha / alpha beta on SWLOR"));
        Assert.That(discord.Deleted, Is.EqualTo(new[] { (100UL, 301UL) }));
    }

    [Test]
    public async Task ExecuteAsync_EnforcesPersistentCooldownAtItsExactBoundary()
    {
        var config = new BotConfiguration { GuildId = 1, Prefix = "?", Answers = [new QuickAnswerOptions { Name = "guide", Responses = ["guide"], Cooldown = TimeSpan.FromMinutes(1) }] };
        var time = new ManualTimeProvider(DateTimeOffset.Parse("2026-10-03T12:00:00Z"));
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "A Player", []) };
        var service = new CommunityService(config, new FakeTicketStore(), discord, time);

        await service.ExecuteAsync(7, 100, 401, "?guide", CancellationToken.None);
        time.Advance(TimeSpan.FromSeconds(59));
        await service.ExecuteAsync(7, 100, 402, "?guide", CancellationToken.None);
        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        time.Advance(TimeSpan.FromSeconds(1));
        await service.ExecuteAsync(7, 100, 403, "?guide", CancellationToken.None);

        Assert.That(discord.Sent, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ExecuteAsync_DoesNotSendOrConsumeCooldownForAnEmptyRenderedAnswer()
    {
        var config = new BotConfiguration
        {
            GuildId = 1,
            Prefix = "?",
            Answers = [new QuickAnswerOptions { Name = "guide", Responses = ["{1}"], Cooldown = TimeSpan.FromMinutes(1) }]
        };
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "A Player", []) };
        var service = new CommunityService(config, store, discord);

        await service.ExecuteAsync(7, 100, 501, "?guide", CancellationToken.None);
        await service.ExecuteAsync(7, 100, 501, "?guide", CancellationToken.None);
        await service.ExecuteAsync(7, 100, 502, "?guide alpha", CancellationToken.None);

        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        Assert.That(discord.Sent[0].Message.Content, Is.EqualTo("alpha"));
    }

    [Test]
    public async Task ExecuteAsync_IgnoresBotsAndWebhooks()
    {
        var config = new BotConfiguration { GuildId = 1, Prefix = "?", Answers = [new QuickAnswerOptions { Name = "hello", Responses = ["hello"] }] };
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "Bot", [], IsBot: true) };
        var service = new CommunityService(config, new FakeTicketStore(), discord);

        await service.ExecuteAsync(7, 100, 300, "?hello", CancellationToken.None);

        Assert.That(discord.Sent, Is.Empty);
    }

    private sealed class FakeTicketStore : ITicketStore
    {
        private readonly HashSet<string> _deliveries = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DeliveryState> _states = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTimeOffset> _cooldowns = new(StringComparer.Ordinal);
        public Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<ITicketSession> LockAsync(CancellationToken ct) => Task.FromResult<ITicketSession>(new Session(_deliveries, _states, _cooldowns));

        private sealed class Session(HashSet<string> deliveries, Dictionary<string, DeliveryState> states, Dictionary<string, DateTimeOffset> cooldowns) : ITicketSession
        {
            public Task<IReadOnlyList<Ticket>> GetTicketsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Ticket>>([]);
            public Task<Ticket> ReserveAsync(string panelId, ulong requesterId, string interactionId, DateTimeOffset now, CancellationToken ct) => throw new NotSupportedException();
            public Task<Ticket?> FindInteractionAsync(string interactionId, CancellationToken ct) => Task.FromResult<Ticket?>(null);
            public Task SaveAsync(Ticket ticket, string action, ulong? actorId, CancellationToken ct) => Task.CompletedTask;
            public Task<bool> TryRecordDeliveryAsync(string key, CancellationToken ct) => Task.FromResult(deliveries.Add(key));
            public Task<DeliveryState> GetOrCreateDeliveryAsync(string key, string intent, CancellationToken ct)
            {
                if (!states.TryGetValue(key, out var state)) states[key] = state = new DeliveryState(intent, false);
                return Task.FromResult(state);
            }
            public Task CompleteDeliveryAsync(string key, CancellationToken ct) { states[key] = states[key] with { Completed = true }; return Task.CompletedTask; }
            public Task<DateTimeOffset?> GetCooldownAsync(string key, CancellationToken ct) => Task.FromResult(cooldowns.TryGetValue(key, out var value) ? (DateTimeOffset?)value : null);
            public Task SetCooldownAsync(string key, DateTimeOffset at, CancellationToken ct) { cooldowns[key] = at; return Task.CompletedTask; }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class FakeCommunityDiscord : ICommunityDiscord
    {
        public string ServerName { get; init; } = "Server";
        public CommunityMember? Member { get; set; }
        public List<CommunityRole> Roles { get; init; } = [];
        public List<(ulong ChannelId, CommunityMessage Message)> Sent { get; } = [];
        public List<string> RoleMutations { get; } = [];
        public List<(ulong ChannelId, ulong MessageId)> Deleted { get; } = [];
        public int SendFailuresRemaining { get; set; }
        public bool FailAfterNextRemove { get; set; }
        private readonly Dictionary<string, ulong> _sentKeys = new(StringComparer.Ordinal);
        public Task<CommunityMember?> GetMemberAsync(ulong userId, CancellationToken ct) => Task.FromResult(Member?.UserId == userId ? Member : null);
        public Task<CommunityRole?> GetRoleAsync(ulong roleId, CancellationToken ct) => Task.FromResult(Roles.FirstOrDefault(x => x.RoleId == roleId));
        public Task AddRoleAsync(ulong userId, ulong roleId, CancellationToken ct)
        {
            RoleMutations.Add($"add:{userId}:{roleId}");
            if (Member is not null) Member = Member with { RoleIds = Member.RoleIds.Append(roleId).Distinct().ToArray() };
            return Task.CompletedTask;
        }
        public Task RemoveRoleAsync(ulong userId, ulong roleId, CancellationToken ct)
        {
            RoleMutations.Add($"remove:{userId}:{roleId}");
            if (Member is not null) Member = Member with { RoleIds = Member.RoleIds.Where(x => x != roleId).ToArray() };
            if (FailAfterNextRemove) { FailAfterNextRemove = false; throw new InvalidOperationException("simulated role removal failure after mutation"); }
            return Task.CompletedTask;
        }
        public Task<ulong?> SendAsync(ulong channelId, CommunityMessage message, CancellationToken ct)
        {
            if (SendFailuresRemaining > 0) { SendFailuresRemaining--; throw new InvalidOperationException("simulated send failure"); }
            if (message.DeliveryKey is { } key && _sentKeys.TryGetValue(key, out var existing)) return Task.FromResult<ulong?>(existing);
            Sent.Add((channelId, message));
            var id = 500 + (ulong)Sent.Count;
            if (message.DeliveryKey is { } deliveryKey) _sentKeys[deliveryKey] = id;
            return Task.FromResult<ulong?>(id);
        }
        public Task<ulong?> SendDirectMessageAsync(ulong userId, CommunityMessage message, CancellationToken ct) => Task.FromResult<ulong?>(null);
        public Task DeleteMessageAsync(ulong channelId, ulong messageId, CancellationToken ct) { Deleted.Add((channelId, messageId)); return Task.CompletedTask; }
    }

    private sealed class ManualTimeProvider(DateTimeOffset currentTime) : TimeProvider
    {
        private DateTimeOffset _currentTime = currentTime;
        public override DateTimeOffset GetUtcNow() => _currentTime;
        public void Advance(TimeSpan amount) => _currentTime += amount;
    }
}
