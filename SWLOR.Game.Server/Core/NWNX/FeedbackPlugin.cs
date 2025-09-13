using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class FeedbackPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Feedback";

        public static int GetFeedbackMessageHidden(FeedbackMessageTypes messageType, uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetMessageHidden");
            NWNXPInvoke.NWNXPushInt((int)messageType);
            NWNXPInvoke.NWNXPushInt(0);
            NWNXPInvoke.NWNXPushObject((uint)player);
            NWNXPInvoke.NWNXCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetFeedbackMessageHidden(FeedbackMessageTypes messageType, bool hide, uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetMessageHidden");
            NWNXPInvoke.NWNXPushInt(hide ? 1 : 0);
            NWNXPInvoke.NWNXPushInt((int)messageType);
            NWNXPInvoke.NWNXPushInt(0);
            NWNXPInvoke.NWNXPushObject((uint)player);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static bool GetCombatLogMessageHidden(FeedbackMessageTypes messageType, uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetMessageHidden");
            NWNXPInvoke.NWNXPushInt((int)messageType);
            NWNXPInvoke.NWNXPushInt(1);
            NWNXPInvoke.NWNXPushObject((uint)player);
            NWNXPInvoke.NWNXCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt() == 1;
        }

        public static void SetCombatLogMessageHidden(CombatLogMessageType messageType, bool hide, uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetMessageHidden");
            NWNXPInvoke.NWNXPushInt(hide ? 1 : 0);
            NWNXPInvoke.NWNXPushInt((int)messageType);
            NWNXPInvoke.NWNXPushInt(1);
            NWNXPInvoke.NWNXPushObject((uint)player);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static int GetJournalUpdatedMessageHidden(uint? player = null)
        {
            if (player == null) player = NWScript.NWScript.OBJECT_INVALID;
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetMessageHidden");
            NWNXPInvoke.NWNXPushInt(0);
            NWNXPInvoke.NWNXPushInt(2);
            NWNXPInvoke.NWNXPushObject((uint)player);
            NWNXPInvoke.NWNXCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetFeedbackMessageMode(int whiteList)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetFeedbackMode");
            NWNXPInvoke.NWNXPushInt(whiteList);
            NWNXPInvoke.NWNXPushInt(0);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetCombatLogMessageMod(int whiteList)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetFeedbackMode");
            NWNXPInvoke.NWNXPushInt(whiteList);
            NWNXPInvoke.NWNXPushInt(1);
            NWNXPInvoke.NWNXCallFunction();
        }
    }
}