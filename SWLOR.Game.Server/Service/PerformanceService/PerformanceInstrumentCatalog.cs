using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// The held instrument accessories players already own and buy. Matching by resref covers copies created
    /// before Performance existed, which carry no instrument locals.
    /// </summary>
    public static class PerformanceInstrumentCatalog
    {
        private static readonly Dictionary<string, PerformanceInstrumentFamily> _families =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["guitar"] = PerformanceInstrumentFamily.Strings,
                ["lute"] = PerformanceInstrumentFamily.Strings,
                ["harp"] = PerformanceInstrumentFamily.Strings,
                ["kloo_horn"] = PerformanceInstrumentFamily.Horns,
                ["slitherhorn"] = PerformanceInstrumentFamily.Horns,
                ["night_microplain"] = PerformanceInstrumentFamily.Vocals,
                ["night_microbrand"] = PerformanceInstrumentFamily.Vocals,
                ["panpipes"] = PerformanceInstrumentFamily.Pipes,
                ["violin_m"] = PerformanceInstrumentFamily.BowedStrings,
                ["violin_f"] = PerformanceInstrumentFamily.BowedStrings,
                ["tambourine"] = PerformanceInstrumentFamily.Percussion,
            };

        /// <summary>Every playable instrument's resref, for checks that the Player Guide's list stays complete.</summary>
        public static IReadOnlyCollection<string> Resrefs => _families.Keys;

        public static bool RequiresBow(PerformanceInstrumentFamily family) => family == PerformanceInstrumentFamily.BowedStrings;

        /// <summary>
        /// These props and the violin's bow are posed without the polearm carry layer. The slitherhorn's mouthpiece
        /// is modelled to reach the lips from that layer, so it keeps the layer.
        /// </summary>
        public static bool PlaysWithoutCarryLayer(string resref) =>
            string.Equals(resref, "kloo_horn", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(resref, "panpipes", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(resref, "tambourine", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(resref, "violin_m", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(resref, "violin_f", StringComparison.OrdinalIgnoreCase);

        public static bool TryGetFamily(string resref, out PerformanceInstrumentFamily family)
        {
            if (!string.IsNullOrEmpty(resref) && _families.TryGetValue(resref, out family))
                return true;

            family = PerformanceInstrumentFamily.None;
            return false;
        }
    }
}
