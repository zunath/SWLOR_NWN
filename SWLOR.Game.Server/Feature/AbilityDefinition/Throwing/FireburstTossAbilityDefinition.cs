using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class FireburstTossAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FireburstToss1(builder);

            return builder.Build();
        }

        private static void FireburstToss1(AbilityBuilder builder)
        {
            builder.Create(FeatType.FireburstToss1, PerkType.FireburstToss)
                .Name("Fireburst Toss")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FireburstToss, 60f)
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
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Throwing, 20, 12, 15, SavingThrow.Fortitude, typeof(ExposedStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f);
                    break;
            }
        }
    }
}
