using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class WhirlwindAssaultAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            WhirlwindAssault1(builder);
            WhirlwindAssault2(builder);

            return builder.Build();
        }

        private static void WhirlwindAssault1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WhirlwindAssault1, PerkType.WhirlwindAssault)
                .Name("Whirlwind Assault I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.WhirlwindAssault, 120f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void WhirlwindAssault2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WhirlwindAssault2, PerkType.WhirlwindAssault)
                .Name("Whirlwind Assault II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.WhirlwindAssault, 120f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroblade, 12, 0, null, true);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroblade, 20, 0, null, true);
                    break;
            }
        }
    }
}
