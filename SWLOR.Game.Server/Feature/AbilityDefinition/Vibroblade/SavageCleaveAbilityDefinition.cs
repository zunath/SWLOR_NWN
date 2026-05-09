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
            builder.Create(FeatType.SavageCleave1, PerkType.SavageCleave)
                .Name("Savage Cleave")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SavageCleave, 45f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Vibroblade, 25, 0, 0, SavingThrow.Will, null, CombatImpactAreaShape.Cone, 0.25f, 5f, 5f);
                    break;
            }
        }
    }
}
