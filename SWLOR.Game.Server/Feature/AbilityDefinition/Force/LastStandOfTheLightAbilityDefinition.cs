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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class LastStandOfTheLightAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            LastStandOfTheLight1(builder);

            return builder.Build();
        }

        private static void LastStandOfTheLight1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.LastStandOfTheLight1, PerkType.LastStandOfTheLight)
                .Name("Last Stand of the Light")
                .Level(1)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.LoopingConjure1)
                .PlaysSoundOnImpact("ksfx_frc_armor")
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(LastStandOfTheLight1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(CapstoneAbility.ForceCost);
        }

        private static void LastStandOfTheLight1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var friendly = AbilityTargeting.ResolveFriendlyTarget(activator, target);
            StatusEffect.ApplyStatusEffect(activator, friendly, typeof(LastStandOfTheLight1StatusEffect), CapstoneAbility.ActiveDurationSeconds);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), friendly);
        }
    }
}
