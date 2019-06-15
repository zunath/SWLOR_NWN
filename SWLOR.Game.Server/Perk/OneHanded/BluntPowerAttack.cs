using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.OneHanded
{
    public class BluntPowerAttack : IPerkHandler
    {
        public PerkType PerkType => PerkType.BluntPowerAttack;

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
            NWNXCreature.RemoveFeat(creature, FEAT_POWER_ATTACK);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_POWER_ATTACK);
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Baton) return;
            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Baton) return;
            if (oItem == creature.LeftHand) return;

            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        private void ApplyFeatChanges(NWCreature creature, NWItem oItem)
        {
            NWItem equipped = oItem ?? creature.RightHand;

            if (Equals(equipped, oItem) || equipped.CustomItemType != CustomItemType.Baton)
            {
                NWNXCreature.RemoveFeat(creature, FEAT_POWER_ATTACK);
                NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_POWER_ATTACK);
                if (_.GetActionMode(creature, ACTION_MODE_POWER_ATTACK) == TRUE)
                {
                    _.SetActionMode(creature, ACTION_MODE_POWER_ATTACK, FALSE);
                }
                if (_.GetActionMode(creature, ACTION_MODE_IMPROVED_POWER_ATTACK) == TRUE)
                {
                    _.SetActionMode(creature, ACTION_MODE_IMPROVED_POWER_ATTACK, FALSE);
                }
                return;
            }

            int perkLevel = PerkService.GetCreaturePerkLevel(creature, PerkType.BluntPowerAttack);
            NWNXCreature.AddFeat(creature, FEAT_POWER_ATTACK);

            if (perkLevel >= 2)
            {
                NWNXCreature.AddFeat(creature, FEAT_IMPROVED_POWER_ATTACK);
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
