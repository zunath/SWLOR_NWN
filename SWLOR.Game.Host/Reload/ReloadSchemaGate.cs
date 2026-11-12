using System.Text.Json;
using Nwn.Hosting.Reload;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.Host.Reload;

/// <summary>
/// The reload's schema gate (MaintainabilityRefactorHotReload.md, "A generation"): server migrations run only at boot, so a
/// candidate whose newest server migration (<c>IGameplayGeneration.RequiredSchemaVersion</c>) is newer than the database's
/// (<c>MigrationVersion</c> of the <c>SWLOR_CONFIG</c> server configuration in Redis) is rejected with a detail saying a
/// restart is required; one at or below it passes. The package calls it right after the candidate's validation, off the
/// engine thread, on a reload only (never for generation 1).
/// </summary>
public sealed class ReloadSchemaGate
{
    /// <summary>The Redis key of the server configuration: the game stores entities as <c>{Type.Name}:{Id}</c>.</summary>
    public const string ConfigurationKey = "ServerConfiguration:SWLOR_CONFIG";

    /// <summary>The field of the server configuration that holds the newest server migration the database ran.</summary>
    public const string MigrationVersionField = "MigrationVersion";

    private readonly Func<CancellationToken, ValueTask<int?>> _readDatabaseVersion;

    /// <summary>A gate that reads the database's version through the host's Redis connection.</summary>
    public ReloadSchemaGate(IRedisConnectionSource redisConnection)
    {
        ArgumentNullException.ThrowIfNull(redisConnection);
        _readDatabaseVersion = cancellationToken => ReadMigrationVersionAsync(redisConnection, cancellationToken);
    }

    /// <summary>For tests: <paramref name="readDatabaseVersion"/> answers the database's version, null when it has none.</summary>
    public ReloadSchemaGate(Func<CancellationToken, ValueTask<int?>> readDatabaseVersion)
    {
        _readDatabaseVersion = readDatabaseVersion ?? throw new ArgumentNullException(nameof(readDatabaseVersion));
    }

    /// <summary>
    /// Accepts a candidate whose required version is at or below the database's and rejects a newer one, or any candidate when
    /// the database's version cannot be read; a rejection leaves the running build untouched and says a restart is required.
    /// </summary>
    public async ValueTask<ReloadSchemaGateResult> CheckAsync(int requiredSchemaVersion, CancellationToken cancellationToken)
    {
        int? database;
        try
        {
            database = await _readDatabaseVersion(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ReloadSchemaGateResult.Incompatible(
                $"The database's server migration version could not be read ({exception.GetType().Name}: {exception.Message}), so the " +
                $"candidate (newest server migration {requiredSchemaVersion}) cannot be shown to need none. Restart the server to load it.");
        }

        if (database is null)
        {
            return ReloadSchemaGateResult.Incompatible(
                $"Redis holds no {MigrationVersionField} under {ConfigurationKey}, so the candidate (newest server migration " +
                $"{requiredSchemaVersion}) cannot be shown to need none. Restart the server to load it.");
        }

        if (requiredSchemaVersion > database.Value)
        {
            return ReloadSchemaGateResult.Incompatible(FormattableString.Invariant(
                $"The candidate needs server migration {requiredSchemaVersion}, but the database is at {database.Value}. Server migrations run only at boot: a restart is required to load this build."));
        }

        return ReloadSchemaGateResult.Ok(FormattableString.Invariant(
            $"The candidate's newest server migration ({requiredSchemaVersion}) is at or below the database's ({database.Value})."));
    }

    /// <summary>The database's <see cref="MigrationVersionField"/>, or null when the server configuration is missing.</summary>
    public static async ValueTask<int?> ReadMigrationVersionAsync(IRedisConnectionSource redisConnection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(redisConnection);
        cancellationToken.ThrowIfCancellationRequested();
        var stored = await redisConnection.Open().GetDatabase().ExecuteAsync("JSON.GET", ConfigurationKey).ConfigureAwait(false);
        return stored.IsNull ? null : ParseMigrationVersion(stored.ToString());
    }

    /// <summary>
    /// The <see cref="MigrationVersionField"/> of a stored server configuration: the document itself, or the one-element array a
    /// JSONPath read returns. Null when the text holds no such number.
    /// </summary>
    public static int? ParseMigrationVersion(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                root = root[0];

            return root.ValueKind == JsonValueKind.Object &&
                   root.TryGetProperty(MigrationVersionField, out var version) &&
                   version.ValueKind == JsonValueKind.Number &&
                   version.TryGetInt32(out var value)
                ? value
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
