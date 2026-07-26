using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>
    /// The rows of the trigger editor's fixed tabs. Behavior owns everything a builder normally
    /// touches; Basic is identity and Advanced is the raw sheet behind it.
    /// </summary>
    /// <remarks>
    /// Geometry is absent by design: a trigger's dimensions are drawn per placement in the area
    /// editor, not typed here. So are the per-placement transition fields, which belong to whichever
    /// behavior claims them.
    /// </remarks>
    public static class TriggerEditorLayout
    {
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
                FieldType = GffFieldType.CExoString
            },
            new TriggerFieldDefinition
            {
                Label = "Blueprint", Name = "TemplateResRef", Kind = TriggerFieldKind.Text,
                FieldType = GffFieldType.ResRef
            },
            new TriggerFieldDefinition
            {
                Label = "Category", Name = "PaletteID", Kind = TriggerFieldKind.Integer,
                FieldType = GffFieldType.Byte,
                Note = "Palette id stored in the blueprint."
            },
            new TriggerFieldDefinition
            {
                Label = "Geometry", Name = string.Empty, Kind = TriggerFieldKind.Statement,
                Note = "Drawn per placement in the area editor. It is not a field here."
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
                Label = "Faction", Name = "Faction", Kind = TriggerFieldKind.Integer,
                FieldType = GffFieldType.Dword, Note = "Row in the module's repute.fac."
            },
            new TriggerFieldDefinition
            {
                Label = "Highlight Height", Name = "HighlightHeight", Kind = TriggerFieldKind.Float,
                FieldType = GffFieldType.Float
            },
            new TriggerFieldDefinition
            {
                Label = "Cursor", Name = "Cursor", Kind = TriggerFieldKind.Integer,
                FieldType = GffFieldType.Byte, Note = "0 = unclickable (no cursor)."
            },
            new TriggerFieldDefinition
            {
                Label = "Key Tag", Name = "KeyName", Kind = TriggerFieldKind.Text,
                FieldType = GffFieldType.CExoString
            },
            new TriggerFieldDefinition
            {
                Label = "Auto-Remove Key", Name = "AutoRemoveKey", Kind = TriggerFieldKind.Check,
                FieldType = GffFieldType.Byte
            }
        };
    }
}
