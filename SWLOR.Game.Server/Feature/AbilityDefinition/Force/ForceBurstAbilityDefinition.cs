using System.Collections.Generic;
using System.Diagnostics.Metrics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceBurstAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceBurst1(builder);
            ForceBurst2(builder);
            ForceBurst3(builder);
            ForceBurst4(builder);

            return builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;
            var willBonus = GetAbilityScore(activator, AbilityType.Willpower);

            switch (level)
            {
                case 1:
                    dmg = willBonus;
                    break;
                case 2:
                    dmg = 10 + (willBonus * 3 / 2);
                    break;
                case 3:
                    dmg = 20 + (willBonus * 2);
                    break;
                case 4:
                    dmg = 30 + (willBonus * 3);
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Force);
            var creature = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Medium, GetLocation(target), true, ObjectType.Creature);
            while (GetIsObjectValid(creature))
            {
                if (GetDistanceBetween(target, creature) <= 4f && GetIsReactionTypeHostile(creature, activator))
                {
                    var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
                    var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
                    var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
                    var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
                    var damage = Combat.CalculateDamage(
                        attack,
                        dmg,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);
                    var delay = GetDistanceBetweenLocations(GetLocation(activator), targetLocation) / 18.0f + 0.35f;

                    var dTarget = creature;
                    AssignCommand(activator, () =>
                    {
                        PlaySound("plr_force_blast");
                        DoFireball(target);
                    });
                    
                    DelayCommand(delay, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), dTarget);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Silence), dTarget);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.VFX_IMP_KIN_L), dTarget);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Pulse_Wind), target);
                    });
                    
                    DelayCommand(delay + 0.1f, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Pulse_Wind), target);
                    });
                    
                    DelayCommand(delay + 0.2f, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Pulse_Wind), target);
                    });

                    CombatPoint.AddCombatPoint(activator, creature, SkillType.Force, 3);
                    Enmity.ModifyEnmity(activator, creature, 250 * level + damage);
                }
                creature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Medium, GetLocation(target), true , ObjectType.Creature);
            }
        }

        private static void ForceBurst1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBurst1, PerkType.ForceBurst)
                .Name("Force Burst I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceBurst, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(1)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceBurst2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBurst2, PerkType.ForceBurst)
                .Name("Force Burst II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceBurst, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceBurst3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBurst3, PerkType.ForceBurst)
                .Name("Force Burst III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceBurst, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceBurst4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBurst4, PerkType.ForceBurst)
                .Name("Force Burst IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ForceBurst, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(7)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }
        private static void DoFireball(uint target)
        {
            var missile = EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv_Fireball);
            ApplyEffectToObject(DurationType.Instant, missile, target);
        }
    }
}