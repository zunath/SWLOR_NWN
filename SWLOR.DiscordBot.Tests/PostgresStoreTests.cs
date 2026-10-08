using Npgsql;
using NUnit.Framework;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Persistence;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class PostgresStoreTests
{
    private const ulong TestGuildId = 484936923341651971;
    private string ConnectionString = "";
    private string? schema;
    private string configuredDatabase = "";

    [SetUp]
    public async Task SetUp()
    {
        var configured = Environment.GetEnvironmentVariable("SWLOR_BOT_TEST_DATABASE");
        if (string.IsNullOrWhiteSpace(configured))
            Assert.Ignore("Set SWLOR_BOT_TEST_DATABASE to an isolated test database to run PostgreSQL integration tests.");
        configuredDatabase = configured!;
        schema = "bot_test_" + Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(configuredDatabase);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE SCHEMA {schema}", connection);
        await command.ExecuteNonQueryAsync();
        ConnectionString = new NpgsqlConnectionStringBuilder(configuredDatabase)
            { SearchPath = schema, MaxPoolSize = 4 }.ConnectionString;
    }

    [TearDown]
    public async Task TearDown()
    {
        if (schema is null) return;
        await using var connection = new NpgsqlConnection(configuredDatabase);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP SCHEMA {schema} CASCADE", connection);
        await command.ExecuteNonQueryAsync();
        schema = null;
    }

    [Test]
    public async Task SavedArchiveGenerationSurvivesStoreRestart()
    {
        Ticket saved;
        await using (var store = new PostgresTicketStore(ConnectionString, TestGuildId))
        {
            await store.InitializeAsync(default);
            await using var session = await store.LockAsync(default);
            var reserved = await session.ReserveAsync("support", 42, "archive-generation", DateTimeOffset.UtcNow, default);
            var directory = Path.Combine(Path.GetTempPath(), "archives", reserved.Id.ToString("N"));
            saved = reserved with
            {
                State = TicketState.Closed, ChannelId = 123, ArchivePath = directory,
                ArchiveComplete = true, ArchiveSnapshotPath = Path.Combine(directory, "snapshots", Guid.NewGuid().ToString("N"))
            };
            await session.SaveAsync(saved, "archived", 42, default);
        }

        await using var restarted = new PostgresTicketStore(ConnectionString, TestGuildId);
        await restarted.InitializeAsync(default);
        await using var verification = await restarted.LockAsync(default);
        Assert.That(await verification.FindInteractionAsync("archive-generation", default), Is.EqualTo(saved));
        Assert.That(await verification.FindByChannelAsync(123, default), Is.EqualTo(saved));
    }

    [Test]
    public async Task DifferentGuildCannotReadMutateOrPruneOwnedDatabase()
    {
        await using var owner = new PostgresTicketStore(ConnectionString, TestGuildId);
        await owner.InitializeAsync(default);
        Ticket retained;
        await using (var session = await owner.LockAsync(default))
        {
            retained = await session.ReserveAsync("support", 42, "owned-ticket", DateTimeOffset.UtcNow, default);
            retained = retained with { State = TicketState.Closed, ChannelId = 123, ArchivePath = "retained/transcript.html" };
            await session.SaveAsync(retained, "closed", 42, default);
            await session.GetOrCreateDeliveryAsync("owned-delivery", "private reply", default);
            await session.CompleteDeliveryAsync("owned-delivery", default);
        }
        await owner.ScheduleDeletionAsync(123, 456, DateTimeOffset.UtcNow.AddDays(-40), default);
        await using (var age = new NpgsqlConnection(ConnectionString))
        {
            await age.OpenAsync();
            await using var command = new NpgsqlCommand(
                "UPDATE swlor_bot_delivery_operations SET updated_at=now()-interval '40 days'", age);
            await command.ExecuteNonQueryAsync();
        }
        await using var other = new PostgresTicketStore(ConnectionString, TestGuildId + 1);
        var error = Assert.ThrowsAsync<InvalidOperationException>(async () => await other.InitializeAsync(default));
        Assert.That(error!.Message, Does.Contain("different Discord guild"));
        Assert.Throws<InvalidOperationException>(() => other.LockAsync(default));
        Assert.Throws<InvalidOperationException>(() => other.LockCommunityAsync(default));
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await other.PersistCommunityDeliveryAsync("other", "other reply", default));
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await other.ScheduleDeletionAsync(123, 789, DateTimeOffset.UtcNow, default));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await other.GetDueDeletionsAsync(DateTimeOffset.UtcNow, default));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await other.CompleteDeletionAsync(123, 456, default));
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await other.RetryDeletionAsync(new(123, 456, DateTimeOffset.UtcNow, 0, null),
                DateTimeOffset.UtcNow.AddDays(1), "other", default));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await other.PruneCompletedDeliveriesAsync(DateTimeOffset.UtcNow, default));

        await using var verify = await owner.LockAsync(default);
        Assert.That(await verify.FindInteractionAsync("owned-ticket", default), Is.EqualTo(retained));
        Assert.That(await verify.GetOrCreateDeliveryAsync("owned-delivery", "replacement", default),
            Is.EqualTo(new DeliveryState("private reply", true)));
        Assert.That(await owner.GetDueDeletionsAsync(DateTimeOffset.UtcNow, default),
            Has.One.Matches<PendingResponseDeletion>(x => x.MessageId == 456 && x.Attempts == 0));
    }

    [Test]
    public async Task ConcurrentFirstInitializationClaimsDatabaseForOnlyOneGuild()
    {
        await using var first = new PostgresTicketStore(ConnectionString, TestGuildId);
        await using var second = new PostgresTicketStore(ConnectionString, TestGuildId + 1);
        async Task<bool> Initialize(PostgresTicketStore store)
        {
            try { await store.InitializeAsync(default); return true; }
            catch (InvalidOperationException error) when (error.Message.Contains("different Discord guild")) { return false; }
        }
        var results = await Task.WhenAll(Initialize(first), Initialize(second));
        Assert.That(results.Count(x => x), Is.EqualTo(1));
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var read = new NpgsqlCommand("SELECT guild_id FROM swlor_bot_database_owner", connection);
        Assert.That(await read.ExecuteScalarAsync(), Is.EqualTo((results[0] ? TestGuildId : TestGuildId + 1)
            .ToString(System.Globalization.CultureInfo.InvariantCulture)));
        await using var successful = await (results[0] ? first : second).LockAsync(default);
        Assert.That(await successful.GetTicketsAsync(default), Is.Empty);
    }

    [Test]
    public async Task VersionFourDatabaseBindsOwnershipWithoutDiscardingRetainedData()
    {
        Ticket retained;
        await using (var original = new PostgresTicketStore(ConnectionString, TestGuildId))
        {
            await original.InitializeAsync(default);
            await using var session = await original.LockAsync(default);
            retained = await session.ReserveAsync("support", 42, "legacy-ticket", DateTimeOffset.UtcNow, default);
            await session.GetOrCreateDeliveryAsync("legacy", "legacy reply", default);
        }
        await using (var connection = new NpgsqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            await using var legacy = new NpgsqlCommand(
                "DROP TABLE swlor_bot_database_owner; DELETE FROM swlor_bot_schema WHERE version=5", connection);
            await legacy.ExecuteNonQueryAsync();
        }
        await using var upgraded = new PostgresTicketStore(ConnectionString, TestGuildId);
        await upgraded.InitializeAsync(default);
        await upgraded.InitializeAsync(default);
        await using var sessionAfterUpgrade = await upgraded.LockAsync(default);
        Assert.That(await sessionAfterUpgrade.FindInteractionAsync("legacy-ticket", default), Is.EqualTo(retained));
        Assert.That(await sessionAfterUpgrade.GetOrCreateDeliveryAsync("legacy", "replacement", default),
            Is.EqualTo(new DeliveryState("legacy reply", false)));
        await sessionAfterUpgrade.DisposeAsync();
        await using var wrongGuild = new PostgresTicketStore(ConnectionString, TestGuildId + 1);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await wrongGuild.InitializeAsync(default));
    }

    [Test]
    public async Task FailedSchemaUpgradeRollsBackOwnershipAndSchemaChanges()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using (var newer = new NpgsqlCommand(
            "CREATE TABLE swlor_bot_schema(version integer PRIMARY KEY, applied_at timestamptz DEFAULT now()); INSERT INTO swlor_bot_schema VALUES (6, now())",
            connection))
            await newer.ExecuteNonQueryAsync();
        await using var store = new PostgresTicketStore(ConnectionString, TestGuildId);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await store.InitializeAsync(default));
        Assert.Throws<InvalidOperationException>(() => store.LockAsync(default));
        await using var owner = new NpgsqlCommand("SELECT to_regclass('swlor_bot_database_owner')::text", connection);
        Assert.That(await owner.ExecuteScalarAsync(), Is.EqualTo(DBNull.Value));
        await using var versions = new NpgsqlCommand("SELECT count(*) FROM swlor_bot_schema", connection);
        Assert.That(await versions.ExecuteScalarAsync(), Is.EqualTo(1L));
    }

    [Test]
    public async Task GuildOwnershipPreservesUnsignedSnowflakeAndRequiresInitialization()
    {
        await using var store = new PostgresTicketStore(ConnectionString, ulong.MaxValue);
        Assert.Throws<InvalidOperationException>(() => store.LockAsync(default));
        await store.InitializeAsync(default);
        await using var restarted = new PostgresTicketStore(ConnectionString, ulong.MaxValue);
        await restarted.InitializeAsync(default);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT guild_id FROM swlor_bot_database_owner", connection);
        Assert.That(await command.ExecuteScalarAsync(), Is.EqualTo("18446744073709551615"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PostgresTicketStore(ConnectionString, 0));
    }

    [Test]
    public async Task TicketAndPendingIntentSurviveStoreRestart()
    {
        var key = Guid.NewGuid().ToString("N");
        Ticket ticket;
        await using (var first = new PostgresTicketStore(ConnectionString, TestGuildId))
        {
            await first.InitializeAsync(default);
            await using var session = await first.LockAsync(default);
            ticket = await session.ReserveAsync("support", ulong.MaxValue, key, DateTimeOffset.UtcNow, default);
            ticket = ticket with { State = TicketState.Closed, ChannelId = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray()),
                ClosedAt = DateTimeOffset.UtcNow, DeleteAfter = DateTimeOffset.UtcNow.AddDays(7) };
            await session.SaveAsync(ticket, "test-close", ulong.MaxValue, default);
            await session.GetOrCreateDeliveryAsync(key, "add-faction:123", default);
            await session.SetCooldownAsync(key, DateTimeOffset.UtcNow, default);
        }
        await using var second = new PostgresTicketStore(ConnectionString, TestGuildId);
        await second.InitializeAsync(default);
        await using var resumed = await second.LockAsync(default);
        Assert.That(await resumed.FindInteractionAsync(key, default), Is.EqualTo(ticket));
        Assert.That(await resumed.GetTicketAsync(ticket.Id, default), Is.EqualTo(ticket), "Ticket id lookup should return the persisted row directly.");
        Assert.That(await resumed.FindByChannelAsync(ticket.ChannelId!.Value, default), Is.EqualTo(ticket), "Channel lookup should use the unique channel_id value.");
        Assert.That(await resumed.GetTicketAsync(Guid.NewGuid(), default), Is.Null);
        Assert.That(await resumed.FindByChannelAsync(0, default), Is.Null);
        var intent = await resumed.GetOrCreateDeliveryAsync(key, "remove-faction:123", default);
        Assert.That(intent, Is.EqualTo(new DeliveryState("add-faction:123", false)));
        await resumed.CompleteDeliveryAsync(key, default);
        Assert.That((await resumed.GetOrCreateDeliveryAsync(key, "new", default)).Completed, Is.True);
        Assert.That(await resumed.GetCooldownAsync(key, default), Is.Not.Null);
    }

    [Test]
    public async Task WelcomeIntentPersistsIndependentlyWhileCommunityDeliveryLockIsHeldAndSurvivesRestart()
    {
        const string key = "welcome:7:100";
        const string intent = """{"Version":1,"Kind":"welcome","UserId":7,"ChannelId":100}""";
        await using (var store = new PostgresTicketStore(ConnectionString, TestGuildId))
        {
            await store.InitializeAsync(default);
            await using var held = await store.LockCommunityAsync(default);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await store.PersistCommunityDeliveryAsync(key, intent, timeout.Token).WaitAsync(timeout.Token);
            Assert.That(await held.GetOrCreateDeliveryAsync(key, "replacement", default),
                Is.EqualTo(new DeliveryState(intent, false)), "The short insert must neither wait for the advisory lock nor overwrite the original intent.");
        }
        await using var restarted = new PostgresTicketStore(ConnectionString, TestGuildId);
        await restarted.InitializeAsync(default);
        await using var resumed = await restarted.LockCommunityAsync(default);
        Assert.That(await resumed.GetPendingDeliveriesAsync(default), Is.EqualTo(new[] { new PendingDelivery(key, intent) }));
    }

    [TestCase("answer:100:300")]
    [TestCase("faction:100:300")]
    public async Task AcceptedPrefixEventPersistsIndependentlyOfHeldDeliveryLockAndSurvivesRestart(string key)
    {
        const string accepted = "accepted prefix event";
        await using (var store = new PostgresTicketStore(ConnectionString, TestGuildId))
        {
            await store.InitializeAsync(default);
            await using var held = await store.LockCommunityAsync(default);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await store.PersistCommunityDeliveryAsync(key, accepted, timeout.Token).WaitAsync(timeout.Token);
            Assert.That(await held.GetOrCreateDeliveryAsync(key, "replacement", default), Is.EqualTo(new DeliveryState(accepted, false)));
        }
        await using var restarted = new PostgresTicketStore(ConnectionString, TestGuildId);
        await restarted.InitializeAsync(default);
        await using var session = await restarted.LockCommunityAsync(default);
        Assert.That(await session.GetPendingDeliveriesAsync(default), Is.EqualTo(new[] { new PendingDelivery(key, accepted) }));
    }

    [Test]
    public async Task PreparedFactionIntentSurvivesRestartAndDuplicateAcceptanceAndCompletedIntentCannotChange()
    {
        const string key = "faction:100:300";
        const string accepted = "accepted faction without role plan";
        const string prepared = "fresh authorized immutable role plan";
        await using (var store = new PostgresTicketStore(ConnectionString, TestGuildId))
        {
            await store.InitializeAsync(default);
            await store.PersistCommunityDeliveryAsync(key, accepted, default);
            await using var session = await store.LockCommunityAsync(default);
            await session.UpdateCommunityDeliveryIntentAsync(key, prepared, default);
        }
        await using var restarted = new PostgresTicketStore(ConnectionString, TestGuildId);
        await restarted.InitializeAsync(default);
        await restarted.PersistCommunityDeliveryAsync(key, accepted, default);
        await using var resumed = await restarted.LockCommunityAsync(default);
        Assert.That(await resumed.GetOrCreateDeliveryAsync(key, accepted, default), Is.EqualTo(new DeliveryState(prepared, false)));
        await resumed.CompleteDeliveryAsync(key, default);
        Assert.ThrowsAsync<InvalidOperationException>(() => resumed.UpdateCommunityDeliveryIntentAsync(key, "replacement", default));
        Assert.That(await resumed.GetOrCreateDeliveryAsync(key, accepted, default), Is.EqualTo(new DeliveryState(prepared, true)));
    }

    [Test]
    public async Task DuplicateJoinPersistencePreservesCompletedIntentAndItsOriginalRetentionTimestamp()
    {
        const string key = "welcome:7:100";
        await using var store = new PostgresTicketStore(ConnectionString, TestGuildId);
        await store.InitializeAsync(default);
        await store.PersistCommunityDeliveryAsync(key, "original welcome destination and content", default);
        await using (var session = await store.LockCommunityAsync(default))
            await session.CompleteDeliveryAsync(key, default);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var timestamp = new NpgsqlCommand("SELECT updated_at FROM swlor_bot_delivery_operations WHERE key=@key", connection);
        timestamp.Parameters.AddWithValue("key", key);
        var completedAt = await timestamp.ExecuteScalarAsync();

        await store.PersistCommunityDeliveryAsync(key, "changed welcome destination and content", default);

        Assert.That(await timestamp.ExecuteScalarAsync(), Is.EqualTo(completedAt));
        await using var verify = await store.LockCommunityAsync(default);
        Assert.That(await verify.GetOrCreateDeliveryAsync(key, "another replacement", default),
            Is.EqualTo(new DeliveryState("original welcome destination and content", true)));
        Assert.That(await verify.GetPendingDeliveriesAsync(default), Is.Empty);
    }
    [Test]
    public async Task PendingCommunityDeliveryCanBeEnumeratedAfterStoreRestart()
    {
        const string key = "answer:100:300";
        const string intent = "{\"Version\":1,\"Kind\":\"answer\",\"ChannelId\":100}";
        await using (var first = new PostgresTicketStore(ConnectionString, TestGuildId))
        {
            await first.InitializeAsync(default);
            await using var session = await first.LockCommunityAsync(default);
            await session.GetOrCreateDeliveryAsync(key, intent, default);
        }

        await using var restarted = new PostgresTicketStore(ConnectionString, TestGuildId);
        await restarted.InitializeAsync(default);
        await using (var session = await restarted.LockCommunityAsync(default))
        {
            Assert.That(await session.GetPendingDeliveriesAsync(default), Is.EqualTo(new[] { new PendingDelivery(key, intent) }));
            await session.CompleteDeliveryAsync(key, default);
            Assert.That(await session.GetPendingDeliveriesAsync(default), Is.Empty);
        }
    }

    [Test]
    public async Task PendingCommunityDeliveryBatchesRotateThroughBacklog()
    {
        await using var store = new PostgresTicketStore(ConnectionString, TestGuildId);
        await store.InitializeAsync(default);
        await using var session = await store.LockCommunityAsync(default);
        for (var index = 0; index < 25; index++)
            await session.GetOrCreateDeliveryAsync($"answer:100:{index + 1}", $"intent-{index}", default);

        var firstBatch = await session.GetPendingDeliveriesAsync(default);
        var secondBatch = await session.GetPendingDeliveriesAsync(default);

        Assert.That(firstBatch, Has.Count.EqualTo(20));
        Assert.That(secondBatch, Has.Count.EqualTo(20));
        Assert.That(secondBatch.Select(x => x.Key).Except(firstBatch.Select(x => x.Key)).Count(), Is.EqualTo(5),
            "Untouched operations must receive a turn even when the previous batch all failed.");
        Assert.That(firstBatch.Concat(secondBatch).Select(x => x.Key).Distinct().Count(), Is.EqualTo(25));
        foreach (var delivery in secondBatch)
            await session.CompleteDeliveryAsync(delivery.Key, default);
        var remaining = await session.GetPendingDeliveriesAsync(default);
        Assert.That(remaining.Select(x => x.Key), Is.EquivalentTo(
            firstBatch.Select(x => x.Key).Except(secondBatch.Select(x => x.Key))));
    }

    [Test]
    public async Task CompetingStoreCannotMutateWhileAnotherSessionOwnsLock()
    {
        await using var first = new PostgresTicketStore(ConnectionString, TestGuildId);
        await using var second = new PostgresTicketStore(ConnectionString, TestGuildId);
        await first.InitializeAsync(default);
        await second.InitializeAsync(default);
        var session = await first.LockAsync(default);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        try
        {
            Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await using var competing = await second.LockAsync(timeout.Token);
            });
        }
        finally { await session.DisposeAsync(); }
        using var available = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var next = await second.LockAsync(available.Token);
        Assert.That(await next.GetTicketsAsync(available.Token), Is.Not.Null);
    }

    [Test]
    public async Task DeletedTicketWithPendingArchiveOwnershipSurvivesRestartAndRemainsInRetentionQuery()
    {
        Ticket pending;
        await using (var first = new PostgresTicketStore(ConnectionString, TestGuildId))
        {
            await first.InitializeAsync(default);
            await using var session = await first.LockAsync(default);
            var reserved = await session.ReserveAsync("support", 7, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, default);
            pending = reserved with { State = TicketState.Deleted, ArchivePath = "/data/archives/" + reserved.Id.ToString("N"),
                ArchiveComplete = false, ArchiveExpiresAt = DateTimeOffset.UtcNow.AddDays(90) };
            await session.SaveAsync(pending, "archive-pending", null, default);
        }
        await using var restarted = new PostgresTicketStore(ConnectionString, TestGuildId);
        await restarted.InitializeAsync(default);
        await using var recovered = await restarted.LockAsync(default);
        Assert.That((await recovered.GetTicketsAsync(default)).Single(), Is.EqualTo(pending));
    }

    [Test]
    public async Task ResponseDeletionDueRetryAndCompletionSurviveRestartWithoutGlobalLock()
    {
        var channel = ulong.MaxValue;
        var message = ulong.MaxValue - 1;
        var due = new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero);
        await using (var first = new PostgresTicketStore(ConnectionString, TestGuildId))
        {
            await first.InitializeAsync(default);
            await using var session = await first.LockAsync(default);
            await using var community = await first.LockCommunityAsync(default);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await first.ScheduleDeletionAsync(channel, message, due, timeout.Token);
            Assert.That(await first.GetDueDeletionsAsync(due.AddTicks(-1), timeout.Token), Is.Empty);
            var deletion = (await first.GetDueDeletionsAsync(due, timeout.Token)).Single();
            await first.RetryDeletionAsync(deletion, due.AddMinutes(1), "Forbidden", timeout.Token);
            // A repeated delivery must preserve both retry metadata and its original due time.
            await first.ScheduleDeletionAsync(channel, message, due.AddHours(5), timeout.Token);
        }
        await using var second = new PostgresTicketStore(ConnectionString, TestGuildId);
        await second.InitializeAsync(default);
        Assert.That(await second.GetDueDeletionsAsync(due, default), Is.Empty);
        var pending = (await second.GetDueDeletionsAsync(due.AddMinutes(1), default)).Single();
        Assert.That(pending, Is.EqualTo(new PendingResponseDeletion(channel, message, due.AddMinutes(1), 1, "Forbidden")));
        await second.CompleteDeletionAsync(channel, message, default);
        await second.ScheduleDeletionAsync(channel, message, due, default);
        Assert.That(await second.GetDueDeletionsAsync(due.AddDays(1), default), Is.Empty);
    }

    [Test]
    public async Task ExistingVersionOneUpgradesWithoutChangingDeliveryData()
    {
        await using (var connection = new NpgsqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            const string versionOne = """
                CREATE TABLE swlor_bot_schema(version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());
                INSERT INTO swlor_bot_schema(version) VALUES (1);
                CREATE TABLE swlor_bot_delivery_operations (
                    key text PRIMARY KEY, intent text NOT NULL, completed boolean NOT NULL DEFAULT false,
                    updated_at timestamptz NOT NULL DEFAULT now());
                INSERT INTO swlor_bot_delivery_operations(key,intent,completed) VALUES
                    ('existing','original',true),
                    ('faction-old','{"Version":1,"Kind":"faction","UserId":42,"SourceMessageId":999}',true),
                    ('answer-old','{"Version":1,"Kind":"answer","UserId":42,"SourceMessageId":700,"CooldownKey":"answer-cooldown:faq:42","Cooldown":"00:00:15"}',false),
                    ('bad-json','not-json',false),
                    ('answer-no-cooldown','{"Version":1,"Kind":"answer","UserId":42,"SourceMessageId":900,"CooldownKey":"answer-cooldown:faq:42"}',false),
                    ('answer-days','{"Version":1,"Kind":"answer","UserId":42,"SourceMessageId":701,"CooldownKey":"answer-cooldown:days:42","Cooldown":"1.00:00:00"}',true),
                    ('answer-zero','{"Version":1,"Kind":"answer","UserId":42,"SourceMessageId":950,"CooldownKey":"answer-cooldown:zero:42","Cooldown":"00:00:00"}',true),
                    ('overflow-id','{"Version":1,"Kind":"faction","UserId":42,"SourceMessageId":18446744073709551616}',true),
                    ('max-id','{"Version":1,"Kind":"faction","UserId":18446744073709551615,"SourceMessageId":18446744073709551615}',false);
                """;
            await using var command = new NpgsqlCommand(versionOne, connection);
            await command.ExecuteNonQueryAsync();
        }
        await using var store = new PostgresTicketStore(ConnectionString, TestGuildId);
        await store.InitializeAsync(default);
        await store.InitializeAsync(default);
        await using var session = await store.LockAsync(default);
        Assert.That(await session.GetOrCreateDeliveryAsync("existing", "replacement", default), Is.EqualTo(new DeliveryState("original", true)));
        Assert.That(await session.TryAdvanceCommunityActionAsync("faction:42", 998, default), Is.False,
            "Migration should seed the newest faction action from existing completed intents.");
        Assert.That(await session.TryAdvanceCommunityActionAsync("faction:42", 999, default), Is.True);
        Assert.That(await session.TryAdvanceCommunityActionAsync("answer-cooldown:faq:42", 699, default), Is.False,
            "Migration should seed cooldown-bearing answer intents, including pending operations.");
        Assert.That(await session.TryAdvanceCommunityActionAsync("answer-cooldown:faq:42", 700, default), Is.True);
        Assert.That(await session.TryAdvanceCommunityActionAsync("answer-cooldown:faq:42", 800, default), Is.True);
        Assert.That(await session.TryAdvanceCommunityActionAsync("answer-cooldown:faq:42", 799, default), Is.False,
            "Advancing a scope must be monotonic.");
        Assert.That(await session.TryAdvanceCommunityActionAsync("answer-cooldown:days:42", 700, default), Is.False);
        Assert.That(await session.TryAdvanceCommunityActionAsync("answer-cooldown:zero:42", 1, default), Is.True,
            "A zero cooldown must not create an answer action cursor during backfill.");
        Assert.That(await session.TryAdvanceCommunityActionAsync("faction:" + ulong.MaxValue, ulong.MaxValue - 1, default), Is.False);
        Assert.That(await session.TryAdvanceCommunityActionAsync("faction:" + ulong.MaxValue, ulong.MaxValue, default), Is.True);
        await store.ScheduleDeletionAsync(10, 11, DateTimeOffset.UtcNow, default);
        Assert.That(await store.GetDueDeletionsAsync(DateTimeOffset.UtcNow.AddSeconds(1), default), Has.Count.EqualTo(1));
        await using var verify = new NpgsqlConnection(ConnectionString);
        await verify.OpenAsync();
        await using var version = new NpgsqlCommand("SELECT max(version) FROM swlor_bot_schema", verify);
        Assert.That(await version.ExecuteScalarAsync(), Is.EqualTo(5));
    }

    [Test]
    public async Task CommunityLockSerializesCommunityAndDoesNotBlockTickets()
    {
        await using var first = new PostgresTicketStore(ConnectionString, TestGuildId);
        await using var second = new PostgresTicketStore(ConnectionString, TestGuildId);
        await first.InitializeAsync(default);
        await second.InitializeAsync(default);
        await using var community = await first.LockCommunityAsync(default);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await using var competing = await second.LockCommunityAsync(timeout.Token);
        });
        using var available = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using (var ticket = await second.LockAsync(available.Token))
            Assert.That(await ticket.GetTicketsAsync(available.Token), Is.Not.Null);
        await community.DisposeAsync();
        await using var nextCommunity = await second.LockCommunityAsync(available.Token);
    }

    [Test]
    public async Task ConfirmedUnlockPreservesNormalPooling()
    {
        var settings = new NpgsqlConnectionStringBuilder(ConnectionString) { MaxPoolSize = 1 };
        await using var store = new PostgresTicketStore(settings.ConnectionString, TestGuildId);
        await store.InitializeAsync(default);
        int backend;
        await using (var session = (PostgresTicketStore.Session)await store.LockAsync(default))
            backend = session.Connection.ProcessID;
        await using var reused = (PostgresTicketStore.Session)await store.LockAsync(default);
        Assert.That(reused.Connection.ProcessID, Is.EqualTo(backend));
    }

    [Test]
    public async Task UnconfirmedUnlockDiscardsBackendInsteadOfReturningItToPool()
    {
        var settings = new NpgsqlConnectionStringBuilder(ConnectionString) { MaxPoolSize = 1 };
        await using var store = new PostgresTicketStore(settings.ConnectionString, TestGuildId);
        await store.InitializeAsync(default);
        var session = (PostgresTicketStore.Session)await store.LockAsync(default);
        var backend = session.Connection.ProcessID;
        await using (var unlock = new NpgsqlCommand("SELECT pg_advisory_unlock_all()", session.Connection))
            await unlock.ExecuteNonQueryAsync();
        Assert.ThrowsAsync<InvalidOperationException>(async () => await session.DisposeAsync());
        await using var fresh = (PostgresTicketStore.Session)await store.LockAsync(default);
        Assert.That(fresh.Connection.ProcessID, Is.Not.EqualTo(backend));
    }

    [Test]
    public async Task CanceledAcquisitionDiscardsWaitingBackend()
    {
        await using var first = new PostgresTicketStore(ConnectionString, TestGuildId);
        await first.InitializeAsync(default);
        var name = "bot_lock_test_" + Guid.NewGuid().ToString("N");
        var settings = new NpgsqlConnectionStringBuilder(ConnectionString) { ApplicationName = name, MaxPoolSize = 1 };
        await using var second = new PostgresTicketStore(settings.ConnectionString, TestGuildId);
        await second.InitializeAsync(default);
        await using var owner = await first.LockAsync(default);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var waiting = second.LockAsync(cancellation.Token);
        await using var observer = new NpgsqlConnection(ConnectionString);
        await observer.OpenAsync();
        int? backend = null;
        for (var attempt = 0; attempt < 100 && backend is null; attempt++)
        {
            await using var find = new NpgsqlCommand("SELECT pid FROM pg_stat_activity WHERE application_name=@name AND wait_event='advisory'", observer);
            find.Parameters.AddWithValue("name", name);
            backend = await find.ExecuteScalarAsync() as int?;
            if (backend is null) await Task.Delay(20);
        }
        await cancellation.CancelAsync();
        Assert.ThrowsAsync<OperationCanceledException>(async () => await waiting);
        Assert.That(backend, Is.Not.Null, "The second backend must reach the contended advisory lock.");
        await using var exists = new NpgsqlCommand("SELECT count(*) FROM pg_stat_activity WHERE pid=@pid", observer);
        exists.Parameters.AddWithValue("pid", backend!.Value);
        long remaining = 1;
        for (var attempt = 0; attempt < 100 && remaining != 0; attempt++)
        {
            remaining = Convert.ToInt64(await exists.ExecuteScalarAsync());
            if (remaining != 0) await Task.Delay(20);
        }
        Assert.That(remaining, Is.Zero, "An unconfirmed lock session must be physically discarded.");
    }

    [Test]
    public async Task DeliveryRetentionSurvivesRestartAndPreservesPendingRecentAndBoundaryRecords()
    {
        var now = new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero);
        await using (var first = new PostgresTicketStore(ConnectionString, TestGuildId))
        {
            await first.InitializeAsync(default);
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            const string seed = """
                INSERT INTO swlor_bot_delivery_operations(key,intent,completed,updated_at) VALUES
                    ('old','large rendered content',true,@old),('recent','recent intent',true,@recent),
                    ('pending','unfinished original intent',false,@old),('boundary','boundary intent',true,@boundary);
                INSERT INTO swlor_bot_response_deletions(channel_id,message_id,due_at,completed,completed_at) VALUES
                    ('1','10',@old,true,@old),('1','11',@old,true,@recent),('1','12',@old,false,NULL);
                INSERT INTO swlor_bot_deliveries(key,at) VALUES ('old-key',@old),('recent-key',@recent);
                INSERT INTO swlor_bot_cooldowns(key,at) VALUES ('old-cooldown',@old),('recent-cooldown',@recent);
                """;
            await using var command = new NpgsqlCommand(seed, connection);
            command.Parameters.AddWithValue("old", now.AddDays(-31));
            command.Parameters.AddWithValue("recent", now.AddDays(-1));
            command.Parameters.AddWithValue("boundary", now - PostgresTicketStore.CompletedDeliveryRetention);
            await command.ExecuteNonQueryAsync();
        }
        await using var restarted = new PostgresTicketStore(ConnectionString, TestGuildId);
        await restarted.InitializeAsync(default);
        await restarted.PruneCompletedDeliveriesAsync(now, default);
        await using var session = await restarted.LockCommunityAsync(default);
        Assert.That(await session.GetOrCreateDeliveryAsync("recent", "replacement", default), Is.EqualTo(new DeliveryState("recent intent", true)));
        Assert.That(await session.GetOrCreateDeliveryAsync("pending", "replacement", default), Is.EqualTo(new DeliveryState("unfinished original intent", false)));
        Assert.That(await session.GetOrCreateDeliveryAsync("boundary", "replacement", default), Is.EqualTo(new DeliveryState("boundary intent", true)));
        Assert.That(await session.GetOrCreateDeliveryAsync("old", "replacement", default), Is.EqualTo(new DeliveryState("replacement", false)));
        Assert.That(await session.TryRecordDeliveryAsync("recent-key", default), Is.False);
        Assert.That(await session.TryRecordDeliveryAsync("old-key", default), Is.True);
        Assert.That(await session.GetCooldownAsync("recent-cooldown", default), Is.EqualTo(now.AddDays(-1)));
        Assert.That(await session.GetCooldownAsync("old-cooldown", default), Is.Null);
        var pending = await restarted.GetDueDeletionsAsync(now, default);
        Assert.That(pending.Single().MessageId, Is.EqualTo(12UL));
        await using var verify = new NpgsqlConnection(ConnectionString);
        await verify.OpenAsync();
        await using var remaining = new NpgsqlCommand("SELECT count(*) FROM swlor_bot_response_deletions", verify);
        Assert.That(await remaining.ExecuteScalarAsync(), Is.EqualTo(2L));
    }

    [Test]
    public async Task VersionTwoTombstonesAndLateDeletesGetFullWindowFromCompletion()
    {
        await using (var connection = new NpgsqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            const string versionTwo = """
                CREATE TABLE swlor_bot_schema(version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());
                INSERT INTO swlor_bot_schema(version) VALUES (2);
                CREATE TABLE swlor_bot_response_deletions (
                    channel_id text NOT NULL,message_id text NOT NULL,due_at timestamptz NOT NULL,
                    attempts integer NOT NULL DEFAULT 0,last_error text,completed boolean NOT NULL DEFAULT false,
                    PRIMARY KEY(channel_id,message_id));
                INSERT INTO swlor_bot_response_deletions(channel_id,message_id,due_at,completed) VALUES
                    ('1','10',now()-interval '90 days',true),('1','11',now()-interval '90 days',false);
                """;
            await using var seed = new NpgsqlCommand(versionTwo, connection);
            await seed.ExecuteNonQueryAsync();
        }
        await using var store = new PostgresTicketStore(ConnectionString, TestGuildId);
        await store.InitializeAsync(default);
        await using var verify = new NpgsqlConnection(ConnectionString);
        await verify.OpenAsync();
        await using var completedAt = new NpgsqlCommand("SELECT completed_at FROM swlor_bot_response_deletions WHERE message_id='10'", verify);
        var migrationCompletion = (DateTime)(await completedAt.ExecuteScalarAsync())!;
        await store.InitializeAsync(default);
        Assert.That(await completedAt.ExecuteScalarAsync(), Is.EqualTo(migrationCompletion));
        await store.CompleteDeletionAsync(1, 11, default);
        await store.PruneCompletedDeliveriesAsync(DateTimeOffset.UtcNow, default);
        await using var count = new NpgsqlCommand("SELECT count(*) FROM swlor_bot_response_deletions WHERE completed_at>now()-interval '1 minute'", verify);
        Assert.That(await count.ExecuteScalarAsync(), Is.EqualTo(2L));
        // Repeated completion does not postpone the tombstone's fixed retention deadline.
        await using var age = new NpgsqlCommand("UPDATE swlor_bot_response_deletions SET completed_at=now()-interval '31 days' WHERE message_id='11'", verify);
        await age.ExecuteNonQueryAsync();
        await store.CompleteDeletionAsync(1, 11, default);
        await store.PruneCompletedDeliveriesAsync(DateTimeOffset.UtcNow, default);
        await using var retained = new NpgsqlCommand("SELECT message_id FROM swlor_bot_response_deletions", verify);
        Assert.That(await retained.ExecuteScalarAsync(), Is.EqualTo("10"));
    }

    [Test]
    public async Task RetentionDrainsMoreThanOneBatchWithoutTouchingUnfinishedWork()
    {
        await using var store = new PostgresTicketStore(ConnectionString, TestGuildId);
        await store.InitializeAsync(default);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string seed = """
            INSERT INTO swlor_bot_delivery_operations(key,intent,completed,updated_at)
                SELECT 'completed-'||value,'old rendered content',true,now()-interval '31 days' FROM generate_series(1,2100) AS value;
            INSERT INTO swlor_bot_delivery_operations(key,intent,completed,updated_at)
                VALUES ('pending','unfinished',false,now()-interval '100 days');
            """;
        await using var insert = new NpgsqlCommand(seed, connection);
        await insert.ExecuteNonQueryAsync();
        await store.PruneCompletedDeliveriesAsync(DateTimeOffset.UtcNow, default);
        await using var count = new NpgsqlCommand("SELECT count(*) FROM swlor_bot_delivery_operations", connection);
        Assert.That(await count.ExecuteScalarAsync(), Is.EqualTo(1L));
        await using var session = await store.LockCommunityAsync(default);
        Assert.That(await session.GetOrCreateDeliveryAsync("pending", "replacement", default), Is.EqualTo(new DeliveryState("unfinished", false)));
    }

    [Test]
    public async Task RepeatedDeliveryCompletionDoesNotExtendReplayRetention()
    {
        await using var store = new PostgresTicketStore(ConnectionString, TestGuildId);
        await store.InitializeAsync(default);
        await using (var session = await store.LockCommunityAsync(default))
        {
            await session.GetOrCreateDeliveryAsync("completed", "original", default);
            await session.CompleteDeliveryAsync("completed", default);
        }
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var age = new NpgsqlCommand("UPDATE swlor_bot_delivery_operations SET updated_at=now()-interval '31 days' WHERE key='completed'", connection);
        await age.ExecuteNonQueryAsync();
        await using (var session = await store.LockCommunityAsync(default))
            await session.CompleteDeliveryAsync("completed", default);
        await store.PruneCompletedDeliveriesAsync(DateTimeOffset.UtcNow, default);
        await using var remaining = new NpgsqlCommand("SELECT count(*) FROM swlor_bot_delivery_operations", connection);
        Assert.That(await remaining.ExecuteScalarAsync(), Is.EqualTo(0L));
    }
}
