using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Doors
{
    /// <summary>The fixed Basic and Advanced rows around the selected door behavior.</summary>
    public static class DoorEditorLayout
    {
        public const int MaxResRefLength = 16;
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
                Label = "Appearance", Name = "Appearance", Kind = BehaviorFieldKind.CompositeChoice,
                FieldType = GffFieldType.Dword, Special = DoorFieldSpecial.Appearance,
                Note = "Generic appearances come from genericdoors.2da; specific models come from doortypes.2da."
            },
            new DoorFieldDefinition
            {
                Label = "Category", Name = "PaletteID", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte, ChoicesKey = DoorChoiceKeys.DoorPaletteCategories
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
            }
        };

        public static IReadOnlyList<DoorFieldDefinition> Advanced { get; } = new[]
        {
            new DoorFieldDefinition
            {
                Label = "Blueprint ResRef", Name = "TemplateResRef", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.ResRef, MaxLength = MaxResRefLength
            },
            new DoorFieldDefinition
            {
                Label = "Initial state", Name = "AnimationState", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte, Choices = AnimationStates
            },
            new DoorFieldDefinition
            {
                Label = "Conversation", Name = "Conversation", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.ResRef, MaxLength = MaxResRefLength
            },
            new DoorFieldDefinition
            {
                Label = "Faction", Name = "Faction", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Dword, ChoicesKey = DoorChoiceKeys.Factions
            },
            new DoorFieldDefinition
            {
                Label = "Portrait", Name = "PortraitId", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Word, ChoicesKey = DoorChoiceKeys.Portraits
            },
            RawCheck("Plot", "Plot"),
            RawCheck("Locked", "Locked"),
            RawCheck("Key Required", "KeyRequired"),
            RawText("Linked To", "LinkedTo", GffFieldType.CExoString),
            RawInteger("Linked To Flags", "LinkedToFlags", GffFieldType.Byte),
            RawCheck("Trap Flag", "TrapFlag"),
            RawInteger("Trap Type", "TrapType", GffFieldType.Byte),
            RawCheck("Trap Detectable", "TrapDetectable"),
            RawInteger("Trap Detect DC", "TrapDetectDC", GffFieldType.Byte),
            RawCheck("Trap Disarmable", "TrapDisarmable"),
            RawInteger("Trap Disarm DC", "DisarmDC", GffFieldType.Byte),
            RawCheck("Trap One Shot", "TrapOneShot")
        };

        private static DoorFieldDefinition RawCheck(string label, string name) =>
            new()
            {
                Label = label, Name = name, Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte, CustomOnly = true
            };

        private static DoorFieldDefinition RawInteger(string label, string name, GffFieldType type) =>
            new()
            {
                Label = label, Name = name, Kind = BehaviorFieldKind.Integer,
                FieldType = type, CustomOnly = true
            };

        private static DoorFieldDefinition RawText(string label, string name, GffFieldType type) =>
            new()
            {
                Label = label, Name = name, Kind = BehaviorFieldKind.Text,
                FieldType = type, CustomOnly = true
            };
    }
}
