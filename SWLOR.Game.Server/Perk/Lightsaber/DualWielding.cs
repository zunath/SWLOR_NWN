using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Lightsaber
{
    public class DualWielding : IPerkBehaviour
    {
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
            RemoveFeats(oPC);
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Lightsaber &&
                oItem.GetLocalInt("LIGHTSABER") == FALSE) 
                return;

            ApplyFeatChanges(oPC, null);
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Lightsaber &&
                oItem.GetLocalInt("LIGHTSABER") == FALSE)
                return;

            ApplyFeatChanges(oPC, oItem);
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }


        private void RemoveFeats(NWPlayer oPC)
        {
            NWNXCreature.RemoveFeat(oPC, FEAT_TWO_WEAPON_FIGHTING);
            NWNXCreature.RemoveFeat(oPC, FEAT_AMBIDEXTERITY);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_TWO_WEAPON_FIGHTING);
        }


        private void ApplyFeatChanges(NWPlayer oPC, NWItem oItem)
        {
            NWItem mainEquipped = oItem ?? oPC.RightHand;
            NWItem offEquipped = oItem ?? oPC.LeftHand;

            // oItem was unequipped.
            if (Equals(mainEquipped, oItem) || Equals(offEquipped, oItem))
            {
                RemoveFeats(oPC);
                return;
            }

            // Main or offhand was invalid (i.e not equipped)
            if (!mainEquipped.IsValid || !offEquipped.IsValid)
            {
                RemoveFeats(oPC);
                return;
            }

            // Main or offhand is not acceptable item type.
            if ((mainEquipped.CustomItemType != CustomItemType.Lightsaber &&
                 mainEquipped.GetLocalInt("LIGHTSABER") == FALSE) ||
                (offEquipped.CustomItemType != CustomItemType.Lightsaber &&
                 offEquipped.GetLocalInt("LIGHTSABER") == FALSE))
            {
                RemoveFeats(oPC);
                return;
            }


            int perkLevel = PerkService.GetPCPerkLevel(oPC, PerkType.LightsaberDualWielding);
            NWNXCreature.AddFeat(oPC, FEAT_TWO_WEAPON_FIGHTING);

            if (perkLevel >= 2)
            {
                NWNXCreature.AddFeat(oPC, FEAT_AMBIDEXTERITY);
            }
            if (perkLevel >= 3)
            {
                NWNXCreature.AddFeat(oPC, FEAT_IMPROVED_TWO_WEAPON_FIGHTING);
            }
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}
