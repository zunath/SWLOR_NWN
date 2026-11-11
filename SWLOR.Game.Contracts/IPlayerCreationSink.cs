namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hook that authorizes initial setup after the engine accepts a new server character.
/// The game installs the hook itself today and its native entry point only forwards here; under the host the host
/// installs the hook once and each game build binds an implementation. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface IPlayerCreationSink
{
    /// <summary><c>CServerExoAppInternal::ValidateCreateServerCharacter</c>.</summary>
    uint Validate(void* server, void* client, void* data, uint size, nint original);
}
