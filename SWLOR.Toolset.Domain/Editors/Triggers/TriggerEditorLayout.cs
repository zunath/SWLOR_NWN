using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>
    /// The rows of the trigger editor's Basic tab: what the trigger is, plus the handful of engine
    /// properties that belong to every behavior rather than to one of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There is no Advanced tab. A second tab of raw fields is a place a builder has to know to
    /// look, and everything that used to sit there is either an ordinary property of the object
    /// (which belongs on Basic) or a value a named behavior owns (which belongs under Custom, beside
    /// the rest of the raw sheet). Trigger Type is the second kind: every other behavior writes it,
    /// so offering it beside them would invite a builder to disagree with the behavior they just
    /// chose - and the behavior would win on the next swap.
    /// </para>
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
        /// A tag is a CExoString, so the <b>engine imposes no maximum</b> — this is the base
        /// toolset's own editor limit, adopted here for parity. Every trigger tag in the module fits
        /// inside it, the longest being 29 characters.
        /// </summary>
        public const int MaxTagLength = 32;

        /// <summary>
        /// A house limit, not an engine one: a name is a CExoString and the engine caps nothing. It
        /// sits well clear of real content — the longest trigger name in the module is 45 characters
        /// — so it bounds the field for the counter's sake without standing in anyone's way.
        /// </summary>
        public const int MaxNameLength = 64;

        /// <summary>
        /// The raw engine Type. Offered by the Custom behavior alone, which is why it lives here
        /// rather than on a tab of its own.
        /// </summary>
        public static IReadOnlyList<BehaviorChoice> TriggerTypeChoices { get; } = new[]
        {
            new BehaviorChoice(0, "Generic"),
            new BehaviorChoice(1, "Area Transition"),
            new BehaviorChoice(2, "Trap")
        };

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
            new BehaviorFieldDefinition
            {
                Label = "ResRef", Name = "TemplateResRef", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.ResRef, MaxLength = NwnResRef.MaxLength, IsRequired = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Category", Name = "PaletteID", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte, ChoicesKey = TriggerChoiceKeys.PaletteCategories,
                IsSearchable = true, IsInlineSearch = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Faction", Name = "Faction", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Dword, ChoicesKey = TriggerChoiceKeys.Factions
            },
            new BehaviorFieldDefinition
            {
                Label = "Highlight Height", Name = "HighlightHeight", Kind = BehaviorFieldKind.Float,
                FieldType = GffFieldType.Float
            },
            new BehaviorFieldDefinition
            {
                Label = "Key Tag", Name = "KeyName", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.CExoString, MaxLength = MaxTagLength
            },
            new BehaviorFieldDefinition
            {
                Label = "Auto-Remove Key", Name = "AutoRemoveKey", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte
            }
        };
    }
}
