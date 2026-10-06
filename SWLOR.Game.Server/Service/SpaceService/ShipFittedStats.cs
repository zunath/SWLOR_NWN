using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    // Recompute from the complete fit. Penalties remain separate from capped positive sources.
    public static class ShipFittedStats
    {
        public static readonly IReadOnlyDictionary<StatType, int> StatUnits = new ReadOnlyDictionary<StatType, int>(Enum.GetValues<StatType>()
            .Select(stat => (stat, attribute: typeof(StatType).GetField(stat.ToString())?.GetCustomAttribute<StatTypeAttribute>()))
            .Where(x => x.attribute?.ShipUnitsPerAmount > 0)
            .ToDictionary(x => x.stat, x => x.attribute.ShipUnitsPerAmount));

        public static IEnumerable<ShipStatus.ShipStatusModule> Modules(ShipStatus status) =>
            status.HighPowerModules.OrderBy(x => x.Key).Select(x => x.Value)
                .Concat(status.LowPowerModules.OrderBy(x => x.Key).Select(x => x.Value));

        public static void Recompute(ShipStatus status, IReadOnlyDictionary<SkillType, int> skills = null,
            IReadOnlyDictionary<StatType, int> adjustments = null, ShipFittingCatalog catalog = null)
        {
            ArgumentNullException.ThrowIfNull(status);
            catalog ??= ShipFittingCatalog.Default;
            var hull = catalog.Hulls[status.ItemTag];
            var alive = status.MaxHull <= 0 || status.Hull > 0;
            status.ResourceDeficits ??= ShipResourceDeficits.Capture(status);
            var positive = new Dictionary<StatType, double>();
            var negative = new Dictionary<StatType, double>();
            var duplicates = new Dictionary<StatType, List<double>>();
            var bases = new Dictionary<StatType, double> {
                [StatType.ShipShieldCapacity] = hull.Shield,
                [StatType.ShipCapacitorCapacity] = hull.Capacitor,
                [StatType.ShipCapacitorRecovery] = hull.CapacitorRecovery };
            var power = 0;
            foreach (var fitted in Modules(status))
            {
                var profile = catalog.Modules[fitted.Design];
                var variant = ShipModuleTuning.Refine(profile, catalog.GetVariant(fitted.Design, fitted.Calibration),
                    fitted.QualityDimension, fitted.Quality);
                power = checked(power + variant.Power);
                if (fitted.Condition <= 0) continue;
                foreach (var modifier in profile.Modifiers)
                {
                    var amount = modifier.Amount;
                    if (modifier.ScalesWithOutput)
                        amount *= profile.Output == 0 ? 0 : variant.Output / profile.Output;
                    if (modifier.Proportional) amount *= bases[modifier.Stat];
                    if (amount > 0 && Stat.GetStatTypeCategory(modifier.Stat) == StatTypeCategory.BeneficialWhenPositive)
                    {
                        if (!duplicates.TryGetValue(modifier.Stat, out var values))
                            duplicates[modifier.Stat] = values = new();
                        values.Add(amount);
                    }
                    else Add(amount >= 0 ? positive : negative, modifier.Stat, Math.Abs(amount));
                }
            }
            foreach (var (stat, values) in duplicates)
                Add(positive, stat, values.OrderByDescending(x => x).Take(4)
                    .Select((value, index) => value / (1 << index)).Sum());
            if (!string.IsNullOrEmpty(status.ConfigurationDesign))
            {
                var configuration = catalog.Configurations[status.ConfigurationDesign];
                power = checked(power + configuration.Power);
                if (status.ConfigurationModules.Count != 1 || status.ConfigurationModules.Values.Single().Design != configuration.Id)
                    throw new InvalidOperationException("Configuration metadata does not match its fitted item.");
                foreach (var modifier in configuration.Modifiers.Where(_ => status.ConfigurationModules.Values.Single().Condition > 0))
                {
                    var amount = modifier.Amount;
                    var fittedConfiguration = status.ConfigurationModules.Values.Single();
                    if (amount > 0 && fittedConfiguration.QualityDimension == ShipQualityDimension.Output)
                        amount *= 1 + ShipModuleTuning.QualityCap * Math.Clamp(fittedConfiguration.Quality, 0, 100) / 100.0;
                    if (modifier.Proportional) amount *= bases[modifier.Stat];
                    Add(amount >= 0 ? positive : negative, modifier.Stat, Math.Abs(amount));
                }
            }
            if (adjustments != null)
                foreach (var (stat, value) in adjustments)
                {
                    if (!StatUnits.TryGetValue(stat, out var units))
                        throw new ArgumentException("Expected a stat with declared ship units: " + stat);
                    Add(value >= 0 ? positive : negative, stat, Math.Abs((double)value) / units);
                }
            Add(positive, StatType.ShipWeaponOutput, ShipFittingCalculator.Rank(skills, SkillType.Gunnery) * .002);
            Add(positive, StatType.ShipRecoveryOutput, ShipFittingCalculator.Rank(skills, SkillType.ShipSystems) * .002);
            Add(positive, StatType.ShipSpeed, ShipFittingCalculator.Rank(skills, SkillType.Piloting) * .002);
            Add(positive, StatType.ShipScannerResolution, ShipFittingCalculator.Rank(skills, SkillType.Astrometrics) * .4);
            Add(positive, StatType.ShipResourceRecovery, ShipFittingCalculator.Rank(skills, SkillType.SpaceIndustry) * .001);
            status.FittingBonuses = positive;
            status.FittingPenalties = negative;
            status.FittingPowerUsed = power;
            status.MaxHull = Math.Max(1, (int)Math.Floor(hull.Hull + Net(StatType.ShipHullCapacity)));
            status.MaxShield = Math.Max(0, (int)Math.Floor(hull.Shield + Net(StatType.ShipShieldCapacity)));
            status.MaxCapacitor = Math.Max(1, (int)Math.Floor(hull.Capacitor + Net(StatType.ShipCapacitorCapacity)));
            var pools = status.ResourceDeficits.Apply(status.MaxHull, status.MaxShield, status.MaxCapacitor);
            status.Hull = alive ? pools.Hull : 0; status.Shield = pools.Shield; status.Capacitor = pools.Capacitor;
            status.CargoCapacity = Math.Max(0, hull.Cargo * (1 + Net(StatType.ShipCargoCapacity)));
            status.Speed = Math.Max(.1, hull.Speed * (1 + Math.Min(.25, Bonus(StatType.ShipSpeed)) - Penalty(StatType.ShipSpeed)));
            status.Signature = hull.Signature;
            status.BaseSpeed = hull.Speed;
            status.HullResistance = Math.Clamp(hull.Resistance + Net(StatType.ShipHullResistance), 0, 60);
            status.ShieldResistance = Math.Clamp(Net(StatType.ShipShieldResistance), 0, 60);
            status.CapacitorRecovery = Math.Max(0, hull.CapacitorRecovery + Net(StatType.ShipCapacitorRecovery));
            status.OutOfCombatShieldRecovery = Math.Max(0, hull.ShieldRecovery + Net(StatType.ShipShieldRecovery));
            status.ProtectedCargo = Math.Max(0, (int)Math.Floor(Net(StatType.ShipProtectedCargo)));
            double Bonus(StatType stat) => positive.GetValueOrDefault(stat);
            double Penalty(StatType stat) => negative.GetValueOrDefault(stat);
            double Net(StatType stat) => Bonus(stat) - Penalty(stat);
        }

        public static double Bonus(ShipStatus status, StatType stat) => status.FittingBonuses.GetValueOrDefault(stat);
        public static double Penalty(ShipStatus status, StatType stat) => status.FittingPenalties.GetValueOrDefault(stat);
        private static void Add(Dictionary<StatType, double> values, StatType stat, double amount) =>
            values[stat] = values.GetValueOrDefault(stat) + amount;
    }
}
