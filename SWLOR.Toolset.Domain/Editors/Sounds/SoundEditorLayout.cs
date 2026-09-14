using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Sounds
{
    /// <summary>
    /// The fixed Basic rows around the behavior-specific sound rows.
    /// </summary>
    /// <remarks>
    /// There is no Advanced tab. The playback flags that used to sit there belong to whichever
    /// behavior is selected, so they are offered under Custom; the rest are ordinary properties of
    /// the blueprint and sit on Basic where they can be found without knowing a second tab exists.
    /// </remarks>
    public static class SoundEditorLayout
    {
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
                Label = "ResRef", Name = "TemplateResRef", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.ResRef, MaxLength = NwnResRef.MaxLength, IsRequired = true
            },
            new BehaviorFieldDefinition
            {
                Label = "Category", Name = "PaletteID", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte, ChoicesKey = SoundChoiceKeys.PaletteCategories,
                IsSearchable = true, IsInlineSearch = true
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
            }
        };

        /// <summary>
        /// The raw playback flags, offered by the Custom behavior alone. Every named behavior pins
        /// the ones its shape requires, so setting them beside one would only be overwritten the
        /// next time that behavior applied itself.
        /// </summary>
        public static IReadOnlyList<BehaviorFieldDefinition> RawPlaybackFields { get; } = new[]
        {
            new BehaviorFieldDefinition
            {
                Label = "Positional", Name = "Positional", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte
            },
            new BehaviorFieldDefinition
            {
                Label = "Random position", Name = "RandomPosition", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte
            },
            new BehaviorFieldDefinition
            {
                Label = "Continuous", Name = "Continuous", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte
            },
            new BehaviorFieldDefinition
            {
                Label = "Looping", Name = "Looping", Kind = BehaviorFieldKind.Check,
                FieldType = GffFieldType.Byte
            },
            new BehaviorFieldDefinition
            {
                Label = "Interval (seconds)", Name = "Interval", Kind = BehaviorFieldKind.Integer,
                FieldType = GffFieldType.Dword
            }
        };
    }
}
