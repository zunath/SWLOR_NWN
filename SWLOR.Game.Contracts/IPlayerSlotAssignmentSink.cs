namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hook that gives each new connection the slot the rotation picks.
/// The game installs the hook itself today and its native entry point only forwards here; under the host the host
/// installs the hook once and each game build binds an implementation. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface IPlayerSlotAssignmentSink
{
    /// <summary><c>CNetLayerInternal::SetSlidingWindow</c>.</summary>
    int SetSlidingWindow(void* netLayer, uint connectionId, uint* windowId, nint original);
}
