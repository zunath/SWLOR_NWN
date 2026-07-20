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
    }

    /// <summary>A titled group of fields rendered as a section.</summary>
    public sealed class FieldGroup
    {
        public required string Title { get; init; }
        public required IReadOnlyList<FieldDescriptor> Fields { get; init; }
    }
}
