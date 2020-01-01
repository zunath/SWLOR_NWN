﻿using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.CustomEffect;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.General
{
    public class Meditate: IPerkHandler
    {
        public PerkType PerkType => PerkType.Meditate;
        public string Name => "Meditate";
        public bool IsActive => true;
        public string Description => "Restores FP quickly as long as you stay in one place. Must be out of combat to use. Moving or combat will interrupt the ability. Shares a cooldown with the Rest perk.";
        public PerkCategoryType Category => PerkCategoryType.General;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.RestAndMeditate;
        public PerkExecutionType ExecutionType => PerkExecutionType.CombatAbility;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 1;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.AllTaggedTargets;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (!MeditateEffect.CanMeditate(oPC))
                return "You cannot meditate while you or a party member are in combat.";
            
            return string.Empty;
        }
        
        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, int spellTier)
        {
            return 1f;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            int perkLevel = PerkService.GetCreaturePerkLevel(oPC, PerkType.Meditate);

            switch (perkLevel)
            {
                case 1: return 300.0f;
                case 2: return 270.0f;
                case 3:
                case 4:
                    return 240.0f;
                case 5:
                    return 210.0f;
                case 6:
                case 7:
                    return 180.0f;
                default: return 300.0f;
            }
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            CustomEffectService.ApplyCustomEffect(creature, creature, CustomEffectType.Meditate, -1, 0, null);
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
        }

        public void OnRemoved(NWCreature creature)
        {
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
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
