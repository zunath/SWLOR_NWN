using System;
using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.NWScript._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class ForceStun : IPerk
    {
        public PerkType PerkType => PerkType.ForceStun;
        public string Name => "Force Stun";
        public bool IsActive => true;
        public string Description => "Tranquilises an enemy or slows their reaction time.";
        public PerkCategoryType Category => PerkCategoryType.ForceAlter;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.ForceStun;
        public PerkExecutionType ExecutionType => PerkExecutionType.ConcentrationAbility;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 10;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.TargetOnly;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Light;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            NWCreature targetCreature = oTarget.Object;
            var concentrationEffect = AbilityService.GetActiveConcentrationEffect(targetCreature);

            switch (spellTier)
            {
                case 1:
                    if (!oTarget.IsCreature)
                        return "This ability can only be used on living creatures.";
                    if (targetCreature.RacialType == RacialType.Robot)
                        return "This ability cannot be used on droids.";
                    if (concentrationEffect.Type == PerkType.MindShield)
                        return "Your target is immune to tranquilization effects.";
                    break;
                case 2:
                    if (!oTarget.IsCreature)
                        return "This ability can only be used on living creatures.";
                    if (targetCreature.RacialType == RacialType.Robot)
                        return "This ability cannot be used on droids.";
                    if (concentrationEffect.Type == PerkType.MindShield)
                        return "Your target is immune to tranquilization effects.";
                    break;
                case 3:
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
            return baseCooldownTime;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
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
                1, new PerkLevel(4, "Single target is Tranquilised while the caster concentrates or, if resisted, gets -5 to AB and AC.",
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 10},
                })
            },
            {
                2, new PerkLevel(7, "Target and nearest other enemy within 10m is Tranquilised while the caster concentrates or, if resisted, get -5 to AB and AC.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 50},
                })
            },
            {
                3, new PerkLevel(10, "Target and all other enemies within 10 are Tranquilised while the caster concentrates or, if resisted, get -5 to AB and AC.", SpecializationType.Consular,
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
                    new PerkFeat {Feat = Feat.ForceStun1, BaseFPCost = 0, ConcentrationFPCost = 8, ConcentrationTickInterval = 6}
                }
            },
            {
                2, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceStun2, BaseFPCost = 0, ConcentrationFPCost = 12, ConcentrationTickInterval = 6}
                }
            },
            {
                3, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceStun3, BaseFPCost = 0, ConcentrationFPCost = 20, ConcentrationTickInterval = 6}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            ApplyEffect(creature, target, perkLevel);
        }

        private void RunEffect(NWCreature creature, NWObject target)
        {
            var concentrationEffect = AbilityService.GetActiveConcentrationEffect(target.Object);

            if (concentrationEffect.Type == PerkType.MindShield)
            {
                creature.SendMessage("Your target is immune to tranquilization effects.");
                return;
            }

            AbilityResistanceResult result = CombatService.CalculateAbilityResistance(creature, target.Object, Skill.ForceAlter, ForceBalanceType.Dark);

            // Tranquilization effect - Daze target(s). Occurs on succeeding the DC check.
            Effect successEffect = EffectDazed();
            successEffect = EffectLinkEffects(successEffect, EffectVisualEffect(Vfx.Vfx_Dur_Iounstone_Blue));
            successEffect = TagEffect(successEffect, "TRANQUILIZER_EFFECT");

            // AC & AB decrease effect - Occurs on failing the DC check.
            Effect failureEffect = EffectLinkEffects(EffectAttackDecrease(5), EffectACDecrease(5));


            if (!result.IsResisted)
            {
                creature.AssignCommand(() =>
                {
                    ApplyEffectToObject(DurationType.Temporary, successEffect, target, 6.1f);
                });
            }
            else
            {
                creature.AssignCommand(() =>
                {
                    ApplyEffectToObject(DurationType.Temporary, failureEffect, target, 6.1f);
                });
            }

            if (creature.IsPlayer)
            {
                SkillService.RegisterPCToNPCForSkill(creature.Object, target, Skill.ForceAlter);
            }

            EnmityService.AdjustEnmity(target.Object, creature, 1);
        }

        private void ApplyEffect(NWCreature creature, NWObject target, int spellTier)
        {
            const float radiusSize = 10.0f;
            NWCreature targetCreature;

            switch (spellTier)
            {
                // Tier 1 - Single target is Tranquilized or, if resisted, receives -5 to AB and AC
                case 1:
                    RunEffect(creature, target);
                    break;
                // Tier 2 - Target and nearest other enemy within 10m are tranquilized using tier 1 rules.
                case 2:
                    RunEffect(creature, target);

                    // Target the next nearest creature and do the same thing.
                    targetCreature = GetFirstObjectInShape(Shape.Sphere, radiusSize, creature.Location, true);
                    while (targetCreature.IsValid)
                    {
                        if (targetCreature != target)
                        {
                            // Apply to nearest other creature, then exit loop.
                            RunEffect(creature, target);
                            break;
                        }

                        targetCreature = GetNextObjectInShape(Shape.Sphere, radiusSize, creature.Location, true);
                    }
                    break;
                // Tier 3 - All creatures within 10m are tranquilized using tier 1 rules.
                case 3:
                    RunEffect(creature, target);

                    targetCreature = GetFirstObjectInShape(Shape.Sphere, radiusSize, creature.Location, true);
                    while (targetCreature.IsValid)
                    {
                        RunEffect(creature, target);
                        targetCreature = GetNextObjectInShape(Shape.Sphere, radiusSize, creature.Location, true);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }
        }
    }
}
