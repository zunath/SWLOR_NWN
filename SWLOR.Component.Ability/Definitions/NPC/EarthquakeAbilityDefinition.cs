using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.NPC
{
    public class EarthquakeAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public EarthquakeAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Earthquake(builder);
            GreaterEarthquake(builder);

            return builder.Build();
        }

        private void Earthquake(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Earthquake, PerkType.Invalid)
                .Name("Earthquake")
                .HasActivationDelay(4.0f)
                .DisplaysVisualEffectWhenActivating(VisualEffectType.Vfx_Dur_Aura_Blue)
                .HasRecastDelay(RecastGroupType.Earthquake, 60f)
                .IsCastedAbility()
                .RequirementStamina(10)
                .HasImpactAction((activator, target, level, location) =>
                {
                    var count = 1;
                    var nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, count);
                    while (GetIsObjectValid(nearest) && GetDistanceBetween(activator, nearest) <= 30f)
                    {
                        if (GetIsEnemy(nearest, activator))
                        {
                            var duration = 8f + Random.NextFloat(1f, 5f);

                            ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), nearest, duration);
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Com_Chunk_Stone_Small), nearest);

                            SendMessageToPC(nearest, "The earthquake knocks you down!");
                        }

                        count++;
                        nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, count);
                    }
                });
        }

        private void GreaterEarthquake(IAbilityBuilder builder)
        {
            builder.Create(FeatType.GreaterEarthquake, PerkType.Invalid)
                .Name("Greater Earthquake")
                .HasActivationDelay(6.0f)
                .DisplaysVisualEffectWhenActivating(VisualEffectType.Vfx_Dur_Aura_Blue)
                .HasRecastDelay(RecastGroupType.GreaterEarthquake, 180f)
                .IsCastedAbility()
                .RequirementStamina(10)
                .HasImpactAction((activator, target, level, location) =>
                {
                    var attackerStat = GetAbilityScore(activator, AbilityType.Might);
                    var dmg = 70;

                    var count = 1;
                    var nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, count);
                    while (GetIsObjectValid(nearest) && GetDistanceBetween(activator, nearest) <= 30f)
                    {
                        if (GetIsEnemy(nearest, activator))
                        {
                            var duration = 18f;

                            ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), nearest, duration);
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Com_Chunk_Stone_Small), nearest);

                            SendMessageToPC(nearest, "The earthquake knocks you down!");

                            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Might, SkillType.Invalid);
                            var defense = _statCalculation.CalculateDefense(nearest);
                            var defenderStat = GetAbilityScore(nearest, AbilityType.Vitality);
                            var damage = CombatService.CalculateDamage(
                                attack,
                                dmg,
                                attackerStat,
                                defense,
                                defenderStat,
                                0);

                            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Sonic), nearest);
                        }

                        count++;
                        nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, count);
                    }
                });
        }
    }
}

