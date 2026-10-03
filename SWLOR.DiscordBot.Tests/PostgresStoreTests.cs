using Npgsql;
using NUnit.Framework;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Persistence;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class PostgresStoreTests
{
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
    public async Task TicketAndPendingIntentSurviveStoreRestart()
    {
        var key = Guid.NewGuid().ToString("N");
        Ticket ticket;
        await using (var first = new PostgresTicketStore(ConnectionString))
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
        await using var second = new PostgresTicketStore(ConnectionString);
        await second.InitializeAsync(default);
        await using var resumed = await second.LockAsync(default);
        Assert.That(await resumed.FindInteractionAsync(key, default), Is.EqualTo(ticket));
        var intent = await resumed.GetOrCreateDeliveryAsync(key, "remove-faction:123", default);
        Assert.That(intent, Is.EqualTo(new DeliveryState("add-faction:123", false)));
        await resumed.CompleteDeliveryAsync(key, default);
        Assert.That((await resumed.GetOrCreateDeliveryAsync(key, "new", default)).Completed, Is.True);
        Assert.That(await resumed.GetCooldownAsync(key, default), Is.Not.Null);
    }

    [Test]
    public async Task CompetingStoreCannotMutateWhileAnotherSessionOwnsLock()
    {
        await using var first = new PostgresTicketStore(ConnectionString);
        await using var second = new PostgresTicketStore(ConnectionString);
        await first.InitializeAsync(default);
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
        await using (var first = new PostgresTicketStore(ConnectionString))
        {
            await first.InitializeAsync(default);
            await using var session = await first.LockAsync(default);
            var reserved = await session.ReserveAsync("support", 7, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, default);
            pending = reserved with { State = TicketState.Deleted, ArchivePath = "/data/archives/" + reserved.Id.ToString("N"),
                ArchiveComplete = false, ArchiveExpiresAt = DateTimeOffset.UtcNow.AddDays(90) };
            await session.SaveAsync(pending, "archive-pending", null, default);
        }
        await using var restarted = new PostgresTicketStore(ConnectionString);
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
        await using (var first = new PostgresTicketStore(ConnectionString))
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
        await using var second = new PostgresTicketStore(ConnectionString);
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
                INSERT INTO swlor_bot_delivery_operations(key,intent,completed) VALUES ('existing','original',true);
                """;
            await using var command = new NpgsqlCommand(versionOne, connection);
            await command.ExecuteNonQueryAsync();
        }
        await using var store = new PostgresTicketStore(ConnectionString);
        await store.InitializeAsync(default);
        await store.InitializeAsync(default);
        await using var session = await store.LockAsync(default);
        Assert.That(await session.GetOrCreateDeliveryAsync("existing", "replacement", default), Is.EqualTo(new DeliveryState("original", true)));
        await store.ScheduleDeletionAsync(10, 11, DateTimeOffset.UtcNow, default);
        Assert.That(await store.GetDueDeletionsAsync(DateTimeOffset.UtcNow.AddSeconds(1), default), Has.Count.EqualTo(1));
        await using var verify = new NpgsqlConnection(ConnectionString);
        await verify.OpenAsync();
        await using var version = new NpgsqlCommand("SELECT max(version) FROM swlor_bot_schema", verify);
        Assert.That(await version.ExecuteScalarAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task CommunityLockSerializesCommunityAndDoesNotBlockTickets()
    {
        await using var first = new PostgresTicketStore(ConnectionString);
        await using var second = new PostgresTicketStore(ConnectionString);
        await first.InitializeAsync(default);
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
        await using var store = new PostgresTicketStore(settings.ConnectionString);
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
        await using var store = new PostgresTicketStore(settings.ConnectionString);
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
        await using var first = new PostgresTicketStore(ConnectionString);
        await first.InitializeAsync(default);
        await using var owner = await first.LockAsync(default);
        var name = "bot_lock_test_" + Guid.NewGuid().ToString("N");
        var settings = new NpgsqlConnectionStringBuilder(ConnectionString) { ApplicationName = name, MaxPoolSize = 1 };
        await using var second = new PostgresTicketStore(settings.ConnectionString);
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
}
