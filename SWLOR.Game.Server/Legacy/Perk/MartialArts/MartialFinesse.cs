using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;

namespace SWLOR.Game.Server.Legacy.Perk.MartialArts
{
    public class MartialFinesse: IPerkHandler
    {
        public PerkType PerkType => PerkType.MartialFinesse;

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

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
            ApplyFeatChanges(creature, null);
        }

        public void OnRemoved(NWCreature creature)
        {
            Creature.RemoveFeat(creature, Feat.WeaponFinesse);
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        private void ApplyFeatChanges(NWCreature creature, NWItem unequippingItem)
        {
            var mainHand = creature.RightHand;
            var offHand = creature.LeftHand;
            var mainType = mainHand.CustomItemType;
            var offType = offHand.CustomItemType;
            var receivesFeat = false;

            if (unequippingItem != null && Equals(unequippingItem, mainHand))
            {
                mainHand = NWScript.OBJECT_INVALID;
            }
            else if (unequippingItem != null && Equals(unequippingItem, offHand))
            {
                offHand = NWScript.OBJECT_INVALID;
            }

            // Main is Martial and off is invalid 
            // OR
            // Main is invalid and off is martial
            if ((mainType == CustomItemType.MartialArtWeapon && !offHand.IsValid) ||
                (offType == CustomItemType.MartialArtWeapon && !mainHand.IsValid))
            {
                receivesFeat = true;
            }
            // Both main and off are invalid
            else if (!mainHand.IsValid && !offHand.IsValid)
            {
                receivesFeat = true;
            }

            if (receivesFeat)
            {
                Creature.AddFeat(creature, Feat.WeaponFinesse);
            }
            else
            {
                Creature.RemoveFeat(creature, Feat.WeaponFinesse);
            }
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}
