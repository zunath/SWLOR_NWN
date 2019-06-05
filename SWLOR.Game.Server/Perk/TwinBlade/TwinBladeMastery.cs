using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Perk.TwinBlade
{
    public class TwinBladeMastery : IPerkHandler
    {
        public PerkType PerkType => PerkType.TwinBladeMastery;

        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellTier)
        {
            return string.Empty;
        }
        
        public int FPCost(NWPlayer oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellTier)
        {
        }

        public void OnPurchased(NWPlayer oPC, int newLevel)
        {
            ApplyFeatChanges(oPC, null);
        }

        public void OnRemoved(NWPlayer oPC)
        {
            RemoveFeats(oPC);
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.TwinBlade) return;
            ApplyFeatChanges(oPC, null);
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.TwinBlade) return;
            ApplyFeatChanges(oPC, oItem);
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        private void RemoveFeats(NWPlayer oPC)
        {
            NWNXCreature.RemoveFeat(oPC, _.FEAT_TWO_WEAPON_FIGHTING);
            NWNXCreature.RemoveFeat(oPC, _.FEAT_AMBIDEXTERITY);
            NWNXCreature.RemoveFeat(oPC, _.FEAT_IMPROVED_TWO_WEAPON_FIGHTING);
        }

        private void ApplyFeatChanges(NWPlayer oPC, NWItem unequippedItem)
        {
            NWItem equipped = unequippedItem ?? oPC.RightHand;

            if (Equals(equipped, unequippedItem) || equipped.CustomItemType != CustomItemType.TwinBlade)
            {
                RemoveFeats(oPC);
                return;
            }

            int perkLevel = PerkService.GetPCPerkLevel(oPC, PerkType.TwinBladeMastery);
            NWNXCreature.AddFeat(oPC, _.FEAT_TWO_WEAPON_FIGHTING);

            if (perkLevel >= 2)
            {
                NWNXCreature.AddFeat(oPC, _.FEAT_AMBIDEXTERITY);
            }
            if (perkLevel >= 3)
            {
                NWNXCreature.AddFeat(oPC, _.FEAT_IMPROVED_TWO_WEAPON_FIGHTING);
            }
        }

        public bool IsHostile()
        {
            return false;
        }

        public void OnConcentrationTick(NWPlayer player, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
