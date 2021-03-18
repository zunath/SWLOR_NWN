using SWLOR.Game.Server.Core.NWNX.Enum;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class Damage
    {
        private const string PLUGIN_NAME = "NWNX_Damage";

        // Set Damage Event Script
        // If oOwner is OBJECT_INVALID, this sets the script globally for all creatures
        // If oOwner is valid, it will set it only for that creature.
        public static void SetDamageEventScript(string script, uint oOwner = OBJECT_INVALID)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEventScript");
            Internal.NativeFunctions.nwnxPushObject(oOwner);
            Internal.NativeFunctions.nwnxPushString(script);
            Internal.NativeFunctions.nwnxPushString("DAMAGE");

            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get Damage Event Data (to use only on Damage Event Script)
        public static DamageEventData GetDamageEventData()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDamageEventData");
            Internal.NativeFunctions.nwnxCallFunction();

            var data = new DamageEventData
            {
                Damager = Internal.NativeFunctions.nwnxPopObject(),
                Bludgeoning = Internal.NativeFunctions.nwnxPopInt(),
                Pierce = Internal.NativeFunctions.nwnxPopInt(),
                Slash = Internal.NativeFunctions.nwnxPopInt(),
                Magical = Internal.NativeFunctions.nwnxPopInt(),
                Acid = Internal.NativeFunctions.nwnxPopInt(),
                Cold = Internal.NativeFunctions.nwnxPopInt(),
                Divine = Internal.NativeFunctions.nwnxPopInt(),
                Electrical = Internal.NativeFunctions.nwnxPopInt(),
                Fire = Internal.NativeFunctions.nwnxPopInt(),
                Negative = Internal.NativeFunctions.nwnxPopInt(),
                Positive = Internal.NativeFunctions.nwnxPopInt(),
                Sonic = Internal.NativeFunctions.nwnxPopInt(),
                Base = Internal.NativeFunctions.nwnxPopInt()
            };

            return data;
        }

        // Set Damage Event Data (to use only on Damage Event Script)
        public static void SetDamageEventData(DamageEventData data)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDamageEventData");
            Internal.NativeFunctions.nwnxPushInt(data.Base);
            Internal.NativeFunctions.nwnxPushInt(data.Sonic);
            Internal.NativeFunctions.nwnxPushInt(data.Positive);
            Internal.NativeFunctions.nwnxPushInt(data.Negative);
            Internal.NativeFunctions.nwnxPushInt(data.Fire);
            Internal.NativeFunctions.nwnxPushInt(data.Electrical);
            Internal.NativeFunctions.nwnxPushInt(data.Divine);
            Internal.NativeFunctions.nwnxPushInt(data.Cold);
            Internal.NativeFunctions.nwnxPushInt(data.Acid);
            Internal.NativeFunctions.nwnxPushInt(data.Magical);
            Internal.NativeFunctions.nwnxPushInt(data.Slash);
            Internal.NativeFunctions.nwnxPushInt(data.Pierce);
            Internal.NativeFunctions.nwnxPushInt(data.Bludgeoning);

            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Set Attack Event Script
        // If oOwner is OBJECT_INVALID, this sets the script globally for all creatures
        // If oOwner is valid, it will set it only for that creature.
        public static void SetAttackEventScript(string script, uint oOwner = OBJECT_INVALID)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEventScript");
            Internal.NativeFunctions.nwnxPushObject(oOwner);
            Internal.NativeFunctions.nwnxPushString(script);
            Internal.NativeFunctions.nwnxPushString("ATTACK");
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get Attack Event Data (to use only on Attack Event Script)
        public static AttackEventData GetAttackEventData()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAttackEventData");
            Internal.NativeFunctions.nwnxCallFunction();

            var data = new AttackEventData
            {
                Target = Internal.NativeFunctions.nwnxPopObject(),
                Bludgeoning = Internal.NativeFunctions.nwnxPopInt(),
                Pierce = Internal.NativeFunctions.nwnxPopInt(),
                Slash = Internal.NativeFunctions.nwnxPopInt(),
                Magical = Internal.NativeFunctions.nwnxPopInt(),
                Acid = Internal.NativeFunctions.nwnxPopInt(),
                Cold = Internal.NativeFunctions.nwnxPopInt(),
                Divine = Internal.NativeFunctions.nwnxPopInt(),
                Electrical = Internal.NativeFunctions.nwnxPopInt(),
                Fire = Internal.NativeFunctions.nwnxPopInt(),
                Negative = Internal.NativeFunctions.nwnxPopInt(),
                Positive = Internal.NativeFunctions.nwnxPopInt(),
                Sonic = Internal.NativeFunctions.nwnxPopInt(),
                Base = Internal.NativeFunctions.nwnxPopInt(),
                AttackNumber = Internal.NativeFunctions.nwnxPopInt(),
                AttackResult = Internal.NativeFunctions.nwnxPopInt(),
                WeaponAttackType = Internal.NativeFunctions.nwnxPopInt(),
                SneakAttack = Internal.NativeFunctions.nwnxPopInt(),
                IsKillingBlow = Internal.NativeFunctions.nwnxPopInt() == 1,
                AttackType = Internal.NativeFunctions.nwnxPopInt()
            };

            return data;
        }

        // Set Attack Event Data (to use only on Attack Event Script)
        public static void SetAttackEventData(AttackEventData data)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAttackEventData");

            Internal.NativeFunctions.nwnxPushInt(data.AttackResult);
            Internal.NativeFunctions.nwnxPushInt(data.Base);
            Internal.NativeFunctions.nwnxPushInt(data.Sonic);
            Internal.NativeFunctions.nwnxPushInt(data.Positive);
            Internal.NativeFunctions.nwnxPushInt(data.Negative);
            Internal.NativeFunctions.nwnxPushInt(data.Fire);
            Internal.NativeFunctions.nwnxPushInt(data.Electrical);
            Internal.NativeFunctions.nwnxPushInt(data.Divine);
            Internal.NativeFunctions.nwnxPushInt(data.Cold);
            Internal.NativeFunctions.nwnxPushInt(data.Acid);
            Internal.NativeFunctions.nwnxPushInt(data.Magical);
            Internal.NativeFunctions.nwnxPushInt(data.Slash);
            Internal.NativeFunctions.nwnxPushInt(data.Pierce);
            Internal.NativeFunctions.nwnxPushInt(data.Bludgeoning);

            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Deal damage to target - permits multiple damage types and checks enhancement bonus for overcoming DR
        public static void DealDamage(DamageData data, uint oTarget, uint oSource, bool iRanged = false)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DealDamage");
            
            Internal.NativeFunctions.nwnxPushInt(iRanged ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(data.Power);
            Internal.NativeFunctions.nwnxPushInt(data.Sonic);
            Internal.NativeFunctions.nwnxPushInt(data.Positive);
            Internal.NativeFunctions.nwnxPushInt(data.Negative);
            Internal.NativeFunctions.nwnxPushInt(data.Fire);
            Internal.NativeFunctions.nwnxPushInt(data.Electrical);
            Internal.NativeFunctions.nwnxPushInt(data.Divine);
            Internal.NativeFunctions.nwnxPushInt(data.Cold);
            Internal.NativeFunctions.nwnxPushInt(data.Acid);
            Internal.NativeFunctions.nwnxPushInt(data.Magical);
            Internal.NativeFunctions.nwnxPushInt(data.Slash);
            Internal.NativeFunctions.nwnxPushInt(data.Pierce);
            Internal.NativeFunctions.nwnxPushInt(data.Bludgeoning);
            Internal.NativeFunctions.nwnxPushObject(oTarget);
            Internal.NativeFunctions.nwnxPushObject(oSource);

            Internal.NativeFunctions.nwnxCallFunction();
        }
    }
}