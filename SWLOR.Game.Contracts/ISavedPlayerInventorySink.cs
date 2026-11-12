namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hook that keeps saved item stacks intact while a player file is being loaded.
/// The hook is installed once per process (SWLOR.Game.NativeHooks, through <see cref="IHookControl"/>); its native entry point
/// forwards here while a game build has bound an implementation (<see cref="INativeSinkBinding"/>) and calls the engine
/// function itself while none is bound. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface ISavedPlayerInventorySink
{
    /// <summary><c>CNWSItem::CompareItem</c>.</summary>
    int CompareItem(void* item, void* other, nint original);
}
