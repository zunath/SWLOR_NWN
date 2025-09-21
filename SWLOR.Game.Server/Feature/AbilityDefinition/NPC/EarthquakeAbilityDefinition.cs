using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class EarthquakeAbilityDefinition : IAbilityListDefinition
    {
        private readonly IRandomService _random;
        private readonly AbilityBuilder _builder = new();

        public EarthquakeAbilityDefinition(IRandomService random)
        {
            _random = random;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Earthquake();
            GreaterEarthquake();

            return _builder.Build();
        }

        private void Earthquake()
        {
            _builder.Create(FeatType.Earthquake, PerkType.Invalid)
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

        private void GreaterEarthquake()
        {
            _builder.Create(FeatType.GreaterEarthquake, PerkType.Invalid)
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

                            var statService = ServiceContainer.GetService<IStatService>();
                            var combatService = ServiceContainer.GetService<ICombatService>();
                            var attack = statService.GetAttack(activator, AbilityType.Might, SkillType.Invalid);
                            var defense = statService.GetDefense(nearest, CombatDamageType.Physical, AbilityType.Vitality);
                            var defenderStat = GetAbilityScore(nearest, AbilityType.Vitality);
                            var damage = combatService.CalculateDamage(
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


                        count++;
                        nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, count);
                    }
                });
        }
    }
}
