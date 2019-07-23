using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;


namespace SWLOR.Game.Server.Perk.MartialArts
{
    public class CircleKick : IPerkHandler
    {
        public PerkType PerkType => PerkType.CircleKick;

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
            NWNXCreature.RemoveFeat(creature, _.FEAT_CIRCLE_KICK);
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
            NWItem mainHand = creature.RightHand;
            NWItem offHand = creature.LeftHand;
            CustomItemType mainType = mainHand.CustomItemType;
            CustomItemType offType = offHand.CustomItemType;
            bool receivesFeat = false;

            if (unequippingItem != null && Equals(unequippingItem, mainHand))
            {
                mainHand = (new NWGameObject());
            }
            else if (unequippingItem != null && Equals(unequippingItem, offHand))
            {
                offHand = (new NWGameObject());
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
                NWNXCreature.AddFeat(creature, _.FEAT_CIRCLE_KICK);
            }
            else
            {
                NWNXCreature.RemoveFeat(creature, _.FEAT_CIRCLE_KICK);
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