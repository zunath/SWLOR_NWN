using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Feature.TradeDefinition;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// NPC trade convoys (plan sections 11 and 13): a hauler carrying commodities and its escorts, flying for a lawful
    /// authority on the Neutral faction. Destroying the hauler spills its cargo as cargo pods.
    /// </summary>
    public static class ConvoyRules
    {
        private static readonly Regex _tierTag = new(@"^t([1-6])(convoy|escort)$", RegexOptions.Compiled);

        public static string GetHaulerTag(int tier) => $"t{Math.Clamp(tier, 1, 6)}convoy";

        public static string GetEscortTag(int tier) => $"t{Math.Clamp(tier, 1, 6)}escort";

        /// <summary>The tier of a convoy hauler's creature tag, or 0 for any other tag.</summary>
        public static int GetHaulerTier(string tag)
        {
            var match = _tierTag.Match(tag ?? string.Empty);
            return match.Success && match.Groups[2].Value == "convoy" ? int.Parse(match.Groups[1].Value) : 0;
        }

        public static int GetEscortCount(int tier) => TradeTuning.ConvoyEscortsByTier[Math.Clamp(tier, 1, 6)];

        /// <summary>The base value of a hauler's cargo at this tier.</summary>
        public static int GetCargoValue(int tier) => TradeTuning.ConvoyValuePerTier * Math.Clamp(tier, 1, 6);

        /// <summary>The authority a convoy flies for: the orbit's customs authority, else Czerka.</summary>
        public static TradeAuthority GetAuthority(TradeOrbitDetail orbit)
        {
            return orbit != null && orbit.CustomsAuthorities.Length > 0
                ? orbit.CustomsAuthorities[0]
                : TradeTuning.ConvoyAuthorityDefault;
        }

        /// <summary>
        /// The goods a convoy may carry: the legal exports of the orbit's docks, or any legal good in an orbit without
        /// a dock.
        /// </summary>
        public static List<CommodityType> GetCandidateGoods(string orbitResref)
        {
            var exports = TradeCatalog.Exchanges
                .Where(x => x.OrbitResref == orbitResref)
                .SelectMany(x => x.Markets.Where(m => m.Role == CommodityRole.Producer).Select(m => m.Commodity))
                .Where(IsLegal)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            return exports.Count > 0
                ? exports
                : TradeCatalog.Commodities.Where(x => x.Legality == CommodityLegality.Legal).Select(x => x.Type).ToList();
        }

        /// <summary>
        /// A hauler's cargo: up to two goods picked by <paramref name="random"/> sharing the tier's value, at least one
        /// unit each.
        /// </summary>
        public static Dictionary<CommodityType, int> BuildCargo(int tier, string orbitResref, System.Random random)
        {
            var candidates = GetCandidateGoods(orbitResref);
            var picks = new List<CommodityType>();
            while (picks.Count < Math.Min(2, candidates.Count))
            {
                var pick = candidates[random.Next(candidates.Count)];
                if (!picks.Contains(pick))
                    picks.Add(pick);
            }

            var cargo = new Dictionary<CommodityType, int>();
            if (picks.Count == 0)
                return cargo;

            var share = GetCargoValue(tier) / picks.Count;
            foreach (var commodity in picks)
            {
                var basePrice = Math.Max(1, TradeLookup.GetCommodity(commodity)?.BasePrice ?? 1);
                cargo[commodity] = Math.Max(1, share / basePrice);
            }

            return cargo;
        }

        private static bool IsLegal(CommodityType commodity)
        {
            return TradeLookup.GetCommodity(commodity)?.Legality == CommodityLegality.Legal;
        }
    }
}
