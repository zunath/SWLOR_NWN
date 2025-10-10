using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.NPC
{
    public class TalonAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public TalonAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatCalculationService CombatCalculationService => _serviceProvider.GetRequiredService<ICombatCalculationService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Talon(builder);

            return builder.Build();
        }

        private void Talon(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Talon, PerkType.Invalid)
                .Name("Talon")
                .HasActivationDelay(2.0f)
                .HasRecastDelay(RecastGroupType.Talon, 40f)
                .IsCastedAbility()
                .RequirementStamina(3)
                .HasImpactAction((activator, target, level, location) =>
                {
                    const int DMG = 1;
                    var attackerStat = GetAbilityScore(activator, AbilityType.Might);
                    var attack = _statCalculation.CalculateAttack(activator, AbilityType.Might, SkillType.Invalid);
                    var defense = _statCalculation.CalculateDefense(target);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = CombatCalculationService.CalculateAbilityDamage(
                        activator,
                        target,
                        DMG,
                        CombatDamageType.Physical,
                        SkillType.Invalid,
                        AbilityType.Might,
                        AbilityType.Vitality
                    );

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Com_Blood_Spark_Medium), target);
                });
        }
    }
}

