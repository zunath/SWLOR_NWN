using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class SuppressiveLineAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SuppressiveLine1(builder);

            return builder.Build();
        }

        private static void SuppressiveLine1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SuppressiveLine1, PerkType.SuppressiveLine)
                .Name("Suppressive Line")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SuppressiveLine, 60f)
                .SkillType(SkillType.Rifle)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasTargetingLine(
                    Spell.SuppressiveLine1,
                    8f,
                    2.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsAreaAbility()
                .HasImpactAction(SuppressiveLine1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void SuppressiveLine1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Rifle, 22, 12, typeof(DisorientedStatusEffect), CombatImpactAreaShape.Line, 0.25f, 8f, 2.5f);
        }
    }
}
