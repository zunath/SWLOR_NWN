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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class InnervateAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Innervate1(builder);
            Innervate2(builder);
            Innervate3(builder);

            return builder.Build();
        }

        private static void Innervate1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Innervate1, PerkType.Innervate)
                .Name("Innervate I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetSalute)
                .HasRecastDelay(RecastGroup.Innervate, 12f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .AllowsSelfQuickCast()
                .IsHealingAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Innervate1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(3);
        }

        private static void Innervate2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Innervate2, PerkType.Innervate)
                .Name("Innervate II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetSalute)
                .HasRecastDelay(RecastGroup.Innervate, 12f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .AllowsSelfQuickCast()
                .IsHealingAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Innervate2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void Innervate3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Innervate3, PerkType.Innervate)
                .Name("Innervate III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetSalute)
                .HasRecastDelay(RecastGroup.Innervate, 12f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .AllowsSelfQuickCast()
                .IsHealingAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Innervate3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(6);
        }

        private static void Innervate1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyInnervate(activator, target, new HealAmount(10, 1.5f));
        }

        private static void Innervate2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyInnervate(activator, target, new HealAmount(31, 2.5f));
        }

        private static void Innervate3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyInnervate(activator, target, new HealAmount(63, 3.5f));
        }

        private static void ApplyInnervate(uint activator, uint target, HealAmount heal)
        {
            var healed = new List<uint>();
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                Heal(activator, friendly, heal);
                healed.Add(friendly);
            }

            // Innervate is cast by a beast, so its owner earns the Beast Mastery credit.
            if (healed.Count > 0)
                CombatPoint.AddSupportCombatPoint(GetMaster(activator), healed, SkillType.BeastMastery);
        }

        private static void Heal(uint activator, uint target, HealAmount heal)
        {
            var amount = heal.ForMaxHitPoints(GetMaxHitPoints(target));
            amount = Stat.ApplyOutgoingAbilityHealingAdjustment(activator, amount);
            amount = Ability.ApplyCombatReadinessToActivatedAbilityMagnitude(activator, amount);
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount, isInstantAbilityHeal: true);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);
        }
    }
}
