using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.NPC
{
    public class EarthquakeAbilityDefinition : IAbilityListDefinition
    {
        private readonly IRandomService _random;
        private readonly IStatService _statService;
        private readonly ICombatService _combatService;

        public EarthquakeAbilityDefinition(IRandomService random, IStatService statService, ICombatService combatService)
        {
            _random = random;
            _statService = statService;
            _combatService = combatService;
        }

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
                .DisplaysVisualEffectWhenActivating(VisualEffect.Vfx_Dur_Aura_Blue)
                .HasRecastDelay(RecastGroup.Earthquake, 60f)
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
                            var duration = 8f + _random.NextFloat(1f, 5f);

                            ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), nearest, duration);
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Chunk_Stone_Small), nearest);

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
                .DisplaysVisualEffectWhenActivating(VisualEffect.Vfx_Dur_Aura_Blue)
                .HasRecastDelay(RecastGroup.GreaterEarthquake, 180f)
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
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Chunk_Stone_Small), nearest);

                            SendMessageToPC(nearest, "The earthquake knocks you down!");

                            var attack = _statService.GetAttack(activator, AbilityType.Might, SkillType.Invalid);
                            var defense = _statService.GetDefense(nearest, CombatDamageType.Physical, AbilityType.Vitality);
                            var defenderStat = GetAbilityScore(nearest, AbilityType.Vitality);
                            var damage = _combatService.CalculateDamage(
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

