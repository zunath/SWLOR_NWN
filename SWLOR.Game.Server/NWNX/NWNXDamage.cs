using NWN;
using SWLOR.Game.Server.NWScript;

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
        public static void SetDamageEventScript(string script, NWGameObject oOwner = null)
        {
            if (oOwner == null) oOwner = new NWGameObject();

            string sFunc = "SetEventScript";

            NWNXCore.NWNX_PushArgumentObject(NWNX_Damage, sFunc, oOwner);
            NWNXCore.NWNX_PushArgumentString(NWNX_Damage, sFunc, script);
            NWNXCore.NWNX_PushArgumentString(NWNX_Damage, sFunc, "DAMAGE");

            NWNXCore.NWNX_CallFunction(NWNX_Damage, sFunc);
        }

        /// <summary>
        /// Get Damage Event Data (to use only on Damage Event Script)
        /// </summary>
        /// <returns></returns>
        public static DamageEventData GetDamageEventData()
        {
            string sFunc = "GetDamageEventData";
            DamageEventData data = new DamageEventData();

            NWNXCore.NWNX_CallFunction(NWNX_Damage, sFunc);

            data.Damager = NWNXCore.NWNX_GetReturnValueObject(NWNX_Damage, sFunc);
            data.Bludgeoning = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Pierce = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Slash = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Magical = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Acid = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Cold = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Divine = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Electrical = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Fire = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Negative = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Positive = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Sonic = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Base = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);

            return data;
        }

        /// <summary>
        /// Set Damage Event Data (to use only on Damage Event Script)
        /// </summary>
        /// <param name="data"></param>
        public static void SetDamageEventData(DamageEventData data)
        {
            string sFunc = "SetDamageEventData";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Base);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Sonic);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Positive);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Negative);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Fire);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Electrical);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Divine);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Cold);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Acid);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Magical);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Slash);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Pierce);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Bludgeoning);

            NWNXCore.NWNX_CallFunction(NWNX_Damage, sFunc);
        }

        /// <summary>
        /// Set Attack Event Script
        /// If oOwner is OBJECT_INVALID, this sets the script globally for all creatures
        /// If oOwner is valid, it will set it only for that creature.
        /// </summary>
        /// <param name="sScript"></param>
        /// <param name="oOwner"></param>
        public static void SetAttackEventScript(string sScript, NWGameObject oOwner = null)
        {
            if (oOwner == null) oOwner = new NWGameObject();

            string sFunc = "SetEventScript";

            NWNXCore.NWNX_PushArgumentObject(NWNX_Damage, sFunc, oOwner);
            NWNXCore.NWNX_PushArgumentString(NWNX_Damage, sFunc, sScript);
            NWNXCore.NWNX_PushArgumentString(NWNX_Damage, sFunc, "ATTACK");

            NWNXCore.NWNX_CallFunction(NWNX_Damage, sFunc);
        }

        /// <summary>
        /// Get Attack Event Data (to use only on Attack Event Script)
        /// </summary>
        /// <returns></returns>
        public static AttackEventData GetAttackEventData()
        {
            string sFunc = "GetAttackEventData";
            AttackEventData data = new AttackEventData();

            NWNXCore.NWNX_CallFunction(NWNX_Damage, sFunc);

            data.Target = NWNXCore.NWNX_GetReturnValueObject(NWNX_Damage, sFunc);
            data.Bludgeoning = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Pierce = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Slash = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Magical = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Acid = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Cold = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Divine = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Electrical = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Fire = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Negative = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Positive = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Sonic = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.Base = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.AttackNumber = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.AttackResult = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.AttackType = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);
            data.SneakAttack = NWNXCore.NWNX_GetReturnValueInt(NWNX_Damage, sFunc);

            return data;
        }

        /// <summary>
        /// Set Attack Event Data (to use only on Attack Event Script)
        /// </summary>
        /// <param name="data"></param>
        public static void SetAttackEventData(AttackEventData data)
        {
            string sFunc = "SetAttackEventData";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Base);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Sonic);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Positive);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Negative);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Fire);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Electrical);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Divine);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Cold);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Acid);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Magical);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Slash);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Pierce);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Bludgeoning);

            NWNXCore.NWNX_CallFunction(NWNX_Damage, sFunc);
        }

        /// <summary>
        /// Deal damage to target - permits multiple damage types and checks enhancement bonus for overcoming DR
        /// </summary>
        /// <param name="data"></param>
        /// <param name="oTarget"></param>
        /// <param name="oSource"></param>
        public static void DealDamage(DamageData data, NWGameObject oTarget, NWGameObject oSource)
        {
            string sFunc = "DealDamage";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Power);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Sonic);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Positive);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Negative);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Fire);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Electrical);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Divine);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Cold);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Acid);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Magical);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Slash);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Pierce);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Damage, sFunc, data.Bludgeoning);
            NWNXCore.NWNX_PushArgumentObject(NWNX_Damage, sFunc, oTarget);
            NWNXCore.NWNX_PushArgumentObject(NWNX_Damage, sFunc, oSource);

            NWNXCore.NWNX_CallFunction(NWNX_Damage, sFunc);
        }

}
}
