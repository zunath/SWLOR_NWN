using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Gets the PC that sent the last player chat (text) message.
        /// </summary>
        /// <returns>The PC that sent the last chat message. Returns OBJECT_INVALID on error</returns>
        /// <remarks>Should only be called from a module's OnPlayerChat event script. Private tells do not trigger an OnPlayerChat event.</remarks>
        public static uint GetPCChatSpeaker()
        {
            return global::NWN.Core.NWScript.GetPCChatSpeaker();
        }

        /// <summary>
        /// Sends a message to all the Dungeon Masters currently on the server.
        /// </summary>
        /// <param name="szMessage">The message to send to all DMs</param>
        public static void SendMessageToAllDMs(string szMessage)
        {
            global::NWN.Core.NWScript.SendMessageToAllDMs(szMessage);
        }

        /// <summary>
        /// Gets the last player chat (text) message that was sent.
        /// </summary>
        /// <returns>The last chat message. Returns empty string on error</returns>
        /// <remarks>Should only be called from a module's OnPlayerChat event script. Private tells do not trigger an OnPlayerChat event.</remarks>
        public static string GetPCChatMessage()
        {
            return global::NWN.Core.NWScript.GetPCChatMessage();
        }

        /// <summary>
        /// Gets the volume of the last player chat (text) message that was sent.
        /// </summary>
        /// <returns>One of the TALKVOLUME_* constants based on the volume setting that the player used to send the chat message. Returns -1 on error</returns>
        /// <remarks>Should only be called from a module's OnPlayerChat event script. Private tells do not trigger an OnPlayerChat event. Possible values: TALKVOLUME_TALK, TALKVOLUME_WHISPER, TALKVOLUME_SHOUT, TALKVOLUME_SILENT_SHOUT (used for DM chat channel), TALKVOLUME_PARTY</remarks>
        public static TalkVolume GetPCChatVolume()
        {
            return (TalkVolume)global::NWN.Core.NWScript.GetPCChatVolume();
        }

        /// <summary>
        /// Sets the last player chat (text) message before it gets sent to other players.
        /// </summary>
        /// <param name="sNewChatMessage">The new chat text to be sent to other players. Setting to an empty string will cause the chat message to be discarded (not sent to other players) (default: empty string)</param>
        /// <remarks>The new chat message gets sent after the OnPlayerChat script exits.</remarks>
        public static void SetPCChatMessage(string sNewChatMessage = "")
        {
            global::NWN.Core.NWScript.SetPCChatMessage(sNewChatMessage);
        }

        /// <summary>
        /// Sets the last player chat (text) volume before it gets sent to other players.
        /// </summary>
        /// <param name="nTalkVolume">The new volume of the chat text to be sent to other players (default: TalkVolume.Talk)</param>
        /// <remarks>The new chat message gets sent after the OnPlayerChat script exits. Possible values: TALKVOLUME_TALK, TALKVOLUME_WHISPER, TALKVOLUME_SHOUT, TALKVOLUME_SILENT_SHOUT (used for DM chat channel), TALKVOLUME_PARTY, TALKVOLUME_TELL (sends the chat message privately back to the original speaker)</remarks>
        public static void SetPCChatVolume(TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            global::NWN.Core.NWScript.SetPCChatVolume((int)nTalkVolume);
        }
    }
}