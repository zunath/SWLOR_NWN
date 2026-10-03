using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using SWLOR.DiscordBot.Core;

namespace SWLOR.DiscordBot.Persistence;

public sealed class PostgresTicketStore(string connectionString) : ITicketStore, IAsyncDisposable
{
    private const long MutationLock = 7821653091;
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
        if (Convert.ToInt32(await versionCommand.ExecuteScalarAsync(ct)) != 1)
            throw new InvalidOperationException("The bot database schema is newer than this application.");
    }

    public async Task<ITicketSession> LockAsync(CancellationToken ct)
    {
        var connection = await _source.OpenConnectionAsync(ct);
        try
        {
            await using var command = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", connection) { CommandTimeout = 60 };
            command.Parameters.AddWithValue("key", MutationLock);
            await command.ExecuteNonQueryAsync(ct);
            return new Session(connection);
        }
        catch { await connection.DisposeAsync(); throw; }
    }

    public ValueTask DisposeAsync() => _source.DisposeAsync();

    private sealed class Session(NpgsqlConnection connection) : ITicketSession
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
            await using var command = new NpgsqlCommand("UPDATE swlor_bot_delivery_operations SET completed=true,updated_at=now() WHERE key=@key", connection);
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
            try
            {
                await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", connection) { CommandTimeout = 5 };
                command.Parameters.AddWithValue("key", MutationLock);
                await command.ExecuteNonQueryAsync(CancellationToken.None);
            }
            finally { await connection.DisposeAsync(); }
        }
    }
}
