using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ForceMendAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceMend1(builder);

            return builder.Build();
        }

        private static void ForceMend1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceMend1, PerkType.ForceMend)
                .Name("Force Mend")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.ForceMend, 30f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(ForceMend1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(6);
        }

        private static void ForceMend1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.RemoveFirstCleanseableStatusEffect(friendly, StatusEffectCleanseType.Purify, false);
                AbilityEffectScaling.ApplyScaledHeal(activator, friendly, 16);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
            }
        }
    }
}
