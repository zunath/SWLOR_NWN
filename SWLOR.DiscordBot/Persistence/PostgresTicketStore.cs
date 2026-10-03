using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using SWLOR.DiscordBot.Core;

namespace SWLOR.DiscordBot.Persistence;

public sealed class PostgresTicketStore(string connectionString) : ITicketStore, IResponseDeletionStore, IAsyncDisposable
{
    // Gateway retries/resume and REST nonce deduplication are much shorter than this replay window.
    // Thirty days also covers the longest permitted quick-answer cooldown.
    internal static readonly TimeSpan CompletedDeliveryRetention = TimeSpan.FromDays(30);
    private const long MutationLock = 7821653091;
    private const long CommunityLock = 7821653092;
    private readonly NpgsqlDataSource _source = NpgsqlDataSource.Create(connectionString);

    public async Task InitializeAsync(CancellationToken ct)
    {
        await using var session = (Session)await LockAsync(ct);
        const string schema = """
            CREATE SEQUENCE IF NOT EXISTS swlor_bot_ticket_numbers;
            CREATE TABLE IF NOT EXISTS swlor_bot_tickets (
                id uuid PRIMARY KEY, interaction_id text NOT NULL UNIQUE,
                state text NOT NULL, channel_id text UNIQUE, archive_path text,
                payload jsonb NOT NULL, updated_at timestamptz NOT NULL DEFAULT now());
            CREATE TABLE IF NOT EXISTS swlor_bot_ticket_audit (
                id bigserial PRIMARY KEY, ticket_id uuid NOT NULL REFERENCES swlor_bot_tickets(id),
                action text NOT NULL, actor_id text, at timestamptz NOT NULL DEFAULT now());
            CREATE TABLE IF NOT EXISTS swlor_bot_deliveries (
                key text PRIMARY KEY, at timestamptz NOT NULL DEFAULT now());
            CREATE TABLE IF NOT EXISTS swlor_bot_delivery_operations (
                key text PRIMARY KEY, intent text NOT NULL, completed boolean NOT NULL DEFAULT false,
                updated_at timestamptz NOT NULL DEFAULT now());
            CREATE TABLE IF NOT EXISTS swlor_bot_cooldowns (
                key text PRIMARY KEY, at timestamptz NOT NULL);
            CREATE TABLE IF NOT EXISTS swlor_bot_schema (
                version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());
            INSERT INTO swlor_bot_schema(version) VALUES (1) ON CONFLICT DO NOTHING;
            """;
        await using var command = new NpgsqlCommand(schema, session.Connection);
        await command.ExecuteNonQueryAsync(ct);
        await using var versionCommand = new NpgsqlCommand("SELECT max(version) FROM swlor_bot_schema", session.Connection);
        if (Convert.ToInt32(await versionCommand.ExecuteScalarAsync(ct)) > 3)
            throw new InvalidOperationException("The bot database schema is newer than this application.");
        await using var transaction = await session.Connection.BeginTransactionAsync(ct);
        const string upgrade = """
            CREATE TABLE IF NOT EXISTS swlor_bot_response_deletions (
                channel_id text NOT NULL, message_id text NOT NULL, due_at timestamptz NOT NULL,
                attempts integer NOT NULL DEFAULT 0, last_error text, completed boolean NOT NULL DEFAULT false,
                PRIMARY KEY(channel_id, message_id));
            CREATE INDEX IF NOT EXISTS swlor_bot_response_deletions_due
                ON swlor_bot_response_deletions(due_at) WHERE NOT completed;
            INSERT INTO swlor_bot_schema(version) VALUES (2) ON CONFLICT DO NOTHING;
            ALTER TABLE swlor_bot_response_deletions ADD COLUMN IF NOT EXISTS completed_at timestamptz;
            -- Older tombstones have no completion timestamp. Start their full replay window on upgrade.
            UPDATE swlor_bot_response_deletions SET completed_at=now() WHERE completed AND completed_at IS NULL;
            CREATE INDEX IF NOT EXISTS swlor_bot_response_deletions_completed
                ON swlor_bot_response_deletions(completed_at) WHERE completed;
            CREATE INDEX IF NOT EXISTS swlor_bot_delivery_operations_completed
                ON swlor_bot_delivery_operations(updated_at) WHERE completed;
            CREATE INDEX IF NOT EXISTS swlor_bot_deliveries_at ON swlor_bot_deliveries(at);
            CREATE INDEX IF NOT EXISTS swlor_bot_cooldowns_at ON swlor_bot_cooldowns(at);
            INSERT INTO swlor_bot_schema(version) VALUES (3) ON CONFLICT DO NOTHING;
            """;
        await using var upgradeCommand = new NpgsqlCommand(upgrade, session.Connection, transaction);
        await upgradeCommand.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public Task<ITicketSession> LockAsync(CancellationToken ct) => AcquireLockAsync(MutationLock, ct);
    public Task<ITicketSession> LockCommunityAsync(CancellationToken ct) => AcquireLockAsync(CommunityLock, ct);

    private async Task<ITicketSession> AcquireLockAsync(long key, CancellationToken ct)
    {
        var connection = await _source.OpenConnectionAsync(ct);
        try
        {
            await using var command = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", connection) { CommandTimeout = 60 };
            command.Parameters.AddWithValue("key", key);
            await command.ExecuteNonQueryAsync(ct);
            return new Session(connection, key, _source);
        }
        catch
        {
            // Cancellation can arrive after PostgreSQL acquired the session lock. Pool reset is deferred,
            // so an unconfirmed session must be physically discarded before returning it to the pool.
            // Clear this explicit data source; NpgsqlConnection.ClearPool only clears implicit pools.
            _source.Clear();
            await connection.DisposeAsync();
            throw;
        }
    }

    private static string Snowflake(ulong id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public async Task ScheduleDeletionAsync(ulong channelId, ulong messageId, DateTimeOffset dueAt, CancellationToken ct)
    {
        await using var connection = await _source.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("INSERT INTO swlor_bot_response_deletions(channel_id,message_id,due_at) VALUES (@channel,@message,@due) ON CONFLICT DO NOTHING", connection);
        command.Parameters.AddWithValue("channel", Snowflake(channelId));
        command.Parameters.AddWithValue("message", Snowflake(messageId));
        command.Parameters.AddWithValue("due", dueAt.ToUniversalTime());
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<PendingResponseDeletion>> GetDueDeletionsAsync(DateTimeOffset now, CancellationToken ct)
    {
        await using var connection = await _source.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT channel_id,message_id,due_at,attempts,last_error FROM swlor_bot_response_deletions WHERE NOT completed AND due_at<=@now ORDER BY due_at LIMIT 100", connection);
        command.Parameters.AddWithValue("now", now.ToUniversalTime());
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<PendingResponseDeletion>();
        while (await reader.ReadAsync(ct))
            result.Add(new(ulong.Parse(reader.GetString(0), System.Globalization.CultureInfo.InvariantCulture),
                ulong.Parse(reader.GetString(1), System.Globalization.CultureInfo.InvariantCulture),
                new DateTimeOffset(reader.GetDateTime(2), TimeSpan.Zero), reader.GetInt32(3),
                reader.IsDBNull(4) ? null : reader.GetString(4)));
        return result;
    }

    public async Task CompleteDeletionAsync(ulong channelId, ulong messageId, CancellationToken ct)
    {
        await using var connection = await _source.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("UPDATE swlor_bot_response_deletions SET completed=true,completed_at=COALESCE(completed_at,now()),last_error=NULL WHERE channel_id=@channel AND message_id=@message", connection);
        command.Parameters.AddWithValue("channel", Snowflake(channelId));
        command.Parameters.AddWithValue("message", Snowflake(messageId));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task RetryDeletionAsync(PendingResponseDeletion deletion, DateTimeOffset dueAt, string error, CancellationToken ct)
    {
        await using var connection = await _source.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("UPDATE swlor_bot_response_deletions SET due_at=@due,attempts=LEAST(attempts::bigint+1,2147483647)::integer,last_error=@error WHERE NOT completed AND channel_id=@channel AND message_id=@message", connection);
        command.Parameters.AddWithValue("channel", Snowflake(deletion.ChannelId));
        command.Parameters.AddWithValue("message", Snowflake(deletion.MessageId));
        command.Parameters.AddWithValue("due", dueAt.ToUniversalTime());
        command.Parameters.AddWithValue("error", error);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task PruneCompletedDeliveriesAsync(DateTimeOffset now, CancellationToken ct)
    {
        // Bounded, indexed deletes avoid holding guild advisory locks or monopolizing the database.
        // Unfinished intentions are never age-expired; retention starts only at confirmed completion.
        await using var connection = await _source.OpenConnectionAsync(ct);
        const string prune = """
            DELETE FROM swlor_bot_delivery_operations WHERE completed AND updated_at<@cutoff AND key IN (
                SELECT key FROM swlor_bot_delivery_operations WHERE completed AND updated_at<@cutoff ORDER BY updated_at LIMIT 1000);
            DELETE FROM swlor_bot_response_deletions WHERE completed AND completed_at<@cutoff AND (channel_id,message_id) IN (
                SELECT channel_id,message_id FROM swlor_bot_response_deletions
                WHERE completed AND completed_at<@cutoff ORDER BY completed_at LIMIT 1000);
            DELETE FROM swlor_bot_deliveries WHERE at<@cutoff AND key IN (
                SELECT key FROM swlor_bot_deliveries WHERE at<@cutoff ORDER BY at LIMIT 1000);
            DELETE FROM swlor_bot_cooldowns WHERE at<@cutoff AND key IN (
                SELECT key FROM swlor_bot_cooldowns WHERE at<@cutoff ORDER BY at LIMIT 1000);
            """;
        await using var command = new NpgsqlCommand(prune, connection);
        command.Parameters.AddWithValue("cutoff", (now - CompletedDeliveryRetention).ToUniversalTime());
        int removed;
        do
        {
            ct.ThrowIfCancellationRequested();
            removed = await command.ExecuteNonQueryAsync(ct);
        } while (removed > 0);
    }

    public ValueTask DisposeAsync() => _source.DisposeAsync();

    internal sealed class Session(NpgsqlConnection connection, long lockKey, NpgsqlDataSource source) : ITicketSession
    {
        internal NpgsqlConnection Connection => connection;
        private bool _disposed;

        public async Task<IReadOnlyList<Ticket>> GetTicketsAsync(CancellationToken ct)
        {
            await using var command = new NpgsqlCommand("SELECT payload FROM swlor_bot_tickets WHERE state <> 'Deleted' OR archive_path IS NOT NULL ORDER BY updated_at", connection);
            await using var reader = await command.ExecuteReaderAsync(ct);
            var result = new List<Ticket>();
            while (await reader.ReadAsync(ct)) result.Add(JsonSerializer.Deserialize<Ticket>(reader.GetString(0)) ?? throw new InvalidDataException("Invalid persisted ticket."));
            return result;
        }

        public async Task<Ticket?> FindInteractionAsync(string interactionId, CancellationToken ct)
        {
            await using var command = new NpgsqlCommand("SELECT payload FROM swlor_bot_tickets WHERE interaction_id=@interaction", connection);
            command.Parameters.AddWithValue("interaction", interactionId);
            var data = await command.ExecuteScalarAsync(ct);
            return data is string json ? JsonSerializer.Deserialize<Ticket>(json) : null;
        }

        public async Task<Ticket> ReserveAsync(string panelId, ulong requesterId, string interactionId, DateTimeOffset now, CancellationToken ct)
        {
            await using var numberCommand = new NpgsqlCommand("SELECT nextval('swlor_bot_ticket_numbers')", connection);
            var number = Convert.ToInt64(await numberCommand.ExecuteScalarAsync(ct));
            var ticket = new Ticket(Guid.NewGuid(), panelId, requesterId, null, TicketState.Creating, number, now);
            await using var transaction = await connection.BeginTransactionAsync(ct);
            await using var command = new NpgsqlCommand("INSERT INTO swlor_bot_tickets(id,interaction_id,state,payload) VALUES (@id,@interaction,@state,@payload)", connection, transaction);
            command.Parameters.AddWithValue("id", ticket.Id);
            command.Parameters.AddWithValue("interaction", interactionId);
            command.Parameters.AddWithValue("state", ticket.State.ToString());
            command.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(ticket));
            await command.ExecuteNonQueryAsync(ct);
            await AuditAsync(ticket.Id, "reserved", requesterId, transaction, ct);
            await transaction.CommitAsync(ct);
            return ticket;
        }

        public async Task SaveAsync(Ticket ticket, string action, ulong? actorId, CancellationToken ct)
        {
            await using var transaction = await connection.BeginTransactionAsync(ct);
            await using var command = new NpgsqlCommand("UPDATE swlor_bot_tickets SET state=@state,channel_id=@channel,archive_path=@archive,payload=@payload,updated_at=now() WHERE id=@id", connection, transaction);
            command.Parameters.AddWithValue("id", ticket.Id);
            command.Parameters.AddWithValue("state", ticket.State.ToString());
            command.Parameters.AddWithValue("channel", NpgsqlDbType.Text, (object?)ticket.ChannelId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? DBNull.Value);
            command.Parameters.AddWithValue("archive", NpgsqlDbType.Text, (object?)ticket.ArchivePath ?? DBNull.Value);
            command.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(ticket));
            if (await command.ExecuteNonQueryAsync(ct) != 1) throw new InvalidOperationException("Ticket record disappeared.");
            await AuditAsync(ticket.Id, action, actorId, transaction, ct);
            await transaction.CommitAsync(ct);
        }

        private async Task AuditAsync(Guid id, string action, ulong? actorId, NpgsqlTransaction transaction, CancellationToken ct)
        {
            await using var command = new NpgsqlCommand("INSERT INTO swlor_bot_ticket_audit(ticket_id,action,actor_id) VALUES (@ticket,@action,@actor)", connection, transaction);
            command.Parameters.AddWithValue("ticket", id);
            command.Parameters.AddWithValue("action", action);
            command.Parameters.AddWithValue("actor", NpgsqlDbType.Text, (object?)actorId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(ct);
        }

        public async Task<bool> TryRecordDeliveryAsync(string key, CancellationToken ct)
        {
            await using var command = new NpgsqlCommand("INSERT INTO swlor_bot_deliveries(key) VALUES (@key) ON CONFLICT DO NOTHING", connection);
            command.Parameters.AddWithValue("key", key);
            return await command.ExecuteNonQueryAsync(ct) == 1;
        }

        public async Task<DeliveryState> GetOrCreateDeliveryAsync(string key, string intent, CancellationToken ct)
        {
            await using var insert = new NpgsqlCommand("INSERT INTO swlor_bot_delivery_operations(key,intent) VALUES (@key,@intent) ON CONFLICT DO NOTHING", connection);
            insert.Parameters.AddWithValue("key", key);
            insert.Parameters.AddWithValue("intent", intent);
            await insert.ExecuteNonQueryAsync(ct);
            await using var select = new NpgsqlCommand("SELECT intent,completed FROM swlor_bot_delivery_operations WHERE key=@key", connection);
            select.Parameters.AddWithValue("key", key);
            await using var reader = await select.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) throw new InvalidOperationException("Delivery record disappeared.");
            return new(reader.GetString(0), reader.GetBoolean(1));
        }

        public async Task CompleteDeliveryAsync(string key, CancellationToken ct)
        {
            await using var command = new NpgsqlCommand("UPDATE swlor_bot_delivery_operations SET completed=true,updated_at=CASE WHEN completed THEN updated_at ELSE now() END WHERE key=@key", connection);
            command.Parameters.AddWithValue("key", key);
            if (await command.ExecuteNonQueryAsync(ct) != 1) throw new InvalidOperationException("Delivery record disappeared.");
        }

        public async Task<DateTimeOffset?> GetCooldownAsync(string key, CancellationToken ct)
        {
            await using var command = new NpgsqlCommand("SELECT at FROM swlor_bot_cooldowns WHERE key=@key", connection);
            command.Parameters.AddWithValue("key", key);
            var result = await command.ExecuteScalarAsync(ct);
            return result is DateTime date ? new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc)) : null;
        }

        public async Task SetCooldownAsync(string key, DateTimeOffset at, CancellationToken ct)
        {
            await using var command = new NpgsqlCommand("INSERT INTO swlor_bot_cooldowns(key,at) VALUES (@key,@at) ON CONFLICT(key) DO UPDATE SET at=excluded.at", connection);
            command.Parameters.AddWithValue("key", key);
            command.Parameters.AddWithValue("at", at.ToUniversalTime());
            await command.ExecuteNonQueryAsync(ct);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            var released = false;
            try
            {
                await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", connection) { CommandTimeout = 5 };
                command.Parameters.AddWithValue("key", lockKey);
                released = await command.ExecuteScalarAsync(CancellationToken.None) is true;
                if (!released) throw new InvalidOperationException("The database session did not confirm advisory lock release.");
            }
            finally
            {
                if (!released) source.Clear();
                await connection.DisposeAsync();
            }
        }
    }
}
