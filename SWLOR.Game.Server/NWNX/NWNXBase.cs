using SWLOR.Game.Server.NWN.Contracts;
using Object = SWLOR.Game.Server.NWN.NWScript.Object;

namespace SWLOR.Game.Server.NWNX
{
    public abstract class NWNXBase
    {
        protected readonly INWScript _;

        protected NWNXBase(INWScript script)
        {
            _ = script;
        }

        protected void NWNX_CallFunction(string pluginName, string functionName)
        {
            NWNX_INTERNAL_CallFunction(pluginName, functionName);
        }

        protected void NWNX_PushArgumentInt(string pluginName, string functionName, int value)
        {
            NWNX_INTERNAL_PushArgument(pluginName, functionName, "0 " + _.IntToString(value));
        }

        protected void NWNX_PushArgumentFloat(string pluginName, string functionName, float value)
        {
            NWNX_INTERNAL_PushArgument(pluginName, functionName, "1 " + _.FloatToString(value));
        }

        protected void NWNX_PushArgumentObject(string pluginName, string functionName, Object value)
        {
            NWNX_INTERNAL_PushArgument(pluginName, functionName, "2 " + value.m_ObjId.ToString("X"));
        }

        protected void NWNX_PushArgumentString(string pluginName, string functionName, string value)
        {
            NWNX_INTERNAL_PushArgument(pluginName, functionName, "3 " + value);
        }

        protected int NWNX_GetReturnValueInt(string pluginName, string functionName)
        {
            return _.StringToInt(NWNX_INTERNAL_GetReturnValueString(pluginName, functionName, "0 "));
        }

        protected float NWNX_GetReturnValueFloat(string pluginName, string functionName)
        {
            return _.StringToFloat(NWNX_INTERNAL_GetReturnValueString(pluginName, functionName, "1 "));
        }

        protected Object NWNX_GetReturnValueObject(string pluginName, string functionName)
        {
            return NWNX_INTERNAL_GetReturnValueObject(pluginName, functionName, "2 ");
        }

        protected string NWNX_GetReturnValueString(string pluginName, string functionName)
        {
            return NWNX_INTERNAL_GetReturnValueString(pluginName, functionName, "3 ");
        }

        protected void NWNX_INTERNAL_CallFunction(string pluginName, string functionName)
        {
            _.SetLocalString(_.GetModule(), "NWNXEE!CALL_FUNCTION!" + pluginName + "!" + functionName, "1");
        }

        protected void NWNX_INTERNAL_PushArgument(string pluginName, string functionName, string value)
        {
            _.SetLocalString(_.GetModule(), "NWNXEE!PUSH_ARGUMENT!" + pluginName + "!" + functionName, value);
        }

        protected string NWNX_INTERNAL_GetReturnValueString(string pluginName, string functionName, string type)
        {
            return _.GetLocalString(_.GetModule(), "NWNXEE!GET_RETURN_VALUE!" + pluginName + "!" + functionName + "!" + type);
        }

        protected Object NWNX_INTERNAL_GetReturnValueObject(string pluginName, string functionName, string type)
        {
            return _.GetLocalObject(_.GetModule(), "NWNXEE!GET_RETURN_VALUE!" + pluginName + "!" + functionName + "!" + type);
        }

    }
}
