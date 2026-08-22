using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public sealed class EmergencyCocktailAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            EmergencyCocktail1(builder);

            return builder.Build();
        }

        private static void EmergencyCocktail1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.EmergencyCocktail1, PerkType.EmergencyCocktail)
                .Name("Emergency Cocktail")
                .Level(1)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetSalute)
                .PlaysSoundOnImpact("ksfx_frc_buff")
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(EmergencyCocktail1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost)
                .RequirementItem("stim_pack", preserveChanceStatType: StatType.StimPackPreserveChance);
        }

        private static void EmergencyCocktail1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = CapstoneAbility.ActiveDurationSeconds;
            var applied = false;
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                Stat.RestoreStamina(friendly, GameMath.PercentOf(Stat.GetMaxStamina(friendly), 25));
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(EmergencyCocktailStatusEffect), duration);
                AbilityEffectScaling.ApplyTemporaryHPPercent(activator, friendly, "EMERGENCY_COCKTAIL", 12, duration);
                StatusEffect.RemoveFirstStatusEffect(friendly, new[] { typeof(PoisonStatusEffect), typeof(ToxinStatusEffect) }, false);
                FirstAidTreatmentAdjustments.ApplyCombatPharmacologyStimRiders(activator, friendly);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), friendly);
                applied = true;
            }

            FirstAidTreatmentAdjustments.GrantCombatPointIfApplied(activator, applied);
        }
    }
}
