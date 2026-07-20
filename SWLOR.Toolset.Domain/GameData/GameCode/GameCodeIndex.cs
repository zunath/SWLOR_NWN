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

        private readonly HashSet<string> _questIds;
        private readonly HashSet<string> _spawnTableIds;

        public bool IsSourceScanAvailable { get; }
        public IReadOnlyDictionary<int, string> NpcGroups { get; }
        public IReadOnlyDictionary<int, string> KeyItems { get; }
        public IReadOnlyCollection<string> QuestIds => _questIds;
        public IReadOnlyCollection<string> SpawnTableIds => _spawnTableIds;

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

            var questDirectory = CombineIfUsable(gameServerSourceRoot, QuestDefinitionRelativePath);
            var spawnDirectory = CombineIfUsable(gameServerSourceRoot, SpawnDefinitionRelativePath);

            IsSourceScanAvailable = questDirectory != null && spawnDirectory != null;

            _questIds = questDirectory != null
                ? SourceIdScanner.ScanBuilderCreateIds(questDirectory)
                : new HashSet<string>(StringComparer.Ordinal);

            _spawnTableIds = spawnDirectory != null
                ? SourceIdScanner.ScanBuilderCreateIds(spawnDirectory)
                : new HashSet<string>(StringComparer.Ordinal);
        }

        public bool IsValidNpcGroup(int npcGroupValue) => NpcGroups.ContainsKey(npcGroupValue);

        public bool IsValidQuestId(string questId) =>
            !string.IsNullOrEmpty(questId) && _questIds.Contains(questId);

        public bool IsValidSpawnTableId(string spawnTableId) =>
            !string.IsNullOrEmpty(spawnTableId) && _spawnTableIds.Contains(spawnTableId);

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
