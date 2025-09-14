using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Determine whether oObject is in conversation.
        /// </summary>
        public static bool IsInConversation(uint oObject)
        {
            return global::NWN.Core.NWScript.IsInConversation(oObject) != 0;
        }

        /// <summary>
        ///   Add a speak action to the action subject.
        ///   - sStringToSpeak: String to be spoken
        ///   - nTalkVolume: TALKVOLUME_*
        /// </summary>
        public static void ActionSpeakString(string sStringToSpeak, TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            global::NWN.Core.NWScript.ActionSpeakString(sStringToSpeak, (int)nTalkVolume);
        }

        /// <summary>
        ///   Use this in a conversation script to get the person with whom you are conversing.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetLastSpeaker()
        {
            return global::NWN.Core.NWScript.GetLastSpeaker();
        }

        /// <summary>
        ///   Use this in an OnDialog script to start up the dialog tree.
        ///   - sResRef: if this is not specified, the default dialog file will be used
        ///   - oObjectToDialog: if this is not specified the person that triggered the
        ///   event will be used
        /// </summary>
        public static int BeginConversation(string sResRef = "", uint oObjectToDialog = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.BeginConversation(sResRef, oObjectToDialog);
        }
    }
}