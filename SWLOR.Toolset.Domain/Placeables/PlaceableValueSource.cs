namespace SWLOR.Toolset.Domain.Placeables
{
    /// <summary>
    /// Where the legal values of a <see cref="PlaceableFieldKind.Choice"/> field come from. Every
    /// source is something the game code or the module already declares, so a picker can never
    /// drift from what the server will accept at runtime.
    /// </summary>
    /// <remarks>
    /// These are the values that used to be free text on a variable row. Each one has a real
    /// failure mode when mistyped: a loot table name that is not registered reaches the player as
    /// "please inform an admin that this site is broken", and a destination tag that matches no
    /// object silently teleports nobody.
    /// </remarks>
    public enum PlaceableValueSource
    {
        /// <summary>No constrained domain; the field is free text or a plain number.</summary>
        None,

        /// <summary>Loot table ids declared by <c>Feature/LootTableDefinition</c>.</summary>
        LootTables,

        /// <summary>Spawn table ids declared by <c>Feature/SpawnDefinition</c>.</summary>
        SpawnTables,

        /// <summary>Quest ids declared by <c>Feature/QuestDefinition</c>.</summary>
        Quests,

        /// <summary>Key item enum values (<c>KeyItemType</c>).</summary>
        KeyItems,

        /// <summary>C# dialog class names (subclasses of <c>DialogBase</c>).</summary>
        Dialogs,

        /// <summary>Tags of objects that exist somewhere in the module.</summary>
        ObjectTags,

        /// <summary>Crafting skill enum values (<c>SkillType</c>).</summary>
        SkillTypes,

        /// <summary>Active player-market regions (<c>MarketRegionType</c>).</summary>
        MarketRegions,

        /// <summary>Visual effect ids (<c>VisualEffect</c>).</summary>
        VisualEffects,

        /// <summary>Placeable blueprints already available in the open module.</summary>
        PlaceableBlueprints,

        /// <summary>Creature blueprints already available in the open module.</summary>
        CreatureBlueprints
    }
}
