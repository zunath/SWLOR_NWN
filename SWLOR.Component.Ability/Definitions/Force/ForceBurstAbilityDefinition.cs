using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Force
{
    public class ForceBurstAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public ForceBurstAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();

        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceBurst1(builder);
            ForceBurst2(builder);
            ForceBurst3(builder);
            ForceBurst4(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
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

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.Force);
            var creature = GetFirstObjectInShape(ShapeType.Sphere, RadiusSize.Medium, GetLocation(target), true, ObjectType.Creature);
            while (GetIsObjectValid(creature))
            {
                if (GetDistanceBetween(target, creature) <= 4f && GetIsReactionTypeHostile(creature, activator))
                {
                    var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
                    var defense = _statCalculation.CalculateForceDefense(target);
                    var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
                    var attack = _statCalculation.CalculateAttack(activator, AbilityType.Willpower, SkillType.Force);
                    var damage = CombatService.CalculateDamage(
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
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Silence), dTarget);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.VFX_IMP_KIN_L), dTarget);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Pulse_Wind), target);
                    });
                    
                    DelayCommand(delay + 0.1f, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Pulse_Wind), target);
                    });
                    
                    DelayCommand(delay + 0.2f, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Pulse_Wind), target);
                    });

                    CombatPointService.AddCombatPoint(activator, creature, SkillType.Force, 3);
                    EnmityService.ModifyEnmity(activator, creature, 250 * level + damage);
                }
                creature = GetNextObjectInShape(ShapeType.Sphere, RadiusSize.Medium, GetLocation(target), true , ObjectType.Creature);
            }
        }

        private void ForceBurst1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBurst1, PerkType.ForceBurst)
                .Name("Force Burst I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ForceBurst, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(1)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private void ForceBurst2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBurst2, PerkType.ForceBurst)
                .Name("Force Burst II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ForceBurst, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private void ForceBurst3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBurst3, PerkType.ForceBurst)
                .Name("Force Burst III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ForceBurst, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private void ForceBurst4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBurst4, PerkType.ForceBurst)
                .Name("Force Burst IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.ForceBurst, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(7)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }
        private static void DoFireball(uint target)
        {
            var missile = EffectVisualEffect(VisualEffectType.Vfx_Imp_Mirv_Fireball);
            ApplyEffectToObject(DurationType.Instant, missile, target);
        }
    }
}

