using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class ELCPlugin
    {
        private const string PLUGIN_NAME = "NWNX_ELC";

        public static void SetELCScript(string script)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetELCScript");
            NWNCore.NativeFunctions.nwnxPushString(script);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void EnableCustomELCCheck(bool enabled)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "EnableCustomELCCheck");
            NWNCore.NativeFunctions.nwnxPushInt(enabled ? 1 : 0);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SkipValidationFailure()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SkipValidationFailure");
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static ElcFailureTypes GetValidationFailureType()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureType");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return (ElcFailureTypes)NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static ElcFailureSubTypes GetValidationFailureSubType()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureSubType");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return (ElcFailureSubTypes)NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static int GetValidationFailureMessageStrRef()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureMessageStrRef");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetValidationFailureMessageStrRef(int strRef)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetValidationFailureMessageStrRef");
            NWNCore.NativeFunctions.nwnxPushInt(strRef);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static uint GetValidationFailureItem()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureItem");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopObject();
        }

        public static int GetValidationFailureLevel()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureLevel");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static int GetValidationFailureSkillID()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureSkillID");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static int GetValidationFailureFeatID()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureFeatID");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static int GetValidationFailureSpellID()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureSpellID");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }
    }
}