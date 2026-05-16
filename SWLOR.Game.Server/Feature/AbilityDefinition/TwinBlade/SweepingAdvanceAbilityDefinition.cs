using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class SweepingAdvanceAbilityDefinition : IAbilityListDefinition
    {
        private const int MomentumTargetThreshold = 3;
        private const int MomentumStaminaRestore = 6;
        private const int MomentumHastePercent = 10;
        private const int MomentumDurationSeconds = 8;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SweepingAdvance1(builder);

            return builder.Build();
        }

        private static void SweepingAdvance1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SweepingAdvance1, PerkType.SweepingAdvance)
                .Name("Sweeping Advance")
                .Level(1)
                .SkillType(SkillType.TwinBlade)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SweepingAdvance, 60f)
                .HasImpactAction(SweepingAdvance1ImpactAction)
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void SweepingAdvance1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.TwinBlade,
                24,
                0,
                null,
                CombatImpactAreaShape.Line,
                0.25f,
                8f,
                2.5f,
                afterImpactAction: summary => ApplySweepingMomentum(activator, summary));
        }

        private static void ApplySweepingMomentum(uint activator, AbilityImpactSummary summary)
        {
            if (summary == null || summary.ImpactedTargetCount < MomentumTargetThreshold)
                return;

            Stat.RestoreStamina(activator, MomentumStaminaRestore);
            TemporaryStatModifier.Replace(
                activator,
                StatType.AttackDelayReductionPercent,
                MomentumHastePercent,
                MomentumDurationSeconds,
                StatType.AttackDelayReductionPercent);
        }
    }
}
