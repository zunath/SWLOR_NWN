using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// A single weighted creature choice within a dungeon tier's ambient spawn pool.
    /// </summary>
    public class DungeonCreatureEntry
    {
        public string Resref { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
    }

    /// <summary>
    /// Tier-specific content for one difficulty tier of a dungeon theme: ambient spawn pool,
    /// per-room spawn counts, boss, and treasure. Consumed by DungeonContentPlacer.Populate.
    /// </summary>
    public class DungeonTierDetail
    {
        public int Tier { get; set; }
        public List<DungeonCreatureEntry> Creatures { get; set; } = new();
        public int MinCreaturesPerRoom { get; set; } = 1;
        public int MaxCreaturesPerRoom { get; set; } = 2;
        public string BossResref { get; set; } = string.Empty;
        public string TreasureLootTableId { get; set; } = string.Empty;
        public int TreasureItemCount { get; set; } = 1;
        /// <summary>Free-text balance note for programmer/DM reference. Not used by the system.</summary>
        public string LevelNote { get; set; } = string.Empty;
    }

    /// <summary>
    /// Per-tile lighting written into every generated tile of a theme. Values are tile light
    /// color indices (see TILE_MAIN_LIGHT_COLOR_* / TILE_SOURCE_LIGHT_COLOR_*); defaults match
    /// the hand-built tdt01 cave areas.
    /// </summary>
    public class DungeonTileLighting
    {
        public int MainLight1 { get; set; }
        public int MainLight2 { get; set; }
        public int SourceLight1 { get; set; } = 8;
        public int SourceLight2 { get; set; } = 8;
    }

    /// <summary>
    /// Everything that is genuinely bound to a tileset: the tileset itself, the placeholder
    /// area cloned for instances, tile lighting, and what the tileset calls its accent terrain
    /// (water pools, pit channels). Layout shapes and content packages compose with any profile.
    /// </summary>
    public class DungeonTilesetProfile
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string TilesetResref { get; set; } = string.Empty;
        public string PlaceholderResref { get; set; } = string.Empty;
        public DungeonTileLighting Lighting { get; set; } = new();

        /// <summary>
        /// Terrain name this tileset uses for accent patches (e.g. "Water" on tdt01, "Pit" on
        /// tds01). Empty = the tileset has no verified accent coverage; compositions skip accents.
        /// </summary>
        public string AccentTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Narrowest corridor/door-gap width (in corners) this tileset can path through. Some
        /// tilesets (zsf01) give every partially-open corner combo a movement-restricted pathnode,
        /// so 1-wide openings fail the engine's path check; 2-wide openings put a fully-open
        /// (pathnode A) tile in the middle. Compositions raise CorridorWidth to at least this.
        /// </summary>
        public int MinimumOpeningWidth { get; set; } = 1;
    }

    /// <summary>
    /// A named layout shape: style plus tuning knobs, independent of any tileset. The template's
    /// AccentTerrain stays empty — a nonzero AccentDensity expresses intent, and the actual
    /// terrain name comes from the tileset profile at composition time.
    /// </summary>
    public class DungeonLayoutProfile
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public MacroLayoutParameters Template { get; set; } = new();
    }

    /// <summary>
    /// A content package plus its default composition: per-tier creatures/boss/treasure, exit and
    /// treasure placeables, and the default tileset/layout profile keys. Any tileset or layout
    /// profile can be substituted at request time — nothing here is tileset-bound.
    /// Definitions are discovered via reflection over IDungeonListDefinition (see DungeonContentPlacer),
    /// mirroring ISpawnListDefinition/ILootTableDefinition/IAbilityListDefinition in this codebase.
    /// </summary>
    public class DungeonDetail
    {
        public string ThemeKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Default tileset profile key; overridable per request.</summary>
        public string TilesetProfileKey { get; set; } = string.Empty;
        /// <summary>Default layout profile key; overridable per request.</summary>
        public string LayoutProfileKey { get; set; } = string.Empty;

        public int MinSize { get; set; } = 8;
        public int MaxSize { get; set; } = 32;

        /// <summary>
        /// Exit placeable spawned in the Entrance room. Must be a useable, non-static blueprint
        /// with a real (non-blank) appearance row — several "door"/"portal" blueprints in the
        /// module are invisible objects or have blank appearance rows; verify in placeables.2da.
        /// </summary>
        public string ExitPlaceableResref { get; set; } = "_mdrn_placedoord";
        public string ExitDisplayName { get; set; } = "Exit";

        /// <summary>
        /// Door blueprint spawned for Door-style transitions (doorway tiles embedded in room walls).
        /// Every tile door slot the generator uses is generic (Type=0), so any generic-door utd fits.
        /// </summary>
        public string ExitDoorResref { get; set; } = "_mdrn_dt_wood";

        /// <summary>Treasure container spawned in the Boss room. Must have HasInventory=1 and a real appearance.</summary>
        public string TreasurePlaceableResref { get; set; } = "structure_rubble";
        public string TreasureDisplayName { get; set; } = "Treasure Cache";

        public Dictionary<int, DungeonTierDetail> Tiers { get; set; } = new();
    }

    /// <summary>
    /// A resolved (content, tileset, layout) triple ready to drive a generation request.
    /// </summary>
    public class DungeonComposition
    {
        public DungeonDetail Content { get; set; }
        public DungeonTilesetProfile Tileset { get; set; }
        public DungeonLayoutProfile Layout { get; set; }

        /// <summary>
        /// Clones the layout template and stamps the tileset's accent terrain name when the
        /// layout wants accents and the tileset supports them; otherwise accents are disabled.
        /// </summary>
        public MacroLayoutParameters BuildLayoutParameters()
        {
            var parameters = Layout.Template.Clone();
            parameters.AccentTerrain =
                parameters.AccentDensity > 0 && !string.IsNullOrEmpty(Tileset.AccentTerrain)
                    ? Tileset.AccentTerrain
                    : string.Empty;
            if (parameters.AccentTerrain.Length == 0)
                parameters.AccentDensity = 0;
            parameters.CorridorWidth = Math.Max(parameters.CorridorWidth, Tileset.MinimumOpeningWidth);
            return parameters;
        }
    }

    /// <summary>
    /// Fluent builder for dungeon theme definitions, matching the SpawnTableBuilder/LootTableBuilder/
    /// PropertyLayoutBuilder convention: Create() starts a new active entry, chained calls mutate it,
    /// Build() returns the completed dictionary.
    /// </summary>
    public class DungeonDefinitionBuilder
    {
        private readonly Dictionary<string, DungeonDetail> _dungeons = new();
        private DungeonDetail _activeDungeon;
        private DungeonTierDetail _activeTier;

        /// <summary>
        /// Creates a new dungeon theme definition with the specified theme key.
        /// </summary>
        /// <param name="themeKey">Unique theme key, e.g. "minecave".</param>
        /// <param name="displayName">Human-readable name for programmer/DM reference.</param>
        public DungeonDefinitionBuilder Create(string themeKey, string displayName)
        {
            _activeDungeon = new DungeonDetail
            {
                ThemeKey = themeKey,
                DisplayName = displayName
            };
            _dungeons[themeKey] = _activeDungeon;
            _activeTier = null;

            return this;
        }

        /// <summary>
        /// Sets the default tileset profile this content package composes with. Overridable per request.
        /// </summary>
        public DungeonDefinitionBuilder TilesetProfile(string tilesetProfileKey)
        {
            _activeDungeon.TilesetProfileKey = tilesetProfileKey;
            return this;
        }

        /// <summary>
        /// Sets the default layout profile this content package composes with. Overridable per request.
        /// </summary>
        public DungeonDefinitionBuilder LayoutProfile(string layoutProfileKey)
        {
            _activeDungeon.LayoutProfileKey = layoutProfileKey;
            return this;
        }

        /// <summary>
        /// Sets the allowed width/height range for generated instances of this theme.
        /// </summary>
        public DungeonDefinitionBuilder SizeRange(int minSize, int maxSize)
        {
            _activeDungeon.MinSize = minSize;
            _activeDungeon.MaxSize = maxSize;
            return this;
        }

        /// <summary>
        /// Sets the exit placeable spawned in the Entrance room and its display name.
        /// </summary>
        public DungeonDefinitionBuilder ExitPlaceable(string resref, string displayName)
        {
            _activeDungeon.ExitPlaceableResref = resref;
            _activeDungeon.ExitDisplayName = displayName;
            return this;
        }

        /// <summary>
        /// Sets the door blueprint used when a transition is realized as a real tileset door.
        /// Must be a generic-door utd (tile door slots are Type=0 generic).
        /// </summary>
        public DungeonDefinitionBuilder ExitDoor(string resref)
        {
            _activeDungeon.ExitDoorResref = resref;
            return this;
        }

        /// <summary>
        /// Sets the treasure container spawned in the Boss room and its display name.
        /// </summary>
        public DungeonDefinitionBuilder TreasurePlaceable(string resref, string displayName)
        {
            _activeDungeon.TreasurePlaceableResref = resref;
            _activeDungeon.TreasureDisplayName = displayName;
            return this;
        }

        /// <summary>
        /// Starts a new tier definition. Tiers must be declared in contiguous order starting at 1
        /// (enforced by DungeonDefinitionTests, not at runtime).
        /// </summary>
        public DungeonDefinitionBuilder Tier(int tier)
        {
            _activeTier = new DungeonTierDetail
            {
                Tier = tier
            };
            _activeDungeon.Tiers[tier] = _activeTier;

            return this;
        }

        /// <summary>
        /// Adds a weighted creature choice to the active tier's ambient spawn pool.
        /// </summary>
        public DungeonDefinitionBuilder AddCreature(string resref, int weight = 10)
        {
            _activeTier.Creatures.Add(new DungeonCreatureEntry
            {
                Resref = resref,
                Weight = weight
            });

            return this;
        }

        /// <summary>
        /// Sets the min/max number of ambient creatures spawned per Standard room for the active tier.
        /// </summary>
        public DungeonDefinitionBuilder CreaturesPerRoom(int min, int max)
        {
            _activeTier.MinCreaturesPerRoom = min;
            _activeTier.MaxCreaturesPerRoom = max;
            return this;
        }

        /// <summary>
        /// Sets the boss creature resref spawned once in the Boss room for the active tier.
        /// </summary>
        public DungeonDefinitionBuilder Boss(string bossResref)
        {
            _activeTier.BossResref = bossResref;
            return this;
        }

        /// <summary>
        /// Sets the loot table and item count used to fill the Boss room's treasure container for the active tier.
        /// </summary>
        public DungeonDefinitionBuilder Treasure(string lootTableId, int itemCount)
        {
            _activeTier.TreasureLootTableId = lootTableId;
            _activeTier.TreasureItemCount = itemCount;
            return this;
        }

        /// <summary>
        /// Attaches a free-text balance note to the active tier for programmer/DM reference.
        /// </summary>
        public DungeonDefinitionBuilder LevelNote(string note)
        {
            _activeTier.LevelNote = note;
            return this;
        }

        /// <summary>
        /// Builds a dictionary of dungeon theme definitions, keyed by theme key.
        /// </summary>
        public Dictionary<string, DungeonDetail> Build()
        {
            return _dungeons;
        }
    }

    /// <summary>Fluent builder for tileset profiles, same conventions as DungeonDefinitionBuilder.</summary>
    public class DungeonTilesetProfileBuilder
    {
        private readonly Dictionary<string, DungeonTilesetProfile> _profiles = new();
        private DungeonTilesetProfile _active;

        public DungeonTilesetProfileBuilder Create(string key, string displayName)
        {
            _active = new DungeonTilesetProfile
            {
                Key = key,
                DisplayName = displayName
            };
            _profiles[key] = _active;
            return this;
        }

        public DungeonTilesetProfileBuilder Tileset(string tilesetResref)
        {
            _active.TilesetResref = tilesetResref;
            return this;
        }

        public DungeonTilesetProfileBuilder Placeholder(string placeholderResref)
        {
            _active.PlaceholderResref = placeholderResref;
            return this;
        }

        public DungeonTilesetProfileBuilder TileLighting(int mainLight1, int mainLight2, int sourceLight1, int sourceLight2)
        {
            _active.Lighting = new DungeonTileLighting
            {
                MainLight1 = mainLight1,
                MainLight2 = mainLight2,
                SourceLight1 = sourceLight1,
                SourceLight2 = sourceLight2
            };
            return this;
        }

        /// <summary>
        /// Declares the terrain name this tileset uses for accent patches. Only set this after
        /// verifying full (open, accent) corner coverage among resolver-usable tiles.
        /// </summary>
        public DungeonTilesetProfileBuilder AccentTerrain(string terrainName)
        {
            _active.AccentTerrain = terrainName;
            return this;
        }

        /// <summary>
        /// Declares the narrowest opening width (in corners) this tileset can path through. Set to
        /// 2 for tilesets whose partially-open corner combos all carry movement-restricted pathnodes.
        /// </summary>
        public DungeonTilesetProfileBuilder MinimumOpeningWidth(int width)
        {
            _active.MinimumOpeningWidth = width;
            return this;
        }

        public Dictionary<string, DungeonTilesetProfile> Build()
        {
            return _profiles;
        }
    }

    /// <summary>Fluent builder for layout profiles, same conventions as DungeonDefinitionBuilder.</summary>
    public class DungeonLayoutProfileBuilder
    {
        private readonly Dictionary<string, DungeonLayoutProfile> _profiles = new();
        private DungeonLayoutProfile _active;

        public DungeonLayoutProfileBuilder Create(string key, string displayName)
        {
            _active = new DungeonLayoutProfile
            {
                Key = key,
                DisplayName = displayName
            };
            _profiles[key] = _active;
            return this;
        }

        /// <summary>
        /// Configures the layout style and tuning knobs. Width/height/terrain labels are stamped
        /// per-request; leave AccentTerrain empty — set AccentDensity to express accent intent and
        /// the composed tileset profile supplies the terrain name.
        /// </summary>
        public DungeonLayoutProfileBuilder Configure(Action<MacroLayoutParameters> configure)
        {
            configure(_active.Template);
            return this;
        }

        public Dictionary<string, DungeonLayoutProfile> Build()
        {
            return _profiles;
        }
    }
}
