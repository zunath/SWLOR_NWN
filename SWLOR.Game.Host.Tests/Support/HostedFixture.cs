using System.Collections.Concurrent;
using Nwn.Hosting.Bootstrap;
using Nwn.Hosting.Testing.Fixtures;
using SWLOR.Game.Host.Bootstrap;
using SWLOR.Game.NativeHooks;

namespace SWLOR.Game.Host.Tests.Support;

/// <summary>
/// A <see cref="SwlorHostMode"/> on the test's own thread (standing in for the engine thread), with a closure handler that
/// records instead of calling NWNX, ready to load a fixture build through the package's <c>ReloadHost</c>.
/// </summary>
internal sealed class HostedFixture
{
    /// <param name="gameAssemblyPath">The build the host loads as generation 1.</param>
    /// <param name="collectible">Whether the build loads into a collectible context.</param>
    /// <param name="variables">More settings the host reads (the environment, the reload switch).</param>
    /// <param name="hostDirectory">The host's own folder (relative settings resolve against it); the build's folder when null.</param>
    /// <param name="reloadPollInterval">How often the reload trigger looks at the signal file.</param>
    public HostedFixture(
        string gameAssemblyPath,
        bool collectible = true,
        IReadOnlyDictionary<string, string?>? variables = null,
        string? hostDirectory = null,
        TimeSpan? reloadPollInterval = null)
    {
        Scheduler = new RecordingClosureScheduler();
        CoreFunctions = new CoreFunctionHandler(Environment.CurrentManagedThreadId, null, Scheduler, RejectedClosureHandling.Drop);
        var settings = new Dictionary<string, string?>
        {
            [HostSettings.GameAssemblyVariable] = gameAssemblyPath,
            [HostSettings.CollectibleVariable] = collectible ? "true" : "false",
            [HostSettings.RedisHostVariable] = "redis:6379"
        };
        foreach (var (name, value) in variables ?? new Dictionary<string, string?>())
            settings[name] = value;
        Settings = HostSettings.Read(name => settings.GetValueOrDefault(name), hostDirectory ?? Path.GetDirectoryName(gameAssemblyPath)!);
        Redis = new FakeRedisConnectionSource();
        Hooks = new NativeHookTable();
        Mode = new SwlorHostMode(CoreFunctions, Settings, Redis, Hooks, (category, message) => Log.Enqueue($"{category} {message}"),
            reloadPollInterval ?? TimeSpan.FromSeconds(1));
        Mode.Services.Add(Record);
    }

    public RecordingClosureScheduler Scheduler { get; }
    public CoreFunctionHandler CoreFunctions { get; }
    public HostSettings Settings { get; }
    public FakeRedisConnectionSource Redis { get; }

    /// <summary>The native hooks the host hands the game; a fixture build never enables them.</summary>
    public NativeHookTable Hooks { get; }
    public SwlorHostMode Mode { get; }

    /// <summary>What the fixture build recorded.</summary>
    public ConcurrentQueue<string> Record { get; } = new();

    /// <summary>What the host wrote to its console log.</summary>
    public ConcurrentQueue<string> Log { get; } = new();

    /// <summary>Starts the mode and loads the fixture build as generation 1.</summary>
    public void StartAndLoad()
    {
        Mode.Start();
        Mode.LoadGame();
    }

    public void ClearRecord()
    {
        while (Record.TryDequeue(out _))
        {
        }
    }
}
