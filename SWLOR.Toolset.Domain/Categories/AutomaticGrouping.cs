namespace SWLOR.Toolset.Domain.Categories
{
    /// <summary>
    /// The default grouping rule: split a resource's display name on its first " - " and use the
    /// leading segment as the folder. "Viscara - Veles" files under Viscara, "Tatooine - Anchorhead -
    /// Spaceport" under Tatooine.
    /// </summary>
    /// <remarks>
    /// Deliberately one rule, stated in one sentence, because a builder has to be able to predict where
    /// something will land and see why it didn't. Measured over the module's 443 areas it groups 362 of
    /// them - Tatooine 60, Viscara 52, Dantooine 36, and so on - and leaves 81 ungrouped, which is what
    /// <see cref="CategorySection.UnsortedFolderName"/> exists for.
    ///
    /// The separator is space-dash-space rather than a bare dash on purpose: "CZ-220 - Hangar" must
    /// group under "CZ-220", not under "CZ".
    /// </remarks>
    public static class AutomaticGrouping
    {
        public const string Separator = " - ";

        /// <summary>Player-facing explanation of the rule, so it can be shown next to the selector.</summary>
        public const string Description = "Grouped by the part of the name before the first dash.";

        /// <summary>The folder a display name belongs in, or null when the name has no separator.</summary>
        public static string? GroupNameFor(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            var index = displayName.IndexOf(Separator, StringComparison.Ordinal);
            if (index <= 0)
                return null;

            var group = displayName[..index].Trim();
            return group.Length == 0 ? null : group;
        }

        /// <summary>
        /// What to show for a name once its folder is established - "Veles" under Viscara rather than
        /// repeating "Viscara - Veles" on the row. Falls back to the whole name when it has no separator.
        /// </summary>
        public static string LeafLabelFor(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return string.Empty;

            var index = displayName.IndexOf(Separator, StringComparison.Ordinal);
            if (index <= 0)
                return displayName.Trim();

            var remainder = displayName[(index + Separator.Length)..].Trim();
            return remainder.Length == 0 ? displayName.Trim() : remainder;
        }
    }
}
