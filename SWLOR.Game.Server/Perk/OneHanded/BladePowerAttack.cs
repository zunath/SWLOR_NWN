using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.OneHanded
{
    public class BladePowerAttack : IPerk
    {
        public PerkType PerkType => PerkType.BladePowerAttack;
        public string Name => "Blade Power Attack";
        public bool IsActive => true;
        public string Description => "Increases damage at the cost of reduced attack rolls. Only available when equipped with a blade.";
        public PerkCategoryType Category => PerkCategoryType.OneHandedVibroblades;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public PerkExecutionType ExecutionType => PerkExecutionType.EquipmentBased;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            return string.Empty;
        }
        
        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, int spellTier)
        {
            return 0f;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
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
            NWNXCreature.RemoveFeat(creature, Feat.Power_Attack);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Power_Attack);
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Vibroblade) return;
            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Vibroblade) return;
            if (oItem == creature.LeftHand) return;

            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        private void ApplyFeatChanges(NWCreature creature, NWItem oItem)
        {
            NWItem equipped = oItem ?? creature.RightHand;
            
            if (Equals(equipped, oItem) || equipped.CustomItemType != CustomItemType.Vibroblade)
            {
                NWNXCreature.RemoveFeat(creature, Feat.Power_Attack);
                NWNXCreature.RemoveFeat(creature, Feat.Improved_Power_Attack);
                if (_.GetActionMode(creature, ActionMode.PowerAttack) == true)
                {
                    _.SetActionMode(creature, ActionMode.PowerAttack, false);
                }
                if (_.GetActionMode(creature, ActionMode.ImprovedPowerAttack) == true)
                {
                    _.SetActionMode(creature, ActionMode.ImprovedPowerAttack, false);
                }
                return;
            }

            int perkLevel = PerkService.GetCreaturePerkLevel(creature, PerkType.BladePowerAttack);
            NWNXCreature.AddFeat(creature, Feat.Power_Attack);

            if (perkLevel >= 2)
            {
                NWNXCreature.AddFeat(creature, Feat.Improved_Power_Attack);
            }
        }

        public bool IsHostile()
        {
            return false;
        }

        public Dictionary<int, PerkLevel> PerkLevels { get; }

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
