namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>What one middle-level row of the Area Contents tree stands for.</summary>
    /// <remarks>
    /// The choice is not cosmetic - it decides whether the tree describes the area or misdescribes
    /// it. See <see cref="Name"/>, which is the default for a reason.
    /// </remarks>
    public enum AreaContentsGrouping
    {
        /// <summary>
        /// One row per distinct display name. The default.
        /// </summary>
        /// <remarks>
        /// Exact-match on the whole name, never a parsed prefix: "Table, Red" and "Table, Blue" are
        /// two rows, and alphabetical order already sits them next to each other without anyone
        /// having to guess that the word before the comma is a category. Half the placeable
        /// blueprints in the module have no comma to split on at all, and the ones that do split
        /// walls three ways - "[SWLOR] Wall,...", "Metal Wall,..." and "Wall, Coronet...".
        /// </remarks>
        Name,

        /// <summary>
        /// One row per blueprint resref. What you want before editing a blueprint, since it answers
        /// "what would that change touch" exactly.
        /// </summary>
        /// <remarks>
        /// Deliberately not the default. Builders here reuse a handful of host blueprints and set the
        /// appearance and name per instance, so this view is honest about files and misleading about
        /// objects: in veles_exterior it files 45 roads, 35 lightposts and 27 fences under
        /// "Rug, Maze (Brown/Cream)", because all of them are placements of _mdrn_pl_carpt04.
        /// </remarks>
        Blueprint,

        /// <summary>One row per tag - the view that finds spawn and script wiring.</summary>
        Tag,

        /// <summary>No grouping: every placement is its own row under its kind.</summary>
        Flat
    }
}
