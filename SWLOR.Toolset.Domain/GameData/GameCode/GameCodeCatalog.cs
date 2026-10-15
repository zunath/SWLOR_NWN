using System.Text.Json;
using SWLOR.Toolset.Domain.Distribution;

namespace SWLOR.Toolset.Domain.GameData.GameCode;

/// <summary>Release-time snapshot of editor facts; no source checkout is needed at runtime.</summary>
public sealed class GameCodeCatalog : IGameCodeIndex
{
    public int SchemaVersion { get; set; } = 1;
    public string ContentRevision { get; set; } = string.Empty;
    public bool IsSourceScanAvailable { get; set; } = true;
    public Dictionary<int, string> NpcGroups { get; set; } = new();
    public Dictionary<int, IReadOnlyList<string>> NpcGroupQuestIds { get; set; } = new();
    public Dictionary<int, string> KeyItems { get; set; } = new();
    public List<string> QuestIds { get; set; } = new();
    public Dictionary<string, QuestDefinitionInfo> Quests { get; set; } = new();
    public Dictionary<int, string> Factions { get; set; } = new();
    public Dictionary<int, string> Skills { get; set; } = new();
    public Dictionary<int, string> SkillEnumNames { get; set; } = new();
    public List<string> SpawnTableIds { get; set; } = new();
    public List<SpawnTableInfo> SpawnTables { get; set; } = new();
    public List<string> FishingSpawnTableIds { get; set; } = new();
    public List<SpawnTableInfo> FishingSpawnTables { get; set; } = new();
    public List<WaypointDestinationInfo> PlanetLandingWaypoints { get; set; } = new();
    public List<WaypointDestinationInfo> OrbitWaypoints { get; set; } = new();
    public List<TaxiDestinationInfo> TaxiDestinations { get; set; } = new();
    public List<string> DeathRespawnWaypointTags { get; set; } = new();
    public List<string> RebuildWaypointTags { get; set; } = new();
    public List<string> LootTableIds { get; set; } = new();
    public List<string> DialogNames { get; set; } = new();
    public Dictionary<int, string> SkillTypes { get; set; } = new();
    public Dictionary<int, string> MarketRegions { get; set; } = new();
    public Dictionary<int, string> VisualEffects { get; set; } = new();
    public Dictionary<int, VisualEffectReferenceInfo> VisualEffectReferences { get; set; } = new();
    IReadOnlyDictionary<int, string> IGameCodeIndex.NpcGroups => NpcGroups;
    IReadOnlyDictionary<int, IReadOnlyList<string>> IGameCodeIndex.NpcGroupQuestIds => NpcGroupQuestIds;
    IReadOnlyDictionary<int, string> IGameCodeIndex.KeyItems => KeyItems;
    IReadOnlyCollection<string> IGameCodeIndex.QuestIds => QuestIds;
    IReadOnlyDictionary<string, QuestDefinitionInfo> IGameCodeIndex.Quests => Quests;
    IReadOnlyDictionary<int, string> IGameCodeIndex.Factions => Factions;
    IReadOnlyDictionary<int, string> IGameCodeIndex.Skills => Skills;
    IReadOnlyDictionary<int, string> IGameCodeIndex.SkillEnumNames => SkillEnumNames;
    IReadOnlyCollection<string> IGameCodeIndex.SpawnTableIds => SpawnTableIds;
    IReadOnlyList<SpawnTableInfo> IGameCodeIndex.SpawnTables => SpawnTables;
    IReadOnlyCollection<string> IGameCodeIndex.FishingSpawnTableIds => FishingSpawnTableIds;
    IReadOnlyList<SpawnTableInfo> IGameCodeIndex.FishingSpawnTables => FishingSpawnTables;
    IReadOnlyList<WaypointDestinationInfo> IGameCodeIndex.PlanetLandingWaypoints => PlanetLandingWaypoints;
    IReadOnlyList<WaypointDestinationInfo> IGameCodeIndex.OrbitWaypoints => OrbitWaypoints;
    IReadOnlyList<TaxiDestinationInfo> IGameCodeIndex.TaxiDestinations => TaxiDestinations;
    IReadOnlyCollection<string> IGameCodeIndex.DeathRespawnWaypointTags => DeathRespawnWaypointTags;
    IReadOnlyCollection<string> IGameCodeIndex.RebuildWaypointTags => RebuildWaypointTags;
    IReadOnlyCollection<string> IGameCodeIndex.LootTableIds => LootTableIds;
    IReadOnlyCollection<string> IGameCodeIndex.DialogNames => DialogNames;
    IReadOnlyDictionary<int, string> IGameCodeIndex.SkillTypes => SkillTypes;
    IReadOnlyDictionary<int, string> IGameCodeIndex.MarketRegions => MarketRegions;
    IReadOnlyDictionary<int, string> IGameCodeIndex.VisualEffects => VisualEffects;
    IReadOnlyDictionary<int, VisualEffectReferenceInfo> IGameCodeIndex.VisualEffectReferences => VisualEffectReferences;
    public static GameCodeCatalog Capture(IGameCodeIndex source, string revision) => new()
    {
        ContentRevision = revision,
            IsSourceScanAvailable = source.IsSourceScanAvailable,
            NpcGroups = source.NpcGroups.ToDictionary(pair => pair.Key, pair => pair.Value),
            NpcGroupQuestIds = source.NpcGroupQuestIds.ToDictionary(pair => pair.Key, pair => pair.Value),
            KeyItems = source.KeyItems.ToDictionary(pair => pair.Key, pair => pair.Value),
            QuestIds = source.QuestIds.ToList(),
            Quests = source.Quests.ToDictionary(pair => pair.Key, pair => pair.Value),
            Factions = source.Factions.ToDictionary(pair => pair.Key, pair => pair.Value),
            Skills = source.Skills.ToDictionary(pair => pair.Key, pair => pair.Value),
            SkillEnumNames = source.SkillEnumNames.ToDictionary(pair => pair.Key, pair => pair.Value),
            SpawnTableIds = source.SpawnTableIds.ToList(),
            SpawnTables = source.SpawnTables.ToList(),
            FishingSpawnTableIds = source.FishingSpawnTableIds.ToList(),
            FishingSpawnTables = source.FishingSpawnTables.ToList(),
            PlanetLandingWaypoints = source.PlanetLandingWaypoints.ToList(),
            OrbitWaypoints = source.OrbitWaypoints.ToList(),
            TaxiDestinations = source.TaxiDestinations.ToList(),
            DeathRespawnWaypointTags = source.DeathRespawnWaypointTags.ToList(),
            RebuildWaypointTags = source.RebuildWaypointTags.ToList(),
            LootTableIds = source.LootTableIds.ToList(),
            DialogNames = source.DialogNames.ToList(),
            SkillTypes = source.SkillTypes.ToDictionary(pair => pair.Key, pair => pair.Value),
            MarketRegions = source.MarketRegions.ToDictionary(pair => pair.Key, pair => pair.Value),
            VisualEffects = source.VisualEffects.ToDictionary(pair => pair.Key, pair => pair.Value),
            VisualEffectReferences = source.VisualEffectReferences.ToDictionary(pair => pair.Key, pair => pair.Value),
    };
    public static GameCodeCatalog Load(string path, string revision)
    {
        var catalog = JsonSerializer.Deserialize<GameCodeCatalog>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException("The editor catalog is empty.");
        if (catalog.SchemaVersion != 1 || catalog.ContentRevision != revision || !ContentPaths.IsRevision(revision))
            throw new InvalidDataException("The editor catalog does not match this workspace. Download its matching content snapshot.");
        if (!catalog.IsSourceScanAvailable || catalog.QuestIds.Count == 0 || catalog.SpawnTableIds.Count == 0)
            throw new InvalidDataException("The editor catalog is incomplete.");
        return catalog;
    }
    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Options));
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, IncludeFields = true };
    public bool IsValidNpcGroup(int value) => NpcGroups.ContainsKey(value);
    public bool IsValidQuestId(string id) => QuestIds.Contains(id, StringComparer.Ordinal);
    public QuestDefinitionInfo? FindQuest(string id) => id != null && Quests.TryGetValue(id, out var quest) ? quest : null;
    public bool IsValidSpawnTableId(string id) => SpawnTableIds.Contains(id, StringComparer.Ordinal);
    public bool IsValidLootTableId(string id) => LootTableIds.Contains(id, StringComparer.Ordinal);
    public bool IsValidDialogName(string id) => DialogNames.Contains(id, StringComparer.Ordinal);
}
