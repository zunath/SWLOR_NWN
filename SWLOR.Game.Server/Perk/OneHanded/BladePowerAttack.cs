using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.OneHanded
{
    public class BladePowerAttack : IPerkBehaviour
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
            NWNXCreature.RemoveFeat(oPC, FEAT_POWER_ATTACK);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_POWER_ATTACK);
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Vibroblade) return;
            ApplyFeatChanges(oPC, null);
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Vibroblade) return;
            if (oItem == oPC.LeftHand) return;

            ApplyFeatChanges(oPC, oItem);
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        private void ApplyFeatChanges(NWPlayer oPC, NWItem oItem)
        {
            NWItem equipped = oItem ?? oPC.RightHand;
            
            if (Equals(equipped, oItem) || equipped.CustomItemType != CustomItemType.Vibroblade)
            {
                NWNXCreature.RemoveFeat(oPC, FEAT_POWER_ATTACK);
                NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_POWER_ATTACK);
                if (_.GetActionMode(oPC, ACTION_MODE_POWER_ATTACK) == TRUE)
                {
                    _.SetActionMode(oPC, ACTION_MODE_POWER_ATTACK, FALSE);
                }
                if (_.GetActionMode(oPC, ACTION_MODE_IMPROVED_POWER_ATTACK) == TRUE)
                {
                    _.SetActionMode(oPC, ACTION_MODE_IMPROVED_POWER_ATTACK, FALSE);
                }
                return;
            }

            int perkLevel = PerkService.GetPCPerkLevel(oPC, PerkType.BladePowerAttack);
            NWNXCreature.AddFeat(oPC, FEAT_POWER_ATTACK);

            if (perkLevel >= 2)
            {
                NWNXCreature.AddFeat(oPC, FEAT_IMPROVED_POWER_ATTACK);
            }
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}
