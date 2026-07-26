using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>
    /// One row a behavior asks the builder to fill in: where the value lives, how it is presented,
    /// and whether the behavior is incomplete without it.
    /// </summary>
    public sealed class TriggerFieldDefinition
    {
        public required string Label { get; init; }

        /// <summary>Field name on the trigger struct, or the local's name when Storage is Local.</summary>
        public required string Name { get; init; }

        public required TriggerFieldKind Kind { get; init; }

        public TriggerFieldStorage Storage { get; init; } = TriggerFieldStorage.Field;

        /// <summary>GFF type used when a Field-stored value has to be created on first write.</summary>
        public GffFieldType FieldType { get; init; } = GffFieldType.Int;

        /// <summary>Marked "required" in the editor, and reported when left empty.</summary>
        public bool IsRequired { get; init; }

        /// <summary>
        /// Characters a text row accepts, or 0 for no limit. Enforced by the box itself, so an
        /// over-long value cannot be typed rather than being truncated behind the builder's back.
        /// </summary>
        public int MaxLength { get; init; }

        /// <summary>Named values for a Choice row, when the set is fixed.</summary>
        public IReadOnlyList<TriggerChoice> Choices { get; init; } = Array.Empty<TriggerChoice>();

        /// <summary>
        /// Lookup key for a Choice row whose values come from game data rather than from this file -
        /// the load screens, say. The app layer resolves it; an unresolved key leaves the row empty
        /// rather than inventing values.
        /// </summary>
        public string? ChoicesKey { get; init; }

        /// <summary>What a Statement row says; also the sub-label under an editable row.</summary>
        public string? Note { get; init; }

        /// <summary>
        /// Shown only under Custom. For a raw field that a behavior would otherwise own, offering it
        /// alongside that behavior invites a builder to set it to something the behavior contradicts.
        /// </summary>
        public bool CustomOnly { get; init; }

        /// <summary>Which tag index a TagReference row resolves against.</summary>
        public TriggerTagScope TagScope { get; init; } = TriggerTagScope.None;
    }
}
