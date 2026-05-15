using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class DevouringStrikeAbilityDefinition : IAbilityListDefinition
    {
        private const float LowHPThreshold = 0.35f;
        private const int LowHPDamagePercentBonus = 40;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            DevouringStrike1(builder);

            return builder.Build();
        }

        private static void DevouringStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DevouringStrike1, PerkType.DevouringStrike)
                .Name("Devouring Strike")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.DevouringStrike, 30f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(DevouringStrike1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(7);
        }

        private static void DevouringStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                12,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                damagePercentAdjustment: creature => IsLowHP(creature) ? LowHPDamagePercentBonus : 0);
        }

        private static bool IsLowHP(uint target)
        {
            return GetIsObjectValid(target) &&
                   GetMaxHitPoints(target) > 0 &&
                   GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * LowHPThreshold;
        }
    }
}
