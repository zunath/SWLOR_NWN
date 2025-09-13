
using SWLOR.NWN.API;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class NWNXCore
    {
        private const string NWNX_PUSH = "NWNXEE!ABIv2!X!Y!PUSH";
        private const string NWNX_POP = "NWNXEE!ABIv2!X!Y!POP";
        
        public static void NWNX_CallFunction(string pluginName, string functionName)
        {
            PlaySound("NWNXEE!ABIv2!" + pluginName + "!" + functionName + "!CALL");
        }

        public static void NWNX_PushArgumentInt(int value)
        {
            SetLocalInt(OBJECT_INVALID, NWNX_PUSH, value);
        }

        public static void NWNX_PushArgumentFloat(float value)
        {
            SetLocalFloat(OBJECT_INVALID, NWNX_PUSH, value);
        }

        public static void NWNX_PushArgumentObject(uint value)
        {
            SetLocalObject(OBJECT_INVALID, NWNX_PUSH, value);
        }

        public static void NWNX_PushArgumentString(string value)
        {
            SetLocalString(OBJECT_INVALID, NWNX_PUSH, value);
        }

        public static void NWNX_PushArgumentEffect(Effect value)
        {
            TagEffect(value, NWNX_PUSH);
        }

        public static void NWNX_PushArgumentItemProperty(ItemProperty value)
        {
            TagItemProperty(value, NWNX_PUSH);
        }

        public static int NWNX_GetReturnValueInt()
        {
            return GetLocalInt(OBJECT_INVALID, NWNX_POP);
        }

        public static float NWNX_GetReturnValueFloat()
        {
            return GetLocalFloat(OBJECT_INVALID, NWNX_POP);
        }

        public static uint NWNX_GetReturnValueObject()
        {
            return GetLocalObject(OBJECT_INVALID, NWNX_POP);
        }

        public static string NWNX_GetReturnValueString()
        {
            return GetLocalString(OBJECT_INVALID, NWNX_POP);
        }

        public static Effect NWNX_GetReturnValueEffect()
        {
            var e = EffectBlindness();
            return TagEffect(e, NWNX_POP);
        }

        public static ItemProperty NWNX_GetReturnValueItemProperty()
        {
            var ip = ItemPropertyTrueSeeing();
            return TagItemProperty(ip, NWNX_POP);
        }
    }
}
