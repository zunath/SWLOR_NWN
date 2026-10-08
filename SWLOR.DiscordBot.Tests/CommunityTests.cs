using System.Net;
using Discord.Net;
using Discord;
using SWLOR.DiscordBot.Discord;
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

    [TestCase(false)]
    [TestCase(true)]
    public async Task HttpPosterRecipientRefusalCompletesLiveAndRecoveredWelcome(bool recovery)
    {
        using var handler = new WelcomePostHandler(HttpStatusCode.Forbidden, "{\"code\":50007,\"message\":\"Cannot send messages to this user\"}");
        using var http = new HttpClient(handler);
        using var poster = new DiscordCommunityPoster(http, new BotSecrets("fake-test-token", "Host=unused"), TimeProvider.System);
        var config = DirectWelcomeConfiguration();
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new(7, "Player", []), DirectMessagePost = (message, ct) => poster.SendAsync(10, message, ct) };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        var joined = DateTimeOffset.UnixEpoch;
        if (recovery)
        {
            await service.PersistWelcomeAsync(7, joined, default);
            Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        }
        else await service.WelcomeAsync(7, joined, default);

        Assert.That(store.IsCompleted($"welcome:7:{joined.UtcTicks}"), Is.True);
        await new CommunityService(config, store, discord, new FakeDeletionStore()).WelcomeAsync(7, joined, default);
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.Zero);
        Assert.That(handler.Requests, Is.EqualTo(1), "Use the actual REST sender's 403/50007 response, then stop retrying this intent.");
        Assert.That(discord.Sent, Is.Empty, "A refused private welcome must not leak into a public channel.");
    }

    [TestCase(HttpStatusCode.Forbidden, "{\"code\":50013}")]
    [TestCase(HttpStatusCode.Forbidden, "{\"code\":50001}")]
    [TestCase(HttpStatusCode.Forbidden, "{\"code\":\"50007\"}")]
    [TestCase(HttpStatusCode.Forbidden, "{}")]
    [TestCase(HttpStatusCode.Forbidden, "<html>proxy-error</html>")]
    [TestCase(HttpStatusCode.BadRequest, "{\"code\":50007}")]
    [TestCase(HttpStatusCode.TooManyRequests, "{\"retry_after\":0,\"global\":false}")]
    public async Task HttpPosterOtherWelcomeFailuresRemainPendingAndRecover(HttpStatusCode status, string body)
    {
        using var handler = new WelcomePostHandler(status, body);
        using var http = new HttpClient(handler);
        using var poster = new DiscordCommunityPoster(http, new BotSecrets("fake-test-token", "Host=unused"), TimeProvider.System);
        var config = DirectWelcomeConfiguration();
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new(7, "Player", []), DirectMessagePost = (message, ct) => poster.SendAsync(10, message, ct) };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        var joined = DateTimeOffset.UnixEpoch;
        Assert.CatchAsync<Exception>(() => service.WelcomeAsync(7, joined, default));
        Assert.That(store.IsCompleted($"welcome:7:{joined.UtcTicks}"), Is.False);
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.Zero);
        Assert.That(store.IsCompleted($"welcome:7:{joined.UtcTicks}"), Is.False);

        handler.Status = HttpStatusCode.OK;
        handler.Body = "{\"id\":\"123\"}";
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(store.IsCompleted($"welcome:7:{joined.UtcTicks}"), Is.True);
        var requests = handler.Requests;
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.Zero);
        Assert.That(handler.Requests, Is.EqualTo(requests));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RefusedWelcomeDmCompletesOnceForLiveOrRecoveryAndAllowsFutureJoin(bool recovery)
    {
        var config = DirectWelcomeConfiguration();
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord
        {
            Member = new(7, "Player", []),
            DirectMessageError = WelcomeHttpError(HttpStatusCode.Forbidden, 50007)
        };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        var joined = DateTimeOffset.UnixEpoch;
        var key = $"welcome:7:{joined.UtcTicks}";
        if (recovery)
        {
            await service.PersistWelcomeAsync(7, joined, default);
            Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        }
        else await service.WelcomeAsync(7, joined, default);

        Assert.That(store.IsCompleted(key), Is.True);
        await new CommunityService(config, store, discord, new FakeDeletionStore()).WelcomeAsync(7, joined, default);
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.Zero);
        Assert.That(discord.DirectMessageAttempts, Is.EqualTo(1));
        Assert.That(discord.DirectMessages, Is.Empty);
        Assert.That(discord.Sent, Is.Empty, "Do not publish a refused private welcome in a public channel.");

        var rejoined = joined.AddMinutes(1);
        await service.WelcomeAsync(7, rejoined, default);
        Assert.That(discord.DirectMessageAttempts, Is.EqualTo(2), "A later join has its own intent; this is not a permanent user blocklist.");
        Assert.That(store.IsCompleted($"welcome:7:{rejoined.UtcTicks}"), Is.True);
    }

    [TestCase(HttpStatusCode.TooManyRequests, 0)]
    [TestCase(HttpStatusCode.ServiceUnavailable, 0)]
    [TestCase(HttpStatusCode.Forbidden, 50013)]
    [TestCase(HttpStatusCode.Forbidden, 50001)]
    [TestCase(HttpStatusCode.Forbidden, 0)]
    [TestCase(HttpStatusCode.BadRequest, 50007)]
    public async Task OtherWelcomeDmHttpFailuresStayPendingAndRecoverAfterRestoration(HttpStatusCode status, int code)
    {
        var config = DirectWelcomeConfiguration();
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new(7, "Player", []), DirectMessageError = WelcomeHttpError(status, code) };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        var joined = DateTimeOffset.UnixEpoch;
        var key = $"welcome:7:{joined.UtcTicks}";
        Assert.ThrowsAsync<HttpException>(() => service.WelcomeAsync(7, joined, default));
        Assert.That(store.IsCompleted(key), Is.False);
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.Zero);
        Assert.That(store.IsCompleted(key), Is.False);
        Assert.That(discord.DirectMessageAttempts, Is.EqualTo(2));

        discord.DirectMessageError = null;
        Assert.That(await new CommunityService(config, store, discord, new FakeDeletionStore()).RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(store.IsCompleted(key), Is.True);
        Assert.That(discord.DirectMessages, Has.Count.EqualTo(1));
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.Zero);
        Assert.That(discord.DirectMessageAttempts, Is.EqualTo(3));
    }

    [Test]
    public async Task RefusedWelcomeUsesPersistedDmRouteAndDoesNotDelayUnrelatedDelivery()
    {
        var config = DirectWelcomeConfiguration();
        config.Answers = [new QuickAnswerOptions { Name = "guide", Responses = ["answer"] }];
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new(7, "Player", []), DirectMessageError = WelcomeHttpError(HttpStatusCode.Forbidden, 50007), SendFailuresRemaining = 1 };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        await service.PersistWelcomeAsync(7, DateTimeOffset.UnixEpoch, default);
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 301, "?guide", default));
        config.Welcome.DirectMessage = false;
        config.Welcome.ChannelId = 200;

        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(2));
        Assert.That(store.IsCompleted($"welcome:7:{DateTimeOffset.UnixEpoch.UtcTicks}"), Is.True);
        Assert.That(discord.DirectMessageAttempts, Is.EqualTo(1));
        Assert.That(discord.Sent.Single().Message.Content, Is.EqualTo("answer"));
        Assert.That(discord.Sent.Single().ChannelId, Is.EqualTo(100));
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.Zero);
    }

    [Test]
    public async Task ChannelWelcomeForbiddenRemainsPendingEvenWithRecipientRefusalCode()
    {
        var config = DirectWelcomeConfiguration();
        config.Welcome.DirectMessage = false;
        config.Welcome.ChannelId = 100;
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new(7, "Player", []), ChannelSendError = WelcomeHttpError(HttpStatusCode.Forbidden, 50007) };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        var joined = DateTimeOffset.UnixEpoch;
        Assert.ThrowsAsync<HttpException>(() => service.WelcomeAsync(7, joined, default));
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.Zero);
        Assert.That(store.IsCompleted($"welcome:7:{joined.UtcTicks}"), Is.False);
        discord.ChannelSendError = null;
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        Assert.That(discord.DirectMessageAttempts, Is.Zero);
    }

    [Test]
    public async Task NullWelcomeDmResultRemainsPendingUntilDelivered()
    {
        var config = DirectWelcomeConfiguration();
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new(7, "Player", []), ReturnNullDirectMessage = true };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        var joined = DateTimeOffset.UnixEpoch;
        Assert.ThrowsAsync<InvalidOperationException>(() => service.WelcomeAsync(7, joined, default));
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.Zero);
        Assert.That(store.IsCompleted($"welcome:7:{joined.UtcTicks}"), Is.False);
        discord.ReturnNullDirectMessage = false;
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(discord.DirectMessages, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RefusedWelcomeCompletionFailureRemainsPendingForRecovery()
    {
        var config = DirectWelcomeConfiguration();
        var store = new FakeTicketStore { BeforeComplete = _ => throw new InvalidOperationException("Completion failed.") };
        var discord = new FakeCommunityDiscord { Member = new(7, "Player", []), DirectMessageError = WelcomeHttpError(HttpStatusCode.Forbidden, 50007) };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        var joined = DateTimeOffset.UnixEpoch;
        Assert.ThrowsAsync<InvalidOperationException>(() => service.WelcomeAsync(7, joined, default));
        Assert.That(store.IsCompleted($"welcome:7:{joined.UtcTicks}"), Is.False);
        store.BeforeComplete = null;
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(store.IsCompleted($"welcome:7:{joined.UtcTicks}"), Is.True);
        Assert.That(discord.DirectMessageAttempts, Is.EqualTo(2));
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.Zero);
    }

    [Test]
    public async Task CancelledWelcomeDmRefusalDoesNotCompleteDelivery()
    {
        var config = DirectWelcomeConfiguration();
        var store = new FakeTicketStore();
        using var cancellation = new CancellationTokenSource();
        var discord = new FakeCommunityDiscord
        {
            Member = new(7, "Player", []),
            DirectMessageError = WelcomeHttpError(HttpStatusCode.Forbidden, 50007),
            BeforeDirectMessageSend = _ => cancellation.Cancel()
        };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        var joined = DateTimeOffset.UnixEpoch;
        Assert.CatchAsync<OperationCanceledException>(() => service.WelcomeAsync(7, joined, cancellation.Token));
        Assert.That(store.IsCompleted($"welcome:7:{joined.UtcTicks}"), Is.False);
        discord.BeforeDirectMessageSend = null;
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(store.IsCompleted($"welcome:7:{joined.UtcTicks}"), Is.True);
    }

    private static BotConfiguration DirectWelcomeConfiguration() => new()
    {
        GuildId = 1,
        Welcome = new WelcomeOptions { Enabled = true, DirectMessage = true, Template = "Welcome {user}" }
    };

    private static HttpException WelcomeHttpError(HttpStatusCode status, int code) =>
        new(status, null!, code == 0 ? null : (DiscordErrorCode)code, "Simulated HTTP failure.", []);

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
                    Fields = [new SWLOR.DiscordBot.Configuration.EmbedField { Name = "[{args}]", Value = "[{args}] / {1}" }]
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

    [TestCase(false)]
    [TestCase(true)]
    public async Task OlderFactionIntentCannotUndoNewerChoiceAcrossChannels(bool retryLiveEvent)
    {
        var config = new BotConfiguration
        {
            GuildId = 1, Prefix = "?",
            Factions = new FactionOptions
            {
                Enabled = true, Exclusive = true, Behavior = "toggle", DeleteCommand = true,
                Roles = [new FactionRole { Name = "A", RoleId = 21 }, new FactionRole { Name = "B", RoleId = 22 }]
            }
        };
        var discord = new FakeCommunityDiscord
        {
            Member = new CommunityMember(7, "Player", []),
            Roles = [new CommunityRole(21, 1), new CommunityRole(22, 1)], SendFailuresRemaining = 1
        };
        var store = new FakeTicketStore();
        var deletions = new FakeDeletionStore();
        var service = new CommunityService(config, store, discord, deletions);
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 101, 1000, "?rank A", default));
        await service.ExecuteAsync(7, 100, 1001, "?rank B", default);
        var mutations = discord.RoleMutations.ToArray();

        var restarted = new CommunityService(config, store, discord, deletions);
        if (retryLiveEvent) await restarted.ExecuteAsync(7, 101, 1000, "?rank A", default);
        else Assert.That(await restarted.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));

        Assert.That(discord.Member!.RoleIds, Is.EqualTo(new[] { 22UL }));
        Assert.That(discord.RoleMutations, Is.EqualTo(mutations));
        Assert.That(discord.Sent.Count, Is.EqualTo(1));
        Assert.That(store.IsCompleted("faction:101:1000"), Is.True);
        Assert.That(deletions.Pending.Keys, Does.Contain((101UL, 1000UL)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task OlderPendingAnswerDoesNotSendOrExtendNewerCooldown(bool cooldownExpired)
    {
        var config = new BotConfiguration
        {
            GuildId = 1, Prefix = "?",
            Answers = [new QuickAnswerOptions { Name = "guide", Responses = ["Guide"], Cooldown = TimeSpan.FromMinutes(1), DeleteCommand = true }]
        };
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var store = new FakeTicketStore();
        var deletions = new FakeDeletionStore();
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "Player", []), SendFailuresRemaining = 1 };
        var service = new CommunityService(config, store, discord, deletions, clock);
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 300, "?guide", default));
        await service.ExecuteAsync(7, 100, 301, "?guide", default);
        var originalCooldown = store.CooldownAt("answer-cooldown:guide:7");
        clock.Advance(cooldownExpired ? TimeSpan.FromMinutes(2) : TimeSpan.FromSeconds(10));

        Assert.That(await new CommunityService(config, store, discord, deletions, clock).RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(discord.Sent.Count, Is.EqualTo(1));
        Assert.That(store.CooldownAt("answer-cooldown:guide:7"), Is.EqualTo(originalCooldown));
        Assert.That(store.IsCompleted("answer:100:300"), Is.True);
        Assert.That(deletions.Pending.Keys, Does.Contain((100UL, 300UL)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PendingAnswerHonorsCurrentCooldownWithoutAnOrderingCursor(bool retryLiveEvent)
    {
        var config = new BotConfiguration
        {
            GuildId = 1, Prefix = "?",
            Answers = [new QuickAnswerOptions { Name = "guide", Responses = ["Guide"], Cooldown = TimeSpan.FromMinutes(1) }]
        };
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "Player", []), SendFailuresRemaining = 1 };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore(), clock);
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 300, "?guide", default));
        store.SeedCooldown("answer-cooldown:guide:7", clock.GetUtcNow());
        clock.Advance(TimeSpan.FromSeconds(10));

        if (retryLiveEvent) await service.ExecuteAsync(7, 100, 300, "?guide", default);
        else Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(discord.Sent, Is.Empty);
        Assert.That(store.IsCompleted("answer:100:300"), Is.True);
        Assert.That(store.CooldownAt("answer-cooldown:guide:7"), Is.EqualTo(DateTimeOffset.UnixEpoch));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PendingZeroCooldownAnswerRecordsNewConfiguredCooldownAfterDelivery(bool retryLiveEvent)
    {
        var answer = new QuickAnswerOptions { Name = "guide", Responses = ["Guide"] };
        var config = new BotConfiguration { GuildId = 1, Prefix = "?", Answers = [answer] };
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "Player", []), SendFailuresRemaining = 1 };
        var deletions = new FakeDeletionStore();
        var service = new CommunityService(config, store, discord, deletions, clock);
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 300, "?guide", default));
        Assert.That(store.HasCooldown("answer-cooldown:guide:7"), Is.False);
        answer.Cooldown = TimeSpan.FromMinutes(1);
        clock.Advance(TimeSpan.FromSeconds(10));
        var restarted = new CommunityService(config, store, discord, deletions, clock);

        if (retryLiveEvent) await restarted.ExecuteAsync(7, 100, 300, "?guide", default);
        else Assert.That(await restarted.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(store.CooldownAt("answer-cooldown:guide:7"), Is.EqualTo(clock.GetUtcNow()));
        Assert.That(store.IsCompleted("answer:100:300"), Is.True);
        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        Assert.That(await restarted.RecoverPendingDeliveriesAsync(default), Is.Zero);

        clock.Advance(TimeSpan.FromSeconds(59));
        await restarted.ExecuteAsync(7, 100, 301, "?guide", default);
        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        clock.Advance(TimeSpan.FromSeconds(1));
        await restarted.ExecuteAsync(7, 100, 302, "?guide", default);
        Assert.That(discord.Sent, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task PendingZeroCooldownAnswersRecoverOnlyOneResponseInsideNewConfiguredCooldown()
    {
        var answer = new QuickAnswerOptions { Name = "guide", Responses = ["Guide"] };
        var config = new BotConfiguration { GuildId = 1, Prefix = "?", Answers = [answer] };
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "Player", []), SendFailuresRemaining = 2 };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore(), clock);
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 300, "?guide", default));
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 301, "?guide", default));
        answer.Cooldown = TimeSpan.FromMinutes(1);
        clock.Advance(TimeSpan.FromSeconds(10));

        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(2));
        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        Assert.That(store.CooldownAt("answer-cooldown:guide:7"), Is.EqualTo(clock.GetUtcNow()));
        Assert.That(store.IsCompleted("answer:100:300"), Is.True);
        Assert.That(store.IsCompleted("answer:100:301"), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task OlderZeroCooldownAnswerDoesNotExtendNewerConfiguredCooldown(bool cooldownExpired)
    {
        var answer = new QuickAnswerOptions { Name = "guide", Responses = ["Guide"] };
        var config = new BotConfiguration { GuildId = 1, Prefix = "?", Answers = [answer] };
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "Player", []), SendFailuresRemaining = 1 };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore(), clock);
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 300, "?guide", default));
        answer.Cooldown = TimeSpan.FromMinutes(1);
        await service.ExecuteAsync(7, 100, 301, "?guide", default);
        var originalCooldown = store.CooldownAt("answer-cooldown:guide:7");
        clock.Advance(cooldownExpired ? TimeSpan.FromMinutes(2) : TimeSpan.FromSeconds(10));

        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(discord.Sent, Has.Count.EqualTo(1));
        Assert.That(store.CooldownAt("answer-cooldown:guide:7"), Is.EqualTo(originalCooldown));
        Assert.That(store.IsCompleted("answer:100:300"), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PendingAnswerRetainsPersistedCooldownWhenCurrentCooldownWasRemoved(bool retryLiveEvent)
    {
        var answer = new QuickAnswerOptions { Name = "guide", Responses = ["Guide"], Cooldown = TimeSpan.FromMinutes(1) };
        var config = new BotConfiguration { GuildId = 1, Prefix = "?", Answers = [answer] };
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var store = new FakeTicketStore();
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "Player", []), SendFailuresRemaining = 1 };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore(), clock);
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 300, "?guide", default));
        store.SeedCooldown("answer-cooldown:guide:7", clock.GetUtcNow());
        answer.Cooldown = TimeSpan.Zero;
        clock.Advance(TimeSpan.FromSeconds(10));

        if (retryLiveEvent) await service.ExecuteAsync(7, 100, 300, "?guide", default);
        else Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(discord.Sent, Is.Empty);
        Assert.That(store.IsCompleted("answer:100:300"), Is.True);
        Assert.That(store.CooldownAt("answer-cooldown:guide:7"), Is.EqualTo(DateTimeOffset.UnixEpoch));
    }

    [Test]
    public async Task FactionRecoveryCompletesLongPlanWhileIndividualStepsKeepProgressing()
    {
        var (config, discord, store, service, clock) = LongFactionRecovery();
        discord.BeforeRoleMutation = ct =>
        {
            clock.Advance(TimeSpan.FromMinutes(3));
            ct.ThrowIfCancellationRequested();
        };
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(clock.GetUtcNow() - DateTimeOffset.UnixEpoch, Is.GreaterThan(CommunityService.RecoveryInactivityTimeout));
        Assert.That(store.IsCompleted("faction:100:300"), Is.True);
        Assert.That(discord.Member!.RoleIds, Is.EqualTo(new[] { 21UL }));
        Assert.That(discord.Sent.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task StalledFactionRecoveryYieldsToLaterAnswerAndRemainsRetryable()
    {
        var (config, discord, store, service, clock) = LongFactionRecovery();
        config.Answers = [new QuickAnswerOptions { Name = "guide", Responses = ["Guide"] }];
        discord.SendFailuresRemaining = 1;
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 301, "?guide", default));
        discord.BeforeRoleMutation = ct =>
        {
            clock.Advance(CommunityService.RecoveryInactivityTimeout + TimeSpan.FromSeconds(1));
            ct.ThrowIfCancellationRequested();
        };

        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(store.IsCompleted("faction:100:300"), Is.False);
        Assert.That(store.IsCompleted("answer:100:301"), Is.True);
        discord.BeforeRoleMutation = null;
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(store.IsCompleted("faction:100:300"), Is.True);
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public async Task WelcomeJoinPersistsBeforeReadinessOrFullQueueAndRecoversOriginalDestination(bool ready, bool queueAccepts)
    {
        var config = new BotConfiguration { GuildId = 1, Welcome = new WelcomeOptions { Enabled = true, ChannelId = 100, Template = "Welcome {user}" } };
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "Player", []) };
        var store = new FakeTicketStore();
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        var joined = DateTimeOffset.UnixEpoch;
        var key = $"welcome:7:{joined.UtcTicks}";
        var queued = 0;
        // Even a delivery already holding the long community lock cannot delay the join commit.
        await using (var held = await store.LockCommunityAsync(default))
        {
            Assert.That(await DiscordGateway.PersistAndQueueWelcomeAsync(service, 7, joined, () => ready, _ =>
            {
                queued++;
                Assert.That(store.HasDelivery(key), Is.True);
                return queueAccepts;
            }, default).WaitAsync(TimeSpan.FromSeconds(2)), Is.False);
            Assert.That(store.HasDelivery(key), Is.True);
            Assert.That(store.IsCompleted(key), Is.False);
            Assert.That(discord.MemberLookups, Is.Zero, "No REST membership lookup may happen before the durable event exists.");
            Assert.That(discord.Sent, Is.Empty);
        }
        Assert.That(queued, Is.EqualTo(ready ? 1 : 0));
        config.Welcome.ChannelId = 200;
        var restarted = new CommunityService(config, store, discord, new FakeDeletionStore());
        Assert.That(await restarted.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(discord.Sent.Single().ChannelId, Is.EqualTo(100));
        await restarted.WelcomeAsync(7, joined, default);
        Assert.That(discord.Sent, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WelcomeQueuedBeforeDisconnectRemainsPendingUntilValidatedRecovery()
    {
        var config = new BotConfiguration { GuildId = 1, Welcome = new WelcomeOptions { Enabled = true, ChannelId = 100, Template = "Welcome {user}" } };
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "Player", []) };
        var store = new FakeTicketStore();
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        var ready = true;
        Func<CancellationToken, Task>? queued = null;
        Assert.That(await DiscordGateway.PersistAndQueueWelcomeAsync(service, 7, DateTimeOffset.UnixEpoch, () => ready,
            job => { queued = job; return true; }, default), Is.True);
        ready = false;
        await queued!(default);
        Assert.That(discord.Sent, Is.Empty);
        Assert.That(discord.MemberLookups, Is.Zero);
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(discord.Sent, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WelcomePersistenceFailurePropagatesBeforeQueueOrRestAndCanBeRetried()
    {
        var config = new BotConfiguration { GuildId = 1, Welcome = new WelcomeOptions { Enabled = true, ChannelId = 100, Template = "Welcome {user}" } };
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "Player", []) };
        var store = new FakeTicketStore { PersistenceFailuresRemaining = 1 };
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        var queued = 0;
        Assert.ThrowsAsync<InvalidOperationException>(() => DiscordGateway.PersistAndQueueWelcomeAsync(service, 7,
            DateTimeOffset.UnixEpoch, () => true, _ => { queued++; return true; }, default));
        Assert.That(queued, Is.Zero);
        Assert.That(discord.MemberLookups, Is.Zero);
        Assert.That(discord.Sent, Is.Empty);
        Assert.That(store.HasDelivery($"welcome:7:{DateTimeOffset.UnixEpoch.UtcTicks}"), Is.False);
        await service.PersistWelcomeAsync(7, DateTimeOffset.UnixEpoch, default);
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
    }

    [TestCase("view")]
    [TestCase("send")]
    [TestCase("delete")]
    public async Task FactionRecoveryChecksFreshChannelPermissionsBeforeAnyRoleMutationAndRetainsRetry(string lostCapability)
    {
        var (config, discord, command, key) = DeletionCase(true);
        var store = new FakeTicketStore();
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        discord.BeforeRoleMutation = _ => throw new InvalidOperationException("Crash after intent commit, before role mutation.");
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 300, command, default));
        discord.BeforeRoleMutation = null;
        discord.PermissionChecks.Clear();
        discord.ChannelPermissions[100] = new ChannelPermissions(viewChannel: lostCapability != "view",
            sendMessages: lostCapability != "send", manageMessages: lostCapability != "delete");
        config.Factions.DeleteCommand = false; // The persisted source deletion requirement still needs Manage Messages.

        Assert.That(await new CommunityService(config, store, discord, new FakeDeletionStore()).RecoverPendingDeliveriesAsync(default), Is.Zero);
        Assert.That(discord.RoleMutations, Is.Empty);
        Assert.That(discord.Member!.RoleIds, Is.Empty);
        Assert.That(discord.Sent, Is.Empty);
        Assert.That(store.IsCompleted(key), Is.False);
        Assert.That(discord.PermissionChecks.Single(), Is.EqualTo((100UL, true, false)));
        discord.ChannelPermissions[100] = new ChannelPermissions(viewChannel: true, sendMessages: true, manageMessages: true);
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(discord.Member.RoleIds, Is.EqualTo(new[] { 21UL }));
        Assert.That(store.IsCompleted(key), Is.True);
    }

    [Test]
    public async Task PendingEmbeddedAnswerRequiresItsPersistedEmbedCapability()
    {
        var config = new BotConfiguration { GuildId = 1, Prefix = "?",
            Answers = [new QuickAnswerOptions { Name = "guide", Embeds = [new AnswerEmbed { Title = "Guide" }] }] };
        var discord = new FakeCommunityDiscord { Member = new CommunityMember(7, "Player", []), SendFailuresRemaining = 1 };
        var store = new FakeTicketStore();
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 300, "?guide", default));
        config.Answers[0].Embeds = [];
        config.Answers[0].Responses = ["Plain configuration now"];
        discord.ChannelPermissions[100] = new ChannelPermissions(viewChannel: true, sendMessages: true);

        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.Zero);
        Assert.That(store.IsCompleted("answer:100:300"), Is.False);
        Assert.That(discord.Sent, Is.Empty);
        Assert.That(discord.PermissionChecks.Last(), Is.EqualTo((100UL, false, true)));
        discord.ChannelPermissions[100] = new ChannelPermissions(viewChannel: true, sendMessages: true, embedLinks: true);
        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(discord.Sent.Single().Message.Embeds, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task MissingFactionChannelPermissionsDoNotBlockUnrelatedAllowedRecovery()
    {
        var (config, discord, command, key) = DeletionCase(true);
        var store = new FakeTicketStore();
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        discord.BeforeRoleMutation = _ => throw new InvalidOperationException("Crash before role mutation.");
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 300, command, default));
        discord.BeforeRoleMutation = null;
        discord.SendFailuresRemaining = 1;
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 200, 301, "?guide", default));
        discord.ChannelPermissions[100] = new ChannelPermissions(viewChannel: true);
        discord.ChannelPermissions[200] = new ChannelPermissions(viewChannel: true, sendMessages: true, manageMessages: true);

        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(discord.RoleMutations, Is.Empty);
        Assert.That(store.IsCompleted(key), Is.False);
        Assert.That(store.IsCompleted("answer:200:301"), Is.True);
        Assert.That(discord.Sent.Single().ChannelId, Is.EqualTo(200));
    }

    [Test]
    public async Task FactionResponseOnlyDeletionPreservesOrdinaryChannelRecovery()
    {
        var (config, discord, command, key) = DeletionCase(true);
        config.Factions.DeleteCommand = false;
        config.Factions.DeleteResponse = true;
        discord.ChannelPermissions[100] = new ChannelPermissions(viewChannel: true, sendMessages: true);
        discord.BeforeRoleMutation = _ => throw new InvalidOperationException("Crash before role mutation.");
        var store = new FakeTicketStore();
        var service = new CommunityService(config, store, discord, new FakeDeletionStore());
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 300, command, default));
        discord.BeforeRoleMutation = null;

        Assert.That(await service.RecoverPendingDeliveriesAsync(default), Is.EqualTo(1));
        Assert.That(store.IsCompleted(key), Is.True);
        Assert.That(discord.Member!.RoleIds, Is.EqualTo(new[] { 21UL }));
    }
    private static (BotConfiguration Config, FakeCommunityDiscord Discord, FakeTicketStore Store,
        CommunityService Service, ManualTimeProvider Clock) LongFactionRecovery()
    {
        var roles = Enumerable.Range(21, 8).Select(id => new FactionRole { Name = "Faction" + id, RoleId = (ulong)id }).ToArray();
        var config = new BotConfiguration
        {
            GuildId = 1, Prefix = "?",
            Factions = new FactionOptions { Enabled = true, Exclusive = true, Behavior = "join", Roles = roles }
        };
        var discord = new FakeCommunityDiscord
        {
            Member = new CommunityMember(7, "Player", roles.Skip(1).Select(role => role.RoleId).ToArray()),
            Roles = roles.Select(role => new CommunityRole(role.RoleId, 1)).ToList(), SendFailuresRemaining = 1
        };
        var store = new FakeTicketStore();
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var service = new CommunityService(config, store, discord, new FakeDeletionStore(), clock);
        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(7, 100, 300, "?rank Faction21", default));
        return (config, discord, store, service, clock);
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
        private readonly Dictionary<string, ulong> _actions = new(StringComparer.Ordinal);
        public DateTimeOffset? CooldownAt(string key) => _cooldowns.TryGetValue(key, out var at) ? at : null;
        public void SeedCooldown(string key, DateTimeOffset at) => _cooldowns[key] = at;
        private readonly SemaphoreSlim _ticketLock = new(1, 1);
        private readonly SemaphoreSlim _communityLock = new(1, 1);
        public int PersistenceFailuresRemaining { get; set; }
        public Action<string>? BeforeComplete { get; set; }
        public Action<string>? AfterComplete { get; set; }
        public bool HasDelivery(string key) => _states.ContainsKey(key);
        public bool IsCompleted(string key) => _states.TryGetValue(key, out var state) && state.Completed;
        public bool HasCooldown(string key) => _cooldowns.ContainsKey(key);
        public void SeedDelivery(string key, string intent, bool completed = false) => _states[key] = new DeliveryState(intent, completed);
        public Task PersistCommunityDeliveryAsync(string key, string intent, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (PersistenceFailuresRemaining > 0)
            {
                PersistenceFailuresRemaining--;
                throw new InvalidOperationException("Simulated durable join persistence failure.");
            }
            _states.TryAdd(key, new DeliveryState(intent, false));
            return Task.CompletedTask;
        }
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
            public Task<Ticket?> GetTicketAsync(Guid id, CancellationToken ct) => Task.FromResult<Ticket?>(null);
            public Task<Ticket?> FindByChannelAsync(ulong channelId, CancellationToken ct) => Task.FromResult<Ticket?>(null);
            public Task<bool> TryAdvanceCommunityActionAsync(string scope, ulong messageId, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                if (owner._actions.TryGetValue(scope, out var latest) && messageId < latest) return Task.FromResult(false);
                owner._actions[scope] = messageId;
                return Task.FromResult(true);
            }
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
        public int MemberLookups { get; private set; }
        public Dictionary<ulong, ChannelPermissions> ChannelPermissions { get; } = [];
        public List<(ulong Channel, bool DeleteSource, bool RequireEmbeds)> PermissionChecks { get; } = [];
        public Func<CommunityMessage, CancellationToken, Task<ulong>>? DirectMessagePost { get; init; }
        public Exception? DirectMessageError { get; set; }
        public Exception? ChannelSendError { get; set; }
        public bool ReturnNullDirectMessage { get; set; }
        public int DirectMessageAttempts { get; private set; }
        public Action<CancellationToken>? BeforeDirectMessageSend { get; set; }
        public List<CommunityMessage> DirectMessages { get; } = [];
        public int SendFailuresRemaining { get; set; }
        public int DeleteFailuresRemaining { get; set; }
        public bool FailAfterNextRemove { get; set; }
        public Action<CancellationToken>? BeforeRoleMutation { get; set; }
        public TaskCompletionSource<bool>? SendStarted { get; init; }
        public TaskCompletionSource<bool>? ReleaseSend { get; init; }
        private readonly Dictionary<string, ulong> _sentKeys = new(StringComparer.Ordinal);
        public Task<CommunityMember?> GetMemberAsync(ulong userId, CancellationToken ct)
        {
            MemberLookups++;
            return Task.FromResult(Member?.UserId == userId ? Member : null);
        }
        public Task ValidateCommunityChannelAsync(ulong channelId, bool deleteSource, bool requireEmbeds, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            PermissionChecks.Add((channelId, deleteSource, requireEmbeds));
            var permissions = ChannelPermissions.GetValueOrDefault(channelId,
                new ChannelPermissions(viewChannel: true, sendMessages: true, manageMessages: true, embedLinks: true));
            DiscordOperations.ValidateTextChannelPermissions(channelId, permissions, deleteSource, requireEmbeds);
            return Task.CompletedTask;
        }
        public Task<CommunityRole?> GetRoleAsync(ulong roleId, CancellationToken ct) => Task.FromResult(Roles.FirstOrDefault(x => x.RoleId == roleId));
        public Task AddRoleAsync(ulong userId, ulong roleId, CancellationToken ct)
        {
            BeforeRoleMutation?.Invoke(ct);
            ct.ThrowIfCancellationRequested();
            RoleMutations.Add($"add:{userId}:{roleId}");
            if (Member is not null) Member = Member with { RoleIds = Member.RoleIds.Append(roleId).Distinct().ToArray() };
            return Task.CompletedTask;
        }
        public Task RemoveRoleAsync(ulong userId, ulong roleId, CancellationToken ct)
        {
            BeforeRoleMutation?.Invoke(ct);
            ct.ThrowIfCancellationRequested();
            RoleMutations.Add($"remove:{userId}:{roleId}");
            if (Member is not null) Member = Member with { RoleIds = Member.RoleIds.Where(x => x != roleId).ToArray() };
            if (FailAfterNextRemove) { FailAfterNextRemove = false; throw new InvalidOperationException("simulated role removal failure after mutation"); }
            return Task.CompletedTask;
        }
        public async Task<ulong?> SendAsync(ulong channelId, CommunityMessage message, CancellationToken ct)
        {
            if (ChannelSendError is { } error) throw error;
            SendStarted?.TrySetResult(true);
            if (ReleaseSend is { } release) await release.Task.WaitAsync(ct);
            if (SendFailuresRemaining > 0) { SendFailuresRemaining--; throw new InvalidOperationException("simulated send failure"); }
            if (message.DeliveryKey is { } key && _sentKeys.TryGetValue(key, out var existing)) return existing;
            Sent.Add((channelId, message));
            var id = 500 + (ulong)Sent.Count;
            if (message.DeliveryKey is { } deliveryKey) _sentKeys[deliveryKey] = id;
            return id;
        }
        public async Task<ulong?> SendDirectMessageAsync(ulong userId, CommunityMessage message, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            DirectMessageAttempts++;
            BeforeDirectMessageSend?.Invoke(ct);
            if (DirectMessageError is { } error) throw error;
            if (DirectMessagePost is { } post) return await post(message, ct);
            if (ReturnNullDirectMessage) return null;
            DirectMessages.Add(message);
            return 900 + (ulong)DirectMessages.Count;
        }
        public Task DeleteMessageAsync(ulong channelId, ulong messageId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (DeleteFailuresRemaining > 0) { DeleteFailuresRemaining--; throw new InvalidOperationException("simulated deletion failure"); }
            Deleted.Add((channelId, messageId));
            return Task.CompletedTask;
        }
    }

    private sealed class WelcomePostHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = status;
        public string Body { get; set; } = body;
        public int Requests { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Assert.That(request.RequestUri?.AbsoluteUri, Is.EqualTo("https://discord.com/api/v10/channels/10/messages"));
            Requests++;
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(Body) });
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset currentTime) : TimeProvider
    {
        private DateTimeOffset _currentTime = currentTime;
        public override DateTimeOffset GetUtcNow() => _currentTime;
        private readonly List<ManualTimer> timers = [];
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            timers.Add(timer);
            timer.Change(dueTime, period);
            return timer;
        }
        public void Advance(TimeSpan amount)
        {
            _currentTime += amount;
            foreach (var timer in timers.ToArray()) timer.FireIfDue();
        }
        private sealed class ManualTimer(ManualTimeProvider clock, TimerCallback callback, object? state) : ITimer
        {
            private DateTimeOffset? due;
            private bool disposed;
            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (disposed) return false;
                due = dueTime == Timeout.InfiniteTimeSpan ? null : clock.GetUtcNow() + dueTime;
                return true;
            }
            public void FireIfDue()
            {
                if (!disposed && due is { } at && clock.GetUtcNow() >= at)
                {
                    due = null;
                    callback(state);
                }
            }
            public void Dispose() { disposed = true; clock.timers.Remove(this); }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
