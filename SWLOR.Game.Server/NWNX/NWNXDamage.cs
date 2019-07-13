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
            if (oOwner == null) oOwner = new NWObject(NWGameObject.OBJECT_INVALID);

            string sFunc = "SetEventScript";

            NWNX_PushArgumentObject(NWNX_Damage, sFunc, oOwner);
            NWNX_PushArgumentString(NWNX_Damage, sFunc, script);
            NWNX_PushArgumentString(NWNX_Damage, sFunc, "DAMAGE");

            NWNX_CallFunction(NWNX_Damage, sFunc);
        }

        /// <summary>
        /// Get Damage Event Data (to use only on Damage Event Script)
        /// </summary>
        /// <returns></returns>
        public static DamageEventData GetDamageEventData()
        {
            string sFunc = "GetDamageEventData";
            DamageEventData data = new DamageEventData();

            NWNX_CallFunction(NWNX_Damage, sFunc);

            data.Damager = NWNX_GetReturnValueObject(NWNX_Damage, sFunc);
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
        public static void SetDamageEventData(DamageEventData data)
        {
            string sFunc = "SetDamageEventData";

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

        /// <summary>
        /// Set Attack Event Script
        /// If oOwner is OBJECT_INVALID, this sets the script globally for all creatures
        /// If oOwner is valid, it will set it only for that creature.
        /// </summary>
        /// <param name="sScript"></param>
        /// <param name="oOwner"></param>
        public static void SetAttackEventScript(string sScript, NWObject oOwner = null)
        {
            if (oOwner == null) oOwner = new NWGameObject();

            string sFunc = "SetEventScript";

            NWNX_PushArgumentObject(NWNX_Damage, sFunc, oOwner);
            NWNX_PushArgumentString(NWNX_Damage, sFunc, sScript);
            NWNX_PushArgumentString(NWNX_Damage, sFunc, "ATTACK");

            NWNX_CallFunction(NWNX_Damage, sFunc);
        }

        /// <summary>
        /// Get Attack Event Data (to use only on Attack Event Script)
        /// </summary>
        /// <returns></returns>
        public static AttackEventData GetAttackEventData()
        {
            string sFunc = "GetAttackEventData";
            AttackEventData data = new AttackEventData();

            NWNX_CallFunction(NWNX_Damage, sFunc);

            data.Target = NWNX_GetReturnValueObject(NWNX_Damage, sFunc);
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
            data.AttackNumber = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.AttackResult = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.AttackType = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.SneakAttack = NWNX_GetReturnValueInt(NWNX_Damage, sFunc);

            return data;
        }

        /// <summary>
        /// Set Attack Event Data (to use only on Attack Event Script)
        /// </summary>
        /// <param name="data"></param>
        public static void SetAttackEventData(AttackEventData data)
        {
            string sFunc = "SetAttackEventData";

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

        /// <summary>
        /// Deal damage to target - permits multiple damage types and checks enhancement bonus for overcoming DR
        /// </summary>
        /// <param name="data"></param>
        /// <param name="oTarget"></param>
        /// <param name="oSource"></param>
        public static void DealDamage(DamageData data, NWObject oTarget, NWObject oSource)
        {
            string sFunc = "DealDamage";

            NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Power);
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
            NWNX_PushArgumentObject(NWNX_Damage, sFunc, oTarget);
            NWNX_PushArgumentObject(NWNX_Damage, sFunc, oSource);

            NWNX_CallFunction(NWNX_Damage, sFunc);
        }

}
}
