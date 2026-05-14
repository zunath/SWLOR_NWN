using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class PinningFireAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PinningFire1(builder);
            PinningFire2(builder);

            return builder.Build();
        }

        private static void PinningFire1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PinningFire1, PerkType.PinningFire)
                .Name("Pinning Fire I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.PinningFire, 45f)
                .RequiresTarget()
                .HasImpactAction(PinningFire1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void PinningFire2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PinningFire2, PerkType.PinningFire)
                .Name("Pinning Fire II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.PinningFire, 60f)
                .HasImpactAction(PinningFire2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void PinningFire1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Rifle, 10, 2, typeof(DazedStatusEffect), false);
        }

        private static void PinningFire2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Rifle, 18, 3, typeof(KnockdownStatusEffect), CombatImpactAreaShape.Line, 0.25f, 8f, 2.5f);
        }
    }
}
