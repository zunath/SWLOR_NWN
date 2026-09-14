namespace SWLOR.Toolset.Domain.Editors.Behaviors
{
    /// <summary>
    /// What every behavior-shaped editor's left rail needs to draw one entry, regardless of whether
    /// the object is a trigger, waypoint, door, or ambient sound.
    /// </summary>
    /// <remarks>
    /// The four catalogs declare different field types and different managed values, but the list
    /// itself only ever asks the same six questions. Naming them here is what lets one list-item view
    /// model and one rail template serve all four editors instead of four near-identical copies that
    /// drift apart one heading style at a time.
    /// </remarks>
    public interface IBehaviorDescriptor
    {
        /// <summary>Stable identifier used by classification and tests; never shown to a builder.</summary>
        string Id { get; }

        /// <summary>What the behavior is called in the list and in the panel heading.</summary>
        string DisplayName { get; }

        /// <summary>Heading this behavior sits under; null sits it above the groups.</summary>
        string? Group { get; }

        /// <summary>Trailing clause on the list row, as in "None — plain trigger".</summary>
        string? Tagline { get; }

        /// <summary>One line under the panel's title saying what this behavior does.</summary>
        string? Summary { get; }

        /// <summary>True only for Custom: the raw VarTable is the builder's to edit.</summary>
        bool AllowsVariables { get; }
    }
}
