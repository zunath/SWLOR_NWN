namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>What kind of object a tag-reference row is allowed to point at.</summary>
    public enum TriggerTagScope
    {
        None,
        Waypoint,
        Door,

        /// <summary>Either — decided by the trigger's own link-target field.</summary>
        WaypointOrDoor
    }
}
