using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.Perk.MartialArts
{
    public class CircleKick : IPerk
    {
        private readonly INWScript _;
        private readonly INWNXCreature _nwnxCreature;

        public CircleKick(INWScript script,
            INWNXCreature nwnxCreature)
        {
            _ = script;
            _nwnxCreature = nwnxCreature;
        }

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
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_CIRCLE_KICK);
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
            ApplyFeatChanges(oPC, oItem);
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
            NWItem fist = oPC.Arms;
            CustomItemType mainType = mainHand.CustomItemType;
            CustomItemType offType = offHand.CustomItemType;
            CustomItemType glovesType = fist.CustomItemType;

            bool receivesFeat = true;

            if (unequippingItem != null && Equals(unequippingItem, mainHand))
            {
                mainHand = (new Object());
            }
            else if (unequippingItem != null && Equals(unequippingItem, offHand))
            {
                offHand = (new Object());
            }

            if (mainType != CustomItemType.MartialArtWeapon)
            {
                if (mainType != CustomItemType.None)
                {
                    receivesFeat = false;
                }
            }

            if (receivesFeat)
            {
                _nwnxCreature.AddFeat(oPC, NWScript.FEAT_CIRCLE_KICK);
            }
            else
            {
                _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_CIRCLE_KICK);
            }
        }
        public bool IsHostile()
        {
            return false;
        }
    }
}