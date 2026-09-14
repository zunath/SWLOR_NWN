using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>The fixed Basic rows every item shows regardless of family or role.</summary>
    public static class ItemEditorLayout
    {
        public const int MaxTagLength = 32;
        public const int MaxNameLength = 64;

        /// <summary>The local the economy classifier reads to keep an item out of player-facing search.</summary>
        public const string NoEconomyLocal = "NO_ECONOMY";

        public static IReadOnlyList<BehaviorFieldDefinition> Basic { get; } = new[]
        {
            new BehaviorFieldDefinition
            {
                Label = "Name", Name = "LocalizedName", Kind = BehaviorFieldKind.LocalizedText,
                FieldType = GffFieldType.CExoLocString, MaxLength = MaxNameLength
            },
            new BehaviorFieldDefinition
            {
                Label = "Tag", Name = "Tag", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.CExoString, MaxLength = MaxTagLength
            },
            // Editable, unlike every other editor's resref: saving under a changed value renames the
            // blueprint file, so the field and the file cannot silently drift apart.
            new BehaviorFieldDefinition
            {
                Label = "ResRef", Name = "TemplateResRef", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.ResRef, MaxLength = NwnResRef.MaxLength, IsRequired = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Base Type", Name = "BaseItem", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Int, ChoicesKey = ItemChoiceKeys.BaseItems,
                IsSearchable = true, IsRequired = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Category", Name = "PaletteID", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte, ChoicesKey = ItemChoiceKeys.PaletteCategories,
                IsSearchable = true, IsInlineSearch = true
            },
            new BehaviorFieldDefinition
            {
                // A stack of nothing is not a thing - new items are created holding one.
                Label = "Stack Size", Name = "StackSize", Kind = BehaviorFieldKind.Integer,
                FieldType = GffFieldType.Word, Minimum = 1
            },
            new BehaviorFieldDefinition
            {
                Label = "Charges", Name = "Charges", Kind = BehaviorFieldKind.Integer,
                FieldType = GffFieldType.Byte
            },
            new BehaviorFieldDefinition
            {
                Label = "Additional Cost", Name = "AddCost", Kind = BehaviorFieldKind.Integer,
                FieldType = GffFieldType.Dword
            },
            // The engine recomputes Cost from the base type and properties; shown, never edited.
            new BehaviorFieldDefinition
            {
                Label = "Total Cost", Name = "Cost", Kind = BehaviorFieldKind.Integer,
                FieldType = GffFieldType.Dword, IsReadOnly = true
            },
            // A UTI carries two descriptions, but SWLOR exposes one. DescIdentified is the value
            // the UI edits; that edit mirrors into Description while preserving distinct legacy
            // values when the builder changes some unrelated field.
            new BehaviorFieldDefinition
            {
                Label = "Description", Name = "DescIdentified", Kind = BehaviorFieldKind.Paragraph,
                FieldType = GffFieldType.CExoLocString
            },
            new BehaviorFieldDefinition
            {
                Label = "Plot", Name = "Plot", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte
            },
            new BehaviorFieldDefinition
            {
                Label = "Stolen", Name = "Stolen", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte
            },
            new BehaviorFieldDefinition
            {
                Label = "Cursed", Name = "Cursed", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte
            },
            new BehaviorFieldDefinition
            {
                Label = "Identified", Name = "Identified", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte
            },
            new BehaviorFieldDefinition
            {
                Label = "No Economy", Name = NoEconomyLocal, Kind = BehaviorFieldKind.Check,
                Storage = BehaviorFieldStorage.Local
            }
        };
    }
}
