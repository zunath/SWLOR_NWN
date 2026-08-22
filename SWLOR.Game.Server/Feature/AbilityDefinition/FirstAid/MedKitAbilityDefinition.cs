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
    public sealed class MedKitAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            MedKit1(builder);
            MedKit2(builder);
            MedKit3(builder);
            MedKit4(builder);

            return builder.Build();
        }

        private static void MedKit1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.MedKit1, PerkType.MedKit)
                .Name("Med Kit I")
                .Level(1)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.LoopingGetMid)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(MedKit1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4)
                .RequirementItem("med_supplies");
        }

        private static void MedKit2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.MedKit2, PerkType.MedKit)
                .Name("Med Kit II")
                .Level(2)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.LoopingGetMid)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(MedKit2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6)
                .RequirementItem("med_supplies");
        }

        private static void MedKit3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.MedKit3, PerkType.MedKit)
                .Name("Med Kit III")
                .Level(3)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.LoopingGetMid)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(MedKit3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(7)
                .RequirementItem("med_supplies");
        }

        private static void MedKit4(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.MedKit4, PerkType.MedKit)
                .Name("Med Kit IV")
                .Level(4)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.LoopingGetMid)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(MedKit4ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8)
                .RequirementItem("med_supplies");
        }

        private static void MedKit1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyMedKit(activator, target, 10);
        }

        private static void MedKit2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyMedKit(activator, target, 20);
        }

        private static void MedKit3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyMedKit(activator, target, 28);
        }

        private static void MedKit4ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyMedKit(activator, target, 36);
        }

        private static void ApplyMedKit(uint activator, uint target, int percent)
        {
            var applied = false;
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                HealPercent(activator, friendly, SkillType.FirstAid, percent);
                FirstAidTreatmentAdjustments.ApplyTraumaMedicRiders(activator, friendly);
                FirstAidTreatmentAdjustments.ApplyMedicalVisualEffect(friendly);
                applied = true;
            }

            if (applied)
                CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid);
        }


        private static void HealPercent(uint activator, uint target, SkillType skill, int percent)
        {
            var ability = skill switch
            {
                SkillType.Leadership => AbilityType.Social,
                SkillType.Devices => AbilityType.Perception,
                SkillType.BeastMastery => AbilityType.Might,
                _ => AbilityType.Willpower
            };
            var baseAmount = GameMath.PercentOf(GetMaxHitPoints(target), percent);
            var amount = SWLOR.Game.Server.Feature.AbilityDefinition.AbilityEffectScaling.ScaleDirectEffect(baseAmount, GetAbilityScore(activator, ability));
            amount = Stat.ApplyOutgoingAbilityHealingAdjustment(activator, amount);
            amount = Ability.ApplyCombatReadinessToActivatedAbilityMagnitude(activator, amount);
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            FirstAidTreatmentAdjustments.ApplyMedicalVisualEffect(target);
        }
    }
}
