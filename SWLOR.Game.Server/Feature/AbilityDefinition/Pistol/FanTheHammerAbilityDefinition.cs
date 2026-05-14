using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class FanTheHammerAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FanTheHammer1(builder);
            FanTheHammer2(builder);

            return builder.Build();
        }

        private static void FanTheHammer1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FanTheHammer1, PerkType.FanTheHammer)
                .Name("Fan the Hammer I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FanTheHammer, 60f)
                .HasImpactAction(FanTheHammer1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void FanTheHammer2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FanTheHammer2, PerkType.FanTheHammer)
                .Name("Fan the Hammer II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FanTheHammer, 75f)
                .HasImpactAction(FanTheHammer2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void FanTheHammer1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Pistol, 12, 0, null, CombatImpactAreaShape.Cone, 0.25f, 5f, 5f, maxTargets: 3);
        }

        private static void FanTheHammer2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Pistol, 20, 0, null, CombatImpactAreaShape.Cone, 0.25f, 5f, 5f, maxTargets: 5);
        }
    }
}
