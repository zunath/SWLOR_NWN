namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the two hooks that let the combat recorder see every damage and heal effect the engine applies.
/// The hook is installed once per process (SWLOR.Game.NativeHooks, through <see cref="IHookControl"/>); its native entry point
/// forwards here while a game build has bound an implementation (<see cref="INativeSinkBinding"/>) and calls the engine
/// function itself while none is bound. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface ICombatRecorderEffectHooksSink
{
    /// <summary><c>CNWSEffectListHandler::OnApplyDamage</c>.</summary>
    int ApplyDamage(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame, nint original);

    /// <summary><c>CNWSEffectListHandler::OnApplyHeal</c>.</summary>
    int ApplyHeal(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame, nint original);
}
