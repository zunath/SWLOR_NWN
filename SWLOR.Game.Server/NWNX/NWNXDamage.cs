using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.NWNX
{
    public class NWNXDamage : NWNXBase, INWNXDamage
    {
        const string NWNX_Damage = "NWNX_Damage";

        public NWNXDamage(INWScript script)
            : base(script)
        {

        }

        public void SetDamageEventScript(string script)
        {
            string sFunc = "SetDamageEventScript";

            NWNX_PushArgumentString(NWNX_Damage, sFunc, script);

            NWNX_CallFunction(NWNX_Damage, sFunc);
        }

        public DamageData GetDamageEventData()
        {
            string sFunc = "GetEventData";
            DamageData data = new DamageData();

            NWNX_CallFunction(NWNX_Damage, sFunc);

            data.Damager = NWObject.Wrap(NWNX_GetReturnValueObject(NWNX_Damage, sFunc));
            data.Bludgeoning = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Pierce = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Slash = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Magical = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Acid = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Cold = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Divine = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Electrical = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Fire = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Negative = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Positive = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Sonic = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Base = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);

            return data;
        }


        public void SetDamageEventData(DamageData data)
        {
            string sFunc = "SetEventData";

            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Base);
            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Sonic);
            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Positive);
            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Negative);
            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Fire);
            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Electrical);
            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Divine);
            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Cold);
            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Acid);
            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Magical);
            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Slash);
            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Pierce);
            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Bludgeoning);

            NWNX_CallFunction(NWNX_Damage, sFunc);
        }
}
}
