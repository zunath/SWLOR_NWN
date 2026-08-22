namespace SWLOR.Toolset.Domain.GameData.GameCode
{
    /// <summary>
    /// Read-only surface over content-validation facts pulled from the SWLOR.Game.Server C# game
    /// code: NPC group and key item enums, plus quest and spawn table string IDs. The editor uses
    /// this to validate creature <c>QUEST_NPC_GROUP_ID</c>/<c>CREATURE_SPAWN_TABLE_ID</c> locals and
    /// encounter-activator quest references against what the game code actually defines.
    /// </summary>
    public interface IGameCodeIndex
    {
        /// <summary>
        /// True when the SWLOR.Game.Server source folders needed to scan quest/spawn table IDs
        /// were found and readable. When false, <see cref="QuestIds"/> and <see cref="SpawnTableIds"/>
        /// are empty, but <see cref="NpcGroups"/> and <see cref="KeyItems"/> remain populated since
        /// they are read via reflection over the referenced assembly, not source scanning.
        /// </summary>
        bool IsSourceScanAvailable { get; }

        /// <summary>NPC group enum value to display name, read from <c>NPCGroupType</c>.</summary>
        IReadOnlyDictionary<int, string> NpcGroups { get; }

        /// <summary>Quest ids whose kill objectives reference each NPC group.</summary>
        IReadOnlyDictionary<int, IReadOnlyList<string>> NpcGroupQuestIds =>
            new Dictionary<int, IReadOnlyList<string>>();

        /// <summary>Key item enum value to display name, read from <c>KeyItemType</c>.</summary>
        IReadOnlyDictionary<int, string> KeyItems { get; }

        /// <summary>Quest IDs declared via <c>QuestBuilder.Create(id, ...)</c> calls.</summary>
        IReadOnlyCollection<string> QuestIds { get; }

        /// <summary>
        /// Full quest definitions by id - name, state count, journal text, repeatability and
        /// prerequisites. Read from the same source scan as <see cref="QuestIds"/>, so it is empty
        /// when <see cref="IsSourceScanAvailable"/> is false.
        /// </summary>
        IReadOnlyDictionary<string, QuestDefinitionInfo> Quests { get; }

        /// <summary>Faction enum value to display name, read from <c>FactionType</c>.</summary>
        IReadOnlyDictionary<int, string> Factions { get; }

        /// <summary>Skill enum value to display name, read from <c>SkillType</c>.</summary>
        IReadOnlyDictionary<int, string> Skills { get; }

        /// <summary>
        /// Skill enum value to its C# member name. Conversations may name a skill either way, so
        /// both spellings have to resolve.
        /// </summary>
        IReadOnlyDictionary<int, string> SkillEnumNames { get; }

        /// <summary>The quest with this id, or null when it is not declared anywhere.</summary>
        QuestDefinitionInfo? FindQuest(string questId);

        /// <summary>Spawn table IDs declared via <c>SpawnTableBuilder.Create(id, ...)</c> calls.</summary>
        IReadOnlyCollection<string> SpawnTableIds { get; }

        /// <summary>Spawn table IDs paired with author-facing names for editor pickers.</summary>
        IReadOnlyList<SpawnTableInfo> SpawnTables { get; }

        /// <summary>The fishing spawn tables declared through CreateFishingPoint helper calls.</summary>
        IReadOnlyCollection<string> FishingSpawnTableIds { get; }

        /// <summary>Fishing spawn table IDs paired with their location names.</summary>
        IReadOnlyList<SpawnTableInfo> FishingSpawnTables { get; }

        /// <summary>Planet landing waypoint tags and their player-facing planet names.</summary>
        IReadOnlyList<WaypointDestinationInfo> PlanetLandingWaypoints { get; }

        /// <summary>Planet orbit waypoint tags and their player-facing planet names.</summary>
        IReadOnlyList<WaypointDestinationInfo> OrbitWaypoints { get; }

        /// <summary>Taxi stop tags and their declared destination metadata.</summary>
        IReadOnlyList<TaxiDestinationInfo> TaxiDestinations { get; }

        /// <summary>Waypoint tags used by death respawn paths.</summary>
        IReadOnlyCollection<string> DeathRespawnWaypointTags { get; }

        /// <summary>Waypoint tags used by character rebuild paths.</summary>
        IReadOnlyCollection<string> RebuildWaypointTags { get; }

        /// <summary>
        /// Loot table IDs declared via <c>LootTableBuilder.Create(id)</c> calls in
        /// <c>Feature/LootTableDefinition</c>. These back the loot-table pickers on gathering
        /// behaviors; a name that is not here reaches the player as a "this site is broken" message.
        /// </summary>
        IReadOnlyCollection<string> LootTableIds { get; }

        /// <summary>Conversation class names a <c>CONVERSATION</c> local can name.</summary>
        IReadOnlyCollection<string> DialogNames { get; }

        /// <summary>Crafting skill enum value to display name, read from <c>SkillType</c>.</summary>
        IReadOnlyDictionary<int, string> SkillTypes { get; }

        /// <summary>Active player-market region value to display name.</summary>
        IReadOnlyDictionary<int, string> MarketRegions { get; }

        /// <summary>Visual effect id to enum name, read from <c>VisualEffect</c>.</summary>
        IReadOnlyDictionary<int, string> VisualEffects { get; }

        /// <summary>Screenshot and selection metadata for visual effects documented by the project.</summary>
        IReadOnlyDictionary<int, VisualEffectReferenceInfo> VisualEffectReferences { get; }

        /// <summary>True if <paramref name="lootTableId"/> matches a declared loot table.</summary>
        bool IsValidLootTableId(string lootTableId);

        /// <summary>True if <paramref name="dialogName"/> matches a real conversation class.</summary>
        bool IsValidDialogName(string dialogName);

        /// <summary>True if <paramref name="npcGroupValue"/> is a known <c>NPCGroupType</c> value.</summary>
        bool IsValidNpcGroup(int npcGroupValue);

        /// <summary>True if <paramref name="questId"/> matches a known quest ID.</summary>
        bool IsValidQuestId(string questId);

        /// <summary>True if <paramref name="spawnTableId"/> matches a known spawn table ID.</summary>
        bool IsValidSpawnTableId(string spawnTableId);
    }
}
