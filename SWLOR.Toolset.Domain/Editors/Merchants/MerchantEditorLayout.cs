using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Merchants
{
    /// <summary>The SWLOR-authored fields exposed by the dedicated merchant editor.</summary>
    public static class MerchantEditorLayout
    {
        public const int MaxNameLength = 64;
        public const int MaxTagLength = 32;

        public static IReadOnlyList<BehaviorFieldDefinition> Details { get; } = new[]
        {
            new BehaviorFieldDefinition
            {
                Label = "Name", Name = "LocName", Kind = BehaviorFieldKind.LocalizedText,
                FieldType = GffFieldType.CExoLocString, MaxLength = MaxNameLength
            },
            new BehaviorFieldDefinition
            {
                Label = "Tag", Name = "Tag", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.CExoString, MaxLength = MaxTagLength
            },
            new BehaviorFieldDefinition
            {
                Label = "ResRef", Name = "ResRef", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.ResRef, MaxLength = NwnResRef.MaxLength,
                IsRequired = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Category", Name = "ID", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte, ChoicesKey = MerchantChoiceKeys.PaletteCategories,
                IsSearchable = true, IsInlineSearch = true
            }
        };

        public static IReadOnlyList<BehaviorFieldDefinition> Pricing { get; } = new[]
        {
            new BehaviorFieldDefinition
            {
                Label = "Sell mark up %", Name = "MarkUp", Kind = BehaviorFieldKind.Integer,
                FieldType = GffFieldType.Int, Minimum = 0
            },
            new BehaviorFieldDefinition
            {
                Label = "Buy mark down %", Name = "MarkDown", Kind = BehaviorFieldKind.Integer,
                FieldType = GffFieldType.Int, Minimum = 0
            }
        };
    }
}
