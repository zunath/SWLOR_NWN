using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Determines whether the specified object is in conversation.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>True if the object is in conversation, false otherwise</returns>
        public static bool IsInConversation(uint oObject)
        {
            return global::NWN.Core.NWScript.IsInConversation(oObject) != 0;
        }

        /// <summary>
        /// Adds a speak action to the action subject.
        /// </summary>
        /// <param name="sStringToSpeak">The string to be spoken</param>
        /// <param name="nTalkVolume">The talk volume (TALKVOLUME_* constants) (default: TalkVolume.Talk)</param>
        public static void ActionSpeakString(string sStringToSpeak, TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            global::NWN.Core.NWScript.ActionSpeakString(sStringToSpeak, (int)nTalkVolume);
        }

        /// <summary>
        /// Gets the person with whom you are conversing.
        /// </summary>
        /// <returns>The last speaker. Returns OBJECT_INVALID if the caller is not a valid creature</returns>
        /// <remarks>Use this in a conversation script.</remarks>
        public static uint GetLastSpeaker()
        {
            return global::NWN.Core.NWScript.GetLastSpeaker();
        }

        /// <summary>
        /// Starts up the dialog tree.
        /// </summary>
        /// <param name="sResRef">The dialog file to use. If not specified, the default dialog file will be used (default: empty string)</param>
        /// <param name="oObjectToDialog">The object to dialog with. If not specified, the person that triggered the event will be used (default: OBJECT_SELF)</param>
        /// <returns>The result of beginning the conversation</returns>
        /// <remarks>Use this in an OnDialog script.</remarks>
        public static int BeginConversation(string sResRef = "", uint oObjectToDialog = OBJECT_INVALID)
        {
            if (oObjectToDialog == OBJECT_INVALID)
                oObjectToDialog = OBJECT_SELF;
            return global::NWN.Core.NWScript.BeginConversation(sResRef, oObjectToDialog);
        }
    }
}