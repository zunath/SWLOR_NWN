using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class ReversalCutAbilityDefinition : IAbilityListDefinition
    {
        private const float RecentDamageTakenWindowSeconds = 8f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ReversalCut1(builder);

            return builder.Build();
        }

        private static void ReversalCut1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ReversalCut1, PerkType.ReversalCut)
                .Name("Reversal Cut")
                .Level(1)
                .SkillType(SkillType.TwinBlade)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ReversalCut, 60f)
                .RequiresTarget()
                .HasCustomValidation((activator, _, _, _) => ValidateRecentDamageTaken(activator))
                .HasImpactAction(ReversalCut1ImpactAction)
                .IsSingleTargetAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void ReversalCut1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            if (!Combat.HasRecentDamageTaken(activator, RecentDamageTakenWindowSeconds))
            {
                SendMessageToPC(activator, "You must be hit before using Reversal Cut.");
                return;
            }

            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwinBlade, 40, 3, typeof(DazedStatusEffect), false);
        }

        private static string ValidateRecentDamageTaken(uint activator)
        {
            return Combat.HasRecentDamageTaken(activator, RecentDamageTakenWindowSeconds)
                ? string.Empty
                : "You must be hit before using Reversal Cut.";
        }
    }
}
