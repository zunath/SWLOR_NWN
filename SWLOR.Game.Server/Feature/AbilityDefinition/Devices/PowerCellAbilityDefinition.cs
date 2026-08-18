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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public sealed class PowerCellAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PowerCell1(builder);
            PowerCell2(builder);
            PowerCell3(builder);

            return builder.Build();
        }

        private static void PowerCell1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PowerCell1, PerkType.PowerCell)
                .Name("Power Cell I")
                .Level(1)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.PowerCell, 24f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(PowerCell1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void PowerCell2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PowerCell2, PerkType.PowerCell)
                .Name("Power Cell II")
                .Level(2)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.PowerCell, 24f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(PowerCell2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void PowerCell3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PowerCell3, PerkType.PowerCell)
                .Name("Power Cell III")
                .Level(3)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.PowerCell, 30f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsAreaAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(PowerCell3ImpactAction)
                .HasTargetingSphere(
                    Spell.PowerCell3,
                    5f,
                    AbilityTargetingFlags.HelpsAllies)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void PowerCell1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var isInitialTarget = true;
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                Stat.RestoreStamina(friendly, GameMath.PercentOf(Stat.GetMaxStamina(friendly), 10));
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(PowerCell1StatusEffect), 30f);
                ApplyPowerCellRiders(activator, friendly, isInitialTarget);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), friendly);
                isInitialTarget = false;
            }
        }

        private static void PowerCell2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var isInitialTarget = true;
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                Stat.RestoreStamina(friendly, GameMath.PercentOf(Stat.GetMaxStamina(friendly), 18));
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(PowerCell2StatusEffect), 30f);
                ApplyPowerCellRiders(activator, friendly, isInitialTarget);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), friendly);
                isInitialTarget = false;
            }
        }

        private static void PowerCell3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var isInitialTarget = true;
            foreach (var friendly in GetPowerCell3Targets(activator, target))
            {
                Stat.RestoreStamina(friendly, GameMath.PercentOf(Stat.GetMaxStamina(friendly), 18));
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(PowerCell3StatusEffect), 30f);
                ApplyPowerCellRiders(activator, friendly, isInitialTarget);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), friendly);
                isInitialTarget = false;
            }
        }

        private static IEnumerable<uint> GetPowerCell3Targets(uint activator, uint target)
        {
            var seen = new HashSet<uint>();
            var selected = AbilityTargeting.ResolveFriendlyTarget(activator, target);
            if (GetIsObjectValid(selected) &&
                !GetIsDead(selected) &&
                GetCurrentHitPoints(selected) > 0 &&
                seen.Add(selected))
            {
                yield return selected;
            }

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(
                         activator,
                         GetLocation(selected),
                         5f))
            {
                if (seen.Add(friendly))
                    yield return friendly;
            }
        }

        private static void ApplyPowerCellRiders(uint activator, uint target, bool isInitialTarget)
        {
            if (isInitialTarget)
            {
                DeviceAbilityEffects.ApplyPowerSurge(activator, target);
            }

            DeviceAbilityEffects.ApplyFieldSupportAllyBuffRiders(activator, target);
        }
    }
}
