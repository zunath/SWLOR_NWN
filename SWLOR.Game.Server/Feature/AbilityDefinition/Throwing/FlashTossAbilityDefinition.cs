using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class FlashTossAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FlashToss1(builder);
            FlashToss2(builder);

            return builder.Build();
        }

        private static void FlashToss1(AbilityBuilder builder)
        {
            builder.Create(FeatType.FlashToss1, PerkType.FlashToss)
                .Name("Flash Toss I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FlashToss, 45f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void FlashToss2(AbilityBuilder builder)
        {
            builder.Create(FeatType.FlashToss2, PerkType.FlashToss)
                .Name("Flash Toss II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FlashToss, 45f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Throwing, 6, 6, typeof(BlindStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f);
                    break;
                case 2:
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Throwing, 22, 10, typeof(BlindStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f);
                    break;
            }
        }
    }
}
