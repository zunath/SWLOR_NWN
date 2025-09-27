using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.NPC
{
    public class SpikesAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public SpikesAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatusEffectService StatusEffectService => _serviceProvider.GetRequiredService<IStatusEffectService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Spikes(builder);

            return builder.Build();
        }

        private void Spikes(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Spikes, PerkType.Invalid)
                .Name("Spikes")
                .HasActivationDelay(3.5f)
                .HasRecastDelay(RecastGroupType.Spikes, 20f)
                .IsCastedAbility()
                .RequirementStamina(8)
                .HasImpactAction((activator, target, level, location) =>
                {
                    const int DMG = 3;
                    var attackerStat = GetAbilityScore(activator, AbilityType.Might);
                    var attack = StatService.GetAttack(activator, AbilityType.Might, SkillType.Invalid);
                    var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = CombatService.CalculateDamage(
                        attack,
                        DMG, 
                        attackerStat, 
                        defense, 
                        defenderStat, 
                        0);

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Wallspike), target);
                    StatusEffectService.Apply(activator, target, StatusEffectType.Bleed, 45f);
                });
        }

    }
}
