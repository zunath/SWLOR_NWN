using System;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// The held instrument accessories players already own and buy. Matching by resref covers copies created
    /// before Performance existed, which carry no instrument locals. The resrefs compare without case.
    /// </summary>
    public static class PerformanceInstrumentCatalog
    {
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
