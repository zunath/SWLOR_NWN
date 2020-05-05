using NWN;
using SWLOR.Game.Server.GameObject;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public class NWNXFeedback
    {
        const string NWNX_Feedback = "NWNX_Feedback";




        /// <summary>
        /// Gets if feedback message nMessage is hidden.
        /// Notes:
        /// If oPC == OBJECT_INVALID it will return the global state:
        ///    TRUE      nMessage is globally hidden
        ///    FALSE     nMessage is not globally hidden
        /// If oPC is a valid player it will return the personal state:
        ///    TRUE      nMessage is hidden for oPC
        ///    FALSE     nMessage is not hidden for oPC
        ///    -1        Personal state is not set
        /// </summary>
        /// <param name="nMessage">The feedback message type. Refer to FeedbackMessageType for IDs.</param>
        /// <param name="oPC">The player who is being hidden.</param>
        /// <returns></returns>
        public static int GetFeedbackMessageHidden(int nMessage, NWPlayer oPC = null)
        {
            if (oPC == null)
                oPC = new NWPlayer(_.OBJECT_INVALID);

            string sFunc = "GetMessageHidden";
            int nMessageType = 0;

            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nMessage);
            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nMessageType);
            NWNX_PushArgumentObject(NWNX_Feedback, sFunc, oPC);
            NWNX_CallFunction(NWNX_Feedback, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Feedback, sFunc);
        }

        /// <summary>
        /// Sets if feedback message nMessage is hidden.
        /// Notes:
        /// If oPC == OBJECT_INVALID it will set the global state:
        ///    TRUE      nMessage is globally hidden
        ///    FALSE     nMessage is not globally hidden
        /// If oPC is a valid player it will set the personal state:
        ///    TRUE      nMessage is hidden for oPC
        ///    FALSE     nMessage is not hidden for oPC
        ///    -1        Remove the personal state
        ///
        /// Personal state overrides the global state which means if a global state is set
        /// to TRUE but the personal state is set to FALSE, the message will be shown to oPC
        /// </summary>
        /// <param name="nMessage">The feedback message type. Refer to FeedbackMessageType for IDs.</param>
        /// <param name="nState">The state to set, TRUE, FALSE, or -1.</param>
        /// <param name="oPC">The player to adjust.</param>
        public static void SetFeedbackMessageHidden(int nMessage, int nState, NWPlayer oPC = null)
        {
            if (oPC == null)
                oPC = new NWPlayer(_.OBJECT_INVALID);

            string sFunc = "SetMessageHidden";
            int nMessageType = 0;

            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nState);
            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nMessage);
            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nMessageType);
            NWNX_PushArgumentObject(NWNX_Feedback, sFunc, oPC);
            NWNX_CallFunction(NWNX_Feedback, sFunc);
        }

        /// <summary> 
        /// Gets if combatlog message nMessage is hidden.
        /// Notes:
        /// If oPC == OBJECT_INVALID it will return the global state:
        ///    TRUE      nMessage is globally hidden
        ///    FALSE     nMessage is not globally hidden
        /// If oPC is a valid player it will return the personal state:
        ///    TRUE      nMessage is hidden for oPC
        ///    FALSE     nMessage is not hidden for oPC
        ///    -1        Personal state is not set
        /// </summary>
        /// <param name="nMessage">The combat message type. Refer to CombatLogMessageType for IDs.</param>
        /// <param name="oPC">The player to adjust.</param>
        /// <returns></returns>
        public static int GetCombatLogMessageHidden(int nMessage, NWPlayer oPC = null)
        {
            if (oPC == null)
                oPC = new NWPlayer(_.OBJECT_INVALID);

            string sFunc = "GetMessageHidden";
            int nMessageType = 1;

            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nMessage);
            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nMessageType);
            NWNX_PushArgumentObject(NWNX_Feedback, sFunc, oPC);
            NWNX_CallFunction(NWNX_Feedback, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Feedback, sFunc);
        }

        /// <summary>
        /// Sets if combatlog message nMessage is hidden.
        /// Notes:
        /// nMessage = NWNX_FEEDBACK_COMBATLOG_* > See Below
        /// If oPC == OBJECT_INVALID it will set the global state:
        ///    TRUE      nMessage is globally hidden
        ///    FALSE     nMessage is not globally hidden
        /// If oPC is a valid player it will set the personal state:
        ///    TRUE      nMessage is hidden for oPC
        ///    FALSE     nMessage is not hidden for oPC
        ///    -1        Remove the personal state
        ///
        /// Personal state overrides the global state which means if a global state is set
        /// to TRUE but the personal state is set to FALSE, the message will be shown to oPC
        /// </summary>
        /// <param name="nMessage">The combat message type. Refer to CombatLogMessageType for IDs.</param>
        /// <param name="nState">The state to set. TRUE, FALSE, or -1</param>
        /// <param name="oPC">The player to adjust.</param>
        public static void SetCombatLogMessageHidden(int nMessage, int nState, NWPlayer oPC = null)
        {
            if (oPC == null)
                oPC = new NWPlayer(_.OBJECT_INVALID);

            string sFunc = "SetMessageHidden";
            int nMessageType = 1;

            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nState);
            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nMessage);
            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nMessageType);
            NWNX_PushArgumentObject(NWNX_Feedback, sFunc, oPC);
            NWNX_CallFunction(NWNX_Feedback, sFunc);
        }

        /// <summary>
        /// Gets if journal updated messages are hidden.
        /// Notes:
        /// If oPC == OBJECT_INVALID it will return the global state:
        ///    TRUE      Journal updated messages are globally hidden
        ///    FALSE     Journal updated messages are not globally hidden
        /// If oPC is a valid player it will return the personal state:
        ///    TRUE      Journal updated messages are hidden for oPC
        ///    FALSE     Journal updated messages are not hidden for oPC
        ///    -1        Personal state is not set
        /// </summary>
        /// <param name="oPC">The player to adjust.</param>
        /// <returns></returns>
        public static int GetJournalUpdatedMessageHidden(NWPlayer oPC = null)
        {
            if (oPC == null)
                oPC = new NWPlayer(_.OBJECT_INVALID);

            string sFunc = "GetMessageHidden";
            int nMessageType = 2;

            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, 0);
            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nMessageType);
            NWNX_PushArgumentObject(NWNX_Feedback, sFunc, oPC);
            NWNX_CallFunction(NWNX_Feedback, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Feedback, sFunc);
        }

        /// <summary>
        /// 
        /// Sets if journal updated messages are hidden.
        /// Notes:
        /// If oPC == OBJECT_INVALID it will set the global state:
        ///    TRUE      Journal updated messages are globally hidden
        ///    FALSE     Journal updated messages are not globally hidden
        /// If oPC is a valid player it will set the personal state:
        ///    TRUE      Journal updated messages are hidden for oPC
        ///    FALSE     Journal updated messages are not hidden for oPC
        ///    -1        Remove the personal state
        ///
        /// Personal state overrides the global state which means if a global state is set
        /// to TRUE but the personal state is set to FALSE, the message will be shown to oPC
        /// </summary>
        /// <param name="nState">The state to set.</param>
        /// <param name="oPC">The player to adjust.</param>
        public static void SetJournalUpdatedMessageHidden(int nState, NWPlayer oPC = null)
        {
            if (oPC == null)
                oPC = new NWPlayer(_.OBJECT_INVALID);
            string sFunc = "SetMessageHidden";
            int nMessageType = 2;

            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nState);
            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, 0);
            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nMessageType);
            NWNX_PushArgumentObject(NWNX_Feedback, sFunc, oPC);
            NWNX_CallFunction(NWNX_Feedback, sFunc);
        }

        /// <summary>
        /// 
        /// Set whether to use a blacklist or whitelist mode for feedback messages
        /// Default: Blacklist
        ///
        /// TRUE = Whitelist, all messages hidden by default
        /// FALSE = Blacklist, all messages shown by default
        /// </summary>
        /// <param name="whitelist">true if all messages are hidden by default. false if all messages are shown by default.</param>
        public static void SetFeedbackMessageMode(bool whitelist)
        {
            string sFunc = "SetFeedbackMode";
            int nMessageType = 0;

            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, whitelist ? 1 : 0);
            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nMessageType);
            NWNX_CallFunction(NWNX_Feedback, sFunc);
        }

        /// <summary>
        /// Set whether to use a blacklist or whitelist mode for combatlog messages
        /// Default: Blacklist
        ///
        /// TRUE = Whitelist, all messages hidden by default
        /// FALSE = Blacklist, all messages shown by default
        ///
        /// NOTE: If using Whitelist, be sure to whitelist NWNX_FEEDBACK_COMBATLOG_FEEDBACK for feedback messages to work
        /// </summary>
        /// <param name="whitelist">true if all messages are hidden by default. false if all messages are shown by default.</param>
        public static void SetCombatLogMessageMode(bool whitelist)
        {
            string sFunc = "SetFeedbackMode";
            int nMessageType = 1;

            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, whitelist ? 1 : 0);
            NWNX_PushArgumentInt(NWNX_Feedback, sFunc, nMessageType);
            NWNX_CallFunction(NWNX_Feedback, sFunc);
        }
    }
}
