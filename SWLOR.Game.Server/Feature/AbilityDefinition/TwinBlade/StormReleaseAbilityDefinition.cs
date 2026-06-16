using System;
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
    public class StormReleaseAbilityDefinition : IAbilityListDefinition
    {
        private const int DamagePerMomentumStack = 15;
        private const int DefaultMomentumHastePercentPerStack = 5;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            StormRelease1(builder);

            return builder.Build();
        }

        private static void StormRelease1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.StormRelease1, PerkType.StormRelease)
                .Name("Storm Release")
                .Level(1)
                .SkillType(SkillType.TwinBlade)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.Whirlwind)
                .HasRecastDelay(RecastGroup.StormRelease, 120f)
                .HasCustomValidation(ValidateHasMomentumStacks)
                .HasImpactAction(StormRelease1ImpactAction)
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void StormRelease1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var momentumStacks = ConsumeMomentumStacks(activator);
            if (momentumStacks <= 0)
                return;

            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.TwinBlade,
                DamagePerMomentumStack * momentumStacks,
                0,
                null,
                true);
        }

        private static string ValidateHasMomentumStacks(uint activator, uint target, int level, Location targetLocation)
        {
            return GetMomentumStackCount(activator) > 0
                ? string.Empty
                : "You have no Momentum stacks.";
        }

        private static int ConsumeMomentumStacks(uint activator)
        {
            var stackCount = GetMomentumStackCount(activator);
            if (stackCount <= 0)
                return 0;

            TemporaryStatModifier.Consume(
                activator,
                StatType.AttackDelayReductionPercent,
                StatType.TwinBladeAreaAbilityHastePercentAdjustment);

            return stackCount;
        }

        private static int GetMomentumStackCount(uint activator)
        {
            var hastePercent = TemporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.AttackDelayReductionPercent,
                StatType.TwinBladeAreaAbilityHastePercentAdjustment);
            if (hastePercent <= 0)
                return 0;

            return Math.Max(1, hastePercent / DefaultMomentumHastePercentPerStack);
        }
    }
}
