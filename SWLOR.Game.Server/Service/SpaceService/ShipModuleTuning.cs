using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public static class ShipModuleTuning
    {
        public const double PermanentOutputCap = 0.40;
        public const double TemporaryOutputCap = 0.30;
        public const double QualityCap = 0.08;
        public const double CapacitorDiscountCap = 0.25;
        public const double CycleFloor = 0.85;
        public const double RecoveryFractionCap = 0.95;

        public static ShipModuleVariant Refine(ShipModuleProfile module, ShipModuleVariant variant,
            ShipQualityDimension dimension, int quality)
        {
            if (module.Id != variant.Design) throw new ArgumentException("Variant belongs to a different module.");
            if (quality < 0 || quality > 100) throw new ArgumentOutOfRangeException(nameof(quality));
            var bits = (int)dimension;
            if (bits != 0 && ((bits & (bits - 1)) != 0 || (module.QualityDimensions & dimension) != dimension))
                throw new ArgumentException("Choose one eligible module quality dimension.", nameof(dimension));
            var refinement = QualityCap * quality / 100.0;
            return dimension switch
            {
                ShipQualityDimension.None => variant,
                ShipQualityDimension.Output => variant with { Output = Output(module.Output, variant.Output, refinement) },
                ShipQualityDimension.Tracking => variant with { Tracking = Output(module.Tracking, variant.Tracking, refinement) },
                ShipQualityDimension.Range => variant with { Range = variant.Range + module.Range * refinement },
                ShipQualityDimension.RecoveryFraction => variant with
                {
                    RecoveryFraction = Math.Min(RecoveryFractionCap,
                        variant.RecoveryFraction + module.RecoveryFraction * refinement)
                },
                ShipQualityDimension.ActivationCost => variant with
                {
                    Capacitor = CapacitorCost(module.Capacitor, variant.CapacitorMultiplier, refinement),
                    CapacitorMultiplier = CostMultiplier(variant.CapacitorMultiplier, refinement)
                },
                ShipQualityDimension.CycleDuration => variant with
                {
                    Cycle = Math.Max(module.Cycle * CycleFloor, variant.Cycle - module.Cycle * refinement)
                },
                _ => throw new ArgumentOutOfRangeException(nameof(dimension))
            };
        }

        public static double Output(double standard, double calibrated, double permanentBonus = 0,
            double penalty = 0, IEnumerable<double> temporaryBonuses = null)
        {
            Validate(standard, calibrated, permanentBonus, penalty);
            if (standard == 0) return 0;
            var calibration = calibrated / standard - 1;
            var temporary = temporaryBonuses?.ToArray() ?? Array.Empty<double>();
            Validate(temporary);
            return Math.Max(0, standard * (1 +
                Math.Min(PermanentOutputCap, permanentBonus + Math.Max(0, calibration)) -
                Math.Max(0, -calibration) - penalty +
                Math.Min(TemporaryOutputCap, temporary.DefaultIfEmpty(0).Max())));
        }

        public static int CapacitorCost(int standard, double calibrationMultiplier,
            double discount = 0, double extraDemand = 0)
        {
            Validate(standard, calibrationMultiplier, discount, extraDemand);
            if (standard == 0) return 0;
            return Math.Max(1, checked((int)Math.Ceiling(standard *
                CostMultiplier(calibrationMultiplier, discount, extraDemand))));
        }

        private static double CostMultiplier(double calibrationMultiplier, double discount, double extraDemand = 0) =>
            1 + Math.Max(0, calibrationMultiplier - 1) + extraDemand -
            Math.Min(CapacitorDiscountCap, Math.Max(0, 1 - calibrationMultiplier) + discount);

        private static void Validate(params double[] values)
        {
            if (values.Any(x => !double.IsFinite(x) || x < 0))
                throw new ArgumentOutOfRangeException(nameof(values), "Sources must be finite and non-negative.");
        }
    }
}
