using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// Pirate ambushes (plan section 13), mirrored by tools/SpaceRevampBalance/trade.py. A ship carrying more than the
    /// threshold may be ambushed when it enters an orbit or jumps; the chance and the squad grow with the value and the
    /// orbit's tier, and every squad leads with an interceptor.
    /// </summary>
    public static class PirateAmbushRules
    {
        /// <summary>What pirates think the ship carries: its commodities at base price and its sealed freight.</summary>
        public static int GetCargoValue(IReadOnlyDictionary<CommodityType, CargoLot> cargo, IEnumerable<SealedFreightLot> sealedFreight)
        {
            var goods = (cargo ?? new Dictionary<CommodityType, CargoLot>())
                .Where(x => x.Value.Units > 0)
                .Sum(x => (long)x.Value.Units * (TradeLookup.GetCommodity(x.Key)?.BasePrice ?? 0));
            var sealedVolume = (sealedFreight ?? Enumerable.Empty<SealedFreightLot>()).Sum(x => (long)Math.Max(0, x.Volume));
            return (int)Math.Min(int.MaxValue, goods + sealedVolume * TradeTuning.SealedValuePerVolume);
        }

        /// <summary>The chance (0-1) of an ambush on entering an orbit of this tier with cargo of this value.</summary>
        public static double GetAmbushChance(int cargoValue, int tier)
        {
            if (cargoValue <= TradeTuning.AmbushThreshold || tier <= 0)
                return 0;

            var chance = Math.Min(TradeTuning.AmbushMaximum,
                TradeTuning.AmbushBase + TradeTuning.AmbushPerTenThousandCredits * (cargoValue - TradeTuning.AmbushThreshold) / 10000.0);
            return chance * (0.8 + TradeTuning.AmbushTierStep * tier);
        }

        /// <summary>The raiders in an ambush: one more per value step over the threshold, and one more from tier 4.</summary>
        public static int GetSquadSize(int cargoValue, int tier)
        {
            var size = TradeTuning.AmbushSquadMinimum +
                       Math.Max(0, cargoValue - TradeTuning.AmbushThreshold) / TradeTuning.AmbushSquadValueStep +
                       (tier >= 4 ? 1 : 0);
            return Math.Clamp(size, TradeTuning.AmbushSquadMinimum, TradeTuning.AmbushSquadMaximum);
        }

        /// <summary>
        /// The raider creature tags of a squad: an interceptor first (it webs a fleeing freighter), a gunship once the
        /// squad has four ships, fighters for the rest.
        /// </summary>
        public static List<string> GetSquad(int tier, int size)
        {
            tier = Math.Clamp(tier, 1, 6);
            var squad = new List<string> { $"t{tier}pirint" };
            if (size >= 4)
                squad.Add($"t{tier}pirgun");
            while (squad.Count < size)
                squad.Add($"t{tier}pirftr");

            return squad;
        }
    }
}
