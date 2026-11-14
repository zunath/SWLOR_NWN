namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hook that serializes player-list additions separately for each observer's private names.
/// The hook is installed once per process (SWLOR.Game.NativeHooks, through <see cref="IHookControl"/>); its native entry point
/// forwards here while a game build has bound an implementation (<see cref="INativeSinkBinding"/>) and calls the engine
/// function itself while none is bound. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface IPlayerListNamePrivacySink
{
    /// <summary><c>CNWSMessage::SendServerToPlayerPlayerList_Add</c>.</summary>
    int SendPlayerListAdd(void* message, uint recipientId, void* newPlayer, nint original);
}
