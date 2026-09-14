using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors
{
    /// <summary>
    /// Describes one editable field of a blueprint document: where it lives in the GFF root,
    /// how it is presented, and which lookup feeds it. Descriptors are pure data — the editor
    /// view model interprets them against a JsonGffDocument.
    /// </summary>
    public sealed class FieldDescriptor
    {
        /// <summary>Display label in the editor.</summary>
        public required string Label { get; init; }

        /// <summary>GFF field name at the document root (e.g. "FirstName", "Appearance_Type").</summary>
        public required string FieldName { get; init; }

        public required EditorKind Kind { get; init; }

        /// <summary>
        /// GFF type used when the field is absent and must be created on first write. Must
        /// match the type nwn_gff uses for this field in the corpus.
        /// </summary>
        public GffFieldType FieldType { get; init; } = GffFieldType.Int;

        /// <summary>Lookup source for TwoDaDropdown fields (a well-known lookup key such as
        /// "appearance", "portraits", "placeables", "doortypes", "factions").</summary>
        public string? LookupKey { get; init; }

        /// <summary>Optional tooltip/help text.</summary>
        public string? Description { get; init; }

        public bool IsReadOnly { get; init; }

        /// <summary>
        /// True when a text or localized-string field is prose rather than a single-line value.
        /// The editor gives these fields a taller box and accepts embedded line breaks.
        /// </summary>
        public bool IsMultiline { get; init; }

        /// <summary>
        /// True when this field must name a real row or enum value, so its editor offers no "(None)".
        /// </summary>
        /// <remarks>
        /// A dropdown gets a synthetic "(None)" so an optional lookup can be cleared. On a field that has
        /// no such state - an item's Base Item, a trigger's Type - picking it wrote the unset sentinel
        /// (-1) into an int the engine expects to be a valid row, which is not a value the editor should
        /// be able to produce at all.
        /// </remarks>
        public bool IsRequired { get; init; }
    }

    /// <summary>A titled group of fields rendered as a section.</summary>
    public sealed class FieldGroup
    {
        public required string Title { get; init; }
        public required IReadOnlyList<FieldDescriptor> Fields { get; init; }

        /// <summary>
        /// Which editor tab this group appears on. Groups sharing a tab keep their declared order
        /// within it, and tabs appear in the order their first group is declared.
        /// </summary>
        /// <remarks>
        /// Blank means the editor shows one unnamed page - the shape every schema had before
        /// placeables needed more than a single scroll, and still the right shape for the types
        /// whose fields fit one.
        /// </remarks>
        public string Tab { get; init; } = string.Empty;
    }
}
