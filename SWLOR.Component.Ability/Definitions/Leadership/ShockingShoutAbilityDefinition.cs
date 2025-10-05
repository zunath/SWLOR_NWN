using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Leadership
{
    public class ShockingShoutAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ShockingShoutAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ShockingShout(builder);

            return builder.Build();
        }

        private void ShockingShout(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ShockingShout, PerkType.ShockingShout)
                .Name("Shocking Shout")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ShockingShout, 180f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(AnimationType.FireForgetTaunt)
                .HasImpactAction((activator, target, level, location) =>
                {
                    const float RangeMeters = 10f;
                    const int MaxTargets = 6;
                    var nth = 1;
                    var count = 0;

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Sound_Burst), activator);

                    var nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, nth);
                    while (GetIsObjectValid(nearest))
                    {

                        if (GetDistanceBetween(activator, nearest) > RangeMeters)
                        {
                            break;
                        }

                        if (GetIsReactionTypeHostile(nearest, activator))
                        {
                            count++;

                            var dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Will, 14);
                            const float BaseDuration = 2f;
                            var bonusDuration = GetAbilityModifier(AbilityType.Social, activator) * 0.5f;
                            var duration = BaseDuration + bonusDuration;

                            var checkResult = WillSave(nearest, dc, SavingThrowType.None, activator);
                            if (checkResult == SavingThrowResultType.Failed)
                            {
                                ApplyEffectToObject(DurationType.Temporary, EffectStunned(), nearest, duration);
                                AbilityService.ApplyTemporaryImmunity(target, duration, ImmunityType.Stun);
                                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Head_Sonic), nearest);
                            }

                            CombatPointService.AddCombatPoint(activator, nearest, SkillType.Leadership, 3);
                            EnmityService.ModifyEnmity(activator, target, 650);
                        }

                        if (count > MaxTargets)
                        {
                            break;
                        }

                        nth++;
                        nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, nth);
                    }

                });
        }
    }
}
