using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityServicex;


using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public class RousingShoutAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            RousingShout(builder);

            return builder.Build();
        }

        private static void RousingShout(IAbilityBuilder builder)
        {
            builder.Create(FeatType.RousingShout, PerkType.RousingShout)
                .Name("Rousing Shout")
                .Level(1)
                .HasRecastDelay(RecastGroup.RousingShout, 300f)
                .HasActivationDelay(8f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
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
                    var perkService = App.Resolve<IPerkService>();
                    var abilityService = App.Resolve<IAbilityService>();
                    var combatPointService = App.Resolve<ICombatPointService>();
                    var enmityService = App.Resolve<IEnmityService>();

                    var perkLevel = perkService.GetPerkLevel(activator, PerkType.RousingShout);

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
                    abilityService.ReapplyPlayerAuraAOE(target);

                    if (hp > 0)
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectHeal(hp), target);
                    }

                    combatPointService.AddCombatPointToAllTagged(activator, SkillType.Leadership, 3);
                    enmityService.ModifyEnmityOnAll(activator, 850);
                });
        }
    }
}
