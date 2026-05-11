using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class CheapShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CheapShot1(builder);
            CheapShot2(builder);

            return builder.Build();
        }

        private static void CheapShot1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CheapShot1, PerkType.CheapShot)
                .Name("Cheap Shot I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.CheapShot, 45f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void CheapShot2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CheapShot2, PerkType.CheapShot)
                .Name("Cheap Shot II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.CheapShot, 45f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroknife, 8, 6, typeof(BlindStatusEffect), false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroknife, 16, 9, typeof(BlindStatusEffect), false);
                    break;
            }
        }
    }
}
