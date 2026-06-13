using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class PointBlankBurstAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PointBlankBurst1(builder);

            return builder.Build();
        }

        private static void PointBlankBurst1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PointBlankBurst1, PerkType.PointBlankBurst)
                .Name("Point Blank Burst")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.PointBlankBurst, 90f)
                .SkillType(SkillType.Pistol)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.PointPistol)
                .IsAreaAbility()
                .HasImpactAction(PointBlankBurst1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void PointBlankBurst1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Pistol, 18, 3, typeof(KnockdownStatusEffect), CombatImpactAreaShape.Cone, 0.25f, 5f, 5f);
        }
    }
}
