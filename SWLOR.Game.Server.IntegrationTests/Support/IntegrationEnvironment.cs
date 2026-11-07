using StackExchange.Redis;

namespace SWLOR.Game.Server.IntegrationTests.Support;

/// <summary>
/// The Redis connection and the loaded database the whole tier shares. Built once by <see cref="IntegrationAssemblySetup"/>;
/// every test starts from <see cref="Wipe"/>.
/// </summary>
public static class IntegrationEnvironment
{
    private const string IndexDefinitionPrefix = "IndexDefinition:";

    public static ConnectionMultiplexer Redis { get; private set; } = null!;

    public static IDatabase Database => Redis.GetDatabase();

    /// <summary>The real database service, loaded: every entity type registered and its search index built.</summary>
    public static IntegrationDBService Db { get; private set; } = null!;

    internal static void Start()
    {
        var endpoint = RedisEnvironment.Endpoint;
        if (!RedisEnvironment.IsLoopback(endpoint))
            throw new InvalidOperationException(
                $"{RedisEnvironment.HostVariable} is '{endpoint}'. The integration tier wipes its Redis between tests, so it only runs against a local one.");

        Redis = RedisEnvironment.Connect(endpoint);
        if (!Database.KeyExists(RedisEnvironment.ThrowawayMarkerKey))
            throw new InvalidOperationException(
                $"The Redis at '{endpoint}' is not marked as a throwaway. Run scripts/ci/run-integration-tests.sh, or mark a container " +
                $"you started for this purpose yourself: SET {RedisEnvironment.ThrowawayMarkerKey} 1.");

        // The container is the tier's own: start from nothing, so a leftover index or record of an earlier run cannot hide a defect.
        Redis.GetServer(Redis.GetEndPoints()[0]).FlushAllDatabases();
        Database.StringSet(RedisEnvironment.ThrowawayMarkerKey, "1");

        Db = new IntegrationDBService();
        Db.Load();
    }

    internal static void Stop()
    {
        Redis?.Dispose();
    }

    /// <summary>
    /// Removes every record and search entry and ends the frame, so a test sees only what it wrote. The search indexes and
    /// their fingerprints stay: building 33 of them would cost more than the tests that use them.
    /// </summary>
    public static void Wipe()
    {
        var server = Redis.GetServer(Redis.GetEndPoints()[0]);
        var doomed = new List<RedisKey>();
        foreach (var key in server.Keys(pattern: "*", pageSize: 1000))
        {
            var text = (string?)key ?? string.Empty;
            if (text == RedisEnvironment.ThrowawayMarkerKey || text.StartsWith(IndexDefinitionPrefix, StringComparison.Ordinal))
                continue;
            doomed.Add(key);
        }

        foreach (var batch in doomed.Chunk(500))
            Database.KeyDelete(batch);

        Db.EndFrame();
    }
}
