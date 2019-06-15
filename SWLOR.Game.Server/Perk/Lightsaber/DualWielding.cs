using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Lightsaber
{
    public class DualWielding : IPerkHandler
    {
        public PerkType PerkType => PerkType.LightsaberDualWielding;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            return string.Empty;
        }
        
        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
            ApplyFeatChanges(creature, null);
        }

        public void OnRemoved(NWCreature creature)
        {
            RemoveFeats(creature);
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Lightsaber &&
                oItem.GetLocalInt("LIGHTSABER") == FALSE) 
                return;

            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Lightsaber &&
                oItem.GetLocalInt("LIGHTSABER") == FALSE)
                return;

            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }


        private void RemoveFeats(NWCreature creature)
        {
            NWNXCreature.RemoveFeat(creature, FEAT_TWO_WEAPON_FIGHTING);
            NWNXCreature.RemoveFeat(creature, FEAT_AMBIDEXTERITY);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_TWO_WEAPON_FIGHTING);
        }


        private void ApplyFeatChanges(NWCreature creature, NWItem oItem)
        {
            NWItem mainEquipped = oItem ?? creature.RightHand;
            NWItem offEquipped = oItem ?? creature.LeftHand;

            // oItem was unequipped.
            if (Equals(mainEquipped, oItem) || Equals(offEquipped, oItem))
            {
                RemoveFeats(creature);
                return;
            }

            // Main or offhand was invalid (i.e not equipped)
            if (!mainEquipped.IsValid || !offEquipped.IsValid)
            {
                RemoveFeats(creature);
                return;
            }

            // Main or offhand is not acceptable item type.
            if ((mainEquipped.CustomItemType != CustomItemType.Lightsaber &&
                 mainEquipped.GetLocalInt("LIGHTSABER") == FALSE) ||
                (offEquipped.CustomItemType != CustomItemType.Lightsaber &&
                 offEquipped.GetLocalInt("LIGHTSABER") == FALSE))
            {
                RemoveFeats(creature);
                return;
            }


            int perkLevel = PerkService.GetCreaturePerkLevel(creature, PerkType.LightsaberDualWielding);
            NWNXCreature.AddFeat(creature, FEAT_TWO_WEAPON_FIGHTING);

            if (perkLevel >= 2)
            {
                NWNXCreature.AddFeat(creature, FEAT_AMBIDEXTERITY);
            }
            if (perkLevel >= 3)
            {
                NWNXCreature.AddFeat(creature, FEAT_IMPROVED_TWO_WEAPON_FIGHTING);
            }
        }

        public bool IsHostile()
        {
            return false;
        }

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
