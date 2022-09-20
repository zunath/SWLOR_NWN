using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public class RousingShoutAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            RousingShout();

            return _builder.Build();
        }

        private void RousingShout()
        {
            _builder.Create(FeatType.RousingShout, PerkType.RousingShout)
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
                .HasImpactAction((activator, target, level, location) =>
                {
                    var social = GetAbilityScore(activator, AbilityType.Social);
                    var targetMaxHP = GetMaxHitPoints(target);
                    int hp;

                    switch (level)
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

                    if (hp > 0)
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectHeal(hp), target);
                    }

                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 3);
                    Enmity.ModifyEnmityOnAll(activator, 850);
                });
        }
    }
}
