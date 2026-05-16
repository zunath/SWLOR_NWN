using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class DuelistsChallengeAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.DuelistsChallenge1, PerkType.DuelistsChallenge)
                .Name("Duelist's Challenge")
                .Level(1)
                .SkillType(SkillType.TwinBlade)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasRecastDelay(RecastGroup.DuelistsChallenge, 120f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    if (StatusEffect.ApplyStatusEffect(activator, target, typeof(DuelistsChallengeStatusEffect), 20f, CombatDamageType.Physical))
                    {
                        StatusEffect.ApplyStatusEffect(target, activator, typeof(DuelistsChallengeSelfStatusEffect), 20f);
                    }

                    Ability.ApplyHostileAbilityEnmity(activator, target);
                })
                .IsSingleTargetAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);

            return builder.Build();
        }
    }
}
