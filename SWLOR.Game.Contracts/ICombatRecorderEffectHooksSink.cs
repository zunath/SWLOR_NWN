namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the two hooks that let the combat recorder see every damage and heal effect the engine applies.
/// The game installs the hook itself today and its native entry point only forwards here; under the host the host
/// installs the hook once and each game build binds an implementation. Parameters are native pointers and numbers.
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
