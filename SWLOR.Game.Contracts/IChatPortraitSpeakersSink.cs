namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the nine <c>CNWSMessage::SendServerToPlayerChat_*</c> hooks that tell the chat portrait tracker who spoke each line to a client.
/// The game installs the hook itself today and its native entry point only forwards here; under the host the host
/// installs the hook once and each game build binds an implementation. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface IChatPortraitSpeakersSink
{
    /// <summary>One chat line the engine sends to one recipient (the same sink serves all nine channels; each channel's own trampoline arrives as <paramref name="original"/>).</summary>
    int Send(void* message, uint recipient, uint speaker, void* text, nint original);
}
