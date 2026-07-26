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
    }
}
