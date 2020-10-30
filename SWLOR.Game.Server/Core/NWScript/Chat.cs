using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Get the PC that sent the last player chat(text) message.
        ///   Should only be called from a module's OnPlayerChat event script.
        ///   * Returns OBJECT_INVALID on error.
        ///   Note: Private tells do not trigger a OnPlayerChat event.
        /// </summary>
        public static uint GetPCChatSpeaker()
        {
            Internal.NativeFunctions.CallBuiltIn(838);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Sends szMessage to all the Dungeon Masters currently on the server.
        /// </summary>
        public static void SendMessageToAllDMs(string szMessage)
        {
            Internal.NativeFunctions.StackPushString(szMessage);
            Internal.NativeFunctions.CallBuiltIn(563);
        }

        /// <summary>
        ///   Get the last player chat(text) message that was sent.
        ///   Should only be called from a module's OnPlayerChat event script.
        ///   * Returns empty string "" on error.
        ///   Note: Private tells do not trigger a OnPlayerChat event.
        /// </summary>
        public static string GetPCChatMessage()
        {
            Internal.NativeFunctions.CallBuiltIn(839);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   Get the volume of the last player chat(text) message that was sent.
        ///   Returns one of the following TALKVOLUME_* constants based on the volume setting
        ///   that the player used to send the chat message.
        ///   TALKVOLUME_TALK
        ///   TALKVOLUME_WHISPER
        ///   TALKVOLUME_SHOUT
        ///   TALKVOLUME_SILENT_SHOUT (used for DM chat channel)
        ///   TALKVOLUME_PARTY
        ///   Should only be called from a module's OnPlayerChat event script.
        ///   * Returns -1 on error.
        ///   Note: Private tells do not trigger a OnPlayerChat event.
        /// </summary>
        public static TalkVolume GetPCChatVolume()
        {
            Internal.NativeFunctions.CallBuiltIn(840);
            return (TalkVolume)Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Set the last player chat(text) message before it gets sent to other players.
        ///   - sNewChatMessage: The new chat text to be sent onto other players.
        ///   Setting the player chat message to an empty string "",
        ///   will cause the chat message to be discarded
        ///   (i.e. it will not be sent to other players).
        ///   Note: The new chat message gets sent after the OnPlayerChat script exits.
        /// </summary>
        public static void SetPCChatMessage(string sNewChatMessage = "")
        {
            Internal.NativeFunctions.StackPushString(sNewChatMessage);
            Internal.NativeFunctions.CallBuiltIn(841);
        }

        /// <summary>
        ///   Set the last player chat(text) volume before it gets sent to other players.
        ///   - nTalkVolume: The new volume of the chat text to be sent onto other players.
        ///   TALKVOLUME_TALK
        ///   TALKVOLUME_WHISPER
        ///   TALKVOLUME_SHOUT
        ///   TALKVOLUME_SILENT_SHOUT (used for DM chat channel)
        ///   TALKVOLUME_PARTY
        ///   TALKVOLUME_TELL (sends the chat message privately back to the original speaker)
        ///   Note: The new chat message gets sent after the OnPlayerChat script exits.
        /// </summary>
        public static void SetPCChatVolume(TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            Internal.NativeFunctions.StackPushInteger((int)nTalkVolume);
            Internal.NativeFunctions.CallBuiltIn(842);
        }
    }
}