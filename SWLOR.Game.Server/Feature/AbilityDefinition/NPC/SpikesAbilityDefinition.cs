using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class SpikesAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Spikes();

            return _builder.Build();
        }

        private void Spikes()
        {
            _builder
                .Create(FeatType.Spikes, PerkType.Invalid)
                .Name("Spikes")
                .HasActivationDelay(3.5f)
                .HasRecastDelay(RecastGroup.Spikes, 20f)
                .IsCastedAbility()
                .IsSingleTargetAbility()
                .RequiresTarget()
                .IsHostileAbility()
                .RequirementStamina(8)
                .HasImpactAction((activator, target, level, location) =>
                {
                    const int DMG = 3;
                    var attackerStat = GetAbilityScore(activator, AbilityType.Might);
                    var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.Invalid);
                    var damageType = CombatDamageType.Physical;
                    var defense = Stat.GetDefense(target, damageType, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(
                        attack,
                        DMG,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);
                    damage = Resistance.ApplyResistanceToDamage(target, damageType, damage);

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Wallspike), target);
                    StatusEffect.ApplyStatusEffect(activator, target, typeof(BleedStatusEffect), 45f, damageType);
                });
        }
    }
}
