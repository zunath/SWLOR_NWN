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
    public sealed class CircleOfHarmonyAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CircleOfHarmony1(builder);

            return builder.Build();
        }

        private static void CircleOfHarmony1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CircleOfHarmony1, PerkType.CircleOfHarmony)
                .Name("Circle of Harmony")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.Force)
                .IsAreaAbility()
                .HasImpactAction(CircleOfHarmony1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(CapstoneAbility.ForceCost);
        }

        private static void CircleOfHarmony1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, true))
            {
                StatusEffect.RemoveFirstCleanseableStatusEffect(friendly, StatusEffectCleanseType.TreatmentKit2, false);
                AbilityEffectScaling.ApplyScaledHeal(activator, friendly, 14);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(CircleOfHarmony1StatusEffect), CapstoneAbility.ActiveDurationSeconds);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
            }
        }
    }
}
