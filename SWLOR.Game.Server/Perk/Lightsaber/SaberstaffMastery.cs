using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.Lightsaber
{
    public class SaberstaffMastery : IPerk
    {
        private readonly INWScript _;
        private readonly INWNXCreature _nwnxCreature;
        private readonly IPerkService _perk;

        public SaberstaffMastery(
            INWScript script,
            INWNXCreature nwnxCreature,
            IPerkService perk)
        {
            _ = script;
            _nwnxCreature = nwnxCreature;
            _perk = perk;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return false;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return null;
        }

        public int FPCost(NWPlayer oPC, int baseFPCost)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime)
        {
            return baseCooldownTime;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel)
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
            if (oItem.CustomItemType != CustomItemType.Saberstaff) return;
            ApplyFeatChanges(oPC, null);
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Saberstaff) return;
            ApplyFeatChanges(oPC, oItem);
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        private void RemoveFeats(NWPlayer oPC)
        {
            _nwnxCreature.RemoveFeat(oPC, FEAT_TWO_WEAPON_FIGHTING);
            _nwnxCreature.RemoveFeat(oPC, FEAT_AMBIDEXTERITY);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_TWO_WEAPON_FIGHTING);
        }

        private void ApplyFeatChanges(NWPlayer oPC, NWItem unequippedItem)
        {
            NWItem equipped = unequippedItem ?? oPC.RightHand;

            if (Equals(equipped, unequippedItem) || equipped.CustomItemType != CustomItemType.Saberstaff)
            {
                RemoveFeats(oPC);
                return;
            }

            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.SaberstaffMastery);
            _nwnxCreature.AddFeat(oPC, FEAT_TWO_WEAPON_FIGHTING);

            if (perkLevel >= 2)
            {
                _nwnxCreature.AddFeat(oPC, FEAT_AMBIDEXTERITY);
            }
            if (perkLevel >= 3)
            {
                _nwnxCreature.AddFeat(oPC, FEAT_IMPROVED_TWO_WEAPON_FIGHTING);
            }
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}
