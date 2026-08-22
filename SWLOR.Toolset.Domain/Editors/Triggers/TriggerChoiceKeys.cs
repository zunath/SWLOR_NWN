namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>
    /// Choice sets a trigger behavior draws from game data rather than declaring itself. The app
    /// layer resolves these; a key it cannot resolve leaves the row's list empty rather than
    /// inventing values.
    /// </summary>
    public static class TriggerChoiceKeys
    {
        /// <summary>loadscreens.2da — the named screens a transition can show while it loads.</summary>
        public const string LoadScreens = "loadscreens";

        /// <summary>The module's repute.fac.</summary>
        public const string Factions = "factions";

        /// <summary>traps.2da — the kinds of trap a trap trigger can be.</summary>
        public const string TrapTypes = "traptypes";

        /// <summary>The trigger palette's own categories, by PaletteID.</summary>
        public const string PaletteCategories = "palettecategories";
    }
}
