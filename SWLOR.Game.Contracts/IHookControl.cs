namespace SWLOR.Game.Contracts;

/// <summary>
/// Installs the process's native hooks when the game asks for them. A hook is installed once for the life of the process
/// and never released, so it outlives every game build: the game calls <see cref="Enable"/> from the same handler and at
/// the same moment as it always installed the hook (the <c>RegisterHook(s)</c> handlers of <c>mod_load</c>, the legacy
/// item properties at module preload, the combat recorder's hooks at its first recording), and binds what the hook runs
/// separately (<see cref="INativeSinkBinding"/>). The native entry uses its own instance, the host entry the host's; both
/// drive the same trampolines (<c>SWLOR.Game.NativeHooks</c>). Every member runs on the engine thread.
/// </summary>
public interface IHookControl
{
    /// <summary>
    /// Asks NWNX for the hooks of <paramref name="hook"/> the first time it is called in the process; a later call does
    /// nothing. A hook whose sink is not bound runs the engine function unchanged.
    /// </summary>
    void Enable(NativeHookId hook);

    /// <summary>True once <see cref="Enable"/> has installed the hooks of <paramref name="hook"/>.</summary>
    bool IsEnabled(NativeHookId hook);
}
