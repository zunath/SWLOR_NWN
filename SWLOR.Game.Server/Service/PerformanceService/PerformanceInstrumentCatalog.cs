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
            };

        public static bool TryGetFamily(string resref, out PerformanceInstrumentFamily family)
        {
            if (!string.IsNullOrEmpty(resref) && _families.TryGetValue(resref, out family))
                return true;

            family = PerformanceInstrumentFamily.None;
            return false;
        }
    }
}
