using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public class RenamePlugin
    {
        private const string PLUGIN_NAME = "NWNX_Rename";

        public static void SetPCNameOverride(uint target, string newName, string prefix, string suffix,
            NameOverrideType playerNameState = NameOverrideType.Default, uint? observer = null)
        {
            if (observer == null) observer = NWScript.NWScript.OBJECT_INVALID;
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPCNameOverride");
            NWNCore.NativeFunctions.nwnxPushObject((uint)observer);
            NWNCore.NativeFunctions.nwnxPushInt((int)playerNameState);
            NWNCore.NativeFunctions.nwnxPushString(suffix);
            NWNCore.NativeFunctions.nwnxPushString(prefix);
            NWNCore.NativeFunctions.nwnxPushString(newName);
            NWNCore.NativeFunctions.nwnxPushObject(target);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static string GetPCNameOverride(uint target, uint? observer = null)
        {
            if (observer == null) observer = NWScript.NWScript.OBJECT_INVALID;
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPCNameOverride");
            NWNCore.NativeFunctions.nwnxPushObject((uint)observer);
            NWNCore.NativeFunctions.nwnxPushObject(target);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        public static void ClearPCNameOverride(uint target, uint? observer = null, bool clearAll = false)
        {
            if (observer == null) observer = NWScript.NWScript.OBJECT_INVALID;
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ClearPCNameOverride");
            NWNCore.NativeFunctions.nwnxPushInt(clearAll ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject((uint)observer);
            NWNCore.NativeFunctions.nwnxPushObject(target);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }
    }
}