using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class ThunderousChallengeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ThunderousChallenge1(builder);

            return builder.Build();
        }

        private static void ThunderousChallenge1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ThunderousChallenge1, PerkType.ThunderousChallenge)
                .Name("Thunderous Challenge")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ThunderousChallenge, 120f)
                .HasImpactAction(ThunderousChallenge1ImpactAction)
                .HasTargetingLine(
                    Spell.ThunderousChallenge1,
                    8f,
                    2.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void ThunderousChallenge1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Lightsaber,
                35,
                0,
                null,
                CombatImpactAreaShape.Line,
                0.25f,
                8f,
                2.5f);
        }
    }
}
