namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>Whether a behavior field's stored value points at something real.</summary>
    public enum BehaviorValueStatus
    {
        /// <summary>Nothing to say: the field is free text, or empty and optional.</summary>
        None,

        /// <summary>The value resolves to a declared loot table, dialog, tag, quest or enum entry.</summary>
        Resolves,

        /// <summary>
        /// The value names something that does not exist. This is the state 23 conversation
        /// references and 11 teleporter destinations in the module are in right now.
        /// </summary>
        Dangling,

        /// <summary>The behavior requires a value and there isn't one.</summary>
        Missing
    }
}
