using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class ELC
    {
        private const string PLUGIN_NAME = "NWNX_ELC";

        public static void SetELCScript(string script)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetELCScript");
            Internal.NativeFunctions.nwnxPushString(script);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void EnableCustomELCCheck(bool enabled)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "EnableCustomELCCheck");
            Internal.NativeFunctions.nwnxPushInt(enabled ? 1 : 0);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SkipValidationFailure()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SkipValidationFailure");
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static ElcFailureTypes GetValidationFailureType()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureType");
            Internal.NativeFunctions.nwnxCallFunction();
            return (ElcFailureTypes)Internal.NativeFunctions.nwnxPopInt();
        }

        public static ElcFailureSubTypes GetValidationFailureSubType()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureSubType");
            Internal.NativeFunctions.nwnxCallFunction();
            return (ElcFailureSubTypes)Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetValidationFailureMessageStrRef()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureMessageStrRef");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void SetValidationFailureMessageStrRef(int strRef)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetValidationFailureMessageStrRef");
            Internal.NativeFunctions.nwnxPushInt(strRef);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static uint GetValidationFailureItem()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureItem");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopObject();
        }

        public static int GetValidationFailureLevel()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureLevel");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetValidationFailureSkillID()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureSkillID");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetValidationFailureFeatID()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureFeatID");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetValidationFailureSpellID()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetValidationFailureSpellID");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }
    }
}