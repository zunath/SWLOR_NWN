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

        /// <summary>Named values for a Choice row.</summary>
        public IReadOnlyList<TriggerChoice> Choices { get; init; } = Array.Empty<TriggerChoice>();

        /// <summary>What a Statement row says; also the sub-label under an editable row.</summary>
        public string? Note { get; init; }

        /// <summary>
        /// True for values that belong to one placement rather than to the blueprint — an
        /// exploration note's message, a doorway's destination. The instance editor says so, and the
        /// blueprint editor warns that a value set here is only a default.
        /// </summary>
        public bool IsPerPlacement { get; init; }

        /// <summary>Which tag index a TagReference row resolves against.</summary>
        public TriggerTagScope TagScope { get; init; } = TriggerTagScope.None;
    }
}
