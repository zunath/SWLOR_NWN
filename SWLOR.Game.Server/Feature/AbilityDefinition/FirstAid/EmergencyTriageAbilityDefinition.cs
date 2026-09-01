using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public sealed class EmergencyTriageAbilityDefinition : IAbilityListDefinition
    {
        private const float RangeMeters = 15f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            EmergencyTriage1(builder);

            return builder.Build();
        }

        private static void EmergencyTriage1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.EmergencyTriage1, PerkType.EmergencyTriage)
                .Name("Emergency Triage")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetSalute)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.EmergencyTriage, 24f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .IsHealingAbility()
                .HasMaxRange(RangeMeters)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(EmergencyTriage1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8)
                .RequirementItem("med_supplies", 2);
        }

        private static void EmergencyTriage1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var friendly = AbilityTargeting.ResolveFriendlyTarget(activator, target);
            var multiplier = GetCurrentHitPoints(friendly) <= GetMaxHitPoints(friendly) * 0.35f ? 2f : 1f;
            FirstAidTreatmentAdjustments.ApplyActivatedMedicalScaledHeal(activator, friendly, 18, multiplier: multiplier);
            FirstAidTreatmentAdjustments.ApplyTraumaMedicRiders(activator, friendly);
            FirstAidTreatmentAdjustments.GrantCombatPoint(activator);
        }
    }
}
