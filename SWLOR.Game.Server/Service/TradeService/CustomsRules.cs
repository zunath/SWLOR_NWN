using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// Customs scans in lawful orbits (plan section 13), mirrored by tools/SpaceRevampBalance/trade.py. A patrol bans
    /// the goods its authority bans; the scan chance grows with their base value and a Smuggling Compartment cuts it.
    /// Caught, the pilot pays a fine and loses the goods, or runs.
    /// </summary>
    public static class CustomsRules
    {
        /// <summary>The base value of the goods aboard that this authority bans.</summary>
        public static int GetBannedValue(IReadOnlyDictionary<CommodityType, CargoLot> cargo, TradeAuthority authority)
        {
            if (cargo == null || authority == TradeAuthority.None)
                return 0;

            return cargo
                .Where(x => x.Value.Units > 0 && IsBanned(x.Key, authority))
                .Sum(x => x.Value.Units * (TradeLookup.GetCommodity(x.Key)?.BasePrice ?? 0));
        }

        /// <summary>Whether this authority bans the commodity.</summary>
        public static bool IsBanned(CommodityType commodity, TradeAuthority authority)
        {
            return TradeLookup.GetCommodity(commodity)?.BannedBy.Contains(authority) ?? false;
        }

        /// <summary>The chance (0-1) that one patrol scan finds goods of this banned value, after a compartment's cut.</summary>
        public static double GetScanChance(int bannedValue, double compartmentPercent)
        {
            if (bannedValue <= 0)
                return 0;

            var chance = Math.Min(TradeTuning.ScanMaximum, TradeTuning.ScanBase + TradeTuning.ScanPerThousandCredits * bannedValue / 1000.0);
            return chance * (1 - Math.Clamp(compartmentPercent, 0, 100) / 100.0);
        }

        /// <summary>The fine for banned goods of this value; the goods are confiscated as well.</summary>
        public static int GetFine(int bannedValue)
        {
            return bannedValue <= 0 ? 0 : (int)Math.Ceiling(bannedValue * TradeTuning.FineRate);
        }

        /// <summary>The scan cut (percent) of a Smuggling Compartment of this rank.</summary>
        public static int GetCompartmentPercent(int rank)
        {
            return TradeTuning.CompartmentBasePercent + Math.Max(0, rank) / TradeTuning.CompartmentRanksPerPercent;
        }

        /// <summary>The cut of the best Smuggling Compartment among these fitted module tags, or 0 with none.</summary>
        public static double GetCompartmentPercent(IEnumerable<string> moduleTags)
        {
            var best = 0;
            foreach (var tag in moduleTags ?? Enumerable.Empty<string>())
            {
                var step = RevampModuleCatalog.Steps.FirstOrDefault(x => x.Tag == tag);
                if (step is { Family: ShipModuleFamily.SmugglingCompartment })
                    best = Math.Max(best, step.ScanReduction);
            }

            return best;
        }

        /// <summary>
        /// The patrol that stops a ship entering the orbit: one of the orbit's authorities, picked by
        /// <paramref name="roll"/> (0-1). None when the orbit is open.
        /// </summary>
        public static TradeAuthority PickPatrol(TradeOrbitDetail orbit, double roll)
        {
            if (orbit == null || orbit.CustomsAuthorities.Length == 0)
                return TradeAuthority.None;

            var index = Math.Clamp((int)(roll * orbit.CustomsAuthorities.Length), 0, orbit.CustomsAuthorities.Length - 1);
            return orbit.CustomsAuthorities[index];
        }

        /// <summary>Removes the goods this authority bans and returns the units taken.</summary>
        public static Dictionary<CommodityType, int> Confiscate(Dictionary<CommodityType, CargoLot> cargo, TradeAuthority authority)
        {
            var taken = new Dictionary<CommodityType, int>();
            if (cargo == null)
                return taken;

            foreach (var commodity in cargo.Keys.Where(x => IsBanned(x, authority)).ToList())
            {
                var units = cargo[commodity].Units;
                if (units <= 0)
                    continue;

                CargoHoldRules.RemoveUnits(cargo, commodity, units);
                taken[commodity] = units;
            }

            return taken;
        }

        /// <summary>The patrol's ships: a gunship and an interceptor of the authority at the orbit's tier, or none.</summary>
        public static IReadOnlyList<string> GetPatrolTags(TradeAuthority authority, int tier)
        {
            var prefix = authority switch
            {
                TradeAuthority.Republic => "repcus",
                TradeAuthority.Czerka => "czkcus",
                TradeAuthority.Sith => "sithcus",
                _ => string.Empty,
            };

            return string.IsNullOrEmpty(prefix)
                ? Array.Empty<string>()
                : new[] { $"t{tier}{prefix}gun", $"t{tier}{prefix}int" };
        }

        /// <summary>"40 Moonspice, 12 Sith Relics".</summary>
        public static string Describe(IReadOnlyDictionary<CommodityType, int> units)
        {
            return string.Join(", ", units
                .Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Key)
                .Select(x => $"{x.Value} {TradeLookup.GetCommodity(x.Key)?.Name ?? x.Key.ToString()}"));
        }

        /// <summary>The banned goods aboard by units, without changing the cargo.</summary>
        public static Dictionary<CommodityType, int> GetBannedUnits(IReadOnlyDictionary<CommodityType, CargoLot> cargo, TradeAuthority authority)
        {
            return (cargo ?? new Dictionary<CommodityType, CargoLot>())
                .Where(x => x.Value.Units > 0 && IsBanned(x.Key, authority))
                .ToDictionary(x => x.Key, x => x.Value.Units);
        }

        /// <summary>The authority's display name: "Republic", "Czerka", "Sith".</summary>
        public static string GetAuthorityName(TradeAuthority authority) => authority.ToString();
    }
}
