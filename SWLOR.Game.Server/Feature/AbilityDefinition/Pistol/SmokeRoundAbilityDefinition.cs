using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class SmokeRoundAbilityDefinition : IAbilityListDefinition
    {
        private const SkillType Skill = SkillType.Pistol;
        private const int EnmityReductionPercent = 10;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SmokeRound1(builder);

            return builder.Build();
        }

        private static void SmokeRound1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SmokeRound1, PerkType.SmokeRound)
                .Name("Smoke Round")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SmokeRound, 120f)
                .SkillType(Skill)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.PointPistol)
                .IsAreaAbility()
                .HasImpactAction(SmokeRound1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void SmokeRound1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                Skill,
                0,
                12,
                typeof(BlindStatusEffect),
                CombatImpactAreaShape.Sphere,
                0.25f,
                5f,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Smoke_Puff,
                afterSuccessfulHit: affectedEnemy => Enmity.ReduceEnmity(activator, affectedEnemy, EnmityReductionPercent),
                alwaysApplyAreaVisualEffect: true);
        }
    }
}
