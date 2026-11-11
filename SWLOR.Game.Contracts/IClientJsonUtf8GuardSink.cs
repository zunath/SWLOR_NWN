namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hook that makes the JSON clients send safe for the engine to decode and log.
/// The game installs the hook itself today and its native entry point only forwards here; under the host the host
/// installs the hook once and each game build binds an implementation. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface IClientJsonUtf8GuardSink
{
    /// <summary><c>CNWMessage::ReadJSON</c>; the decoded json comes back through the hidden result pointer, which is also the return value.</summary>
    void* ReadJson(void* result, void* message, nint original);
}
