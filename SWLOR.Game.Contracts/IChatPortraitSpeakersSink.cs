namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the nine <c>CNWSMessage::SendServerToPlayerChat_*</c> hooks that tell the chat portrait tracker who spoke each line to a client.
/// The hook is installed once per process (SWLOR.Game.NativeHooks, through <see cref="IHookControl"/>); its native entry point
/// forwards here while a game build has bound an implementation (<see cref="INativeSinkBinding"/>) and calls the engine
/// function itself while none is bound. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface IChatPortraitSpeakersSink
{
    /// <summary>One chat line the engine sends to one recipient (the same sink serves all nine channels; each channel's own trampoline arrives as <paramref name="original"/>).</summary>
    int Send(void* message, uint recipient, uint speaker, void* text, nint original);
}
