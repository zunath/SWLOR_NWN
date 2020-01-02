using System;
using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceControl
{
    public class ForceSpeed : IPerk
    {
        public PerkType PerkType => PerkType.ForceSpeed;
        public string Name => "Force Speed";
        public bool IsActive => true;
        public string Description => "Unlocks new fabrication blueprints on every odd level (1, 3, 5, 7) and adds an enhancement slot for every even level (2, 4, 6, 8) for fabrication.";
        public PerkCategoryType Category => PerkCategoryType.ForceControl;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.ForceSpeed;
        public PerkExecutionType ExecutionType => PerkExecutionType.ForceAbility;
        public bool IsTargetSelfOnly => true;
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
            switch (spellTier)
            {
                case 1: return 2;
                case 2: return 4;
                case 3: return 6;
                case 4: return 8;
                case 5: return 20;
            }

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
            Effect effect;
            float duration;
            switch (spellTier)
            {
                case 1:
                    effect = _.EffectMovementSpeedIncrease(10);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(Ability.Dexterity, 2));
                    duration = 60f;
                    break;
                case 2:
                    effect = _.EffectMovementSpeedIncrease(20);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(Ability.Dexterity, 4));
                    duration = 90f;
                    break;
                case 3:
                    effect = _.EffectMovementSpeedIncrease(30);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(Ability.Dexterity, 6));
                    effect = _.EffectLinkEffects(effect, _.EffectModifyAttacks(1));
                    duration = 120f;
                    break;
                case 4:
                    effect = _.EffectMovementSpeedIncrease(40);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(Ability.Dexterity, 8));
                    effect = _.EffectLinkEffects(effect, _.EffectModifyAttacks(1));
                    duration = 150f;
                    break;
                case 5:
                    effect = _.EffectMovementSpeedIncrease(50);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(Ability.Dexterity, 10));
                    effect = _.EffectLinkEffects(effect, _.EffectModifyAttacks(1));
                    duration = 180f;
                    break;
                default:
                    throw new ArgumentException(nameof(perkLevel) + " invalid. Value " + perkLevel + " is unhandled.");
            }

            // Check lucky chance.
            int luck = PerkService.GetCreaturePerkLevel(creature, PerkType.Lucky);
            if (RandomService.D100(1) <= luck)
            {
                duration *= 2;
                creature.SendMessage("Lucky Force Speed!");
            }

            _.ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Imp_Ac_Bonus), target);

            if (creature.IsPlayer)
            {
                NWPlayer player = creature.Object;
                int skillLevel = SkillService.GetPCSkillRank(player, SkillType.ForceControl);
                int xp = skillLevel * 10 + 50;
                SkillService.GiveSkillXP(player, SkillType.ForceControl, xp);
            }
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

        public Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
        {
            {
                1, new PerkLevel(2, "Increases movement speed by 10% and Dexterity by 2.",
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceControl, 0},
                })
            },
            {
                2, new PerkLevel(2, "Increases movement speed by 20% and Dexterity by 4.",
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceControl, 10},
                })
            },
            {
                3, new PerkLevel(3, "Increases movement speed by 30%, Dexterity by 6 and grants an extra attack.",
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceControl, 25},
                })
            },
            {
                4, new PerkLevel(3, "Increases movement speed by 40%, Dexterity by 8 and grants an extra attack.", SpecializationType.Guardian,
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceControl, 40},
                })
            },
            {
                5, new PerkLevel(12, "Increases movement speed by 50%, Dexterity by 10 and grants an extra attack.", SpecializationType.Guardian,
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceControl, 80},
                })
            },
        };


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
        }
    }
}
