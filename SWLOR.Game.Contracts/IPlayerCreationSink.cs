namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hook that authorizes initial setup after the engine accepts a new server character.
/// The hook is installed once per process (SWLOR.Game.NativeHooks, through <see cref="IHookControl"/>); its native entry point
/// forwards here while a game build has bound an implementation (<see cref="INativeSinkBinding"/>) and calls the engine
/// function itself while none is bound. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface IPlayerCreationSink
{
    /// <summary><c>CServerExoAppInternal::ValidateCreateServerCharacter</c>.</summary>
    uint Validate(void* server, void* client, void* data, uint size, nint original);
}
