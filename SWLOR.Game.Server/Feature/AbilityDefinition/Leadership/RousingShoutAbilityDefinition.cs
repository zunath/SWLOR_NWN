using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

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
            _builder
                .Create(FeatType.RousingShout, PerkType.RousingShout)
                .Name("Rousing Shout")
                .Level(1)
                .HasRecastDelay(RecastGroup.RousingShout, 300f)
                .HasActivationDelay(8f)
                .RequirementStamina(6)
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
                    var socAbove10 = Math.Max(0, social - 10);
                    var temporaryHPPercent = 0.04f + socAbove10 * 0.0015f;
                    int hp;
                    var perkLevel = Perk.GetPerkLevel(activator, PerkType.RousingShout);

                    switch (perkLevel)
                    {
                        default:
                        case 1:
                            hp = 1;
                            break;
                        case 2:
                            hp = (int)Math.Ceiling((0.08f + socAbove10 * 0.004f) * targetMaxHP);
                            temporaryHPPercent = 0.06f + socAbove10 * 0.002f;
                            break;
                        case 3:
                            hp = (int)Math.Ceiling((0.14f + socAbove10 * 0.006f) * targetMaxHP);
                            temporaryHPPercent = 0.08f + socAbove10 * 0.0025f;
                            break;
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectResurrection(), target);
                    ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints((int)Math.Ceiling(targetMaxHP * temporaryHPPercent)), target, 10f);
                    Ability.ReapplyPlayerAuraAOE(target);
                    DelayCommand(0.1f, () => Ability.ReapplyAuraEffectsForCreature(target));

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
