using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class FeedbackPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Feedback";

        public static int GetFeedbackMessageHidden(FeedbackMessageTypes messageType, uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMessageHidden");
            NWNCore.NativeFunctions.nwnxPushInt((int)messageType);
            NWNCore.NativeFunctions.nwnxPushInt(0);
            NWNCore.NativeFunctions.nwnxPushObject((uint)player);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetFeedbackMessageHidden(FeedbackMessageTypes messageType, bool hide, uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMessageHidden");
            NWNCore.NativeFunctions.nwnxPushInt(hide ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt((int)messageType);
            NWNCore.NativeFunctions.nwnxPushInt(0);
            NWNCore.NativeFunctions.nwnxPushObject((uint)player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static bool GetCombatLogMessageHidden(FeedbackMessageTypes messageType, uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMessageHidden");
            NWNCore.NativeFunctions.nwnxPushInt((int)messageType);
            NWNCore.NativeFunctions.nwnxPushInt(1);
            NWNCore.NativeFunctions.nwnxPushObject((uint)player);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt() == 1;
        }

        public static void SetCombatLogMessageHidden(CombatLogMessageType messageType, bool hide, uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMessageHidden");
            NWNCore.NativeFunctions.nwnxPushInt(hide ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt((int)messageType);
            NWNCore.NativeFunctions.nwnxPushInt(1);
            NWNCore.NativeFunctions.nwnxPushObject((uint)player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static int GetJournalUpdatedMessageHidden(uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMessageHidden");
            NWNCore.NativeFunctions.nwnxPushInt(0);
            NWNCore.NativeFunctions.nwnxPushInt(2);
            NWNCore.NativeFunctions.nwnxPushObject((uint)player);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetFeedbackMessageMode(int whiteList)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFeedbackMode");
            NWNCore.NativeFunctions.nwnxPushInt(whiteList);
            NWNCore.NativeFunctions.nwnxPushInt(0);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetCombatLogMessageMod(int whiteList)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFeedbackMode");
            NWNCore.NativeFunctions.nwnxPushInt(whiteList);
            NWNCore.NativeFunctions.nwnxPushInt(1);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }
    }
}