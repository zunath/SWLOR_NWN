using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Devices
{
    public class WristRocketAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public WristRocketAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            WristRocket1(builder);
            WristRocket2(builder);
            WristRocket3(builder);

            return builder.Build();
        }

        private void Impact(uint activator, uint target, int dmg, int dc)
        {
            var targetDistance = GetDistanceBetween(activator, target);
            var delay = (float)(targetDistance / (3.0 * log(targetDistance) + 2.0));
            var defense = _statCalculation.CalculateDefense(target);
            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Perception, SkillType.Devices);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = CombatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Mirv), target);
            });

            DelayCommand(delay, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), target);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Fnf_Fireball), target);

                if (dc > 0)
                {
                    const float Duration = 1f;
                    dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Fortitude, dc, AbilityType.Perception);
                    var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
                    if (checkResult == SavingThrowResultType.Failed)
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, Duration);

                        AbilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Knockdown);
                    }
                }
            });
        }

        private void WristRocket1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.WristRocket1, PerkType.WristRocket)
                .Name("Wrist Rocket I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.WristRocket, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(1)
                .UsesAnimation(AnimationType.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasImpactAction((activator,target, _, targetLocation) =>
                {
                    var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                    Impact(activator, target, perBonus, -1);

                    EnmityService.ModifyEnmity(activator, target, 180);
                    CombatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
                });
        }

        private void WristRocket2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.WristRocket2, PerkType.WristRocket)
                .Name("Wrist Rocket II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.WristRocket, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(2)
                .UsesAnimation(AnimationType.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                    var perDMG = 25 + perBonus;
                    Impact(activator, target, perDMG, 8);

                    EnmityService.ModifyEnmity(activator, target, 280);
                    CombatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
                });
        }

        private void WristRocket3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.WristRocket3, PerkType.WristRocket)
                .Name("Wrist Rocket III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.WristRocket, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .UsesAnimation(AnimationType.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                    var perDMG = 50 + perBonus * 2;
                    Impact(activator, target, perDMG, 12);

                    EnmityService.ModifyEnmity(activator, target, 380);
                    CombatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
                });
        }
    }
}
