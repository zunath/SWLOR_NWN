using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class ForceBreach: IPerkHandler
    {
        public PerkType PerkType => PerkType.ForceBreach;
        public string Name => "Force Breach";
        public bool IsActive => true;
        public string Description => "Deals direct damage to a single target.";
        public PerkCategoryType Category => PerkCategoryType.ForceAlter;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.ForceBreach;
        public PerkExecutionType ExecutionType => PerkExecutionType.ForceAbility;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;

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

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            int damage;

            switch (spellTier)
            {
                case 1:
                    damage = 100;
                    break;
                case 2:
                    damage = 125;
                    break;
                case 3:
                    damage = 160;
                    break;
                case 4:
                    damage = 200;
                    break;
                case 5:
                    damage = 250;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

            var result = CombatService.CalculateAbilityResistance(creature, target.Object, SkillType.ForceAlter, ForceBalanceType.Dark, true);

            // +/- percent change based on resistance
            float delta = 0.01f * result.Delta;
            damage = damage + (int)(damage * delta);

            creature.AssignCommand(() =>
            {
                _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(damage), target);
            });

            if (creature.IsPlayer)
            {
                SkillService.RegisterPCToNPCForSkill(creature.Object, target, SkillType.ForceAlter);
            }

            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Imp_Silence), target);
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
