using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class SapVitalityAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SapVitality1(builder);
            SapVitality2(builder);

            return builder.Build();
        }

        private static void SapVitality1(AbilityBuilder builder)
        {
            builder.Create(FeatType.SapVitality1, PerkType.SapVitality)
                .Name("Sap Vitality")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SapVitality, 60f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void SapVitality2(AbilityBuilder builder)
        {
            builder.Create(FeatType.SapVitality2, PerkType.SapVitality)
                .Name("Sap Vitality II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SapVitality, 60f)
                .RequiresTarget()
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
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroknife, 20, 15, 12, SavingThrow.Fortitude, typeof(ExhaustedStatusEffect), false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroknife, 35, 15, 16, SavingThrow.Fortitude, typeof(ExhaustedStatusEffect), false);
                    break;
            }
        }
    }
}
