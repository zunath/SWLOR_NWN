using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public sealed record ShipFittingModule(string Design, string Calibration = "Standard",
        ShipQualityDimension QualityDimension = ShipQualityDimension.None, int Quality = 0);

    public sealed record ShipFittingResult(ShipHullProfile Hull, int PowerUsed, int HighSlotsUsed,
        int LowSlotsUsed, IReadOnlyList<ShipModuleVariant> Modules, IReadOnlyList<string> Errors)
    {
        public bool IsLegal => Errors.Count == 0;
    }

    public static class ShipFittingCalculator
    {
        public static ShipFittingResult Calculate(string hullId, IEnumerable<ShipFittingModule> fittings,
            IReadOnlyDictionary<SkillType, int> skills, string configurationId = null,
            ShipFittingCatalog catalog = null)
        {
            catalog ??= ShipFittingCatalog.Default;
            var hull = catalog.Hulls[hullId];
            var errors = new List<string>();
            var modules = new List<ShipModuleVariant>();
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var power = 0;
            var high = 0;
            var low = 0;
            if (Rank(skills, SkillType.Piloting) < hull.PilotingRank)
                errors.Add($"{hull.Name} requires Piloting rank {hull.PilotingRank}.");
            foreach (var fitting in fittings)
            {
                var module = catalog.Modules[fitting.Design];
                var variant = ShipModuleTuning.Refine(module, catalog.GetVariant(fitting.Design, fitting.Calibration),
                    fitting.QualityDimension, fitting.Quality);
                modules.Add(variant);
                power = checked(power + variant.Power);
                if (module.Slot == ShipFittingSlot.High) high++; else low++;
                if (!hull.Allows(module.Mount)) errors.Add($"{module.Name} requires a {module.Mount} mount.");
                if (Rank(skills, module.OperatorSkill) < module.OperatorRank)
                    errors.Add($"{module.Name} requires {module.OperatorSkill} rank {module.OperatorRank}.");
                counts.TryGetValue(module.Id, out var count);
                counts[module.Id] = count + 1;
                if (module.MaxFitted > 0 && count + 1 > module.MaxFitted)
                    errors.Add($"{module.Name} permits at most {module.MaxFitted} fitted.");
            }
            if (!string.IsNullOrEmpty(configurationId))
                power = checked(power + catalog.Configurations[configurationId].Power);
            if (power > hull.Power) errors.Add($"Fitting power {power} exceeds {hull.Power}.");
            if (high > hull.HighSlots) errors.Add($"High slots {high} exceed {hull.HighSlots}.");
            if (low > hull.LowSlots) errors.Add($"Low slots {low} exceed {hull.LowSlots}.");
            return new(hull, power, high, low, modules.AsReadOnly(), errors.AsReadOnly());
        }

        public static int Rank(IReadOnlyDictionary<SkillType, int> skills, SkillType skill)
        {
            if (skills == null || !skills.TryGetValue(skill, out var rank)) return 0;
            if (rank < 0 || rank > 50) throw new ArgumentOutOfRangeException(nameof(skills), "Operating ranks must be between 0 and 50.");
            return rank;
        }
    }

    // Keep the unclamped deficits across refits; a smaller pool must not erase damage.
    public sealed record ShipResourceDeficits(int HullDamage, int ShieldDamage, int CapacitorExpenditure)
    {
        public static ShipResourceDeficits Capture(ShipStatus status) => new(
            Deficit(status.MaxHull, status.Hull), Deficit(status.MaxShield, status.Shield),
            Deficit(status.MaxCapacitor, status.Capacitor));

        public (int Hull, int Shield, int Capacitor) Apply(int maxHull, int maxShield, int maxCapacitor)
        {
            if (maxHull <= 0 || maxShield < 0 || maxCapacitor < 0 ||
                HullDamage < 0 || ShieldDamage < 0 || CapacitorExpenditure < 0)
                throw new ArgumentOutOfRangeException(nameof(maxHull));
            return (Math.Max(1, maxHull - HullDamage), Math.Max(0, maxShield - ShieldDamage),
                Math.Max(0, maxCapacitor - CapacitorExpenditure));
        }

        private static int Deficit(int maximum, int current)
        {
            if (maximum < 0) throw new ArgumentOutOfRangeException(nameof(maximum));
            return maximum - Math.Clamp(current, 0, maximum);
        }
    }
}
