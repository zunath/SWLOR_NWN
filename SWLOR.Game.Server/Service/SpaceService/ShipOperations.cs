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
            int perception = 10, IReadOnlyDictionary<StatType, double> temporary = null, ShipFittingCatalog catalog = null, ShipTemporarySources temporarySources = null)
        {
            catalog ??= ShipFittingCatalog.Default;
            var profile = catalog.Modules[fitted.Design];
            if (fitted.Condition <= 0) throw new InvalidOperationException("That module needs servicing at a dock.");
            var baseline = catalog.GetVariant(fitted.Design, fitted.Calibration);
            var variant = ShipModuleTuning.Refine(profile, baseline, fitted.QualityDimension, fitted.Quality);
            var quality = ShipModuleTuning.QualityCap * fitted.Quality / 100.0;
            double Temp(StatType stat) => temporarySources?.Net(stat) ?? temporary?.GetValueOrDefault(stat) ?? 0;
            double TempPositive(StatType stat) => temporarySources?.Positive.GetValueOrDefault(stat) ?? Math.Max(0, Temp(stat));
            double TempNegative(StatType stat) => temporarySources?.Negative.GetValueOrDefault(stat) ?? Math.Max(0, -Temp(stat));
            double Bonus(StatType stat) => ShipFittedStats.Bonus(status, stat);
            double Penalty(StatType stat) => ShipFittedStats.Penalty(status, stat);
            var weapon = profile.Action == ShipModuleAction.Weapon;
            var ordnance = weapon && profile.Mount == ShipMount.Ordnance;
            var external = profile.Action is ShipModuleAction.ShieldRepair or ShipModuleAction.HullRepair or ShipModuleAction.RepairField;
            var recovery = external || profile.Action is ShipModuleAction.SelfShieldRepair or ShipModuleAction.SelfHullRepair;
            var outputStat = weapon ? StatType.ShipWeaponOutput : StatType.ShipRecoveryOutput;
            var permanent = ((weapon || recovery) ? Bonus(outputStat) : 0) + (fitted.QualityDimension == ShipQualityDimension.Output ? quality : 0);
            var penalty = (weapon || recovery) ? Penalty(outputStat) : 0;
            var temporaryOutput = (weapon || recovery) ? TempPositive(outputStat) : 0;
            var temporaryPenalty = (weapon || recovery) ? TempNegative(outputStat) : 0;
            if (weapon) permanent += .0025 * (Math.Clamp(perception, 10, 26) - 10);
            if (ordnance)
            {
                permanent += Bonus(StatType.ShipOrdnanceOutput);
                penalty += Penalty(StatType.ShipOrdnanceOutput);
                temporaryOutput = Math.Max(temporaryOutput, TempPositive(StatType.ShipOrdnanceOutput));
                temporaryPenalty += TempNegative(StatType.ShipOrdnanceOutput);
            }
            if (external)
            {
                permanent += Bonus(StatType.ShipExternalRecoveryOutput);
                penalty += Penalty(StatType.ShipExternalRecoveryOutput);
                temporaryOutput = Math.Max(temporaryOutput, TempPositive(StatType.ShipExternalRecoveryOutput));
                temporaryPenalty += TempNegative(StatType.ShipExternalRecoveryOutput);
            }
            var output = ShipModuleTuning.Output(profile.Output, baseline.Output, permanent,
                penalty + temporaryPenalty, new[] { temporaryOutput });
            var trackingQuality = fitted.QualityDimension == ShipQualityDimension.Tracking ? quality : 0;
            var calibrationTracking = profile.Tracking == 0 ? 0 : baseline.Tracking / profile.Tracking - 1;
            var trackingBonus = Bonus(StatType.ShipTracking) + (ordnance ? Bonus(StatType.ShipOrdnanceTracking) : 0);
            var trackingPenalty = Penalty(StatType.ShipTracking) + (ordnance ? Penalty(StatType.ShipOrdnanceTracking) : 0);
            var tracking = Math.Max(0, profile.Tracking * (1 + Math.Min(.4, trackingBonus + trackingQuality + Math.Max(0, calibrationTracking))
                - trackingPenalty - Math.Max(0, -calibrationTracking) + Math.Min(.3, TempPositive(StatType.ShipTracking)) - TempNegative(StatType.ShipTracking)));
            var demand = Bonus(StatType.ShipCapacitorDemand) + TempPositive(StatType.ShipCapacitorDemand) +
                (weapon ? Bonus(StatType.ShipWeaponCapacitorDemand) + TempPositive(StatType.ShipWeaponCapacitorDemand) : 0);
            var demandDiscount = Penalty(StatType.ShipCapacitorDemand) + TempNegative(StatType.ShipCapacitorDemand) + (weapon ? Penalty(StatType.ShipWeaponCapacitorDemand) + TempNegative(StatType.ShipWeaponCapacitorDemand) : 0);
            var discount = Bonus(StatType.ShipCapacitorDiscount) + demandDiscount + TempPositive(StatType.ShipCapacitorDiscount);
            if (profile.DiscountStat != StatType.ShipCapacitorDiscount)
                discount += Bonus(profile.DiscountStat) + TempPositive(profile.DiscountStat);
            var cost = ShipModuleTuning.CapacitorCost(profile.Capacitor, variant.CapacitorMultiplier, discount,
                demand + Penalty(StatType.ShipCapacitorDiscount) + TempNegative(StatType.ShipCapacitorDiscount));
            // Transfers conserve the paid energy; output quality and generic recovery never create capacitor.
            if (profile.Action == ShipModuleAction.CapacitorTransfer) cost = profile.Capacitor;
            if (profile.RangeStat.HasValue)
                variant = variant with { Range = Math.Max(0, variant.Range + profile.Range *
                    (Bonus(profile.RangeStat.Value) - Penalty(profile.RangeStat.Value) + Temp(profile.RangeStat.Value))) };
            var cycleStat = profile.Action == ShipModuleAction.Survey ? StatType.ShipSurveyCycleDuration :
                profile.Action is ShipModuleAction.BulkSalvage or ShipModuleAction.IntactSalvage ? StatType.ShipSalvageCycleDuration : StatType.ShipCycleDuration;
            variant = variant with { Cycle = Math.Max(profile.Cycle * ShipModuleTuning.CycleFloor,
                variant.Cycle + profile.Cycle * (Bonus(cycleStat) - Penalty(cycleStat) + Temp(cycleStat))) };

            return new(profile, variant, output, tracking, cost);
        }

        public static double MovementSpeed(ShipStatus status, DateTime now)
        {
            var sources = ShipTemporaryStats.Sources(status, now);
            if (sources.Net(StatType.ShipMovementLock) > 0) return 0;
            var basis = status.BaseSpeed > 0 ? status.BaseSpeed : status.Speed;
            return Math.Max(.1, status.Speed + basis * (Math.Min(.35, sources.Positive.GetValueOrDefault(StatType.ShipSpeed)) - sources.Negative.GetValueOrDefault(StatType.ShipSpeed)));
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
