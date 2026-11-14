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

    /// <summary>
    /// The hot reload switch: <c>true</c> lets a developer replace the running game build (<c>SWLOR.CLI --reload</c>).
    /// Off unless set, and refused on a production server, on a server whose environment is not named explicitly and for
    /// a non-collectible build (<see cref="HotReloadRefusal"/>).
    /// </summary>
    public const string HotReloadVariable = "SWLOR_HOT_RELOAD";

    /// <summary>Where the reload signal and its receipts are; the default is <see cref="DefaultReloadDirectoryRelativePath"/>.</summary>
    public const string ReloadDirectoryVariable = "SWLOR_HOT_RELOAD_DIRECTORY";

    /// <summary>Where the game build sits relative to the host's own directory: <c>dotnet/game/</c> beside <c>dotnet/</c>.</summary>
    public const string DefaultGameAssemblyRelativePath = "game/SWLOR.Game.Server.dll";

    /// <summary>
    /// Where the reload signal and receipts sit relative to the host's own directory: <c>reload/</c> in the server home,
    /// beside <c>dotnet/</c>, which the developer writes the signal to and the server writes its receipts to.
    /// </summary>
    public const string DefaultReloadDirectoryRelativePath = "../reload";

    /// <summary>The value of <see cref="EnvironmentName"/> on a production server.</summary>
    public const string ProductionEnvironment = "Production";

    /// <summary>The value of <see cref="EnvironmentName"/> on a test server.</summary>
    public const string TestEnvironment = "Test";

    /// <summary>The value of <see cref="EnvironmentName"/> on a development server, and for a missing or unrecognized value.</summary>
    public const string DevelopmentEnvironment = "Development";

    private HostSettings(
        string gameAssemblyPath,
        bool collectible,
        string environmentName,
        bool environmentIsExplicit,
        string? redisHost,
        bool hotReloadRequested,
        string? hotReloadRefusal,
        string generationRoot,
        string reloadDirectory)
    {
        GameAssemblyPath = gameAssemblyPath;
        Collectible = collectible;
        EnvironmentName = environmentName;
        EnvironmentIsExplicit = environmentIsExplicit;
        RedisHost = redisHost;
        HotReloadRequested = hotReloadRequested;
        HotReloadRefusal = hotReloadRefusal;
        GenerationRoot = generationRoot;
        ReloadDirectory = reloadDirectory;
    }

    /// <summary>The full path of the game assembly the host loads as generation 1.</summary>
    public string GameAssemblyPath { get; }

    /// <summary>Whether the game loads into a collectible context.</summary>
    public bool Collectible { get; }

    /// <summary>True when <c>SWLOR_ENVIRONMENT</c> names production.</summary>
    public bool IsProduction => EnvironmentName == ProductionEnvironment;

    /// <summary>
    /// The server environment as the game reads it: <see cref="ProductionEnvironment"/>, <see cref="TestEnvironment"/> or
    /// <see cref="DevelopmentEnvironment"/> (also for a missing or unrecognized value).
    /// </summary>
    public string EnvironmentName { get; }

    /// <summary>True only when <c>SWLOR_ENVIRONMENT</c> held a recognized spelling, as the game's <c>ApplicationSettings</c> decides.</summary>
    public bool EnvironmentIsExplicit { get; }

    /// <summary>The Redis endpoint, or null when it is not set.</summary>
    public string? RedisHost { get; }

    /// <summary>True when <see cref="HotReloadVariable"/> asks for hot reload, whether or not the host allows it.</summary>
    public bool HotReloadRequested { get; }

    /// <summary>
    /// True when the host accepts reload signals: <see cref="HotReloadVariable"/> asks for them and nothing refuses it.
    /// Never on a production server.
    /// </summary>
    public bool HotReload => HotReloadRequested && HotReloadRefusal is null;

    /// <summary>Why the host keeps hot reload off although <see cref="HotReloadVariable"/> asks for it, or null.</summary>
    public string? HotReloadRefusal { get; }

    /// <summary>
    /// The directory reload candidates are published under: the host's own directory, so a candidate is
    /// <c>dotnet/generations/&lt;id&gt;/</c> beside the game build in <c>dotnet/game/</c>.
    /// </summary>
    public string GenerationRoot { get; }

    /// <summary>The full path of the directory that holds the reload signal and the receipts.</summary>
    public string ReloadDirectory { get; }

    /// <summary>Reads the settings from the process environment; the host's own directory anchors the default paths.</summary>
    public static HostSettings FromEnvironment() =>
        Read(Environment.GetEnvironmentVariable, Path.GetDirectoryName(typeof(HostSettings).Assembly.Location) ?? AppContext.BaseDirectory);

    /// <summary>Reads the settings through <paramref name="variable"/>; relative paths resolve against <paramref name="hostDirectory"/>.</summary>
    public static HostSettings Read(Func<string, string?> variable, string hostDirectory)
    {
        ArgumentNullException.ThrowIfNull(variable);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostDirectory);

        var (environmentName, environmentIsExplicit) = ParseEnvironment(variable(EnvironmentVariable));

        var configuredPath = variable(GameAssemblyVariable)?.Trim();
        var gameAssemblyPath = Path.GetFullPath(string.IsNullOrEmpty(configuredPath) ? DefaultGameAssemblyRelativePath : configuredPath, hostDirectory);

        var collectible = ParseBool(variable(CollectibleVariable), environmentName != ProductionEnvironment);
        var redisHost = variable(RedisHostVariable);

        var hotReloadRequested = ParseBool(variable(HotReloadVariable), false);
        var refusal = hotReloadRequested ? HotReloadRefusalFor(environmentName, environmentIsExplicit, collectible) : null;
        var configuredReloadDirectory = variable(ReloadDirectoryVariable)?.Trim();
        var reloadDirectory = Path.GetFullPath(
            string.IsNullOrEmpty(configuredReloadDirectory) ? DefaultReloadDirectoryRelativePath : configuredReloadDirectory, hostDirectory);

        return new HostSettings(
            gameAssemblyPath,
            collectible,
            environmentName,
            environmentIsExplicit,
            string.IsNullOrWhiteSpace(redisHost) ? null : redisHost,
            hotReloadRequested,
            refusal,
            Path.GetFullPath(hostDirectory),
            reloadDirectory);
    }

    /// <summary>
    /// Production never reloads. A server whose environment is missing or misspelled may be production (the engine-test
    /// runner fails closed the same way), and a non-collectible build can never be replaced.
    /// </summary>
    private static string? HotReloadRefusalFor(string environmentName, bool environmentIsExplicit, bool collectible)
    {
        if (environmentName == ProductionEnvironment)
            return $"{HotReloadVariable} is set but {EnvironmentVariable} is production: production never reloads.";
        if (!environmentIsExplicit)
            return $"{HotReloadVariable} is set but {EnvironmentVariable} is missing or unrecognized: reload needs an explicit dev or test environment.";
        if (!collectible)
            return $"{HotReloadVariable} is set but {CollectibleVariable} is false: a non-collectible game build can never be replaced.";
        return null;
    }

    // The same spellings as the game's ApplicationSettings.
    private static (string Name, bool Explicit) ParseEnvironment(string? value)
    {
        var environment = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return environment switch
        {
            "prod" or "production" => (ProductionEnvironment, true),
            "test" or "testing" => (TestEnvironment, true),
            "dev" or "development" => (DevelopmentEnvironment, true),
            _ => (DevelopmentEnvironment, false)
        };
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
