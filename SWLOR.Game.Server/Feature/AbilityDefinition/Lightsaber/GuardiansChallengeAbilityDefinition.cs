using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class GuardiansChallengeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            GuardiansChallenge1(builder);
            GuardiansChallenge2(builder);

            return builder.Build();
        }

        private static void GuardiansChallenge1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardiansChallenge1, PerkType.GuardiansChallenge)
                .Name("Guardian's Challenge I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.GuardiansChallenge, 90f)
                .HasImpactAction(GuardiansChallenge1ImpactAction)
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void GuardiansChallenge1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Lightsaber, 35, 0, null, CombatImpactAreaShape.Cone, 0.25f, 5f, 5f);
        }

        private static void GuardiansChallenge2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardiansChallenge2, PerkType.GuardiansChallenge)
                .Name("Guardian's Challenge II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.GuardiansChallenge, 120f)
                .HasImpactAction(GuardiansChallenge2ImpactAction)
                .HasTargetingLine(
                    Spell.GuardiansChallenge2,
                    8f,
                    2.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void GuardiansChallenge2ImpactAction(uint activator, uint target, int level, Location targetLocation)
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
