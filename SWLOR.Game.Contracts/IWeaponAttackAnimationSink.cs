namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the two per-object update hooks that play the queued weapon attack animations.
/// The game installs the hook itself today and its native entry point only forwards here; under the host the host
/// installs the hook once and each game build binds an implementation. Parameters are native pointers and numbers.
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
