namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the two per-object update hooks that play the queued weapon attack animations.
/// The hook is installed once per process (SWLOR.Game.NativeHooks, through <see cref="IHookControl"/>); its native entry point
/// forwards here while a game build has bound an implementation (<see cref="INativeSinkBinding"/>) and calls the engine
/// function itself while none is bound. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface IWeaponAttackAnimationSink
{
    /// <summary><c>CNWSMessage::ComputeUpdateRequired</c>; runs per object per frame.</summary>
    uint ComputeUpdate(void* message, void* player, void* obj, void* last, int playerObject, nint original);

    /// <summary><c>CNWSMessage::WriteGameObjUpdate_UpdateObject</c>; runs per object per frame.</summary>
    void WriteUpdate(void* message, void* player, void* obj, void* last, uint updates, uint appearance, nint original);
}
