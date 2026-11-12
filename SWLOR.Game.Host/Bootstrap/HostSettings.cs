namespace SWLOR.Game.Host.Bootstrap;

/// <summary>
/// The host's options, read once from the process environment (<c>swlor.env</c> through Docker Compose).
/// </summary>
public sealed class HostSettings
{
    /// <summary>The setting that names the game assembly the host loads; the default is <see cref="DefaultGameAssemblyRelativePath"/>.</summary>
    public const string GameAssemblyVariable = "SWLOR_GAME_ASSEMBLY";

    /// <summary>
    /// The setting that decides whether the game loads into a collectible context (<c>true</c>) or a non-collectible one
    /// (<c>false</c>). Blank: collectible everywhere except production.
    /// </summary>
    public const string CollectibleVariable = "SWLOR_GAME_COLLECTIBLE";

    /// <summary>The server environment, read the way the game's <c>ApplicationSettings</c> reads it.</summary>
    public const string EnvironmentVariable = "SWLOR_ENVIRONMENT";

    /// <summary>The Redis endpoint the host connects to, the one the game's own connection uses.</summary>
    public const string RedisHostVariable = "NWNX_REDIS_HOST";

    /// <summary>Where the game build sits relative to the host's own directory: <c>dotnet/game/</c> beside <c>dotnet/</c>.</summary>
    public const string DefaultGameAssemblyRelativePath = "game/SWLOR.Game.Server.dll";

    private HostSettings(string gameAssemblyPath, bool collectible, bool isProduction, string? redisHost)
    {
        GameAssemblyPath = gameAssemblyPath;
        Collectible = collectible;
        IsProduction = isProduction;
        RedisHost = redisHost;
    }

    /// <summary>The full path of the game assembly the host loads as generation 1.</summary>
    public string GameAssemblyPath { get; }

    /// <summary>Whether the game loads into a collectible context.</summary>
    public bool Collectible { get; }

    /// <summary>True when <c>SWLOR_ENVIRONMENT</c> names production.</summary>
    public bool IsProduction { get; }

    /// <summary>The Redis endpoint, or null when it is not set.</summary>
    public string? RedisHost { get; }

    /// <summary>Reads the settings from the process environment; the host's own directory anchors the default game path.</summary>
    public static HostSettings FromEnvironment() =>
        Read(Environment.GetEnvironmentVariable, Path.GetDirectoryName(typeof(HostSettings).Assembly.Location) ?? AppContext.BaseDirectory);

    /// <summary>Reads the settings through <paramref name="variable"/>; relative game paths resolve against <paramref name="hostDirectory"/>.</summary>
    public static HostSettings Read(Func<string, string?> variable, string hostDirectory)
    {
        ArgumentNullException.ThrowIfNull(variable);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostDirectory);

        var environment = variable(EnvironmentVariable)?.Trim().ToLowerInvariant() ?? string.Empty;
        var isProduction = environment is "prod" or "production";

        var configuredPath = variable(GameAssemblyVariable)?.Trim();
        var gameAssemblyPath = Path.GetFullPath(string.IsNullOrEmpty(configuredPath) ? DefaultGameAssemblyRelativePath : configuredPath, hostDirectory);

        var collectible = ParseBool(variable(CollectibleVariable), !isProduction);
        var redisHost = variable(RedisHostVariable);

        return new HostSettings(gameAssemblyPath, collectible, isProduction, string.IsNullOrWhiteSpace(redisHost) ? null : redisHost);
    }

    // The same spellings as the game's ApplicationSettings; an unrecognized value keeps the default.
    private static bool ParseBool(string? value, bool defaultValue)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "true" or "1" or "yes" => true,
            "false" or "0" or "no" => false,
            _ => defaultValue
        };
    }
}
