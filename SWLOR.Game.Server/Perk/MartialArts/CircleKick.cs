using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;


namespace SWLOR.Game.Server.Perk.MartialArts
{
    public class CircleKick : IPerkHandler
    {
        public PerkType PerkType => PerkType.CircleKick;

        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellFeatID)
        {
            return string.Empty;
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
            NWNXCreature.RemoveFeat(oPC, _.FEAT_CIRCLE_KICK);
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
            ApplyFeatChanges(oPC, null);
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
            ApplyFeatChanges(oPC, oItem);
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        private void ApplyFeatChanges(NWPlayer oPC, NWItem unequippingItem)
        {
            NWItem mainHand = oPC.RightHand;
            NWItem offHand = oPC.LeftHand;
            CustomItemType mainType = mainHand.CustomItemType;
            CustomItemType offType = offHand.CustomItemType;
            bool receivesFeat = true;

            if (unequippingItem != null && Equals(unequippingItem, mainHand))
            {
                mainHand = (new Object());
            }
            else if (unequippingItem != null && Equals(unequippingItem, offHand))
            {
                offHand = (new Object());
            }

            if ((!mainHand.IsValid && !offHand.IsValid) ||
                (mainType != CustomItemType.MartialArtWeapon || offType != CustomItemType.MartialArtWeapon))
            {
                receivesFeat = false;
            }

            if (receivesFeat)
            {
                NWNXCreature.AddFeat(oPC, _.FEAT_CIRCLE_KICK);
            }
            else
            {
                NWNXCreature.RemoveFeat(oPC, _.FEAT_CIRCLE_KICK);
            }
        }
        public bool IsHostile()
        {
            return false;
        }

        public void OnConcentrationTick(NWPlayer player, int perkLevel, int spellFeatID)
        {
            
        }
    }
}