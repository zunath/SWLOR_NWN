using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using SWLOR.DiscordBot.Core;

namespace SWLOR.DiscordBot.Persistence;

public sealed class PostgresTicketStore(string connectionString, ulong guildId) : ITicketStore, IResponseDeletionStore, IAsyncDisposable
{
    // Gateway retries/resume and REST nonce deduplication are much shorter than this replay window.
    // Thirty days also covers the longest permitted quick-answer cooldown.
    internal static readonly TimeSpan CompletedDeliveryRetention = TimeSpan.FromDays(30);
    private const long MutationLock = 7821653091;
    private const long CommunityLock = 7821653092;
    private readonly ulong _guildId = guildId != 0 ? guildId : throw new ArgumentOutOfRangeException(nameof(guildId));
    private volatile bool _initialized;
    private readonly NpgsqlDataSource _source = NpgsqlDataSource.Create(connectionString);

    public async Task InitializeAsync(CancellationToken ct)
    {
        _initialized = false;
        await using var session = (Session)await AcquireLockAsync(MutationLock, ct);
        await using var transaction = await session.Connection.BeginTransactionAsync(ct);
        const string ownership = """
            CREATE TABLE IF NOT EXISTS swlor_bot_database_owner (
                singleton boolean PRIMARY KEY DEFAULT true CHECK(singleton),
                guild_id text NOT NULL);
            INSERT INTO swlor_bot_database_owner(singleton,guild_id)
                VALUES (true,@guild) ON CONFLICT(singleton) DO NOTHING;
            SELECT guild_id FROM swlor_bot_database_owner WHERE singleton;
            """;
        await using var ownerCommand = new NpgsqlCommand(ownership, session.Connection, transaction);
        ownerCommand.Parameters.AddWithValue("guild", Snowflake(_guildId));
        var owner = await ownerCommand.ExecuteScalarAsync(ct);
        if (owner is not string ownedGuild || ownedGuild != Snowflake(_guildId))
            throw new InvalidOperationException("This bot database belongs to a different Discord guild. Use a separate database for this guild.");
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
        await using var command = new NpgsqlCommand(schema, session.Connection, transaction);
        await command.ExecuteNonQueryAsync(ct);
        await using var versionCommand = new NpgsqlCommand("SELECT max(version) FROM swlor_bot_schema", session.Connection, transaction);
        if (Convert.ToInt32(await versionCommand.ExecuteScalarAsync(ct)) > 5)
            throw new InvalidOperationException("The bot database schema is newer than this application.");
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
            CREATE INDEX IF NOT EXISTS swlor_bot_delivery_operations_pending
                ON swlor_bot_delivery_operations(updated_at, key) WHERE NOT completed;
            CREATE INDEX IF NOT EXISTS swlor_bot_deliveries_at ON swlor_bot_deliveries(at);
            CREATE INDEX IF NOT EXISTS swlor_bot_cooldowns_at ON swlor_bot_cooldowns(at);
            INSERT INTO swlor_bot_schema(version) VALUES (3) ON CONFLICT DO NOTHING;
            CREATE TABLE IF NOT EXISTS swlor_bot_community_actions (
                scope text PRIMARY KEY, message_id numeric(20,0) NOT NULL);
            DO $$ BEGIN
            IF NOT EXISTS (SELECT 1 FROM swlor_bot_schema WHERE version = 4) THEN
            -- Recover the newest durable action per user/faction or answer cooldown scope.
            -- Materialize guarded JSON parsing before touching any nested fields: old deployments may
            -- contain legacy free-form intent text in the same operations table.
            WITH parsed AS MATERIALIZED (
                SELECT CASE WHEN pg_input_is_valid(intent, 'jsonb') THEN intent::jsonb ELSE NULL END AS doc
                FROM swlor_bot_delivery_operations
            ), raw AS MATERIALIZED (
                SELECT doc, doc->>'Kind' AS kind, doc->>'UserId' AS user_id,
                    doc->>'SourceMessageId' AS source_message_id,
                    doc->>'CooldownKey' AS cooldown_key, doc->>'Cooldown' AS cooldown
                FROM parsed WHERE jsonb_typeof(doc) = 'object' AND doc->>'Version' = '1'
            ), validated AS MATERIALIZED (
                SELECT *,
                    CASE WHEN user_id ~ '^[0-9]{1,20}$' THEN user_id::numeric END AS user_num,
                    CASE WHEN source_message_id ~ '^[0-9]{1,20}$' THEN source_message_id::numeric END AS message_num,
                    CASE WHEN cooldown ~ '^([0-9]+\.)?[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]+)?$'
                        THEN regexp_replace(cooldown, '[0.:]', '', 'g') <> '' ELSE false END AS positive_cooldown
                FROM raw
            ), candidates AS (
                SELECT 'faction:' || user_id AS scope, source_message_id AS message_id
                FROM validated
                WHERE kind = 'faction'
                  AND user_num BETWEEN 1 AND 18446744073709551615
                  AND message_num BETWEEN 1 AND 18446744073709551615
                UNION ALL
                SELECT cooldown_key AS scope, source_message_id AS message_id
                FROM validated
                WHERE kind = 'answer' AND cooldown_key IS NOT NULL AND cooldown_key <> ''
                  AND positive_cooldown
                  AND user_num BETWEEN 1 AND 18446744073709551615
                  AND message_num BETWEEN 1 AND 18446744073709551615
            )
            INSERT INTO swlor_bot_community_actions(scope, message_id)
            SELECT scope, max(message_id::numeric) FROM candidates GROUP BY scope
            ON CONFLICT(scope) DO UPDATE SET message_id = GREATEST(swlor_bot_community_actions.message_id, EXCLUDED.message_id);
            INSERT INTO swlor_bot_schema(version) VALUES (4) ON CONFLICT DO NOTHING;
            END IF;
            END $$;
            INSERT INTO swlor_bot_schema(version) VALUES (5) ON CONFLICT DO NOTHING;
            """;
        await using var upgradeCommand = new NpgsqlCommand(upgrade, session.Connection, transaction);
        await upgradeCommand.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
        _initialized = true;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("Bot database guild ownership has not been validated.");
    }

    public Task<ITicketSession> LockAsync(CancellationToken ct)
    {
        EnsureInitialized();
        return AcquireLockAsync(MutationLock, ct);
    }

    public Task<ITicketSession> LockCommunityAsync(CancellationToken ct)
    {
        EnsureInitialized();
        return AcquireLockAsync(CommunityLock, ct);
    }

    public async Task PersistCommunityDeliveryAsync(string key, string intent, CancellationToken ct)
    {
        EnsureInitialized();
        // Accepted community events commit independently of long-running community delivery locks.
        await using var connection = await _source.OpenConnectionAsync(ct);
        await using var insert = new NpgsqlCommand(
            "INSERT INTO swlor_bot_delivery_operations(key,intent) VALUES (@key,@intent) ON CONFLICT DO NOTHING", connection)
            { CommandTimeout = 10 };
        insert.Parameters.AddWithValue("key", key);
        insert.Parameters.AddWithValue("intent", intent);
        await insert.ExecuteNonQueryAsync(ct);
    }
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
        EnsureInitialized();
        await using var connection = await _source.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("INSERT INTO swlor_bot_response_deletions(channel_id,message_id,due_at) VALUES (@channel,@message,@due) ON CONFLICT DO NOTHING", connection);
        command.Parameters.AddWithValue("channel", Snowflake(channelId));
        command.Parameters.AddWithValue("message", Snowflake(messageId));
        command.Parameters.AddWithValue("due", dueAt.ToUniversalTime());
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<PendingResponseDeletion>> GetDueDeletionsAsync(DateTimeOffset now, CancellationToken ct)
    {
        EnsureInitialized();
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
        EnsureInitialized();
        await using var connection = await _source.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("UPDATE swlor_bot_response_deletions SET completed=true,completed_at=COALESCE(completed_at,now()),last_error=NULL WHERE channel_id=@channel AND message_id=@message", connection);
        command.Parameters.AddWithValue("channel", Snowflake(channelId));
        command.Parameters.AddWithValue("message", Snowflake(messageId));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task RetryDeletionAsync(PendingResponseDeletion deletion, DateTimeOffset dueAt, string error, CancellationToken ct)
    {
        EnsureInitialized();
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
        EnsureInitialized();
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

        public async Task<IReadOnlyList<PendingDelivery>> GetPendingDeliveriesAsync(CancellationToken ct)
        {
            const string select = """
                WITH pending AS (
                    SELECT key FROM swlor_bot_delivery_operations
                    WHERE NOT completed ORDER BY updated_at, key LIMIT 20 FOR UPDATE SKIP LOCKED
                )
                UPDATE swlor_bot_delivery_operations AS delivery
                SET updated_at=now()
                FROM pending
                WHERE delivery.key=pending.key
                RETURNING delivery.key, delivery.intent
                """;
            await using var command = new NpgsqlCommand(select, connection);
            await using var reader = await command.ExecuteReaderAsync(ct);
            var result = new List<PendingDelivery>();
            while (await reader.ReadAsync(ct)) result.Add(new(reader.GetString(0), reader.GetString(1)));
            return result;
        }

        public async Task<Ticket?> GetTicketAsync(Guid id, CancellationToken ct)
        {
            await using var command = new NpgsqlCommand("SELECT payload FROM swlor_bot_tickets WHERE id=@id", connection);
            command.Parameters.AddWithValue("id", id);
            var data = await command.ExecuteScalarAsync(ct);
            return data is string json ? JsonSerializer.Deserialize<Ticket>(json) : null;
        }

        public async Task<Ticket?> FindByChannelAsync(ulong channelId, CancellationToken ct)
        {
            await using var command = new NpgsqlCommand("SELECT payload FROM swlor_bot_tickets WHERE channel_id=@channel", connection);
            command.Parameters.AddWithValue("channel", channelId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var data = await command.ExecuteScalarAsync(ct);
            return data is string json ? JsonSerializer.Deserialize<Ticket>(json) : null;
        }

        public async Task UpdateCommunityDeliveryIntentAsync(string key, string intent, CancellationToken ct)
        {
            await using var command = new NpgsqlCommand("UPDATE swlor_bot_delivery_operations SET intent=@intent,updated_at=now() WHERE key=@key AND NOT completed", connection);
            command.Parameters.AddWithValue("key", key);
            command.Parameters.AddWithValue("intent", intent);
            if (await command.ExecuteNonQueryAsync(ct) != 1) throw new InvalidOperationException("Pending community delivery record disappeared.");
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

        public async Task<bool> TryAdvanceCommunityActionAsync(string scope, ulong messageId, CancellationToken ct)
        {
            const string upsert = """
                INSERT INTO swlor_bot_community_actions(scope, message_id) VALUES (@scope, @message)
                ON CONFLICT(scope) DO UPDATE
                SET message_id = GREATEST(swlor_bot_community_actions.message_id, EXCLUDED.message_id)
                RETURNING message_id = @message
                """;
            await using var command = new NpgsqlCommand(upsert, connection);
            command.Parameters.AddWithValue("scope", scope);
            command.Parameters.AddWithValue("message", NpgsqlDbType.Numeric, (decimal)messageId);
            return await command.ExecuteScalarAsync(ct) is true;
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
