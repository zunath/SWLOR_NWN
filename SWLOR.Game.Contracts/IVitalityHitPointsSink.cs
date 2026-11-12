namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the seven hooks that give player Vitality its half-level HP contribution at odd scores.
/// The hook is installed once per process (SWLOR.Game.NativeHooks, through <see cref="IHookControl"/>); its native entry point
/// forwards here while a game build has bound an implementation (<see cref="INativeSinkBinding"/>) and calls the engine
/// function itself while none is bound. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface IVitalityHitPointsSink
{
    /// <summary><c>CNWSCreature::GetMaxHitPoints</c>; runs per object per frame.</summary>
    short GetMaximum(void* pointer, int includeToughness, nint original);

    /// <summary><c>CNWSCreatureStats::SetCONBase</c>.</summary>
    void SetBase(void* pointer, byte score, int recalculateHP, nint original);

    /// <summary><c>CNWSObject::ApplyEffect</c>.</summary>
    void ApplyEffect(void* objectPointer, void* effectPointer, int loading, int initial, nint original);

    /// <summary><c>CNWSObject::RemoveEffect</c>.</summary>
    void RemoveEffect(void* objectPointer, void* effectPointer, nint original);

    /// <summary><c>CNWSObject::RemoveEffectById</c>.</summary>
    int RemoveEffectById(void* objectPointer, ulong id, nint original);

    /// <summary><c>CNWSCreatureStats::CalcStatModifier</c>; runs per object per frame.</summary>
    sbyte CalculateModifier(void* pointer, byte score, nint original);

    /// <summary>The <c>CNWSCreatureStats</c> destructor.</summary>
    void DestroyStats(void* pointer, nint original);
}
