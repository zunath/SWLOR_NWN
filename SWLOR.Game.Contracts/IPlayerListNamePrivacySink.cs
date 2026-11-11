namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hook that serializes player-list additions separately for each observer's private names.
/// The game installs the hook itself today and its native entry point only forwards here; under the host the host
/// installs the hook once and each game build binds an implementation. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface IPlayerListNamePrivacySink
{
    /// <summary><c>CNWSMessage::SendServerToPlayerPlayerList_Add</c>.</summary>
    int SendPlayerListAdd(void* message, uint recipientId, void* newPlayer, nint original);
}
