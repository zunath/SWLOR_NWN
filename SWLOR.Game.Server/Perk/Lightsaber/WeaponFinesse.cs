using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.Lightsaber
{
    public class WeaponFinesse : IPerk
    {
        private readonly INWScript _;
        private readonly INWNXCreature _nwnxCreature;
        private readonly IPerkService _perk;

        public WeaponFinesse(INWScript script,
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
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FINESSE);
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Lightsaber && oItem.CustomItemType != CustomItemType.Saberstaff && oItem.GetLocalInt("LIGHTSABER") == FALSE) return;
            ApplyFeatChanges(oPC, null);
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Lightsaber && oItem.CustomItemType != CustomItemType.Saberstaff && oItem.GetLocalInt("LIGHTSABER") == FALSE) return;
            if (oItem == oPC.LeftHand) return;
            ApplyFeatChanges(oPC, oItem);
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        private void ApplyFeatChanges(NWPlayer oPC, NWItem oItem)
        {
            NWItem equipped = oItem ?? oPC.RightHand;
            if (Equals(equipped, oItem) || (equipped.CustomItemType != CustomItemType.Lightsaber && equipped.CustomItemType != CustomItemType.Saberstaff && oItem.GetLocalInt("LIGHTSABER") == FALSE))
            {
                _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FINESSE);
                return;
            }
            _nwnxCreature.AddFeat(oPC, NWScript.FEAT_WEAPON_FINESSE);
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}
