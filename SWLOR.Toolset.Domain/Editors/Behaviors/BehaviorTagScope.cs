namespace SWLOR.Toolset.Domain.Editors.Behaviors
{
    /// <summary>What kind of object a tag-reference row is allowed to point at.</summary>
    public enum BehaviorTagScope
    {
        None,
        Waypoint,
        Door,

        /// <summary>Either kind of transition destination.</summary>
        WaypointOrDoor,

        /// <summary>An item blueprint carrying the referenced tag.</summary>
        Item
    }
}
