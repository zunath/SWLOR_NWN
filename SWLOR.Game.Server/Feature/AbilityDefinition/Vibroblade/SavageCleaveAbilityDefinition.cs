using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class SavageCleaveAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SavageCleave1(builder);

            return builder.Build();
        }

        private static void SavageCleave1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SavageCleave1, PerkType.SavageCleave)
                .Name("Savage Cleave")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SavageCleave, 45f)
                .IsAreaAbility()
                .HasImpactAction(SavageCleave1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void SavageCleave1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Vibroblade, 25, 0, null, CombatImpactAreaShape.Cone, 0.25f, 5f, 5f);
        }
    }
}
