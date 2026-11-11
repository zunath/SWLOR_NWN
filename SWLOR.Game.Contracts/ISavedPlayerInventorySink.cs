namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hook that keeps saved item stacks intact while a player file is being loaded.
/// The game installs the hook itself today and its native entry point only forwards here; under the host the host
/// installs the hook once and each game build binds an implementation. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface ISavedPlayerInventorySink
{
    /// <summary><c>CNWSItem::CompareItem</c>.</summary>
    int CompareItem(void* item, void* other, nint original);
}
