using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Sounds
{
    /// <summary>Fixed Basic and Advanced rows around the behavior-specific sound rows.</summary>
    public static class SoundEditorLayout
    {
        public const int MaxResRefLength = 16;
        public const int MaxNameLength = 64;

        public static IReadOnlyList<BehaviorFieldDefinition> Basic { get; } = new[]
        {
            new BehaviorFieldDefinition
            {
                Label = "Name", Name = "LocName", Kind = BehaviorFieldKind.LocalizedText,
                FieldType = GffFieldType.CExoLocString, MaxLength = MaxNameLength
            },
            new BehaviorFieldDefinition
            {
                Label = "Tag", Name = "Tag", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.CExoString
            },
            new BehaviorFieldDefinition
            {
                Label = "Category", Name = "PaletteID", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte, ChoicesKey = SoundChoiceKeys.PaletteCategories
            }
        };

        public static IReadOnlyList<BehaviorFieldDefinition> Advanced { get; } = new[]
        {
            new BehaviorFieldDefinition
            {
                Label = "Blueprint ResRef", Name = "TemplateResRef", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.ResRef, MaxLength = MaxResRefLength, IsReadOnly = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Active", Name = "Active", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte
            },
            new BehaviorFieldDefinition
            {
                Label = "Priority", Name = "Priority", Kind = BehaviorFieldKind.Statement,
                FieldType = GffFieldType.Byte,
                Note = "Managed automatically by the selected behavior."
            },
            new BehaviorFieldDefinition
            {
                Label = "Volume variation", Name = "VolumeVrtn", Kind = BehaviorFieldKind.Integer,
                FieldType = GffFieldType.Byte
            },
            new BehaviorFieldDefinition
            {
                Label = "Positional", Name = "Positional", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte, CustomOnly = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Random position", Name = "RandomPosition", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte, CustomOnly = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Continuous", Name = "Continuous", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte, CustomOnly = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Looping", Name = "Looping", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte, CustomOnly = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Interval (seconds)", Name = "Interval", Kind = BehaviorFieldKind.Integer,
                FieldType = GffFieldType.Dword, CustomOnly = true
            }
        };
    }
}
