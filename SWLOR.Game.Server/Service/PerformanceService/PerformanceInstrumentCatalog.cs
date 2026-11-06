using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// The held instrument accessories players already own and buy. Matching by resref covers copies created
    /// before Performance existed, which carry no instrument locals. The resrefs compare without case.
    /// </summary>
    public static class PerformanceInstrumentCatalog
    {
        private static readonly ImmutableArray<string> _resrefs =
            ImmutableArray.Create("guitar", "lute", "harp", "kloo_horn", "slitherhorn");

        /// <summary>Every playable instrument's resref, for checks that the Player Guide's list stays complete.</summary>
        public static IReadOnlyCollection<string> Resrefs => _resrefs;

        /// <summary>
        /// The kloo horn only reaches the lips once the polearm carry layer is gone. The slitherhorn's mouthpiece is
        /// modelled to reach them from that layer, so it keeps the layer.
        /// </summary>
        public static bool PlaysWithoutCarryLayer(string resref) =>
            string.Equals(resref, "kloo_horn", StringComparison.OrdinalIgnoreCase);

        public static bool TryGetFamily(string resref, out PerformanceInstrumentFamily family)
        {
            if (!string.IsNullOrEmpty(resref))
            {
                if (IsOneOf(resref, "guitar", "lute", "harp"))
                {
                    family = PerformanceInstrumentFamily.Strings;
                    return true;
                }

                if (IsOneOf(resref, "kloo_horn", "slitherhorn"))
                {
                    family = PerformanceInstrumentFamily.Horns;
                    return true;
                }
            }

            family = PerformanceInstrumentFamily.None;
            return false;
        }

        private static bool IsOneOf(string resref, params string[] names)
        {
            foreach (var name in names)
            {
                if (string.Equals(resref, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
