using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class Appearance
    {
        private const string PLUGIN_NAME = "NWNX_Appearance";

        // Override oCreature's nType to nValue for oPlayer
        // - oCreature can be a PC
        //
        // nType = NWNX_APPEARANCE_TYPE_APPEARANCE
        // nValue = APPEARANCE_TYPE_* or -1 to remove
        //
        // nType = NWNX_APPEARANCE_TYPE_GENDER
        // nValue = GENDER_* or -1 to remove
        //
        // nType = NWNX_APPEARANCE_TYPE_HITPOINTS
        // nValue = 0-GetMaxHitPoints(oCreature) or -1 to remove
        // NOTE: This is visual only. Does not change the Examine Window health status
        //
        // nType = NWNX_APPEARANCE_TYPE_HAIR_COLOR
        // nType = NWNX_APPEARANCE_TYPE_SKIN_COLOR
        // nValue = 0-175 or -1 to remove
        //
        // nType = NWNX_APPEARANCE_TYPE_PHENOTYPE
        // nValue = PHENOTYPE_* or -1 to remove
        //
        // nType = NWNX_APPEARANCE_TYPE_HEAD_TYPE
        // nValue = 0-? or -1 to remove
        //
        // nType = NWNX_APPEARANCE_TYPE_SOUNDSET
        // nValue = See soundset.2da or -1 to remove
        //
        // nType = NWNX_APPEARANCE_TYPE_TAIL_TYPE
        // nValue = CREATURE_WING_TYPE_* or see wingmodel.2da, -1 to remove
        //
        // nType = NWNX_APPEARANCE_TYPE_WING_TYPE
        // nValue = CREATURE_TAIL_TYPE_* or see tailmodel.2da, -1 to remove
        //
        // nType = NWNX_APPEARANCE_TYPE_FOOTSTEP_SOUND
        // nValue = 0-17 or see footstepsounds.2da, -1 to remove
        //
        // nType = NWNX_APPEARANCE_TYPE_PORTRAIT
        // nValue = See portraits.2da, -1 to remove
        // NOTE: Does not change the Examine Window portrait
        public static void SetOverride(uint player, uint creature, OverrideType type, int value)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetOverride");
            Internal.NativeFunctions.nwnxPushInt(value);
            Internal.NativeFunctions.nwnxPushInt((int)type);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get oCreature's nValue of nType for oPlayer
        // - oCreature can be a PC
        // Returns -1 when not set
        public static int GetOverride(uint player, uint creature, OverrideType type)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetOverride");
            Internal.NativeFunctions.nwnxPushInt((int)type);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }
    }
}