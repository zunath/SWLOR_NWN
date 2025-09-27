using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Leadership
{
    public class RousingShoutAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public RousingShoutAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            RousingShout(builder);

            return builder.Build();
        }

        private void RousingShout(IAbilityBuilder builder)
        {
            builder.Create(FeatType.RousingShout, PerkType.RousingShout)
                .Name("Rousing Shout")
                .Level(1)
                .HasRecastDelay(RecastGroup.RousingShout, 300f)
                .HasActivationDelay(8f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(AnimationType.FireForgetTaunt)
                .HasCustomValidation((activator, target, level, location) =>
                {
                    if (!GetIsDead(target))
                    {
                        return "Your target is not unconscious.";
                    }

                    if (GetArea(activator) != GetArea(target))
                    {
                        return "Your target is too far away.";
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, target, _, _) =>
                {
                    var social = GetAbilityScore(activator, AbilityType.Social);
                    var targetMaxHP = GetMaxHitPoints(target);
                    int hp;
                    var perkLevel = PerkService.GetPerkLevel(activator, PerkType.RousingShout);

                    switch (perkLevel)
                    {
                        default:
                        case 1:
                            hp = 0;
                            break;
                        case 2:
                            hp = (int)(social * 0.01f * targetMaxHP);
                            break;
                        case 3:
                            hp = (int)(2 * social * 0.01f * targetMaxHP);
                            break;
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectResurrection(), target);
                    AbilityService.ReapplyPlayerAuraAOE(target);

                    if (hp > 0)
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectHeal(hp), target);
                    }

                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Leadership, 3);
                    EnmityService.ModifyEnmityOnAll(activator, 850);
                });
        }
    }
}
