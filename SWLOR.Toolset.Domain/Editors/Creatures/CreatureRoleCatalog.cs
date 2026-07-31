namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>Creature roles are additive panels, not mutually-exclusive presets.</summary>
    public static class CreatureRoleCatalog
    {
        public static IReadOnlyList<CreatureRole> All { get; } = new[]
        {
            new CreatureRole
            {
                Id = "quest_target", DisplayName = "Quest Target", Group = "GAMEPLAY",
                Summary = "Counts this creature toward kill objectives that use the selected group.",
                Fields = CreatureEditorLayout.QuestTarget
            },
            new CreatureRole
            {
                Id = "dialog", DisplayName = "Dialog", Group = "GAMEPLAY",
                Summary = "Starts a scripted conversation when a player speaks to this creature.",
                Fields = CreatureEditorLayout.DialogRole
            },
            new CreatureRole
            {
                Id = "guild_master", DisplayName = "Guild Master", Group = "SERVICES",
                Summary = "Offers the guild stores available at each membership rank.",
                Fields = CreatureEditorLayout.GuildMaster
            },
            new CreatureRole
            {
                Id = "beast_dna", DisplayName = "Beast DNA", Group = "GAMEPLAY",
                Summary = "Allows DNA extraction for the selected beast type. Extraction level follows NPC Level.",
                Fields = CreatureEditorLayout.BeastDna
            },
            new CreatureRole
            {
                Id = "presentation", DisplayName = "Presentation", Group = "PRESENTATION",
                Summary = "Controls persistent visual and motion cues.",
                Fields = CreatureEditorLayout.Presentation
            },
            new CreatureRole
            {
                Id = "custom", DisplayName = "Custom Variables",
                Summary = "Edit unrecognized local variables without hiding them.",
                AllowsVariables = true
            }
        };

        public static CreatureRole Default => All[0];
    }
}
