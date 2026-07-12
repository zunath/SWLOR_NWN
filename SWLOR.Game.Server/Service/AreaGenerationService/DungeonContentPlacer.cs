using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// Result of populating one generated dungeon instance, reported back to the consumer
    /// (e.g. the /genarea chat command) for confirmation messaging.
    /// </summary>
    public class DungeonPopulationResult
    {
        public int RoomsPopulated { get; set; }
        public int CreaturesSpawned { get; set; }
        public bool BossSpawned { get; set; }
        public string BossResref { get; set; } = string.Empty;
        public bool TreasurePlaced { get; set; }
        public uint TreasureContainer { get; set; } = OBJECT_INVALID;
        public int TreasureItemsSpawned { get; set; }
        public int ExitsPlaced { get; set; }
        public bool ExitPlaced => ExitsPlaced > 0;
    }

    /// <summary>
    /// Populates a freshly generated area with tier-scaled content: ambient creature spawns in
    /// Standard rooms, a boss + treasure container in the Boss room, and an exit placeable in the
    /// Entrance room that returns players to RuntimeAreaInstance.ExitLocation. This is the M4
    /// "content loop" consumer described in design/ProceduralAreaGeneration.md.
    ///
    /// Dungeon theme definitions are discovered via reflection over IDungeonListDefinition at
    /// module load, mirroring how Spawn.cs/Loot.cs/Ability.cs cache their own definitions.
    /// </summary>
    public static class DungeonContentPlacer
    {
        private static readonly Dictionary<string, DungeonDetail> _dungeons = new();
        private static readonly Dictionary<string, DungeonTilesetProfile> _tilesetProfiles = new();
        private static readonly Dictionary<string, DungeonLayoutProfile> _layoutProfiles = new();

        // World-space tile size/offset used by the tile resolver's grid (matches
        // AreaGenerationChatCommand's entrance-jump math: tile (x,y) -> world (x*10+5, y*10+5)).
        private const float TileSize = 10f;
        private const float TileHalf = 5f;

        // Small random offset so multiple creatures placed on the same tile don't stack exactly.
        private const float PositionJitter = 3f;

        // Offset from the boss room/entrance room center used for the treasure container and
        // exit placeable, so they don't spawn exactly on top of the boss or the player's landing spot.
        private const float FeatureOffset = 2.5f;


        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheDungeonDefinitions()
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var type in allTypes.Where(t => typeof(IDungeonTilesetProfileListDefinition).IsAssignableFrom(t)))
            {
                var instance = (IDungeonTilesetProfileListDefinition)Activator.CreateInstance(type);
                foreach (var (key, profile) in instance.BuildTilesetProfiles())
                {
                    if (string.IsNullOrWhiteSpace(key) || _tilesetProfiles.ContainsKey(key))
                    {
                        Log.Write(LogGroup.Error, $"Tileset profile '{key}' in {type.Name} is invalid or duplicated.");
                        continue;
                    }

                    _tilesetProfiles[key] = profile;
                }
            }

            foreach (var type in allTypes.Where(t => typeof(IDungeonLayoutProfileListDefinition).IsAssignableFrom(t)))
            {
                var instance = (IDungeonLayoutProfileListDefinition)Activator.CreateInstance(type);
                foreach (var (key, profile) in instance.BuildLayoutProfiles())
                {
                    if (string.IsNullOrWhiteSpace(key) || _layoutProfiles.ContainsKey(key))
                    {
                        Log.Write(LogGroup.Error, $"Layout profile '{key}' in {type.Name} is invalid or duplicated.");
                        continue;
                    }

                    _layoutProfiles[key] = profile;
                }
            }

            foreach (var type in allTypes.Where(t => typeof(IDungeonListDefinition).IsAssignableFrom(t)))
            {
                var instance = (IDungeonListDefinition)Activator.CreateInstance(type);
                var builtDungeons = instance.BuildDungeons();

                foreach (var dungeon in builtDungeons)
                {
                    if (string.IsNullOrWhiteSpace(dungeon.Key))
                    {
                        Log.Write(LogGroup.Error, $"Dungeon definition in {type.Name} has an invalid theme key.");
                        continue;
                    }

                    if (_dungeons.ContainsKey(dungeon.Key))
                    {
                        Log.Write(LogGroup.Error, $"Dungeon theme '{dungeon.Key}' has already been registered. Please make sure all dungeon themes use a unique key.");
                        continue;
                    }

                    if (!_tilesetProfiles.ContainsKey(dungeon.Value.TilesetProfileKey))
                    {
                        Log.Write(LogGroup.Error, $"Dungeon theme '{dungeon.Key}' references unknown tileset profile '{dungeon.Value.TilesetProfileKey}'.");
                        continue;
                    }

                    if (!_layoutProfiles.ContainsKey(dungeon.Value.LayoutProfileKey))
                    {
                        Log.Write(LogGroup.Error, $"Dungeon theme '{dungeon.Key}' references unknown layout profile '{dungeon.Value.LayoutProfileKey}'.");
                        continue;
                    }

                    _dungeons[dungeon.Key] = dungeon.Value;
                }
            }
        }

        /// <summary>
        /// Resolves a (content, tileset, layout) composition. The theme supplies content and its
        /// default profiles; either profile can be overridden per request — nothing ties a content
        /// package to a specific tileset.
        /// </summary>
        public static DungeonComposition GetComposition(string themeKey, string tilesetProfileKey = null, string layoutProfileKey = null)
        {
            var content = GetDungeonDetail(themeKey);
            var tilesetKey = string.IsNullOrEmpty(tilesetProfileKey) ? content.TilesetProfileKey : tilesetProfileKey;
            var layoutKey = string.IsNullOrEmpty(layoutProfileKey) ? content.LayoutProfileKey : layoutProfileKey;

            if (!_tilesetProfiles.TryGetValue(tilesetKey, out var tileset))
                throw new Exception($"Tileset profile '{tilesetKey}' is not registered.");
            if (!_layoutProfiles.TryGetValue(layoutKey, out var layout))
                throw new Exception($"Layout profile '{layoutKey}' is not registered.");

            return new DungeonComposition
            {
                Content = content,
                Tileset = tileset,
                Layout = layout
            };
        }

        public static bool TilesetProfileExists(string key)
        {
            return _tilesetProfiles.ContainsKey(key);
        }

        public static bool LayoutProfileExists(string key)
        {
            return _layoutProfiles.ContainsKey(key);
        }

        public static IReadOnlyDictionary<string, DungeonTilesetProfile> GetAllTilesetProfiles()
        {
            return _tilesetProfiles;
        }

        public static IReadOnlyDictionary<string, DungeonLayoutProfile> GetAllLayoutProfiles()
        {
            return _layoutProfiles;
        }

        /// <summary>
        /// Retrieves a dungeon theme definition by its unique key.
        /// Throws if the theme is not registered.
        /// </summary>
        public static DungeonDetail GetDungeonDetail(string themeKey)
        {
            if (!_dungeons.TryGetValue(themeKey, out var detail))
                throw new Exception($"Dungeon theme '{themeKey}' is not registered. Did you enter the right key?");

            return detail;
        }

        /// <summary>
        /// Checks whether a dungeon theme is defined by the specified key.
        /// </summary>
        public static bool DungeonThemeExists(string themeKey)
        {
            return _dungeons.ContainsKey(themeKey);
        }

        /// <summary>
        /// Returns every registered dungeon theme, keyed by theme key. Exposed primarily for tests.
        /// </summary>
        public static IReadOnlyDictionary<string, DungeonDetail> GetAllDungeonThemes()
        {
            return _dungeons;
        }

        /// <summary>
        /// Populates a freshly generated area instance with tier-scaled content for the given dungeon
        /// theme: ambient creatures in every Standard room, a boss + filled treasure container in the
        /// Boss room, and an exit placeable at every Exit transition point. All randomness derives
        /// from the instance's layout seed (plus the tier), so a given (seed, tier) always produces
        /// the same population. Safe to call once, immediately after a successful Generate/QueueGeneration.
        /// </summary>
        public static DungeonPopulationResult Populate(RuntimeAreaInstance instance, string themeKey, int tier)
        {
            var detail = GetDungeonDetail(themeKey);
            if (!detail.Tiers.TryGetValue(tier, out var tierDetail))
                throw new Exception($"Dungeon theme '{themeKey}' has no tier {tier} defined.");

            var rng = new System.Random(instance.Layout.Seed ^ (tier * 397));
            var area = instance.Area;
            var result = new DungeonPopulationResult();

            foreach (var room in instance.Layout.Rooms)
            {
                switch (room.Role)
                {
                    case RoomRole.Standard:
                        PopulateStandardRoom(area, room, tierDetail, rng, instance, result);
                        break;

                    case RoomRole.Boss:
                        PopulateBossRoom(area, room, detail, tierDetail, rng, instance, result);
                        break;
                }
            }

            foreach (var transition in instance.Layout.Transitions)
            {
                if (transition.Kind == TransitionKind.Exit)
                    PlaceExit(area, transition, detail, instance, result);
            }

            return result;
        }

        private static void PopulateStandardRoom(
            uint area,
            LayoutRoom room,
            DungeonTierDetail tier,
            System.Random rng,
            RuntimeAreaInstance instance,
            DungeonPopulationResult result)
        {
            if (room.Tiles.Count == 0 || tier.Creatures.Count == 0)
                return;

            var count = rng.Next(tier.MinCreaturesPerRoom, tier.MaxCreaturesPerRoom + 1);
            var spawnedInRoom = 0;

            for (var i = 0; i < count; i++)
            {
                var tile = room.Tiles[rng.Next(room.Tiles.Count)];
                var location = JitteredTileLocation(area, tile, rng);
                var resref = PickWeightedCreature(tier.Creatures, rng);

                var creature = CreateObject(ObjectType.Creature, resref, location);
                if (!GetIsObjectValid(creature))
                    continue;

                instance.SpawnedObjects.Add(creature);
                spawnedInRoom++;
                result.CreaturesSpawned++;
            }

            if (spawnedInRoom > 0)
                result.RoomsPopulated++;
        }

        private static void PopulateBossRoom(
            uint area,
            LayoutRoom room,
            DungeonDetail detail,
            DungeonTierDetail tier,
            System.Random rng,
            RuntimeAreaInstance instance,
            DungeonPopulationResult result)
        {
            var centerPosition = RoomCenterPosition(area, room);

            if (!string.IsNullOrWhiteSpace(tier.BossResref))
            {
                var bossLocation = Location(area, centerPosition, 0f);
                var boss = CreateObject(ObjectType.Creature, tier.BossResref, bossLocation);

                if (GetIsObjectValid(boss))
                {
                    instance.SpawnedObjects.Add(boss);
                    result.BossSpawned = true;
                    result.BossResref = tier.BossResref;
                }
            }

            if (!string.IsNullOrWhiteSpace(tier.TreasureLootTableId) && Loot.LootTableExists(tier.TreasureLootTableId))
            {
                var treasurePosition = GroundedPosition(area, centerPosition.X + FeatureOffset, centerPosition.Y + FeatureOffset);
                var treasureLocation = Location(area, treasurePosition, 0f);
                var container = CreateObject(ObjectType.Placeable, detail.TreasurePlaceableResref, treasureLocation);

                if (GetIsObjectValid(container))
                {
                    SetName(container, detail.TreasureDisplayName);
                    // Several suitable container blueprints double as scavenge points; strip the
                    // markers so the scavenging system never claims a generated treasure cache.
                    DeleteLocalInt(container, "SCAVENGE_POINT_LEVEL");
                    DeleteLocalString(container, "SCAVENGE_POINT_LOOT_TABLE_NAME");
                    instance.SpawnedObjects.Add(container);

                    result.TreasurePlaced = true;
                    result.TreasureContainer = container;

                    // A placeable's inventory rejects CreateItemOnObject within the script context
                    // that created it (verified on a live server: fills fail same-script even in
                    // boot-time areas, succeed on pre-existing placeables). Fill on a later tick;
                    // the result object's item count updates when the fill completes.
                    Scheduler.Schedule(() =>
                    {
                        result.TreasureItemsSpawned = FillTreasureContainer(container, tier, rng);
                        if (result.TreasureItemsSpawned == 0)
                            Log.Write(LogGroup.Error, $"Treasure fill produced no items for table '{tier.TreasureLootTableId}'.");
                    }, TimeSpan.FromSeconds(1));
                }
            }
        }

        private static void PlaceExit(
            uint area,
            TransitionPoint transition,
            DungeonDetail detail,
            RuntimeAreaInstance instance,
            DungeonPopulationResult result)
        {
            var flat = TileCenter(transition.Tile.X, transition.Tile.Y);
            var exitPosition = GroundedPosition(area, flat.X + FeatureOffset, flat.Y - FeatureOffset);
            var exitLocation = Location(area, exitPosition, 0f);

            var exit = CreateObject(ObjectType.Placeable, detail.ExitPlaceableResref, exitLocation);
            if (!GetIsObjectValid(exit))
                return;

            SetName(exit, detail.ExitDisplayName);
            SetPlotFlag(exit, true);
            SetEventScript(exit, EventScript.Placeable_OnUsed, ScriptName.OnDungeonExitUsed);

            instance.SpawnedObjects.Add(exit);
            result.ExitsPlaced++;
        }

        /// <summary>
        /// Fills a treasure container from the tier's loot table, mirroring Loot.SpawnLoot's item
        /// selection but driven by the population's seeded RNG for deterministic content.
        /// Items spawn at the container's location and are force-acquired: CreateItemOnObject
        /// fails outright against dynamically created placeables (verified on a live server),
        /// while ground creation plus NWNX AcquireItem works.
        /// </summary>
        private static int FillTreasureContainer(uint container, DungeonTierDetail tier, System.Random rng)
        {
            var table = Loot.GetLootTableByName(tier.TreasureLootTableId);
            var containerLocation = GetLocation(container);
            var spawned = 0;

            for (var i = 0; i < tier.TreasureItemCount; i++)
            {
                var item = table.GetRandomItem();
                var quantity = rng.Next(item.MaxQuantity) + 1;

                var created = CreateObject(ObjectType.Item, item.Resref, containerLocation);
                if (!GetIsObjectValid(created))
                {
                    Log.Write(LogGroup.Error, $"Treasure fill: could not create item '{item.Resref}'.", true);
                    continue;
                }

                if (quantity > 1)
                    SetItemStackSize(created, quantity);

                if (!SWLOR.NWN.API.NWNX.ObjectPlugin.AcquireItem(container, created))
                {
                    Log.Write(LogGroup.Error, $"Treasure fill: container refused item '{item.Resref}'.", true);
                    DestroyObject(created);
                    continue;
                }

                item.OnSpawn?.Invoke(created);
                spawned++;
            }

            return spawned;
        }

        /// <summary>
        /// Picks a creature resref from the tier's weighted pool using the population's seeded RNG.
        /// </summary>
        private static string PickWeightedCreature(List<DungeonCreatureEntry> creatures, System.Random rng)
        {
            var totalWeight = creatures.Sum(c => c.Weight);
            if (totalWeight <= 0)
                return creatures[0].Resref;

            var roll = rng.Next(totalWeight);
            var cumulative = 0;

            foreach (var creature in creatures)
            {
                cumulative += creature.Weight;
                if (roll < cumulative)
                    return creature.Resref;
            }

            return creatures[^1].Resref;
        }

        private static Vector3 TileCenter(int tileX, int tileY)
        {
            return new Vector3(tileX * TileSize + TileHalf, tileY * TileSize + TileHalf, 0f);
        }

        private static Vector3 RoomCenterPosition(uint area, LayoutRoom room)
        {
            var flat = TileCenter(room.CenterTile.X, room.CenterTile.Y);
            return GroundedPosition(area, flat.X, flat.Y);
        }

        private static Vector3 GroundedPosition(uint area, float x, float y)
        {
            var probe = Location(area, new Vector3(x, y, 0f), 0f);
            var z = GetGroundHeight(probe);
            return new Vector3(x, y, z);
        }

        private static Location JitteredTileLocation(uint area, (int X, int Y) tile, System.Random rng)
        {
            var flat = TileCenter(tile.X, tile.Y);
            var jitterX = (float)(rng.NextDouble() * (PositionJitter * 2) - PositionJitter);
            var jitterY = (float)(rng.NextDouble() * (PositionJitter * 2) - PositionJitter);
            var facing = (float)(rng.NextDouble() * 360.0);

            var position = GroundedPosition(area, flat.X + jitterX, flat.Y + jitterY);
            return Location(area, position, facing);
        }

        /// <summary>
        /// Handles a player using a dungeon exit placeable: returns them to the location the instance
        /// was entered from. If the instance hasn't had its ExitLocation calibrated (should not happen
        /// for consumers that follow the /genarea pattern), the player is told rather than left stuck.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDungeonExitUsed)]
        public static void UseDungeonExit()
        {
            var user = GetLastUsedBy();
            var placeable = OBJECT_SELF;
            var area = GetArea(placeable);

            if (!RuntimeAreaRegistry.TryGetByArea(area, out var instance) || instance.ExitLocation == null)
            {
                SendMessageToPC(user, "This exit hasn't been calibrated. Inform a DM.");
                return;
            }

            AssignCommand(user, () => ActionJumpToLocation(instance.ExitLocation));
        }
    }
}
