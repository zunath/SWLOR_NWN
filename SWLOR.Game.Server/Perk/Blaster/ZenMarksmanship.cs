using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class ZenMarksmanship : IPerkHandler
    {
        public PerkType PerkType => PerkType.ZenMarksmanship;
        
        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return false;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return null;
        }

        public int FPCost(NWPlayer oPC, int baseFPCost, int spellFeatID)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime, int spellFeatID)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellFeatID)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID)
        {
        }

        public void OnPurchased(NWPlayer oPC, int newLevel)
        {
            ApplyFeatChanges(oPC, null);
        }

        public void OnRemoved(NWPlayer oPC)
        {
            NWNXCreature.RemoveFeat(oPC, _.FEAT_ZEN_ARCHERY);
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.BlasterPistol &&
                oItem.CustomItemType != CustomItemType.BlasterRifle) return;

            ApplyFeatChanges(oPC, null);
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.BlasterPistol &&
                oItem.CustomItemType != CustomItemType.BlasterRifle) return;

            ApplyFeatChanges(oPC, oItem);
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        private void ApplyFeatChanges(NWPlayer oPC, NWItem oItem)
        {
            NWItem equipped = oItem ?? oPC.RightHand;

            if (equipped.Equals(oItem) || 
                    (equipped.CustomItemType != CustomItemType.BlasterPistol && 
                     equipped.CustomItemType != CustomItemType.BlasterRifle))
            {
                NWNXCreature.RemoveFeat(oPC, _.FEAT_ZEN_ARCHERY);
                return;
            }

            NWNXCreature.AddFeat(oPC, _.FEAT_ZEN_ARCHERY);
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}
