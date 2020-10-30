using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class Feedback
    {
        private const string PLUGIN_NAME = "NWNX_Feedback";

        public static int GetFeedbackMessageHidden(FeedbackMessageTypes messageType, uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMessageHidden");
            Internal.NativeFunctions.nwnxPushInt((int)messageType);
            Internal.NativeFunctions.nwnxPushInt(0);
            Internal.NativeFunctions.nwnxPushObject((uint)player);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void SetFeedbackMessageHidden(FeedbackMessageTypes messageType, bool hide, uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMessageHidden");
            Internal.NativeFunctions.nwnxPushInt(hide ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt((int)messageType);
            Internal.NativeFunctions.nwnxPushInt(0);
            Internal.NativeFunctions.nwnxPushObject((uint)player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int GetCombatLogMessageHidden(FeedbackMessageTypes messageType, uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMessageHidden");
            Internal.NativeFunctions.nwnxPushInt((int)messageType);
            Internal.NativeFunctions.nwnxPushInt(1);
            Internal.NativeFunctions.nwnxPushObject((uint)player);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void SetCombatLogMessageHidden(FeedbackMessageTypes messageType, int hide,
            uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMessageHidden");
            Internal.NativeFunctions.nwnxPushInt(hide);
            Internal.NativeFunctions.nwnxPushInt((int)messageType);
            Internal.NativeFunctions.nwnxPushInt(1);
            Internal.NativeFunctions.nwnxPushObject((uint)player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int GetJournalUpdatedMessageHidden(uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMessageHidden");
            Internal.NativeFunctions.nwnxPushInt(0);
            Internal.NativeFunctions.nwnxPushInt(2);
            Internal.NativeFunctions.nwnxPushObject((uint)player);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void SetFeedbackMessageMode(int whiteList)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFeedbackMode");
            Internal.NativeFunctions.nwnxPushInt(whiteList);
            Internal.NativeFunctions.nwnxPushInt(0);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetCombatLogMessageMod(int whiteList)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFeedbackMode");
            Internal.NativeFunctions.nwnxPushInt(whiteList);
            Internal.NativeFunctions.nwnxPushInt(1);
            Internal.NativeFunctions.nwnxCallFunction();
        }
    }
}