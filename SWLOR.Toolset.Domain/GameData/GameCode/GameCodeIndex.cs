namespace SWLOR.Toolset.Domain.GameData.GameCode
{
    /// <summary>
    /// Indexes validation-relevant facts from the SWLOR.Game.Server C# game code.
    ///
    /// NPC groups and key items are read via <see cref="ReflectionEnumReader"/> - direct reflection
    /// over the compile-time referenced assembly. That is always available (it does not depend on
    /// any file path) since SWLOR.Toolset.Domain already has a ProjectReference to
    /// SWLOR.Game.Server.
    ///
    /// Quest and spawn table IDs are declared as string arguments inside builder method bodies
    /// (<c>QuestBuilder.Create(id, ...)</c> / <c>SpawnTableBuilder.Create(id, ...)</c>), which
    /// reflection cannot see. Those are recovered by <see cref="SourceIdScanner"/> scanning the
    /// SWLOR.Game.Server source tree, so they require a valid <c>gameServerSourceRoot</c>.
    /// </summary>
    public sealed class GameCodeIndex : IGameCodeIndex
    {
        private const string QuestDefinitionRelativePath = "Feature/QuestDefinition";
        private const string SpawnDefinitionRelativePath = "Feature/SpawnDefinition";
        private const string LootTableDefinitionRelativePath = "Feature/LootTableDefinition";

        private readonly HashSet<string> _questIds;
        private readonly HashSet<string> _spawnTableIds;
        private readonly HashSet<string> _fishingSpawnTableIds;
        private readonly HashSet<string> _lootTableIds;
        private readonly HashSet<string> _dialogNames;
        private readonly Dictionary<string, QuestDefinitionInfo> _quests;

        public bool IsSourceScanAvailable { get; }
        public IReadOnlyDictionary<int, string> NpcGroups { get; }
        public IReadOnlyDictionary<int, IReadOnlyList<string>> NpcGroupQuestIds { get; }
        public IReadOnlyDictionary<int, string> KeyItems { get; }
        public IReadOnlyDictionary<int, string> Factions { get; }
        public IReadOnlyDictionary<int, string> Skills { get; }
        public IReadOnlyDictionary<int, string> SkillEnumNames { get; }
        public IReadOnlyCollection<string> QuestIds => _questIds;
        public IReadOnlyDictionary<string, QuestDefinitionInfo> Quests => _quests;
        public IReadOnlyCollection<string> SpawnTableIds => _spawnTableIds;
        public IReadOnlyList<SpawnTableInfo> SpawnTables { get; }
        public IReadOnlyCollection<string> FishingSpawnTableIds => _fishingSpawnTableIds;
        public IReadOnlyList<SpawnTableInfo> FishingSpawnTables { get; }
        public IReadOnlyList<WaypointDestinationInfo> PlanetLandingWaypoints { get; }
        public IReadOnlyList<WaypointDestinationInfo> OrbitWaypoints { get; }
        public IReadOnlyList<TaxiDestinationInfo> TaxiDestinations { get; }
        public IReadOnlyCollection<string> DeathRespawnWaypointTags { get; }
        public IReadOnlyCollection<string> RebuildWaypointTags { get; }
        public IReadOnlyCollection<string> LootTableIds => _lootTableIds;
        public IReadOnlyCollection<string> DialogNames => _dialogNames;
        public IReadOnlyDictionary<int, string> SkillTypes { get; }
        public IReadOnlyDictionary<int, string> MarketRegions { get; }
        public IReadOnlyDictionary<int, string> VisualEffects { get; }
        public IReadOnlyDictionary<int, VisualEffectReferenceInfo> VisualEffectReferences { get; }

        /// <summary>
        /// Builds the index. <paramref name="gameServerSourceRoot"/> should point at the
        /// SWLOR.Game.Server project directory (the one containing a <c>Feature</c> subfolder). If
        /// it is null, blank, or the expected Feature/QuestDefinition and Feature/SpawnDefinition
        /// subfolders can't be found there, quest/spawn ID collections come back empty and
        /// <see cref="IsSourceScanAvailable"/> is false. This constructor never throws - every
        /// probe is defensive so a missing or relocated source tree just degrades the source-scan
        /// half of the index rather than blocking construction.
        /// </summary>
        public GameCodeIndex(string? gameServerSourceRoot)
        {
            NpcGroups = ReflectionEnumReader.ReadNpcGroups();
            KeyItems = ReflectionEnumReader.ReadKeyItems();
            SkillTypes = ReflectionGameplayEnumReader.ReadSkillTypes();
            MarketRegions = ReflectionGameplayEnumReader.ReadMarketRegions();
            VisualEffects = ReflectionGameplayEnumReader.ReadVisualEffects();
            VisualEffectReferences = VisualEffectReferenceReader.Read(gameServerSourceRoot);
            _dialogNames = new HashSet<string>(ReflectionDialogReader.ReadDialogNames(), StringComparer.Ordinal);
            Factions = ReflectionEnumReader.ReadFactions();
            Skills = ReflectionEnumReader.ReadSkills();
            SkillEnumNames = ReflectionEnumReader.ReadSkillEnumNames();
            PlanetLandingWaypoints = ReflectionWaypointReader.ReadPlanetLandings();
            OrbitWaypoints = ReflectionWaypointReader.ReadPlanetOrbits();
            TaxiDestinations = ReflectionWaypointReader.ReadTaxiDestinations();
            DeathRespawnWaypointTags = new[]
            {
                "DEATH_DEFAULT_RESPAWN_POINT",
                "DTH_DEFAULT_RESPAWN_POINT"
            };
            RebuildWaypointTags = new[]
            {
                "REBUILD_LANDING",
                "REBUILD_TO_SPENDING_LANDING"
            };

            var questDirectory = CombineIfUsable(gameServerSourceRoot, QuestDefinitionRelativePath);
            var spawnDirectory = CombineIfUsable(gameServerSourceRoot, SpawnDefinitionRelativePath);

            var questComplete = false;
            var spawnComplete = false;

            _questIds = questDirectory != null
                ? SourceIdScanner.ScanBuilderCreateIds(questDirectory, out questComplete)
                : new HashSet<string>(StringComparer.Ordinal);

            _quests = questDirectory != null
                ? QuestSourceScanner.Scan(questDirectory, out _)
                : new Dictionary<string, QuestDefinitionInfo>(StringComparer.Ordinal);
            NpcGroupQuestIds = NpcGroupQuestSourceScanner.Scan(questDirectory);

            // The two scans reach different quests: SourceIdScanner only expands helpers whose
            // FIRST parameter is a string, while the guild helpers (Smithery, Engineering, ...)
            // take a QuestBuilder first and the id second - their hundreds of quests appear only
            // in the detailed scan. The pickers read QuestIds, so merge the detailed keys in or
            // eng_tsk_* and friends vanish from the Quest Activator list.
            foreach (var questId in _quests.Keys)
                _questIds.Add(questId);

            _spawnTableIds = spawnDirectory != null
                ? SourceIdScanner.ScanBuilderCreateIds(spawnDirectory, out spawnComplete)
                : new HashSet<string>(StringComparer.Ordinal);
            _fishingSpawnTableIds = new HashSet<string>(
                _spawnTableIds.Where(id => id.StartsWith("FP_", StringComparison.Ordinal)),
                StringComparer.Ordinal);
            SpawnTables = SpawnTableSourceReader.Read(spawnDirectory, _spawnTableIds);
            FishingSpawnTables = SpawnTables
                .Where(table => _fishingSpawnTableIds.Contains(table.Id))
                .ToList();

            // Loot table ids are declared with exactly the same _builder.Create("ID") shape, so the
            // existing scanner reads them unchanged. Their completeness is not folded into
            // IsSourceScanAvailable: an incomplete loot scan degrades a picker to free text, which
            // is a lesser failure than reporting real quest ids as unknown.
            var lootDirectory = CombineIfUsable(gameServerSourceRoot, LootTableDefinitionRelativePath);
            _lootTableIds = lootDirectory != null
                ? SourceIdScanner.ScanBuilderCreateIds(lootDirectory)
                : new HashSet<string>(StringComparer.Ordinal);

            // "Available" has to mean the scan actually read everything, not merely that the directories
            // exist. A denied enumeration or an unreadable file yields a partial set, and callers treat
            // an available scan as authoritative - so validation would report real quest and spawn-table
            // ids as unknown.
            IsSourceScanAvailable = questDirectory != null && spawnDirectory != null &&
                                    questComplete && spawnComplete;
        }

        public bool IsValidNpcGroup(int npcGroupValue) => NpcGroups.ContainsKey(npcGroupValue);

        public bool IsValidQuestId(string questId) =>
            !string.IsNullOrEmpty(questId) && _questIds.Contains(questId);

        public QuestDefinitionInfo? FindQuest(string questId) =>
            !string.IsNullOrEmpty(questId) && _quests.TryGetValue(questId, out var quest) ? quest : null;

        public bool IsValidSpawnTableId(string spawnTableId) =>
            !string.IsNullOrEmpty(spawnTableId) && _spawnTableIds.Contains(spawnTableId);

        public bool IsValidLootTableId(string lootTableId) =>
            !string.IsNullOrEmpty(lootTableId) && _lootTableIds.Contains(lootTableId);

        public bool IsValidDialogName(string dialogName) =>
            !string.IsNullOrEmpty(dialogName) && _dialogNames.Contains(dialogName);

        /// <summary>
        /// Combines <paramref name="root"/> with <paramref name="relative"/> and returns the result
        /// only if it resolves to a directory that actually exists. Returns null (never throws) for
        /// a missing root, an invalid path, or a directory that isn't there.
        /// </summary>
        private static string? CombineIfUsable(string? root, string relative)
        {
            if (string.IsNullOrWhiteSpace(root))
                return null;

            try
            {
                var combined = Path.Combine(root, relative);
                return Directory.Exists(combined) ? combined : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
