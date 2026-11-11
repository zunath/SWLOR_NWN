namespace SWLOR.Game.Contracts;

/// <summary>
/// The managed body of the hook that projects the equipped neck, shoulder and part-skin models into the appearance the engine sends.
/// The game installs the hook itself today and its native entry point only forwards here; under the host the host
/// installs the hook once and each game build binds an implementation. Parameters are native pointers and numbers.
/// Every method ends with <c>original</c>, the trampoline of the function the hook wraps: the body calls it where the
/// engine's own work belongs, so the engine behaves as before.
/// </summary>
public unsafe interface INeckAppearanceSerializationSink
{
    /// <summary><c>CNWSMessage::AddItemAppearanceToMessage</c>.</summary>
    void Write(void* message, void* player, void* itemPointer, nint original);
}
