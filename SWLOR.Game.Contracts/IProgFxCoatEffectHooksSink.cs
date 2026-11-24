namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the four hooks that tell the tint-map ProgFX coat renderer when a coating visual effect or
/// petrification is applied to or removed from a creature. The hook is installed once per process (SWLOR.Game.NativeHooks,
/// through <see cref="IHookControl"/>); its native entry point forwards here while a game build has bound an implementation
/// (<see cref="INativeSinkBinding"/>) and calls the engine function itself while none is bound. Parameters are native
/// pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface IProgFxCoatEffectHooksSink
{
    /// <summary><c>CNWSEffectListHandler::OnApplyVisualEffect</c>.</summary>
    int ApplyVisualEffect(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame, nint original);

    /// <summary><c>CNWSEffectListHandler::OnRemoveVisualEffect</c>.</summary>
    int RemoveVisualEffect(void* effectListHandler, void* pObject, void* pEffect, nint original);

    /// <summary><c>CNWSEffectListHandler::OnApplyPetrify</c>.</summary>
    int ApplyPetrify(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame, nint original);

    /// <summary><c>CNWSEffectListHandler::OnRemovePetrify</c>.</summary>
    int RemovePetrify(void* effectListHandler, void* pObject, void* pEffect, nint original);
}
