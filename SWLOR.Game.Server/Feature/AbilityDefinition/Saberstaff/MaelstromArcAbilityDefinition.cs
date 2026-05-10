using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class MaelstromArcAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            MaelstromArc1(builder);
            MaelstromArc2(builder);

            return builder.Build();
        }

        private static void MaelstromArc1(AbilityBuilder builder)
        {
            builder.Create(FeatType.MaelstromArc1, PerkType.MaelstromArc)
                .Name("Maelstrom Arc I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.MaelstromArc, 60f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void MaelstromArc2(AbilityBuilder builder)
        {
            builder.Create(FeatType.MaelstromArc2, PerkType.MaelstromArc)
                .Name("Maelstrom Arc II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.MaelstromArc, 60f)
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
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Saberstaff, 22, 12, typeof(DisorientedStatusEffect), CombatImpactAreaShape.Cone, 0.25f, 5f, 5f);
                    break;
                case 2:
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Saberstaff, 32, 15, typeof(DisorientedStatusEffect), CombatImpactAreaShape.Cone, 0.25f, 5f, 5f);
                    break;
            }
        }
    }
}
