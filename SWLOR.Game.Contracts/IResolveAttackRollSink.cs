namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hook that replaces the engine's attack roll. The engine function is never called, so there is no original.
/// The hook is installed once per process (SWLOR.Game.NativeHooks, through <see cref="IHookControl"/>); its native entry point
/// forwards here while a game build has bound an implementation (<see cref="INativeSinkBinding"/>) and calls the engine
/// function itself while none is bound. Parameters are native pointers and numbers.
/// </summary>
public unsafe interface IResolveAttackRollSink
{
    /// <summary><c>CNWSCreature::ResolveAttackRoll</c>.</summary>
    void ResolveAttackRoll(void* thisPtr, void* pTarget);
}
