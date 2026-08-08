using SWLOR.Toolset.Domain.GameData.Lookups;

namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// Which baseitems.2da rows the Base Type list actually offers. baseitems.2da carries 353 junk
    /// rows left behind by base-game and SWLOR row churn - some relabeled "DELETED", some left as
    /// bare "Padding", some with a real label but no ItemClass (nothing an icon could be named
    /// after), and some whose Name column strref resolves against a broken base-game dialog.tlk
    /// entry (the literal text "Bad Strref" - a real artifact of dialog.tlk, not something this
    /// toolset invents) - none of which is a base type a builder could ever choose.
    /// </summary>
    public static class BaseItemChoicePolicy
    {
        /// <summary>The literal text a broken base-game dialog.tlk strref resolves to.</summary>
        private const string BadStrrefSentinel = "Bad Strref";

        private static bool IsDisplayOffered(string? display)
        {
            return TwoDaChoicePolicy.IsSelectableLabel(display) &&
                   !string.Equals(display!.Trim(), BadStrrefSentinel, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>True when this row belongs in the Base Type list.</summary>
        /// <param name="display">
        /// The row's resolved display text (label or TLK name), when the caller has one. Null skips
        /// this check - callers that only have the raw label/ItemClass still get the rest of the
        /// policy applied.
        /// </param>
        public static bool IsOffered(string? label, string? itemClass, string? display = null)
        {
            if (!TwoDaChoicePolicy.IsSelectableLabel(label))
                return false;

            if (string.IsNullOrWhiteSpace(itemClass))
                return false;

            if (display != null && !IsDisplayOffered(display))
                return false;

            return true;
        }
    }
}
