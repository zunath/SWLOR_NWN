using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// The cargo bay's size (plan section 13 and Appendix B), mirrored by tools/SpaceRevampBalance/trade.py: the hull's
    /// Cargo stat, plus each fitted Cargo Expander's share of it. Expanders stack, and each costs 5% speed through its
    /// step's SpeedPercent. An expander works from a Helm slot only: a station slot does nothing while its seat is empty,
    /// and a bay must not shrink when a crew member leaves.
    /// </summary>
    public static class ShipCargoRules
    {
        /// <summary>The share of the hull's cargo (percent) a Cargo Expander of this rank adds.</summary>
        public static int GetExpanderPercent(int rank)
        {
            return TradeTuning.CargoExpanderBasePercent + Math.Max(0, rank) / TradeTuning.CargoExpanderRanksPerPercent;
        }

        /// <summary>The cargo bay of a hull with these fitted module tags, none with a Module Bonus. A hull with no cargo has none, expanders or not.</summary>
        public static int GetCapacity(int hullCargo, IEnumerable<string> moduleTags) =>
            GetCapacity(hullCargo, (moduleTags ?? Enumerable.Empty<string>()).Select(x => (x, 0)));

        /// <summary>
        /// The cargo bay of a hull with these fitted modules: each expander's share grows with its Module Bonus
        /// (<see cref="ShipTraitRules.Apply"/>), and the deed's Cargo enhancements add
        /// <paramref name="deedCargoPercent"/> percent of the hull's cargo. A hull with no cargo has none, expanders or not.
        /// </summary>
        public static int GetCapacity(int hullCargo, IEnumerable<(string Tag, int ModuleBonus)> modules, int deedCargoPercent = 0)
        {
            var cargo = Math.Max(0, hullCargo);
            var capacity = cargo + cargo * Math.Max(0, deedCargoPercent) / 100;
            foreach (var (tag, moduleBonus) in modules ?? Enumerable.Empty<(string, int)>())
            {
                var step = RevampModuleCatalog.Steps.FirstOrDefault(x => x.Tag == tag);
                if (step is { Family: ShipModuleFamily.CargoExpander })
                    capacity += cargo * ShipTraitRules.Apply(step, null, moduleBonus).CargoPercent / 100;
            }

            return capacity;
        }

        /// <summary>Whether a module tag is a Cargo Expander.</summary>
        public static bool IsCargoExpander(string tag) =>
            RevampModuleCatalog.Steps.Any(x => x.Tag == tag && x.Family == ShipModuleFamily.CargoExpander);

        /// <summary>
        /// Whether a module works from a Helm slot only: a Cargo Expander or a Smuggling Compartment. A station slot does
        /// nothing while its seat is empty, and neither the bay nor the customs cut may change when a crew member leaves.
        /// </summary>
        public static bool IsHelmOnlyModule(string tag) =>
            RevampModuleCatalog.Steps.Any(x => x.Tag == tag &&
                                               x.Family is ShipModuleFamily.CargoExpander or ShipModuleFamily.SmugglingCompartment);

        /// <summary>The cargo bay of a hull with the modules fitted on this status: its Helm-slot expanders count.</summary>
        public static int GetCapacity(int hullCargo, ShipStatus status)
        {
            return GetCapacity(hullCargo, GetHelmModules(status), GetDeedCargoPercent(status));
        }

        /// <summary>The percent of the hull's cargo the deed's Cargo enhancements add, 0 with none.</summary>
        public static int GetDeedCargoPercent(ShipStatus status) => status?.DeedBonuses?.Cargo ?? 0;

        /// <summary>Every module fitted in a Helm slot of the status, in any row.</summary>
        public static IEnumerable<string> GetHelmModuleTags(ShipStatus status) => GetHelmModules(status).Select(x => x.Tag);

        /// <summary>Every module fitted in a Helm slot of the status, in any row, with its Module Bonus after Yield traits.</summary>
        public static IEnumerable<(string Tag, int ModuleBonus)> GetHelmModules(ShipStatus status)
        {
            if (status == null)
                return Enumerable.Empty<(string, int)>();

            return new[]
                {
                    (Row: ShipModulePowerType.High, Modules: status.HighPowerModules),
                    (Row: ShipModulePowerType.Mid, Modules: status.MidPowerModules),
                    (Row: ShipModulePowerType.Low, Modules: status.LowPowerModules),
                }
                .SelectMany(row => row.Modules
                    .Where(x => x.Value != null &&
                                ShipCrewSeats.GetSeatForSlot(status.Size, status.SlotTags, ShipStatCalculator.GetHotbarSlot(row.Row, x.Key)) ==
                                ShipCrewSeat.Pilot)
                    .Select(x => (x.Value.ItemTag, ShipTraitRules.ModuleBonus(x.Value))));
        }

        /// <summary>Every module fitted on the status, in any row.</summary>
        public static IEnumerable<string> GetFittedModuleTags(ShipStatus status)
        {
            if (status == null)
                return Enumerable.Empty<string>();

            return status.HighPowerModules.Values
                .Concat(status.MidPowerModules.Values)
                .Concat(status.LowPowerModules.Values)
                .Where(x => x != null)
                .Select(x => x.ItemTag);
        }

        /// <summary>
        /// Whether a fitted module can come off: a Cargo Expander cannot while the bay holds more than it would without
        /// it. <paramref name="usedVolume"/> is the bay's filled volume (commodities and sealed freight).
        /// </summary>
        public static bool CanRemoveModule(int hullCargo, IReadOnlyCollection<string> moduleTags, string removedTag, int usedVolume) =>
            CanRemoveModule(hullCargo, (moduleTags ?? Array.Empty<string>()).Select(x => (x, 0)).ToList(), (removedTag, 0), usedVolume);

        /// <summary>
        /// Whether a fitted module can come off, counting each expander's Module Bonus: <paramref name="removed"/> is the
        /// module coming off (its tag and bonus), one of <paramref name="modules"/>.
        /// </summary>
        public static bool CanRemoveModule(int hullCargo, IReadOnlyCollection<(string Tag, int ModuleBonus)> modules,
            (string Tag, int ModuleBonus) removed, int usedVolume, int deedCargoPercent = 0)
        {
            var all = (modules ?? Array.Empty<(string, int)>()).ToList();
            var remaining = all.ToList();
            if (!remaining.Remove(removed))
                return true;

            var without = GetCapacity(hullCargo, remaining, deedCargoPercent);
            return without >= usedVolume || without >= GetCapacity(hullCargo, all, deedCargoPercent);
        }
    }
}
