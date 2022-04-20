using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class DialogPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Dialog";

        public static int GetCurrentNodeType()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentNodeType");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static int GetCurrentScriptType()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentScriptType");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static int GetCurrentNodeID()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentNodeID");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static int GetCurrentNodeIndex()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentNodeIndex");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static string GetCurrentNodeText(DialogLanguages language = DialogLanguages.English,
            Gender gender = Gender.Male)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentNodeText");
            NWNCore.NativeFunctions.nwnxPushInt((int)gender);
            NWNCore.NativeFunctions.nwnxPushInt((int)language);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        public static void SetCurrentNodeText(string text, DialogLanguages language = DialogLanguages.English,
            Gender gender = Gender.Male)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCurrentNodeText");
            NWNCore.NativeFunctions.nwnxPushInt((int)gender);
            NWNCore.NativeFunctions.nwnxPushInt((int)language);
            NWNCore.NativeFunctions.nwnxPushString(text);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void End(uint player)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "End");
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }
    }
}