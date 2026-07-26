using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Waypoints
{
    public static class WaypointEditorLayout
    {
        public const int MaxResRefLength = 16;
        public const int MaxTagLength = 32;
        public const int MaxNameLength = 64;

        public static IReadOnlyList<BehaviorFieldDefinition> Basic { get; } = new[]
        {
            new BehaviorFieldDefinition
            {
                Label = "Name", Name = "LocalizedName", Kind = BehaviorFieldKind.LocalizedText,
                FieldType = GffFieldType.CExoLocString, MaxLength = MaxNameLength
            },
            new BehaviorFieldDefinition
            {
                Label = "Blueprint", Name = "TemplateResRef", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.ResRef, MaxLength = MaxResRefLength
            },
            new BehaviorFieldDefinition
            {
                Label = "Category", Name = "PaletteID", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte, ChoicesKey = WaypointChoiceKeys.PaletteCategories
            }
        };

        public static IReadOnlyList<BehaviorFieldDefinition> Advanced { get; } = new[]
        {
            new BehaviorFieldDefinition
            {
                Label = "Appearance", Name = "Appearance", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte, ChoicesKey = WaypointChoiceKeys.Appearances,
                CustomOnly = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Tag", Name = "Tag", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.CExoString, MaxLength = MaxTagLength, CustomOnly = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Has Map Note", Name = "HasMapNote", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte, CustomOnly = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Map Note", Name = "MapNote", Kind = BehaviorFieldKind.LocalizedText,
                FieldType = GffFieldType.CExoLocString, CustomOnly = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Shown on Map", Name = "MapNoteEnabled", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte, CustomOnly = true
            }
        };
    }
}
