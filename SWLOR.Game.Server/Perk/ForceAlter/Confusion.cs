using System;
using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class Confusion : IPerk
    {
        public PerkType PerkType => PerkType.Confusion;
        public string Name => "Confusion";
        public bool IsActive => true;
        public string Description => "Applies Confusion effect to organic creatures with lower WIS than the caster.";
        public PerkCategoryType Category => PerkCategoryType.ForceAlter;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.Confusion;
        public PerkExecutionType ExecutionType => PerkExecutionType.ForceAbility;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Light;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            switch (spellTier)
            {
                case 1:
                    if (!oTarget.IsCreature)
                        return "This ability can only be used on living creatures.";
                    NWCreature targetCreature = oTarget.Object;
                    if (targetCreature.RacialType == RacialType.Robot)
                        return "This ability cannot be used on droids.";
                    break;
                case 2:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

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
            if (spellTier == 1) return 300; // 5 minutes
            else if (spellTier == 2) return 1800; // 30 minutes

            return baseCooldownTime;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            ApplyEffect(creature, target, spellTier);
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
                1, new PerkLevel(7, "Applies Confusion effect to a single non-mechanical target with lower WIS than the caster, while the caster concentrates.",
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 40},
                })
            },
            {
                2, new PerkLevel(7, "Applies Confusion effect to all hostile non-mechanical targets within 10m with lower WIS than the caster, while the caster concentrates.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 80},
                })
            },
        };

        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.Confusion1, BaseFPCost = 8, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
            {
                2, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.Confusion2, BaseFPCost = 20, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int spellTier, int tick)
        {
        }

        private void ApplyEffect(NWCreature creature, NWObject target, int spellTier)
        {
            float radiusSize = RadiusSize.Small;

            Effect confusionEffect = _.EffectConfused();

            // Handle effects for differing spellTier values
            switch (spellTier)
            {
                case 1:
                    if ((creature.Wisdom > _.GetAbilityModifier(Ability.Wisdom, target) || creature == target) && _.GetDistanceBetween(creature.Object, target) <= radiusSize)
                    {
                        creature.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(DurationType.Temporary, confusionEffect, target, 6.1f);
                            // Play VFX
                            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Imp_Confusion_S), target);
                        });
                        if (!creature.IsPlayer)
                        {
                            SkillService.RegisterPCToNPCForSkill(creature.Object, target, Skill.ForceAlter);
                        }
                    }
                    else
                    {
                        creature.SendMessage("Confusion failed.");
                    }
                    break;
                case 2:
                    NWCreature targetCreature = _.GetFirstObjectInShape(Shape.Sphere, radiusSize, creature.Location, true, ObjectType.Creature);
                    while (targetCreature.IsValid)
                    {
                        if (targetCreature.RacialType == RacialType.Robot || !_.GetIsReactionTypeHostile(targetCreature, creature))
                        {
                            // Do nothing against droids or non-hostile creatures, skip object
                            targetCreature = _.GetNextObjectInShape(Shape.Sphere, radiusSize, creature.Location, true, ObjectType.Creature);
                            continue;
                        }

                        if (creature.Wisdom > targetCreature.Wisdom)
                        {
                            var targetCreatureCopy = targetCreature; // Closure can modify the iteration variable so we copy it first.
                            creature.AssignCommand(() =>
                            {
                                _.ApplyEffectToObject(DurationType.Temporary, confusionEffect, targetCreatureCopy, 6.1f);
                                // Play VFX
                                _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Imp_Confusion_S), targetCreatureCopy);
                            });

                            if (!creature.IsPlayer)
                            {
                                SkillService.RegisterPCToNPCForSkill(creature.Object, targetCreature, Skill.ForceAlter);
                            }
                        }
                        else
                        {
                            creature.SendMessage("Confusion failed.");
                        }

                        targetCreature = _.GetNextObjectInShape(Shape.Sphere, radiusSize, creature.Location, true, ObjectType.Creature);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }
        }
    }
}
