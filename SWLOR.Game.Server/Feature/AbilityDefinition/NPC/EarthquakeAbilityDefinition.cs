using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class EarthquakeAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Earthquake();
            GreaterEarthquake();

            return _builder.Build();
        }

        private void Earthquake()
        {
            _builder
                .Create(FeatType.Earthquake, PerkType.Invalid)
                .Name("Earthquake")
                .HasActivationDelay(4.0f)
                .DisplaysVisualEffectWhenActivating(VisualEffect.Vfx_Dur_Aura_Blue)
                .HasRecastDelay(RecastGroup.Earthquake, 60f)
                .IsCastedAbility()
                .HasMaxRange(30f)
                .IsAreaAbility()
                .RequiresTarget()
                .IsHostileAbility()
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

                            var damageType = CombatDamageType.Physical;
                            StatusEffect.ApplyStatusEffect(activator, nearest, typeof(KnockdownStatusEffect), duration, damageType);
                            Ability.ApplyHostileAbilityEnmity(activator, nearest);
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
            _builder
                .Create(FeatType.GreaterEarthquake, PerkType.Invalid)
                .Name("Greater Earthquake")
                .HasActivationDelay(6.0f)
                .DisplaysVisualEffectWhenActivating(VisualEffect.Vfx_Dur_Aura_Blue)
                .HasRecastDelay(RecastGroup.GreaterEarthquake, 180f)
                .IsCastedAbility()
                .HasMaxRange(30f)
                .IsAreaAbility()
                .RequiresTarget()
                .IsHostileAbility()
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
                            var damageType = CombatDamageType.Physical;

                            StatusEffect.ApplyStatusEffect(activator, nearest, typeof(KnockdownStatusEffect), duration, damageType);
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Chunk_Stone_Small), nearest);

                            SendMessageToPC(nearest, "The earthquake knocks you down!");

                            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.Invalid);
                            var defense = Stat.GetDefense(nearest, damageType, AbilityType.Vitality);
                            var defenderStat = GetAbilityScore(nearest, AbilityType.Vitality);
                            var damage = Combat.CalculateDamage(
                                attack,
                                dmg,
                                attackerStat,
                                defense,
                                defenderStat,
                                0);
                            damage = Resistance.ApplyResistanceToDamage(nearest, damageType, damage);

                            var damageTarget = nearest;
                            AssignCommand(activator, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, damageType.GetNWScriptDamageType()), damageTarget));
                            Ability.ApplyHostileAbilityEnmity(activator, damageTarget, damage);
                        }

                        count++;
                        nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, count);
                    }
                });
        }
    }
}
