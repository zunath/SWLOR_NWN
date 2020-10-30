
namespace SWLOR.Game.Server.Core.NWNX
{
    public static class NWNXCore
    {
        private static string NWNX_INTERNAL_BuildString(string pluginName, string functionName, string operation)
        {
            return "NWNXEE!ABIv2!" + pluginName + "!" + functionName + "!" + operation;
        }

        public static void NWNX_CallFunction(string pluginName, string functionName)
        {
            NWScript.NWScript.PlaySound(NWNX_INTERNAL_BuildString(pluginName, functionName, "CALL"));
        }

        public static void NWNX_PushArgumentInt(string pluginName, string functionName, int value)
        {
            NWScript.NWScript.SetLocalInt(NWScript.NWScript.OBJECT_INVALID, NWNX_INTERNAL_BuildString(pluginName, functionName, "PUSH"), value);
        }

        public static void NWNX_PushArgumentFloat(string pluginName, string functionName, float value)
        {
            NWScript.NWScript.SetLocalFloat(NWScript.NWScript.OBJECT_INVALID, NWNX_INTERNAL_BuildString(pluginName, functionName, "PUSH"), value);
        }

        public static void NWNX_PushArgumentObject(string pluginName, string functionName, uint value)
        {
            NWScript.NWScript.SetLocalObject(NWScript.NWScript.OBJECT_INVALID, NWNX_INTERNAL_BuildString(pluginName, functionName, "PUSH"), value);
        }

        public static void NWNX_PushArgumentString(string pluginName, string functionName, string value)
        {
            NWScript.NWScript.SetLocalString(NWScript.NWScript.OBJECT_INVALID, NWNX_INTERNAL_BuildString(pluginName, functionName, "PUSH"), value);
        }

        public static void NWNX_PushArgumentEffect(string pluginName, string functionName, Core.Effect value)
        {
            NWScript.NWScript.TagEffect(value, NWNX_INTERNAL_BuildString(pluginName, functionName, "PUSH"));
        }

        public static void NWNX_PushArgumentItemProperty(string pluginName, string functionName, Core.ItemProperty value)
        {
            NWScript.NWScript.TagItemProperty(value, NWNX_INTERNAL_BuildString(pluginName, functionName, "PUSH"));
        }

        public static int NWNX_GetReturnValueInt(string pluginName, string functionName)
        {
            return NWScript.NWScript.GetLocalInt(NWScript.NWScript.OBJECT_INVALID, NWNX_INTERNAL_BuildString(pluginName, functionName, "POP"));
        }

        public static float NWNX_GetReturnValueFloat(string pluginName, string functionName)
        {
            return NWScript.NWScript.GetLocalFloat(NWScript.NWScript.OBJECT_INVALID, NWNX_INTERNAL_BuildString(pluginName, functionName, "POP"));
        }

        public static uint NWNX_GetReturnValueObject(string pluginName, string functionName)
        {
            return NWScript.NWScript.GetLocalObject(NWScript.NWScript.OBJECT_INVALID, NWNX_INTERNAL_BuildString(pluginName, functionName, "POP"));
        }

        public static string NWNX_GetReturnValueString(string pluginName, string functionName)
        {
            return NWScript.NWScript.GetLocalString(NWScript.NWScript.OBJECT_INVALID, NWNX_INTERNAL_BuildString(pluginName, functionName, "POP"));
        }

        public static Core.Effect NWNX_GetReturnValueEffect(string pluginName, string functionName)
        {
            var e = NWScript.NWScript.EffectBlindness();
            return NWScript.NWScript.TagEffect(e, NWNX_INTERNAL_BuildString(pluginName, functionName, "POP"));
        }

        public static Core.ItemProperty NWNX_GetReturnValueItemProperty(string pluginName, string functionName)
        {
            var ip = NWScript.NWScript.ItemPropertyTrueSeeing();
            return NWScript.NWScript.TagItemProperty(ip, NWNX_INTERNAL_BuildString(pluginName, functionName, "POP"));
        }
    }
}
