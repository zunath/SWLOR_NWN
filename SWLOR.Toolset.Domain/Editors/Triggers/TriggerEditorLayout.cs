using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>
    /// The rows of the trigger editor's fixed tabs. Behavior owns everything a builder normally
    /// touches; Basic is identity and Advanced is the raw sheet behind it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Geometry is absent by design and no longer even mentioned: a trigger's dimensions are drawn
    /// per placement in the area editor. So are the per-placement transition fields, which belong to
    /// whichever behavior claims them.
    /// </para>
    /// <para>
    /// Cursor is absent too. It is not a choice a builder makes - it follows from what the trigger
    /// is, which is why the Area Transition behavior sets it and everything else leaves it at the
    /// engine default of 0.
    /// </para>
    /// </remarks>
    public static class TriggerEditorLayout
    {
        /// <summary>
        /// A resref is 16 characters. This is a real engine limit rather than a convention: the GFF
        /// ResRef field is a fixed 16 bytes, the longest resref anywhere in the module is exactly 16,
        /// and <c>ResRefLengthRule</c> already validates against the same number.
        /// </summary>
        public const int MaxResRefLength = 16;

        /// <summary>
        /// A tag is a CExoString, so the <b>engine imposes no maximum</b> — this is the base
        /// toolset's own editor limit, adopted here for parity. Every trigger tag in the module fits
        /// inside it, the longest being 29 characters.
        /// </summary>
        public const int MaxTagLength = 32;

        private static readonly IReadOnlyList<TriggerChoice> TriggerTypeChoices = new[]
        {
            new TriggerChoice(0, "Generic"),
            new TriggerChoice(1, "Area Transition"),
            new TriggerChoice(2, "Trap")
        };

        public static IReadOnlyList<TriggerFieldDefinition> Basic { get; } = new[]
        {
            new TriggerFieldDefinition
            {
                Label = "Name", Name = "LocalizedName", Kind = TriggerFieldKind.LocalizedText,
                FieldType = GffFieldType.CExoLocString
            },
            new TriggerFieldDefinition
            {
                Label = "Tag", Name = "Tag", Kind = TriggerFieldKind.Text,
                FieldType = GffFieldType.CExoString, MaxLength = MaxTagLength
            },
            new TriggerFieldDefinition
            {
                Label = "Blueprint", Name = "TemplateResRef", Kind = TriggerFieldKind.Text,
                FieldType = GffFieldType.ResRef, MaxLength = MaxResRefLength
            },
            new TriggerFieldDefinition
            {
                Label = "Category", Name = "PaletteID", Kind = TriggerFieldKind.Choice,
                FieldType = GffFieldType.Byte, ChoicesKey = TriggerChoiceKeys.PaletteCategories
            }
        };

        public static IReadOnlyList<TriggerFieldDefinition> Advanced { get; } = new[]
        {
            new TriggerFieldDefinition
            {
                Label = "Trigger Type", Name = "Type", Kind = TriggerFieldKind.Choice,
                FieldType = GffFieldType.Int, Choices = TriggerTypeChoices,
                Note = "Most behaviors set this for you."
            },
            new TriggerFieldDefinition
            {
                Label = "Faction", Name = "Faction", Kind = TriggerFieldKind.Choice,
                FieldType = GffFieldType.Dword, ChoicesKey = TriggerChoiceKeys.Factions
            },
            new TriggerFieldDefinition
            {
                Label = "Highlight Height", Name = "HighlightHeight", Kind = TriggerFieldKind.Float,
                FieldType = GffFieldType.Float
            },
            new TriggerFieldDefinition
            {
                Label = "Key Tag", Name = "KeyName", Kind = TriggerFieldKind.Text,
                FieldType = GffFieldType.CExoString, MaxLength = MaxTagLength
            },
            new TriggerFieldDefinition
            {
                Label = "Auto-Remove Key", Name = "AutoRemoveKey", Kind = TriggerFieldKind.Check,
                FieldType = GffFieldType.Byte
            }
        };
    }
}
