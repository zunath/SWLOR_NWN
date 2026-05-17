using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class LowShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            LowShot1(builder);

            return builder.Build();
        }

        private static void LowShot1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.LowShot1, PerkType.LowShot)
                .Name("Low Shot")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.LowShot, 60f)
                .SkillType(SkillType.Pistol)
                .HasMaxRange(PistolAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(LowShot1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void LowShot1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Pistol, 20, 12, typeof(DisorientedStatusEffect), false);
        }
    }
}
