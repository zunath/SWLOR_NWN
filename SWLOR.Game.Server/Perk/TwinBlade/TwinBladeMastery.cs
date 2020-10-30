using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.Legacy;


namespace SWLOR.Game.Server.Perk.TwinBlade
{
    public class TwinBladeMastery : IPerkHandler
    {
        public PerkType PerkType => PerkType.TwinBladeMastery;

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
            if (oItem.CustomItemType != CustomItemType.TwinBlade) return;
            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.TwinBlade) return;
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

        private void ApplyFeatChanges(NWCreature creature, NWItem unequippedItem)
        {
            var equipped = unequippedItem ?? creature.RightHand;

            if (Equals(equipped, unequippedItem) || equipped.CustomItemType != CustomItemType.TwinBlade)
            {
                RemoveFeats(creature);
                return;
            }

            var perkLevel = PerkService.GetCreaturePerkLevel(creature, PerkType.TwinBladeMastery);
            Creature.AddFeat(creature, Feat.TwoWeaponFighting);

            if (perkLevel >= 2)
            {
                Creature.AddFeat(creature, Feat.Ambidexterity);
            }
            if (perkLevel >= 3)
            {
                Creature.AddFeat(creature, Feat.TwoWeaponFighting);
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
