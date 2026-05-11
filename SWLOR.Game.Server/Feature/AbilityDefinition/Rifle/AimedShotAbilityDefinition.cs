using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class AimedShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            AimedShot1(builder);
            AimedShot2(builder);
            AimedShot3(builder);

            return builder.Build();
        }

        private static void AimedShot1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AimedShot1, PerkType.AimedShot)
                .Name("Aimed Shot I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.AimedShot, 30f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void AimedShot2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AimedShot2, PerkType.AimedShot)
                .Name("Aimed Shot II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.AimedShot, 30f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void AimedShot3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AimedShot3, PerkType.AimedShot)
                .Name("Aimed Shot III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.AimedShot, 30f)
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
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Rifle, 18, 0, null, false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Rifle, 32, 0, null, false);
                    break;
                case 3:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Rifle, 46, 0, null, false);
                    break;
            }
        }
    }
}
