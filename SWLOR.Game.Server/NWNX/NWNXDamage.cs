using NWN;
using SWLOR.Game.Server.GameObject;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXDamage
    {
        private const string NWNX_Damage = "NWNX_Damage";
        
        /// <summary>
        /// Set Damage Event Script
        /// If oOwner is OBJECT_INVALID, this sets the script globally for all creatures
        /// If oOwner is valid, it will set it only for that creature.
        /// </summary>
        /// <param name="script"></param>
        /// <param name="oOwner"></param>
        public static void SetDamageEventScript(string script, NWObject oOwner = null)
        {
            if(oOwner == null) oOwner = (new Object());

            string sFunc = "SetDamageEventScript";

            NWNX_PushArgumentObject(NWNX_Damage, sFunc, oOwner.Object);
            NWNX_PushArgumentString(NWNX_Damage, sFunc, script);

            NWNX_CallFunction(NWNX_Damage, sFunc);
        }

        /// <summary>
        /// Get Damage Event Data (to use only on Damage Event Script)
        /// </summary>
        /// <returns></returns>
        public static DamageData GetDamageEventData()
        {
            string sFunc = "GetEventData";
            DamageData data = new DamageData();

            NWNX_CallFunction(NWNX_Damage, sFunc);

            data.Damager = (NWNX_GetReturnValueObject(NWNX_Damage, sFunc));
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

        /// <summary>
        /// Set Damage Event Data (to use only on Damage Event Script)
        /// </summary>
        /// <param name="data"></param>
        public static void SetDamageEventData(DamageData data)
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
