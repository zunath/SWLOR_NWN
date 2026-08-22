using SWLOR.Toolset.Domain.GameData.Tlk;

namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// Shared strref-to-text fallback used by every lookup service in this namespace: resolve a
    /// nullable 2DA strref column through <see cref="TlkService"/> when present, falling back to
    /// the row's own 2DA label/identifier text when the strref column is empty (****), negative,
    /// or the TLK has no entry for it (for example a base-game dialog.tlk strref when no base TLK
    /// was supplied - this is the normal case in a test environment with no NWN install).
    /// </summary>
    internal static class DisplayNameResolver
    {
        public static string Resolve(TlkService tlk, int? strref, string fallback)
        {
            if (strref.HasValue && strref.Value >= 0)
            {
                var text = tlk.GetString((uint)strref.Value);
                if (!string.IsNullOrEmpty(text))
                    return text;
            }

            return fallback;
        }
    }
}
