using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Behaviors
{
    /// <summary>
    /// One row a behavior asks the builder to fill in: where the value lives, how it is presented,
    /// and whether the behavior is incomplete without it.
    /// </summary>
    public class BehaviorFieldDefinition
    {
        public required string Label { get; init; }

        /// <summary>Field name on the object struct, or the local's name when Storage is Local.</summary>
        public required string Name { get; init; }

        public required BehaviorFieldKind Kind { get; init; }

        public BehaviorFieldStorage Storage { get; init; } = BehaviorFieldStorage.Field;

        /// <summary>GFF type used when a Field-stored value has to be created on first write.</summary>
        public GffFieldType FieldType { get; init; } = GffFieldType.Int;

        /// <summary>
        /// A tighter numeric floor than <see cref="FieldType"/> alone implies, when the field has one
        /// (a stack of zero items is not a stack). Null leaves the storage type's own range.
        /// </summary>
        public long? Minimum { get; init; }

        /// <summary>A tighter numeric ceiling than the storage type's, or null for the type's own.</summary>
        public long? Maximum { get; init; }

        /// <summary>Marked "required" in the editor, and reported when left empty.</summary>
        public bool IsRequired { get; init; }

        public bool IsReadOnly { get; init; }

        /// <summary>
        /// Characters a text row accepts, or 0 for no limit. Enforced by the box itself, so an
        /// over-long value cannot be typed rather than being truncated behind the builder's back.
        /// </summary>
        public int MaxLength { get; init; }

        /// <summary>Named values for a Choice row, when the set is fixed.</summary>
        public IReadOnlyList<BehaviorChoice> Choices { get; init; } = Array.Empty<BehaviorChoice>();

        /// <summary>
        /// Whether a Choice row uses a searchable selection list. The typed text filters the
        /// declared choices; only selecting one of them writes a value.
        /// </summary>
        public bool IsSearchable { get; init; }

        /// <summary>
        /// Keeps a searchable selector visible as part of the form instead of placing it behind an
        /// open button. Palette categories use this consistently across blueprint editors because
        /// refiling an object is a primary editing action, not a heavyweight catalog workflow.
        /// Results remain virtualized and paged.
        /// </summary>
        public bool IsInlineSearch { get; init; }

        /// <summary>
        /// Keeps a visual choice gallery in the editor page even when its catalog is large. The
        /// gallery still publishes tiles and requests artwork progressively; this only removes the
        /// extra click needed to reveal a picker whose options are the page's primary content.
        /// </summary>
        public bool IsInlineGallery { get; init; }

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
        public BehaviorTagScope TagScope { get; init; } = BehaviorTagScope.None;

        /// <summary>Maximum entries accepted by a list row, or zero for no limit.</summary>
        public int MaxItems { get; init; }
    }
}
