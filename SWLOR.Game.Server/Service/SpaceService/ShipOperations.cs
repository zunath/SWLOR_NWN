using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public sealed record ShipModuleOperation(ShipModuleProfile Profile, ShipModuleVariant Variant,
        double Output, double Tracking, int CapacitorCost);

    public static class ShipOperations
    {
        public static ShipModuleOperation Resolve(ShipStatus status, ShipStatus.ShipStatusModule fitted,
            int perception = 10, IReadOnlyDictionary<StatType, double> temporary = null, ShipFittingCatalog catalog = null)
        {
            catalog ??= ShipFittingCatalog.Default;
            var profile = catalog.Modules[fitted.Design];
            if (fitted.Condition <= 0) throw new InvalidOperationException("That module needs servicing at a dock.");
            var baseline = catalog.GetVariant(fitted.Design, fitted.Calibration);
            var variant = ShipModuleTuning.Refine(profile, baseline, fitted.QualityDimension, fitted.Quality);
            var quality = ShipModuleTuning.QualityCap * fitted.Quality / 100.0;
            double Temp(StatType stat) => temporary?.GetValueOrDefault(stat) ?? 0;
            double Bonus(StatType stat) => ShipFittedStats.Bonus(status, stat);
            double Penalty(StatType stat) => ShipFittedStats.Penalty(status, stat);
            var outputStat = profile.Action == ShipModuleAction.Weapon ? StatType.ShipWeaponOutput : StatType.ShipRecoveryOutput;
            var scalesWithMastery = profile.Action is ShipModuleAction.Weapon or ShipModuleAction.SelfShieldRepair or ShipModuleAction.SelfHullRepair or ShipModuleAction.ShieldRepair or ShipModuleAction.HullRepair or ShipModuleAction.RepairField;
            var permanent = (scalesWithMastery ? Bonus(outputStat) : 0) + (fitted.QualityDimension == ShipQualityDimension.Output ? quality : 0);
            if (profile.Action == ShipModuleAction.Weapon)
                permanent += .0025 * (Math.Clamp(perception, 10, 26) - 10);
            if (profile.Action is ShipModuleAction.ShieldRepair or ShipModuleAction.HullRepair or ShipModuleAction.RepairField)
                permanent += Bonus(StatType.ShipExternalRecoveryOutput);
            var output = ShipModuleTuning.Output(profile.Output, baseline.Output, permanent, (scalesWithMastery ? Penalty(outputStat) + Math.Max(0, -Temp(outputStat)) : 0), new[] { scalesWithMastery ? Math.Max(0, Temp(outputStat)) : 0 });
            var trackingQuality = fitted.QualityDimension == ShipQualityDimension.Tracking ? quality : 0;
            var calibrationTracking = profile.Tracking == 0 ? 0 : baseline.Tracking / profile.Tracking - 1;
            var tracking = Math.Max(0, profile.Tracking * (1 + Math.Min(.4, Bonus(StatType.ShipTracking) + trackingQuality + Math.Max(0, calibrationTracking))
                - Penalty(StatType.ShipTracking) - Math.Max(0, -calibrationTracking) + Math.Min(.3, Temp(StatType.ShipTracking))));
            var demand = profile.Action == ShipModuleAction.Weapon ? Bonus(StatType.ShipWeaponCapacitorDemand) : 0;
            var demandDiscount = profile.Action == ShipModuleAction.Weapon ? Penalty(StatType.ShipWeaponCapacitorDemand) : 0;
            var cost = ShipModuleTuning.CapacitorCost(profile.Capacitor, variant.CapacitorMultiplier,
                Bonus(StatType.ShipCapacitorDiscount) + demandDiscount + Math.Max(0, Temp(StatType.ShipCapacitorDiscount)),
                demand + Penalty(StatType.ShipCapacitorDiscount) + Math.Max(0, -Temp(StatType.ShipCapacitorDiscount)));
            return new(profile, variant, output, tracking, cost);
        }

        public static double MovementSpeed(ShipStatus status, DateTime now)
        {
            var temporary = ShipTemporaryStats.Current(status, now).GetValueOrDefault(StatType.ShipSpeed);
            var basis = status.BaseSpeed > 0 ? status.BaseSpeed : status.Speed;
            return Math.Max(.1, status.Speed + basis * (Math.Min(.35, Math.Max(0, temporary)) + Math.Min(0, temporary)));
        }

        public static double ResistanceMultiplier(double permanentRating, IEnumerable<double> temporaryRatings = null)
        {
            var temporary = temporaryRatings?.DefaultIfEmpty(0).Max() ?? 0;
            if (!double.IsFinite(permanentRating) || !double.IsFinite(temporary) || permanentRating < 0 || temporary < 0)
                throw new ArgumentOutOfRangeException(nameof(permanentRating));
            var rating = Math.Min(85, Math.Min(60, permanentRating) + Math.Min(25, temporary));
            return 100 / (100 + rating);
        }

        public static (double Shield, double Hull) ApplyDamage(ShipStatus target, double rawDamage,
            double shieldMultiplier = 1, double hullMultiplier = 1, double temporaryShieldResistance = 0, double temporaryHullResistance = 0)
        {
            if (!double.IsFinite(rawDamage) || rawDamage < 0 || !double.IsFinite(shieldMultiplier) || shieldMultiplier <= 0 ||
                !double.IsFinite(hullMultiplier) || hullMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(rawDamage));
            var shieldScale = shieldMultiplier * ResistanceMultiplier(target.ShieldResistance, new[] { temporaryShieldResistance });
            var shieldRaw = Math.Min(rawDamage, ShipResources.Available(target, ShipResource.Shield) / shieldScale);
            var shield = ShipResources.SpendPrecise(target, ShipResource.Shield, shieldRaw * shieldScale);
            var hull = ShipResources.SpendPrecise(target, ShipResource.Hull, Math.Max(0, rawDamage - shieldRaw) * hullMultiplier *
                ResistanceMultiplier(target.HullResistance, new[] { temporaryHullResistance }));
            return (shield, hull);
        }

        public static void Recover(ShipStatus status, DateTime now, double elapsedSeconds = 1)
        {
            if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            ShipResources.RestorePrecise(status, ShipResource.Capacitor, status.CapacitorRecovery * elapsedSeconds);
            if (now >= status.LastHostileActivity.AddSeconds(10))
                ShipResources.RestorePrecise(status, ShipResource.Shield, status.OutOfCombatShieldRecovery * elapsedSeconds);
        }
    }
}
