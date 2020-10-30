using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class Damage
    {
        private const string PLUGIN_NAME = "NWNX_Damage";

        // Set Damage Event Script
        // If oOwner is OBJECT_INVALID, this sets the script globally for all creatures
        // If oOwner is valid, it will set it only for that creature.
        public static void SetDamageEventScript(string script, uint? oOwner = null)
        {
            if (oOwner == null) oOwner = NWScript.NWScript.OBJECT_INVALID;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEventScript");
            Internal.NativeFunctions.nwnxPushObject((uint)oOwner);
            Internal.NativeFunctions.nwnxPushString(script);
            Internal.NativeFunctions.nwnxPushString("DAMAGE");
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get Damage Event Data (to use only on Damage Event Script)
        public static DamageEventData GetDamageEventData()
        {
            var data = new DamageEventData();
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDamageEventData");
            Internal.NativeFunctions.nwnxCallFunction();
            data.Damager = Internal.NativeFunctions.nwnxPopObject();
            data.Bludgeoning = Internal.NativeFunctions.nwnxPopInt();
            data.Pierce = Internal.NativeFunctions.nwnxPopInt();
            data.Slash = Internal.NativeFunctions.nwnxPopInt();
            data.Magical = Internal.NativeFunctions.nwnxPopInt();
            data.Acid = Internal.NativeFunctions.nwnxPopInt();
            data.Cold = Internal.NativeFunctions.nwnxPopInt();
            data.Divine = Internal.NativeFunctions.nwnxPopInt();
            data.Electrical = Internal.NativeFunctions.nwnxPopInt();
            data.Fire = Internal.NativeFunctions.nwnxPopInt();
            data.Negative = Internal.NativeFunctions.nwnxPopInt();
            data.Positive = Internal.NativeFunctions.nwnxPopInt();
            data.Sonic = Internal.NativeFunctions.nwnxPopInt();
            data.Base = Internal.NativeFunctions.nwnxPopInt();
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
        public static void SetAttackEventScript(string script, uint? oOwner = null)
        {
            if (oOwner == null) oOwner = NWScript.NWScript.OBJECT_INVALID;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEventScript");
            Internal.NativeFunctions.nwnxPushObject((uint)oOwner);
            Internal.NativeFunctions.nwnxPushString(script);
            Internal.NativeFunctions.nwnxPushString("ATTACK");
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get Attack Event Data (to use only on Attack Event Script)
        public static AttackEventData GetAttackEventData()
        {
            var data = new AttackEventData();
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAttackEventData");
            Internal.NativeFunctions.nwnxCallFunction();
            data.Target = Internal.NativeFunctions.nwnxPopObject();
            data.Bludgeoning = Internal.NativeFunctions.nwnxPopInt();
            data.Pierce = Internal.NativeFunctions.nwnxPopInt();
            data.Slash = Internal.NativeFunctions.nwnxPopInt();
            data.Magical = Internal.NativeFunctions.nwnxPopInt();
            data.Acid = Internal.NativeFunctions.nwnxPopInt();
            data.Cold = Internal.NativeFunctions.nwnxPopInt();
            data.Divine = Internal.NativeFunctions.nwnxPopInt();
            data.Electrical = Internal.NativeFunctions.nwnxPopInt();
            data.Fire = Internal.NativeFunctions.nwnxPopInt();
            data.Negative = Internal.NativeFunctions.nwnxPopInt();
            data.Positive = Internal.NativeFunctions.nwnxPopInt();
            data.Sonic = Internal.NativeFunctions.nwnxPopInt();
            data.Base = Internal.NativeFunctions.nwnxPopInt();
            data.AttackNumber = Internal.NativeFunctions.nwnxPopInt();
            data.AttackResult = Internal.NativeFunctions.nwnxPopInt();
            data.AttackType = Internal.NativeFunctions.nwnxPopInt();
            data.SneakAttack = Internal.NativeFunctions.nwnxPopInt();
            return data;
        }

        // Set Attack Event Data (to use only on Attack Event Script)
        public static void SetAttackEventData(AttackEventData data)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAttackEventData");
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
        public static void DealDamage(DamageEventData data, uint oTarget, uint oSource, bool iRanged = false)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAttackEventData");
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DealDamage");
            Internal.NativeFunctions.nwnxPushInt(iRanged ? 1 : 0);
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
            Internal.NativeFunctions.nwnxPushObject(oTarget);
            Internal.NativeFunctions.nwnxPushObject(oSource);
            Internal.NativeFunctions.nwnxCallFunction();
        }
    }
}