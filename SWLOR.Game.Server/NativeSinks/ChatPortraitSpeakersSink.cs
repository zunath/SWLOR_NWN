using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Domain.Foundation;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>
/// Watches each chat line the engine sends to one client, so <see cref="ChatPortraitTells"/> knows which speaker's
/// portrait that client's chat log holds for each connection slot. Every player chat channel the engine fans out,
/// including NWNX Chat sends and SpeakString, reaches one of the nine hooks per recipient
/// (<see cref="Native.ChatPortraitSpeakers"/>), and all of them end here.
/// </summary>
public sealed unsafe class ChatPortraitSpeakersSink : IChatPortraitSpeakersSink
{
    public int Send(void* message, uint recipient, uint speaker, void* text, nint original)
    {
        try
        {
            ChatPortraitTells.RecordChatLine(recipient, speaker);
        }
        catch (Exception exception)
        {
            Log.WriteError(exception, "Could not record a chat portrait speaker");
        }

        return ((delegate* unmanaged<void*, uint, uint, void*, int>)original)(message, recipient, speaker, text);
    }
}
