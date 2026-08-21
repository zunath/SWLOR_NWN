using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Doors
{
    /// <summary>The fixed Basic rows shared by every door behavior.</summary>
    public static class DoorEditorLayout
    {
        public const int MaxTagLength = 32;
        public const int MaxNameLength = 64;

        private static readonly IReadOnlyList<BehaviorChoice> AnimationStates = new[]
        {
            new BehaviorChoice(0, "Closed"),
            new BehaviorChoice(1, "Open"),
            new BehaviorChoice(2, "Destroyed")
        };

        public static IReadOnlyList<DoorFieldDefinition> Basic { get; } = new[]
        {
            new DoorFieldDefinition
            {
                Label = "Name", Name = "LocName", Kind = BehaviorFieldKind.LocalizedText,
                FieldType = GffFieldType.CExoLocString, MaxLength = MaxNameLength
            },
            new DoorFieldDefinition
            {
                Label = "Tag", Name = "Tag", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.CExoString, MaxLength = MaxTagLength
            },
            new DoorFieldDefinition
            {
                Label = "Category", Name = "PaletteID", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte, ChoicesKey = DoorChoiceKeys.DoorPaletteCategories,
                IsSearchable = true, IsInlineSearch = true
            },
            new DoorFieldDefinition
            {
                Label = "Description", Name = "Description", Kind = BehaviorFieldKind.Paragraph,
                FieldType = GffFieldType.CExoLocString
            },
            new DoorFieldDefinition
            {
                Label = "Closes itself after opening", Name = "OnOpen", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.ResRef, Special = DoorFieldSpecial.SelfClosing
            },
            new DoorFieldDefinition
            {
                Label = "ResRef", Name = "TemplateResRef", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.ResRef, MaxLength = NwnResRef.MaxLength, IsRequired = true
            },
            new DoorFieldDefinition
            {
                Label = "Initial state", Name = "AnimationState", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte, Choices = AnimationStates
            },
            new DoorFieldDefinition
            {
                Label = "Faction", Name = "Faction", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Dword, ChoicesKey = DoorChoiceKeys.Factions,
                IsSearchable = true
            },
            new DoorFieldDefinition
            {
                Label = "Portrait", Name = "PortraitId", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Word, ChoicesKey = DoorChoiceKeys.Portraits
            }
        };
    }
}
