#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
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
        /// The blueprint supplies default appearance and behavior for generic SET slots (Type=0);
        /// a positive slot type replaces its appearance with the matching doortypes.2da row.
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
        /// Adds a weighted decorative placeable to the theme's curated palette for one placement
        /// context. Call repeatedly to build out each <see cref="DecorationContext"/> bucket.
        /// </summary>
        public DungeonDefinitionBuilder Decoration(string resref, int weight, DecorationContext context)
        {
            _activeDungeon.Decorations.Add(new DungeonDecorationEntry
            {
                Resref = resref,
                Weight = weight,
                Context = context
            });

            return this;
        }

        /// <summary>
        /// Sets the theme's base decoration density (target placeables per total area tile at 100%
        /// request density). See <see cref="DungeonDetail.DecorationBaseDensity"/>.
        /// </summary>
        public DungeonDefinitionBuilder DecorationDensity(double baseDensity)
        {
            _activeDungeon.DecorationBaseDensity = baseDensity;
            return this;
        }

        /// <summary>
        /// Requests a NAMED decoration profile of whatever tileset this theme composes with -- see
        /// <see cref="DungeonDetail.DecorationProfile"/>. Falls back to the tileset's standard
        /// palette when the composed tileset never declared the name.
        /// </summary>
        public DungeonDefinitionBuilder DecorationProfile(string profileName)
        {
            _activeDungeon.DecorationProfile = profileName ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Requests a NAMED atmosphere of whatever tileset this theme composes with -- see
        /// <see cref="DungeonDetail.AtmosphereProfile"/>. Falls back to the tileset's standard
        /// atmosphere when the composed tileset never declared the name.
        /// </summary>
        public DungeonDefinitionBuilder AtmosphereProfile(string profileName)
        {
            _activeDungeon.AtmosphereProfile = profileName ?? string.Empty;
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
}
