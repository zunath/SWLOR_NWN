namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hook that converts retired item-property rows before the engine loads an item.
/// The hook is installed once per process (SWLOR.Game.NativeHooks, through <see cref="IHookControl"/>); its native entry point
/// forwards here while a game build has bound an implementation (<see cref="INativeSinkBinding"/>) and calls the engine
/// function itself while none is bound. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface ILegacyItemPropertiesSink
{
    /// <summary><c>CNWSItem::LoadItem</c>.</summary>
    int LoadItem(void* item, void* resource, void* structure, int firstFlag, int secondFlag, nint original);
}
