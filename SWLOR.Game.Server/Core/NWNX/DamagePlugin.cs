using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class DamagePlugin
    {
        private const string PLUGIN_NAME = "NWNX_Damage";

        // Set Damage Event Script
        // If oOwner is OBJECT_INVALID, this sets the script globally for all creatures
        // If oOwner is valid, it will set it only for that creature.
        public static void SetDamageEventScript(string script, uint oOwner = OBJECT_INVALID)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEventScript");
            NWNCore.NativeFunctions.nwnxPushObject(oOwner);
            NWNCore.NativeFunctions.nwnxPushString(script);
            NWNCore.NativeFunctions.nwnxPushString("DAMAGE");

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get Damage Event Data (to use only on Damage Event Script)
        public static DamageEventData GetDamageEventData()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDamageEventData");
            NWNCore.NativeFunctions.nwnxCallFunction();

            var data = new DamageEventData
            {
                Damager = NWNCore.NativeFunctions.nwnxPopObject(),
                Bludgeoning = NWNCore.NativeFunctions.nwnxPopInt(),
                Pierce = NWNCore.NativeFunctions.nwnxPopInt(),
                Slash = NWNCore.NativeFunctions.nwnxPopInt(),
                Magical = NWNCore.NativeFunctions.nwnxPopInt(),
                Acid = NWNCore.NativeFunctions.nwnxPopInt(),
                Cold = NWNCore.NativeFunctions.nwnxPopInt(),
                Divine = NWNCore.NativeFunctions.nwnxPopInt(),
                Electrical = NWNCore.NativeFunctions.nwnxPopInt(),
                Fire = NWNCore.NativeFunctions.nwnxPopInt(),
                Negative = NWNCore.NativeFunctions.nwnxPopInt(),
                Positive = NWNCore.NativeFunctions.nwnxPopInt(),
                Sonic = NWNCore.NativeFunctions.nwnxPopInt(),
                Base = NWNCore.NativeFunctions.nwnxPopInt()
            };

            return data;
        }

        // Set Damage Event Data (to use only on Damage Event Script)
        public static void SetDamageEventData(DamageEventData data)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDamageEventData");
            NWNCore.NativeFunctions.nwnxPushInt(data.Base);
            NWNCore.NativeFunctions.nwnxPushInt(data.Sonic);
            NWNCore.NativeFunctions.nwnxPushInt(data.Positive);
            NWNCore.NativeFunctions.nwnxPushInt(data.Negative);
            NWNCore.NativeFunctions.nwnxPushInt(data.Fire);
            NWNCore.NativeFunctions.nwnxPushInt(data.Electrical);
            NWNCore.NativeFunctions.nwnxPushInt(data.Divine);
            NWNCore.NativeFunctions.nwnxPushInt(data.Cold);
            NWNCore.NativeFunctions.nwnxPushInt(data.Acid);
            NWNCore.NativeFunctions.nwnxPushInt(data.Magical);
            NWNCore.NativeFunctions.nwnxPushInt(data.Slash);
            NWNCore.NativeFunctions.nwnxPushInt(data.Pierce);
            NWNCore.NativeFunctions.nwnxPushInt(data.Bludgeoning);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Set Attack Event Script
        // If oOwner is OBJECT_INVALID, this sets the script globally for all creatures
        // If oOwner is valid, it will set it only for that creature.
        public static void SetAttackEventScript(string script, uint oOwner = OBJECT_INVALID)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEventScript");
            NWNCore.NativeFunctions.nwnxPushObject(oOwner);
            NWNCore.NativeFunctions.nwnxPushString(script);
            NWNCore.NativeFunctions.nwnxPushString("ATTACK");
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get Attack Event Data (to use only on Attack Event Script)
        public static AttackEventData GetAttackEventData()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAttackEventData");
            NWNCore.NativeFunctions.nwnxCallFunction();

            var data = new AttackEventData
            {
                Target = NWNCore.NativeFunctions.nwnxPopObject(),
                Bludgeoning = NWNCore.NativeFunctions.nwnxPopInt(),
                Pierce = NWNCore.NativeFunctions.nwnxPopInt(),
                Slash = NWNCore.NativeFunctions.nwnxPopInt(),
                Magical = NWNCore.NativeFunctions.nwnxPopInt(),
                Acid = NWNCore.NativeFunctions.nwnxPopInt(),
                Cold = NWNCore.NativeFunctions.nwnxPopInt(),
                Divine = NWNCore.NativeFunctions.nwnxPopInt(),
                Electrical = NWNCore.NativeFunctions.nwnxPopInt(),
                Fire = NWNCore.NativeFunctions.nwnxPopInt(),
                Negative = NWNCore.NativeFunctions.nwnxPopInt(),
                Positive = NWNCore.NativeFunctions.nwnxPopInt(),
                Sonic = NWNCore.NativeFunctions.nwnxPopInt(),
                Base = NWNCore.NativeFunctions.nwnxPopInt(),
                AttackNumber = NWNCore.NativeFunctions.nwnxPopInt(),
                AttackResult = NWNCore.NativeFunctions.nwnxPopInt(),
                WeaponAttackType = NWNCore.NativeFunctions.nwnxPopInt(),
                SneakAttack = NWNCore.NativeFunctions.nwnxPopInt(),
                IsKillingBlow = NWNCore.NativeFunctions.nwnxPopInt() == 1,
                AttackType = NWNCore.NativeFunctions.nwnxPopInt()
            };

            return data;
        }

        // Set Attack Event Data (to use only on Attack Event Script)
        public static void SetAttackEventData(AttackEventData data)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAttackEventData");

            NWNCore.NativeFunctions.nwnxPushInt(data.AttackResult);
            NWNCore.NativeFunctions.nwnxPushInt(data.Base);
            NWNCore.NativeFunctions.nwnxPushInt(data.Sonic);
            NWNCore.NativeFunctions.nwnxPushInt(data.Positive);
            NWNCore.NativeFunctions.nwnxPushInt(data.Negative);
            NWNCore.NativeFunctions.nwnxPushInt(data.Fire);
            NWNCore.NativeFunctions.nwnxPushInt(data.Electrical);
            NWNCore.NativeFunctions.nwnxPushInt(data.Divine);
            NWNCore.NativeFunctions.nwnxPushInt(data.Cold);
            NWNCore.NativeFunctions.nwnxPushInt(data.Acid);
            NWNCore.NativeFunctions.nwnxPushInt(data.Magical);
            NWNCore.NativeFunctions.nwnxPushInt(data.Slash);
            NWNCore.NativeFunctions.nwnxPushInt(data.Pierce);
            NWNCore.NativeFunctions.nwnxPushInt(data.Bludgeoning);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Deal damage to target - permits multiple damage types and checks enhancement bonus for overcoming DR
        public static void DealDamage(DamageData data, uint oTarget, uint oSource, bool iRanged = false)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DealDamage");
            
            NWNCore.NativeFunctions.nwnxPushInt(iRanged ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(data.Power);
            NWNCore.NativeFunctions.nwnxPushInt(data.Sonic);
            NWNCore.NativeFunctions.nwnxPushInt(data.Positive);
            NWNCore.NativeFunctions.nwnxPushInt(data.Negative);
            NWNCore.NativeFunctions.nwnxPushInt(data.Fire);
            NWNCore.NativeFunctions.nwnxPushInt(data.Electrical);
            NWNCore.NativeFunctions.nwnxPushInt(data.Divine);
            NWNCore.NativeFunctions.nwnxPushInt(data.Cold);
            NWNCore.NativeFunctions.nwnxPushInt(data.Acid);
            NWNCore.NativeFunctions.nwnxPushInt(data.Magical);
            NWNCore.NativeFunctions.nwnxPushInt(data.Slash);
            NWNCore.NativeFunctions.nwnxPushInt(data.Pierce);
            NWNCore.NativeFunctions.nwnxPushInt(data.Bludgeoning);
            NWNCore.NativeFunctions.nwnxPushObject(oTarget);
            NWNCore.NativeFunctions.nwnxPushObject(oSource);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }
    }
}