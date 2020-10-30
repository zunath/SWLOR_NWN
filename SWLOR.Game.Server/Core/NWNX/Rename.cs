using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public class Rename
    {
        private const string PLUGIN_NAME = "NWNX_Rename";

        public static void SetPCNameOverride(uint target, string newName, string prefix, string suffix,
            NameOverrideType playerNameState = NameOverrideType.Default, uint? observer = null)
        {
            if (observer == null) observer = NWScript.NWScript.OBJECT_INVALID;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPCNameOverride");
            Internal.NativeFunctions.nwnxPushObject((uint)observer);
            Internal.NativeFunctions.nwnxPushInt((int)playerNameState);
            Internal.NativeFunctions.nwnxPushString(suffix);
            Internal.NativeFunctions.nwnxPushString(prefix);
            Internal.NativeFunctions.nwnxPushString(newName);
            Internal.NativeFunctions.nwnxPushObject(target);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static string GetPCNameOverride(uint target, uint? observer = null)
        {
            if (observer == null) observer = NWScript.NWScript.OBJECT_INVALID;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPCNameOverride");
            Internal.NativeFunctions.nwnxPushObject((uint)observer);
            Internal.NativeFunctions.nwnxPushObject(target);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        public static void ClearPCNameOverride(uint target, uint? observer = null, bool clearAll = false)
        {
            if (observer == null) observer = NWScript.NWScript.OBJECT_INVALID;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ClearPCNameOverride");
            Internal.NativeFunctions.nwnxPushInt(clearAll ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject((uint)observer);
            Internal.NativeFunctions.nwnxPushObject(target);
            Internal.NativeFunctions.nwnxCallFunction();
        }
    }
}