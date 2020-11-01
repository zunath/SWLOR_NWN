using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;

namespace SWLOR.Game.Server.Legacy.Perk.OneHanded
{
    public class DualWielding : IPerkHandler
    {
        public PerkType PerkType => PerkType.OneHandedDualWielding;

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
            if (oItem.CustomItemType != CustomItemType.Vibroblade &&
                oItem.CustomItemType != CustomItemType.Baton &&
                oItem.CustomItemType != CustomItemType.FinesseVibroblade)
            {
                return;
            }

            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Vibroblade &&
                oItem.CustomItemType != CustomItemType.Baton &&
                oItem.CustomItemType != CustomItemType.FinesseVibroblade)
            {
                return;
            }
            
            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }


        private void RemoveFeats(NWCreature creature)
        {
            Creature.RemoveFeat(creature, Feat.TwoWeaponFighting);
            Creature.RemoveFeat(creature, Feat.Ambidexterity);
            Creature.RemoveFeat(creature, Feat.ImprovedTwoWeaponFighting);
        }

        private void ApplyFeatChanges(NWCreature creature, NWItem oItem)
        {
            var mainEquipped = oItem ?? creature.RightHand;
            var offEquipped = oItem ?? creature.LeftHand;
            
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
            if (mainEquipped.CustomItemType != CustomItemType.Vibroblade &&
                mainEquipped.CustomItemType != CustomItemType.Baton &&
                mainEquipped.CustomItemType != CustomItemType.FinesseVibroblade ||
                offEquipped.CustomItemType != CustomItemType.Vibroblade && 
                offEquipped.CustomItemType != CustomItemType.Baton && 
                offEquipped.CustomItemType != CustomItemType.FinesseVibroblade)
            {
                RemoveFeats(creature);
                return;
            }


            var perkLevel = PerkService.GetCreaturePerkLevel(creature, PerkType.OneHandedDualWielding);
            Creature.AddFeat(creature, Feat.TwoWeaponFighting);

            if (perkLevel >= 2)
            {
                Creature.AddFeat(creature, Feat.Ambidexterity);
            }
            if (perkLevel >= 3)
            {
                Creature.AddFeat(creature, Feat.ImprovedTwoWeaponFighting);
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
