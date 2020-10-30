using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class Dialog
    {
        private const string PLUGIN_NAME = "NWNX_Dialog";

        public static int GetCurrentNodeType()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentNodeType");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetCurrentScriptType()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentScriptType");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetCurrentNodeID()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentNodeID");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetCurrentNodeIndex()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentNodeIndex");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static string GetCurrentNodeText(DialogLanguages language = DialogLanguages.English,
            Gender gender = Gender.Male)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentNodeText");
            Internal.NativeFunctions.nwnxPushInt((int)gender);
            Internal.NativeFunctions.nwnxPushInt((int)language);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        public static void SetCurrentNodeText(string text, DialogLanguages language = DialogLanguages.English,
            Gender gender = Gender.Male)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCurrentNodeText");
            Internal.NativeFunctions.nwnxPushInt((int)gender);
            Internal.NativeFunctions.nwnxPushInt((int)language);
            Internal.NativeFunctions.nwnxPushString(text);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void End(uint player)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "End");
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }
    }
}