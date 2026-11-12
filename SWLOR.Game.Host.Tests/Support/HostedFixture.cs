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
    public HostedFixture(string gameAssemblyPath, bool collectible = true)
    {
        Scheduler = new RecordingClosureScheduler();
        CoreFunctions = new CoreFunctionHandler(Environment.CurrentManagedThreadId, null, Scheduler, RejectedClosureHandling.Drop);
        var variables = new Dictionary<string, string?>
        {
            [HostSettings.GameAssemblyVariable] = gameAssemblyPath,
            [HostSettings.CollectibleVariable] = collectible ? "true" : "false",
            [HostSettings.RedisHostVariable] = "redis:6379"
        };
        Settings = HostSettings.Read(name => variables.GetValueOrDefault(name), Path.GetDirectoryName(gameAssemblyPath)!);
        Redis = new FakeRedisConnectionSource();
        Hooks = new NativeHookTable();
        Mode = new SwlorHostMode(CoreFunctions, Settings, Redis, Hooks, (category, message) => Log.Enqueue($"{category} {message}"));
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
