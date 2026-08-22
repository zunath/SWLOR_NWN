using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>The stable field layout of the creature editor's direct UTC surfaces.</summary>
    public static class CreatureEditorLayout
    {
        public const int MaxTagLength = 32;
        public const int MaxNameLength = 64;

        public static IReadOnlyList<BehaviorFieldDefinition> Basic { get; } = new[]
        {
            Field("First Name", "FirstName", BehaviorFieldKind.LocalizedText, GffFieldType.CExoLocString,
                maxLength: MaxNameLength),
            Field("Last Name", "LastName", BehaviorFieldKind.LocalizedText, GffFieldType.CExoLocString,
                maxLength: MaxNameLength),
            Field("Tag", "Tag", BehaviorFieldKind.Text, GffFieldType.CExoString, maxLength: MaxTagLength),
            Field("ResRef", "TemplateResRef", BehaviorFieldKind.Text, GffFieldType.ResRef,
                maxLength: NwnResRef.MaxLength, readOnly: true),
            Choice("Category", "PaletteID", GffFieldType.Byte, CreatureChoiceKeys.PaletteCategories,
                searchable: true, inlineSearch: true),
            Choice("Movement", "WalkRate", GffFieldType.Int, CreatureChoiceKeys.MovementRates),
            Field("Description", "Description", BehaviorFieldKind.Paragraph, GffFieldType.CExoLocString)
        };

        public static IReadOnlyList<BehaviorFieldDefinition> Flags { get; } = new[]
        {
            Field("Plot", "Plot", BehaviorFieldKind.Check, GffFieldType.Byte),
            Field("Immortal", "IsImmortal", BehaviorFieldKind.Check, GffFieldType.Byte),
            Field("No Permanent Death", "NoPermDeath", BehaviorFieldKind.Check, GffFieldType.Byte),
            Field("Disarmable", "Disarmable", BehaviorFieldKind.Check, GffFieldType.Byte)
        };

        public static IReadOnlyList<BehaviorFieldDefinition> Ai { get; } = new[]
        {
            Choice("Faction", "FactionID", GffFieldType.Word, CreatureChoiceKeys.Factions)
        };

        public static IReadOnlyList<BehaviorFieldDefinition> Appearance { get; } = new[]
        {
            Choice("Race", "Race", GffFieldType.Byte, CreatureChoiceKeys.Races,
                searchable: true, inlineSearch: true),
            Choice("Portrait", "PortraitId", GffFieldType.Word, CreatureChoiceKeys.Portraits,
                searchable: true, inlineGallery: true),
            Choice("Gender", "Gender", GffFieldType.Byte, CreatureChoiceKeys.Genders),
            Choice("Phenotype", "Phenotype", GffFieldType.Int, CreatureChoiceKeys.Phenotypes),
            Choice("Sound Set", "SoundSetFile", GffFieldType.Word, CreatureChoiceKeys.SoundSets,
                searchable: true, inlineSearch: true)
        };

        public static IReadOnlyList<BehaviorFieldDefinition> QuestTarget { get; } = new[]
        {
            Choice("Quest Target Group", "QUEST_NPC_GROUP_ID", GffFieldType.Int,
                CreatureChoiceKeys.NpcGroups, true, local: true)
        };

        public static IReadOnlyList<BehaviorFieldDefinition> DialogRole { get; } = new[]
        {
            Choice("Conversation Blueprint", "Conversation", GffFieldType.ResRef,
                CreatureChoiceKeys.Dialogs, true),
            Choice("Scripted Dialog", "CONVERSATION", GffFieldType.CExoString,
                CreatureChoiceKeys.DialogDefinitions, true, local: true)
        };

        public static IReadOnlyList<BehaviorFieldDefinition> GuildMaster { get; } = new[]
        {
            Choice("Guild", "GUILD_ID", GffFieldType.Int,
                CreatureChoiceKeys.Guilds, local: true),
            Choice("Rank 1 Store", "STORE_TAG_RANK_1", GffFieldType.CExoString,
                CreatureChoiceKeys.GuildStores, true, local: true),
            Choice("Rank 2 Store", "STORE_TAG_RANK_2", GffFieldType.CExoString,
                CreatureChoiceKeys.GuildStores, true, local: true),
            Choice("Rank 3 Store", "STORE_TAG_RANK_3", GffFieldType.CExoString,
                CreatureChoiceKeys.GuildStores, true, local: true),
            Choice("Rank 4 Store", "STORE_TAG_RANK_4", GffFieldType.CExoString,
                CreatureChoiceKeys.GuildStores, true, local: true),
            Choice("Rank 5 Store", "STORE_TAG_RANK_5", GffFieldType.CExoString,
                CreatureChoiceKeys.GuildStores, true, local: true)
        };

        public static IReadOnlyList<BehaviorFieldDefinition> BeastDna { get; } = new[]
        {
            Choice("Beast Type", "BEAST_TYPE", GffFieldType.Int,
                CreatureChoiceKeys.BeastTypes, true, local: true)
        };

        public static IReadOnlyList<BehaviorFieldDefinition> Presentation { get; } = new[]
        {
            Choice("Permanent Visual Effect", "PERMANENT_VFX_ID", GffFieldType.Int,
                CreatureChoiceKeys.VisualEffects, true, local: true),
            Field("Statue", "PARALYZE", BehaviorFieldKind.Check, GffFieldType.Int, local: true),
            Field("Never Attacks", "DAZE", BehaviorFieldKind.Check, GffFieldType.Int, local: true)
        };

        private static BehaviorFieldDefinition Field(
            string label,
            string name,
            BehaviorFieldKind kind,
            GffFieldType type,
            bool local = false,
            int maxLength = 0,
            bool readOnly = false,
            long? minimum = null,
            long? maximum = null,
            string? note = null) => new()
        {
            Label = label,
            Name = name,
            Kind = kind,
            FieldType = type,
            Storage = local ? BehaviorFieldStorage.Local : BehaviorFieldStorage.Field,
            MaxLength = maxLength,
            IsReadOnly = readOnly,
            Minimum = minimum,
            Maximum = maximum,
            Note = note
        };

        private static BehaviorFieldDefinition Choice(
            string label,
            string name,
            GffFieldType type,
            string choicesKey,
            bool searchable = false,
            bool local = false,
            bool inlineSearch = false,
            bool inlineGallery = false) => new()
        {
            Label = label,
            Name = name,
            Kind = BehaviorFieldKind.Choice,
            FieldType = type,
            Storage = local ? BehaviorFieldStorage.Local : BehaviorFieldStorage.Field,
            ChoicesKey = choicesKey,
            IsSearchable = searchable,
            IsInlineSearch = inlineSearch,
            IsInlineGallery = inlineGallery
        };
    }
}
