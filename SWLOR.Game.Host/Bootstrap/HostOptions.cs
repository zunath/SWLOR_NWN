using Nwn.Hosting.Bootstrap;
using Nwn.Hosting.Reload;
using SWLOR.Game.Host.Hooks;
using SWLOR.Game.NativeHooks;

namespace SWLOR.Game.Host.Bootstrap;

/// <summary>How SWLOR starts the package's native host and loads the game build.</summary>
public static class HostOptions
{
    /// <summary>The first word of every console line the host writes (<c>NativeHost.Log</c>).</summary>
    public const string LogPrefix = "SWLOR_HOST";

    /// <summary>The mode name in the host's bootstrap line.</summary>
    public const string ModeName = "swlor";

    /// <summary>The prefix of the game's load context name.</summary>
    public const string ContextNamePrefix = "swlor-game";

    /// <summary>
    /// The assemblies every generation shares with the host by name, besides the ones the package always shares
    /// (<c>NWN.Core</c>, <c>NWN.Native</c>, <c>NWNX.NET</c>, <c>Nwn.Hosting.Contracts</c>): the SWLOR contracts the host
    /// hands across, the native hook trampolines (the game's own reference to them must reach the host's copy, whose
    /// hooks NWNX calls), and the Redis client with its dependency closure, because the host owns the one Redis connection
    /// for the life of the process and the game uses its types. Everything else (the DI container, Newtonsoft.Json,
    /// NRediSearch, NReJSON, Serilog, SWLOR.NWN.API) loads with each generation.
    /// </summary>
    public static IReadOnlyList<string> SharedAssemblyNames { get; } =
    [
        "SWLOR.Game.Contracts",
        "SWLOR.Game.NativeHooks",
        "StackExchange.Redis",
        "RESPite",
        "System.IO.Hashing",
        "Microsoft.Extensions.Logging.Abstractions",
        "Microsoft.Extensions.DependencyInjection.Abstractions"
    ];

    /// <summary>
    /// The native host options SWLOR always uses. A server that registered its own NWNX handlers and let NWN.Core keep
    /// its closures, as the native entry (<c>ServerManager.Bootstrap</c>) does, gets the same behaviour only with all three:
    /// frames run inline at the start of the main loop with <c>OBJECT_SELF</c> invalid from the first frame, a closure
    /// NWNX refuses is dropped without an exception, and pending closures are unbounded like the native entry's
    /// dictionary. One extension per hook file of the game (<see cref="NativeHookExtension"/>) finds its engine functions
    /// when the host starts; the game installs the hooks through <paramref name="hooks"/> at its own boot moments.
    /// </summary>
    public static NativeHostOptions Create(Func<CoreFunctionHandler, IHostMode> createMode, NativeHookTable hooks)
    {
        ArgumentNullException.ThrowIfNull(createMode);
        ArgumentNullException.ThrowIfNull(hooks);
        return new NativeHostOptions(LogPrefix, ModeName, createMode, NativeHookExtension.ForEveryHook(hooks))
        {
            MaximumPendingClosures = null,
            FrameDelivery = FrameDelivery.Inline,
            RejectedClosures = RejectedClosureHandling.Drop
        };
    }

    /// <summary>How the game build loads: the shared names above, and collectible as <paramref name="settings"/> says.</summary>
    public static GenerationLoadOptions LoadOptions(HostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new GenerationLoadOptions(SharedAssemblyNames, ContextNamePrefix) { Collectible = settings.Collectible };
    }
}
