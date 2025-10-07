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
    public class FireBreathAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public FireBreathAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            FireBreath(builder);
            FlameBlast(builder);

            return builder.Build();
        }

        private void FireBreath(IAbilityBuilder builder)
        {
            builder.Create(FeatType.FireBreath, PerkType.Invalid)
                .Name("Fire Breath")
                .HasActivationDelay(2.0f)
                .HasRecastDelay(RecastGroupType.FireBreath, 60f)
                .IsCastedAbility()
                .RequirementStamina(6)
                .HasImpactAction((activator, target, level, location) =>
                {
                    var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
                    var dmg = 3;
                    
                    var coneTarget = GetFirstObjectInShape(ShapeType.SpellCone, 14.0f, location);
                    while (GetIsObjectValid(coneTarget))
                    {
                        if (GetIsEnemy(coneTarget, activator))
                        {
                            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Might, SkillType.Invalid);
                            var defense = _statCalculation.CalculateDefense(coneTarget);
                            var defenderStat = GetAbilityScore(coneTarget, AbilityType.Vitality);
                            var damage = CombatService.CalculateDamage(
                                attack, 
                                dmg, 
                                attackerStat, 
                                defense, 
                                defenderStat, 
                                0);

                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Com_Hit_Fire), coneTarget);
                            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), coneTarget);
                        }

                        coneTarget = GetNextObjectInShape(ShapeType.SpellCone, 14.0f, location);
                    }
                });
        }

        private void FlameBlast(IAbilityBuilder builder)
        {
            builder.Create(FeatType.FlameBlast, PerkType.Invalid)
                .Name("Flame Blast")
                .HasActivationDelay(4.0f)
                .HasRecastDelay(RecastGroupType.FlameBlast, 30f)
                .IsCastedAbility()
                .RequirementStamina(3)
                .HasImpactAction((activator, target, level, location) =>
                {
                    var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
                    var dmg = 120;

                    var coneTarget = GetFirstObjectInShape(ShapeType.SpellCone, 14.0f, location);
                    while (GetIsObjectValid(coneTarget))
                    {
                        if (GetIsEnemy(coneTarget, activator))
                        {
                            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Might, SkillType.Invalid);
                            var defense = _statCalculation.CalculateDefense(coneTarget);
                            var defenderStat = GetAbilityScore(coneTarget, AbilityType.Vitality);
                            var damage = CombatService.CalculateDamage(
                                attack,
                                dmg,
                                attackerStat,
                                defense,
                                defenderStat,
                                0);

                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Com_Hit_Fire), coneTarget);
                            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), coneTarget);
                        }

                        coneTarget = GetNextObjectInShape(ShapeType.SpellCone, 14.0f, location);
                    }
                });
        }
    }
}
