using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class ConcussiveTossAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConcussiveToss1(builder);
            ConcussiveToss2(builder);

            return builder.Build();
        }

        private static void ConcussiveToss1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ConcussiveToss1, PerkType.ConcussiveToss)
                .Name("Concussive Toss I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ConcussiveToss, 60f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void ConcussiveToss2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ConcussiveToss2, PerkType.ConcussiveToss)
                .Name("Concussive Toss II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ConcussiveToss, 60f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Throwing, 14, 2, typeof(DazedStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f);
                    break;
                case 2:
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Throwing, 26, 3, typeof(DazedStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f);
                    break;
            }
        }
    }
}
