using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Waypoints
{
    public static class WaypointEditorLayout
    {
        public static IReadOnlyList<BehaviorFieldDefinition> Basic { get; } = new[]
        {
            new BehaviorFieldDefinition
            {
                Label = "Name", Name = "LocalizedName", Kind = BehaviorFieldKind.LocalizedText,
                FieldType = GffFieldType.CExoLocString
            },
            new BehaviorFieldDefinition
            {
                Label = "ResRef", Name = "TemplateResRef", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.ResRef, MaxLength = NwnResRef.MaxLength, IsRequired = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Category", Name = "PaletteID", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte, ChoicesKey = WaypointChoiceKeys.PaletteCategories,
                IsSearchable = true, IsInlineSearch = true
            }
        };

        public static IReadOnlyList<BehaviorFieldDefinition> Custom { get; } = new[]
        {
            new BehaviorFieldDefinition
            {
                Label = "Appearance", Name = "Appearance", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte, ChoicesKey = WaypointChoiceKeys.Appearances
            },
            new BehaviorFieldDefinition
            {
                Label = "Tag", Name = "Tag", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.CExoString
            },
            new BehaviorFieldDefinition
            {
                Label = "Has Map Note", Name = "HasMapNote", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte
            },
            new BehaviorFieldDefinition
            {
                Label = "Map Note", Name = "MapNote", Kind = BehaviorFieldKind.LocalizedText,
                FieldType = GffFieldType.CExoLocString
            },
            new BehaviorFieldDefinition
            {
                Label = "Shown on Map", Name = "MapNoteEnabled", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte
            }
        };
    }
}
