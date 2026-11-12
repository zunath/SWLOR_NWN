using Nwn.Hosting.Bootstrap;
using SWLOR.Game.Contracts;
using SWLOR.Game.NativeHooks;

namespace SWLOR.Game.Host.Hooks;

/// <summary>
/// One hook file of the game as a host extension: when the native host starts (<see cref="Install"/>) it finds the engine
/// functions the file hooks, and requests nothing. The hooks are installed only when the game enables them
/// (<see cref="IHookControl.Enable"/>, from the same handlers and at the same moments as on the native entry), so hook
/// timing is the same on both entries.
/// </summary>
public sealed class NativeHookExtension : INativeHostExtension
{
    private readonly NativeHookTable _hooks;

    public NativeHookExtension(NativeHookId hook, NativeHookTable hooks)
    {
        Hook = hook;
        _hooks = hooks ?? throw new ArgumentNullException(nameof(hooks));
    }

    /// <summary>The hook file this extension stands for.</summary>
    public NativeHookId Hook { get; }

    /// <inheritdoc/>
    public void Install() => _hooks.Resolve(Hook);

    /// <summary>One extension per hook file, in the order of <see cref="NativeHookId"/>.</summary>
    public static IReadOnlyList<INativeHostExtension> ForEveryHook(NativeHookTable hooks)
    {
        ArgumentNullException.ThrowIfNull(hooks);
        return Enum.GetValues<NativeHookId>().Select(hook => (INativeHostExtension)new NativeHookExtension(hook, hooks)).ToList();
    }
}
