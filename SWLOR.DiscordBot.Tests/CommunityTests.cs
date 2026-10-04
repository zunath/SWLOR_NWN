using Microsoft.Extensions.Logging.Abstractions;
using SWLOR.DiscordBot.Hosting;
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
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
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
        var service = new CommunityService(config, new FakeTicketStore(), discord, new FakeDeletionStore());
        var joinedAt = DateTimeOffset.Parse("2026-10-03T12:00:00Z");

        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.WelcomeAsync(7, joinedAt, CancellationToken.None));
        await service.WelcomeAsync(7, joinedAt, CancellationToken.None);

        Assert.That(discord.Sent, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RecoverPendingDeliveriesAsync_ResumesWelcomeAtItsPersistedDestination()
    {
        var config = new BotConfiguration { GuildId = 1, Welcome = new WelcomeOptions { Enabled = true, ChannelId = 100, Template = "Welcome {user}" } };
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "A Player", []), SendFailuresRemaining = 1 };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        var joinedAt = DateTimeOffset.Parse("2026-10-03T12:00:00Z");

        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.WelcomeAsync(7, joinedAt, CancellationToken.None));
        config.Welcome.ChannelId = 200;
        var restarted = new CommunityService(config, store, discord, new FakeDeletionStore());

        Assert.That(await restarted.RecoverPendingDeliveriesAsync(CancellationToken.None), Is.EqualTo(1));
        Assert.That(discord.Sent.Single().ChannelId, Is.EqualTo(100));
        Assert.That(store.IsCompleted($"welcome:7:{joinedAt.UtcTicks}"), Is.True);
    }

    [Test]
    public async Task RecoverPendingDeliveriesAsync_ResumesAnswerAndItsSourceDeletion()
    {
        var config = new BotConfiguration
        {
            GuildId = 1, Prefix = "?",
            Answers = [new QuickAnswerOptions { Name = "guide", Responses = ["Guide"], AllowedRoleIds = [9], Cooldown = TimeSpan.FromMinutes(1), DeleteCommand = true }]
        };
        var store = new FakeTicketStore();
        var deletions = new FakeDeletionStore();
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "A Player", [9]), SendFailuresRemaining = 1 };
        var service = new CommunityService(config, store, discord, deletions);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.ExecuteAsync(7, 100, 300, "?guide", CancellationToken.None));
        var restarted = new CommunityService(config, store, discord, deletions);

        Assert.That(await restarted.RecoverPendingDeliveriesAsync(CancellationToken.None), Is.EqualTo(1));
        Assert.That(discord.Sent.Single().ChannelId, Is.EqualTo(100));
        Assert.That(discord.Deleted, Is.EqualTo(new[] { (100UL, 300UL) }));
        Assert.That(store.IsCompleted("answer:100:300"), Is.True);
        Assert.That(store.HasCooldown("answer-cooldown:guide:7"), Is.True);
    }

    [Test]
    public async Task RecoverPendingDeliveriesAsync_SkipsAnswerWhenCurrentRoleAuthorizationWasRevoked()
    {
        var config = new BotConfiguration
        {
            GuildId = 1, Prefix = "?",
            Answers = [new QuickAnswerOptions { Name = "guide", Responses = ["Guide"], AllowedRoleIds = [9] }]
        };
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "A Player", [9]), SendFailuresRemaining = 1 };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());

        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.ExecuteAsync(7, 100, 301, "?guide", CancellationToken.None));
        discord.Member = discord.Member! with { RoleIds = [] };

        Assert.That(await service.RecoverPendingDeliveriesAsync(CancellationToken.None), Is.EqualTo(1));
        Assert.That(discord.Sent, Is.Empty);
        Assert.That(store.IsCompleted("answer:100:301"), Is.True);
    }

    [Test]
    public async Task RecoverPendingDeliveriesAsync_ResumesFactionPlanWithoutRecomputingToggle()
    {
        var config = new BotConfiguration
        {
            GuildId = 1, Prefix = "?",
            Factions = new FactionOptions
            {
                Enabled = true, Behavior = "toggle", DeleteCommand = true,
                Roles = [new FactionRole { Name = "Republic Navy", RoleId = 21 }]
            }
        };
        var store = new FakeTicketStore();
        var deletions = new FakeDeletionStore();
        var discord = new FakeCommunityDiscord
        {
            Member = new CommunityMember(7, "A Player", []), Roles = [new CommunityRole(21, 1)], SendFailuresRemaining = 1
        };
        var service = new CommunityService(config, store, discord, deletions);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.ExecuteAsync(7, 100, 302, "?rank Republic Navy", CancellationToken.None));
        var restarted = new CommunityService(config, store, discord, deletions);

        Assert.That(await restarted.RecoverPendingDeliveriesAsync(CancellationToken.None), Is.EqualTo(1));
        Assert.That(discord.Member!.RoleIds, Does.Contain(21));
        Assert.That(discord.RoleMutations, Does.Not.Contain("remove:7:21"));
        Assert.That(discord.Sent.Single().ChannelId, Is.EqualTo(100));
        Assert.That(discord.Deleted, Is.EqualTo(new[] { (100UL, 302UL) }));
    }

    [Test]
    public async Task RecoverPendingDeliveriesAsync_CompletesLegacyPayloadWithoutInventingItsRouteOrActor()
    {
        var store = new FakeTicketStore();
        store.SeedDelivery("answer:100:303", "{\"Content\":\"old answer\",\"Embeds\":[]}");
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "A Player", []) };
        var service = new CommunityService(new BotConfiguration { GuildId = 1 }, store, discord, new FakeDeletionStore());

        Assert.That(await service.RecoverPendingDeliveriesAsync(CancellationToken.None), Is.EqualTo(1));
        Assert.That(discord.Sent, Is.Empty);
        Assert.That(store.IsCompleted("answer:100:303"), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ExecuteAsync_DuplicateCompletedLegacyIntentDoesNotInventSourceDeletion(bool faction)
    {
        var config = new BotConfiguration
        {
            GuildId = 1, Prefix = "?",
            Answers = [new QuickAnswerOptions { Name = "guide", Responses = ["Guide"], DeleteCommand = true }],
            Factions = new FactionOptions
            {
                Enabled = true, DeleteCommand = true,
                Roles = [new FactionRole { Name = "Republic Navy", RoleId = 21 }]
            }
        };
        var key = faction ? "faction:100:304" : "answer:100:304";
        var legacyIntent = faction ? "{\"RoleOperations\":[],\"Response\":\"old response\"}" : "{\"Content\":\"old response\",\"Embeds\":[]}";
        var store = new FakeTicketStore();
        store.SeedDelivery(key, legacyIntent, completed: true);
        var discord = new FakeCommunityDiscord
        {
            Member = new CommunityMember(7, "A Player", []), Roles = [new CommunityRole(21, 1)]
        };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());

        await service.ExecuteAsync(7, 100, 304, faction ? "?rank Republic Navy" : "?guide", CancellationToken.None);

        Assert.That(discord.Sent, Is.Empty);
        Assert.That(discord.Deleted, Is.Empty);
        Assert.That(discord.RoleMutations, Is.Empty);
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
        var service = new CommunityService(config, new FakeTicketStore(), discord, new FakeDeletionStore());

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
        var service = new CommunityService(config, new FakeTicketStore(), discord, new FakeDeletionStore());

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
        var service = new CommunityService(config, new FakeTicketStore(), discord, new FakeDeletionStore());

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
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());

        await service.ExecuteAsync(7, 999, 300, "?guide alpha beta", CancellationToken.None);
        Assert.That(discord.Sent, Is.Empty, "a channel outside the allow list must receive no answer");
        await service.ExecuteAsync(7, 100, 301, "?guide alpha beta", CancellationToken.None);
        await service.ExecuteAsync(7, 100, 302, "?guide other", CancellationToken.None);

        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        Assert.That(discord.Sent[0].Message.Content, Is.EqualTo("Hi <@7>: alpha / alpha beta on SWLOR"));
        Assert.That(discord.Deleted, Is.EqualTo(new[] { (100UL, 301UL) }));
    }

    [TestCase("alpha   beta  ", "alpha", "beta")]
    [TestCase("alpha\tbeta\n gamma", "alpha", "beta")]
    [TestCase("  alpha beta", "alpha", "beta")]
    [TestCase("{1} {user}", "{1}", "{user}")]
    [TestCase("", "", "")]
    public async Task ExecuteAsync_PreservesRawArgumentsInContentAndEmbeds(string rawArguments, string first, string second)
    {
        var config = new BotConfiguration
        {
            GuildId = 1,
            Prefix = "?",
            Answers = [new QuickAnswerOptions
            {
                Name = "guide",
                Responses = ["[{args}] / {1} / {2}"],
                Embeds = [new AnswerEmbed
                {
                    Title = "[{args}]",
                    Description = "{1} / {2}",
                    Fields = [new EmbedField { Name = "[{args}]", Value = "[{args}] / {1}" }]
                }]
            }]
        };
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "A Player", []) };
        var service = new CommunityService(config, new FakeTicketStore(), discord, new FakeDeletionStore());

        await service.ExecuteAsync(7, 100, 301, "?guide " + rawArguments, CancellationToken.None);

        var message = discord.Sent.Single().Message;
        Assert.Multiple(() =>
        {
            Assert.That(message.Content, Is.EqualTo($"[{rawArguments}] / {first} / {second}"));
            Assert.That(message.Embeds.Single().Title, Is.EqualTo($"[{rawArguments}]"));
            Assert.That(message.Embeds.Single().Description, Is.EqualTo($"{first} / {second}"));
            Assert.That(message.Embeds.Single().Fields.Single().Name, Is.EqualTo($"[{rawArguments}]"));
            Assert.That(message.Embeds.Single().Fields.Single().Value, Is.EqualTo($"[{rawArguments}] / {first}"));
        });
    }

    [Test]
    public async Task ExecuteAsync_EnforcesPersistentCooldownAtItsExactBoundary()
    {
        var config = new BotConfiguration { GuildId = 1, Prefix = "?", Answers = [new QuickAnswerOptions { Name = "guide", Responses = ["guide"], Cooldown = TimeSpan.FromMinutes(1) }] };
        var time = new ManualTimeProvider(DateTimeOffset.Parse("2026-10-03T12:00:00Z"));
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "A Player", []) };
        var service = new CommunityService(config, new FakeTicketStore(), discord, new FakeDeletionStore(), time);

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
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());

        await service.ExecuteAsync(7, 100, 501, "?guide", CancellationToken.None);
        await service.ExecuteAsync(7, 100, 501, "?guide", CancellationToken.None);
        await service.ExecuteAsync(7, 100, 502, "?guide alpha", CancellationToken.None);

        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        Assert.That(discord.Sent[0].Message.Content, Is.EqualTo("alpha"));
    }

    [Test]
    public async Task ExecuteAsync_HoldsCommunityLockWithoutBlockingTicketOperations()
    {
        var config = new BotConfiguration
        {
            GuildId = 1,
            Prefix = "?",
            Answers = [new QuickAnswerOptions { Name = "guide", Responses = ["guide"], Cooldown = TimeSpan.FromMinutes(1) }]
        };
        var store = new FakeTicketStore();
        var sendStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSend = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var discord = new FakeCommunityDiscord
        {
            Member = new CommunityMember(7, "A Player", []),
            SendStarted = sendStarted,
            ReleaseSend = releaseSend
        };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());

        var communityOperation = service.ExecuteAsync(7, 100, 601, "?guide", CancellationToken.None);
        await sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await using (await store.LockAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2))) { }

        releaseSend.TrySetResult(true);
        await communityOperation;
        await service.ExecuteAsync(7, 100, 602, "?guide", CancellationToken.None);

        Assert.That(discord.Sent, Has.Count.EqualTo(1), "the serialized community session must still persist the cooldown");
    }

    [Test]
    public async Task ExecuteAsync_IgnoresBotsAndWebhooks()
    {
        var config = new BotConfiguration { GuildId = 1, Prefix = "?", Answers = [new QuickAnswerOptions { Name = "hello", Responses = ["hello"] }] };
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "Bot", [], IsBot: true) };
        var service = new CommunityService(config, new FakeTicketStore(), discord, new FakeDeletionStore());

        await service.ExecuteAsync(7, 100, 300, "?hello", CancellationToken.None);

        Assert.That(discord.Sent, Is.Empty);
    }

    [Test]
    public async Task EmptyAnswerDoesNotScheduleOrDeleteItsUnrepliedSourceCommand()
    {
        var config = new BotConfiguration
        {
            GuildId = 1, Prefix = "?",
            Answers = [new QuickAnswerOptions { Name = "guide", Responses = ["{1}"], DeleteCommand = true }]
        };
        var store = new FakeTicketStore();
        var deletions = new FakeDeletionStore();
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "A Player", []) };
        var service = new CommunityService(config, store, discord, deletions);
        await service.ExecuteAsync(7, 100, 300, "?guide", default);
        await service.ExecuteAsync(7, 100, 300, "?guide", default);
        Assert.That(store.IsCompleted("answer:100:300"), Is.True);
        Assert.That(discord.Sent, Is.Empty);
        Assert.That(discord.Deleted, Is.Empty);
        Assert.That(deletions.Pending, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CommandDeletionSurvivesShutdownAfterDeliveryWithoutReplayingSourceEvent(bool faction)
    {
        var (config, discord, command, key) = DeletionCase(faction);
        var store = new FakeTicketStore();
        var deletions = new FakeDeletionStore();
        var time = new ManualTimeProvider(DateTimeOffset.Parse("2026-10-03T12:00:00Z"));
        using var shutdown = new CancellationTokenSource();
        store.BeforeComplete = completedKey =>
        {
            Assert.That(completedKey, Is.EqualTo(key));
            Assert.That(discord.Sent, Has.Count.EqualTo(1));
            Assert.That(deletions.Pending.Keys, Does.Contain((100UL, 300UL)));
        };
        store.AfterComplete = _ => shutdown.Cancel();
        var service = new CommunityService(config, store, discord, deletions, time);

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.ExecuteAsync(7, 100, 300, command, shutdown.Token));
        Assert.That(store.IsCompleted(key), Is.True);
        Assert.That(discord.Deleted, Is.Empty);

        var restartedCleanup = new ResponseDeletionQueue(deletions, time, NullLogger<ResponseDeletionQueue>.Instance);
        await restartedCleanup.DeleteDueAsync(discord, default);
        Assert.That(discord.Deleted, Is.EqualTo(new[] { (100UL, 300UL) }));
        Assert.That(deletions.Pending, Is.Empty);
        Assert.That(discord.Sent, Has.Count.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CommandDeletionSchedulingFailureLeavesResponseIncompleteAndRetryUsesOriginalIntent(bool faction)
    {
        var (config, discord, command, key) = DeletionCase(faction);
        var store = new FakeTicketStore();
        var deletions = new FakeDeletionStore { ScheduleFailuresRemaining = 1 };
        var service = new CommunityService(config, store, discord, deletions);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.ExecuteAsync(7, 100, 300, command, default));
        Assert.That(store.IsCompleted(key), Is.False);
        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        Assert.That(discord.Deleted, Is.Empty);
        Assert.That(deletions.Pending, Is.Empty);

        var restarted = new CommunityService(config, store, discord, deletions);
        await restarted.ExecuteAsync(7, 100, 300, command, default);
        Assert.That(store.IsCompleted(key), Is.True);
        Assert.That(discord.Sent, Has.Count.EqualTo(1), "the response retains its idempotent delivery key");
        Assert.That(discord.Deleted, Is.EqualTo(new[] { (100UL, 300UL) }));
        Assert.That(deletions.Pending, Is.Empty);
        if (faction) Assert.That(discord.RoleMutations, Does.Not.Contain("remove:7:21"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CommandDeletionRemainsQueuedAfterGatewayRetriesAreExhausted(bool faction)
    {
        var (config, discord, command, key) = DeletionCase(faction);
        discord.DeleteFailuresRemaining = 3;
        var store = new FakeTicketStore();
        var deletions = new FakeDeletionStore();
        var service = new CommunityService(config, store, discord, deletions);
        for (var retry = 0; retry < 3; retry++)
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.ExecuteAsync(7, 100, 300, command, default));
        Assert.That(store.IsCompleted(key), Is.True);
        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        Assert.That(deletions.Pending, Has.Count.EqualTo(1));
        await new ResponseDeletionQueue(deletions, TimeProvider.System, NullLogger<ResponseDeletionQueue>.Instance)
            .DeleteDueAsync(discord, default);
        Assert.That(discord.Deleted, Is.EqualTo(new[] { (100UL, 300UL) }));
        Assert.That(deletions.Pending, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CommandDeletionIsNotScheduledWhenResponseSendFails(bool faction)
    {
        var (config, discord, command, key) = DeletionCase(faction);
        discord.SendFailuresRemaining = 1;
        var store = new FakeTicketStore();
        var deletions = new FakeDeletionStore();
        var service = new CommunityService(config, store, discord, deletions);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.ExecuteAsync(7, 100, 300, command, default));
        Assert.That(store.IsCompleted(key), Is.False);
        Assert.That(discord.Deleted, Is.Empty);
        Assert.That(deletions.Pending, Is.Empty);
    }

    private static (BotConfiguration Config, FakeCommunityDiscord Discord, string Command, string Key) DeletionCase(bool faction)
    {
        var config = new BotConfiguration
        {
            GuildId = 1, Prefix = "?",
            Answers = [new QuickAnswerOptions { Name = "guide", Responses = ["guide"], DeleteCommand = true }],
            Factions = new FactionOptions
            {
                Enabled = true, Behavior = "toggle", DeleteCommand = true,
                Roles = [new FactionRole { Name = "Republic Navy", RoleId = 21 }]
            }
        };
        var discord = new FakeCommunityDiscord
        {
            Member = new CommunityMember(7, "A Player", []), Roles = [new CommunityRole(21, 1)]
        };
        return (config, discord, faction ? "?rank Republic Navy" : "?guide", faction ? "faction:100:300" : "answer:100:300");
    }

    private sealed class FakeDeletionStore : IResponseDeletionStore
    {
        public readonly Dictionary<(ulong, ulong), PendingResponseDeletion> Pending = new();
        private readonly HashSet<(ulong, ulong)> completed = [];
        public int ScheduleFailuresRemaining { get; set; }
        public Task ScheduleDeletionAsync(ulong channelId, ulong messageId, DateTimeOffset dueAt, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (ScheduleFailuresRemaining > 0) { ScheduleFailuresRemaining--; throw new InvalidOperationException("simulated deletion queue failure"); }
            if (!completed.Contains((channelId, messageId)))
                Pending.TryAdd((channelId, messageId), new(channelId, messageId, dueAt));
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<PendingResponseDeletion>> GetDueDeletionsAsync(DateTimeOffset now, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<PendingResponseDeletion>>(Pending.Values.Where(x => x.DueAt <= now).ToArray());
        }
        public Task CompleteDeletionAsync(ulong channelId, ulong messageId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            completed.Add((channelId, messageId));
            Pending.Remove((channelId, messageId));
            return Task.CompletedTask;
        }
        public Task RetryDeletionAsync(PendingResponseDeletion deletion, DateTimeOffset dueAt, string error, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Pending[(deletion.ChannelId, deletion.MessageId)] = deletion with { DueAt = dueAt, Attempts = deletion.Attempts + 1, LastError = error };
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTicketStore : ITicketStore
    {
        private readonly HashSet<string> _deliveries = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DeliveryState> _states = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTimeOffset> _cooldowns = new(StringComparer.Ordinal);
        private readonly SemaphoreSlim _ticketLock = new(1, 1);
        private readonly SemaphoreSlim _communityLock = new(1, 1);
        public Action<string>? BeforeComplete { get; set; }
        public Action<string>? AfterComplete { get; set; }
        public bool IsCompleted(string key) => _states.TryGetValue(key, out var state) && state.Completed;
        public bool HasCooldown(string key) => _cooldowns.ContainsKey(key);
        public void SeedDelivery(string key, string intent, bool completed = false) => _states[key] = new DeliveryState(intent, completed);
        public Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;
        public async Task<ITicketSession> LockAsync(CancellationToken ct)
        {
            await _ticketLock.WaitAsync(ct);
            return new Session(_deliveries, _states, _cooldowns, _ticketLock, this);
        }
        public async Task<ITicketSession> LockCommunityAsync(CancellationToken ct)
        {
            await _communityLock.WaitAsync(ct);
            return new Session(_deliveries, _states, _cooldowns, _communityLock, this);
        }

        private sealed class Session(HashSet<string> deliveries, Dictionary<string, DeliveryState> states, Dictionary<string, DateTimeOffset> cooldowns, SemaphoreSlim heldLock, FakeTicketStore owner) : ITicketSession
        {
            private int _disposed;
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
            public Task<IReadOnlyList<PendingDelivery>> GetPendingDeliveriesAsync(CancellationToken ct) =>
                Task.FromResult<IReadOnlyList<PendingDelivery>>(states.Where(x => !x.Value.Completed)
                    .Select(x => new PendingDelivery(x.Key, x.Value.Intent)).ToArray());
            public Task CompleteDeliveryAsync(string key, CancellationToken ct)
            {
                owner.BeforeComplete?.Invoke(key);
                ct.ThrowIfCancellationRequested();
                states[key] = states[key] with { Completed = true };
                owner.AfterComplete?.Invoke(key);
                return Task.CompletedTask;
            }
            public Task<DateTimeOffset?> GetCooldownAsync(string key, CancellationToken ct) => Task.FromResult(cooldowns.TryGetValue(key, out var value) ? (DateTimeOffset?)value : null);
            public Task SetCooldownAsync(string key, DateTimeOffset at, CancellationToken ct) { cooldowns[key] = at; return Task.CompletedTask; }
            public ValueTask DisposeAsync()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0) heldLock.Release();
                return ValueTask.CompletedTask;
            }
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
        public int DeleteFailuresRemaining { get; set; }
        public bool FailAfterNextRemove { get; set; }
        public TaskCompletionSource<bool>? SendStarted { get; init; }
        public TaskCompletionSource<bool>? ReleaseSend { get; init; }
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
        public async Task<ulong?> SendAsync(ulong channelId, CommunityMessage message, CancellationToken ct)
        {
            SendStarted?.TrySetResult(true);
            if (ReleaseSend is { } release) await release.Task.WaitAsync(ct);
            if (SendFailuresRemaining > 0) { SendFailuresRemaining--; throw new InvalidOperationException("simulated send failure"); }
            if (message.DeliveryKey is { } key && _sentKeys.TryGetValue(key, out var existing)) return existing;
            Sent.Add((channelId, message));
            var id = 500 + (ulong)Sent.Count;
            if (message.DeliveryKey is { } deliveryKey) _sentKeys[deliveryKey] = id;
            return id;
        }
        public Task<ulong?> SendDirectMessageAsync(ulong userId, CommunityMessage message, CancellationToken ct) => Task.FromResult<ulong?>(null);
        public Task DeleteMessageAsync(ulong channelId, ulong messageId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (DeleteFailuresRemaining > 0) { DeleteFailuresRemaining--; throw new InvalidOperationException("simulated deletion failure"); }
            Deleted.Add((channelId, messageId));
            return Task.CompletedTask;
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset currentTime) : TimeProvider
    {
        private DateTimeOffset _currentTime = currentTime;
        public override DateTimeOffset GetUtcNow() => _currentTime;
        public void Advance(TimeSpan amount) => _currentTime += amount;
    }
}
