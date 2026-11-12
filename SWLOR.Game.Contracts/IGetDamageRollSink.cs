namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hook that replaces the engine's weapon damage roll. The engine function is never called, so there is no original.
/// The hook is installed once per process (SWLOR.Game.NativeHooks, through <see cref="IHookControl"/>); its native entry point
/// forwards here while a game build has bound an implementation (<see cref="INativeSinkBinding"/>) and calls the engine
/// function itself while none is bound. Parameters are native pointers and numbers.
/// </summary>
public unsafe interface IGetDamageRollSink
{
    /// <summary><c>CNWSCreatureStats::GetDamageRoll</c>.</summary>
    int GetDamageRoll(void* thisPtr, void* pTarget, int bOffHand, int bCritical, int bSneakAttack, int bDeathAttack, int bForceMax);
}
