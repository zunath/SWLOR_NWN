namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>
    /// Deep links into the community NWN Lexicon, the fullest NWScript reference there is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Links rather than bundles, deliberately. The Lexicon's content is GFDL 1.1-or-later
    /// ("2002 onwards NWN Lexicon Group"), so shipping a copy would mean carrying its licence text
    /// and attribution alongside this project's GPL-3.0 — permitted as mere aggregation, but a real
    /// obligation and a body of prose that goes stale. A link costs nothing, is always current, and
    /// carries no licensing weight at all.
    /// </para>
    /// <para>
    /// It works because the Lexicon is MediaWiki and its function pages are titled with the exact
    /// engine function name, so <c>ScriptFunction.Name</c> maps onto a page title one-to-one with no
    /// lookup table to maintain and nothing to keep in sync.
    /// </para>
    /// </remarks>
    public static class ScriptLexicon
    {
        /// <summary>The wiki's article base. Public so a future settings override has somewhere to go.</summary>
        public const string BaseUrl = "https://nwnlexicon.com/index.php";

        /// <summary>
        /// The Lexicon page for a symbol, or null when <paramref name="name"/> could not be a page
        /// title. Never throws: this backs a menu item, and an odd selection should disable the
        /// action rather than fail.
        /// </summary>
        public static string? UrlFor(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var trimmed = name.Trim();

            // Only real identifiers. Anything else is a stray selection, and sending it would land on
            // the wiki's "page does not exist" screen — worse than the action simply being unavailable.
            if (!IsLinkableName(trimmed))
                return null;

            return $"{BaseUrl}/{Uri.EscapeDataString(trimmed)}";
        }

        /// <summary>Whether a name is shaped like something the Lexicon could have a page for.</summary>
        public static bool IsLinkableName(string? name) =>
            !string.IsNullOrWhiteSpace(name) &&
            (char.IsLetter(name[0]) || name[0] == '_') &&
            name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}
