using SWLOR.Game.Contracts;
using SWLOR.Game.Host.Bootstrap;

namespace SWLOR.Game.Host.Services;

/// <summary>
/// The host's <see cref="IHostLifecycle"/>: what every generation reads about the process. The environment and the
/// reload switch come from the host's settings; the host marks the module loaded when NWNX signals
/// <c>ON_MODULE_LOAD_FINISH</c>, so a generation activated after that knows the engine will not run the boot scripts or
/// that signal again.
/// </summary>
public sealed class HostLifecycle : IHostLifecycle
{
    /// <summary>The NWNX signal that ends the module load; the host marks the module loaded when it arrives.</summary>
    public const string ModuleLoadFinishSignal = "ON_MODULE_LOAD_FINISH";

    private volatile bool _moduleLoaded;

    /// <param name="settings">The host's settings: the environment and whether reload is on.</param>
    public HostLifecycle(HostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Environment = settings.EnvironmentName;
        ReloadEnabled = settings.HotReload;
    }

    /// <inheritdoc/>
    public bool ModuleLoaded => _moduleLoaded;

    /// <inheritdoc/>
    public string Environment { get; }

    /// <inheritdoc/>
    public bool ReloadEnabled { get; }

    /// <summary>Marks the module loaded. Returns true the first time.</summary>
    public bool MarkModuleLoaded()
    {
        if (_moduleLoaded)
            return false;

        _moduleLoaded = true;
        return true;
    }
}
