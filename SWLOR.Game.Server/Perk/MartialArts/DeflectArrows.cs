using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.Perk.MartialArts
{
    public class DeflectArrows : IPerk
    {
        private readonly INWScript _;
        private readonly INWNXCreature _nwnxCreature;

        public DeflectArrows(INWScript script,
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

        public int ManaCost(NWPlayer oPC, int baseManaCost)
        {
            return baseManaCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime)
        {
            return baseCooldownTime;
        }

        public void OnImpact(NWPlayer oPC, NWObject oTarget)
        {
        }

        public void OnPurchased(NWPlayer oPC, int newLevel)
        {
            ApplyFeatChanges(oPC, null);
        }

        public void OnRemoved(NWPlayer oPC)
        {
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_DEFLECT_ARROWS);
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
                mainHand = NWItem.Wrap(new Object());
            }
            else if (unequippingItem != null && Equals(unequippingItem, offHand))
            {
                offHand = NWItem.Wrap(new Object());
            }

            if ((!mainHand.IsValid && !offHand.IsValid) ||
                (mainType != CustomItemType.MartialArtWeapon || offType != CustomItemType.MartialArtWeapon))
            {
                receivesFeat = false;
            }

            if (receivesFeat)
            {
                _nwnxCreature.AddFeat(oPC, NWScript.FEAT_DEFLECT_ARROWS);
            }
            else
            {
                _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_DEFLECT_ARROWS);
            }
        }
        public bool IsHostile()
        {
            return false;
        }
    }
}
