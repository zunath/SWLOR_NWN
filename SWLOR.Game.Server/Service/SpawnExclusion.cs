using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Keeps new hostile creatures and harvest or scavenge nodes off a player city's buildings. Both keep the buffer
    /// around city buildings and houses; hostiles also stay out of the Settled zone around City Hall. Every other
    /// spawn (friendly NPCs, terminals, fishing points) is left exactly as it is without a city.
    /// Fixed spawns inside a zone are skipped unless a quest depends on them. A city's zone is cached and rebuilt on
    /// first use after its structures or level change. The rules live in <see cref="SpawnExclusionRules"/>.
    /// </summary>
    public static class SpawnExclusion
    {
        /// <summary>
        /// Local string on a harvest node's blueprint naming its harvesting loot table.
        /// </summary>
        public const string HarvestingLootTableVariable = "HARVESTING_LOOT_TABLE";

        /// <summary>
        /// Local string on a scavenge site's blueprint naming its scavenging loot table.
        /// </summary>
        public const string ScavengeLootTableVariable = "SCAVENGE_POINT_LOOT_TABLE_NAME";

        private static readonly Dictionary<string, SpawnExclusionZone> _zonesByCityId = new();
        private static readonly Dictionary<(ObjectType Type, string Resref), SpawnExclusionClass> _classesByResref = new();

        /// <summary>
        /// Drops a city's cached zone so the next spawn in its area rebuilds it. Call when a qualifying structure is
        /// placed, moved or removed, when the city level changes, and when the city is deleted.
        /// </summary>
        /// <param name="cityPropertyId">The city's property Id. Anything else is ignored.</param>
        public static void InvalidateCity(string cityPropertyId)
        {
            if (string.IsNullOrWhiteSpace(cityPropertyId))
                return;

            _zonesByCityId.Remove(cityPropertyId);
        }

        /// <summary>
        /// Returns true when the area is a player city, so its spawns follow the exclusion rules. In any other area
        /// nothing is excluded.
        /// </summary>
        /// <param name="area">The area being spawned into.</param>
        public static bool HasZone(uint area)
        {
            return GetZone(area) != SpawnExclusionZone.None;
        }

        /// <summary>
        /// Classifies a spawned or hand-placed object: a creature in the Hostile faction is Hostile, a placeable that
        /// carries a harvesting or scavenging loot table is a ResourceNode, and everything else is Other.
        /// </summary>
        /// <param name="spawn">The object to classify. It must exist.</param>
        public static SpawnExclusionClass Classify(uint spawn)
        {
            var type = GetObjectType(spawn);

            if (type == ObjectType.Creature)
                return SpawnExclusionRules.GetCreatureClass(CreaturePlugin.GetFaction(spawn));

            if (type == ObjectType.Placeable)
            {
                return SpawnExclusionRules.GetPlaceableClass(
                    !string.IsNullOrWhiteSpace(GetLocalString(spawn, HarvestingLootTableVariable)),
                    !string.IsNullOrWhiteSpace(GetLocalString(spawn, ScavengeLootTableVariable)));
            }

            return SpawnExclusionClass.Other;
        }

        /// <summary>
        /// Looks up the class remembered for a spawn table blueprint.
        /// </summary>
        /// <param name="type">The object type the spawn table creates.</param>
        /// <param name="resref">The blueprint resref.</param>
        /// <param name="spawnClass">The remembered class when this returns true.</param>
        /// <returns>False when the blueprint has not been classified yet: a blueprint only tells its class once an
        /// object exists, so the first spawn of it in a city creates one and passes it to <see cref="Remember"/>.</returns>
        public static bool TryGetClass(ObjectType type, string resref, out SpawnExclusionClass spawnClass)
        {
            return _classesByResref.TryGetValue((type, resref), out spawnClass);
        }

        /// <summary>
        /// Classifies a freshly created spawn table object and remembers the class for its blueprint.
        /// </summary>
        /// <param name="type">The object type the spawn table creates.</param>
        /// <param name="resref">The blueprint resref.</param>
        /// <param name="spawn">The object just created from the blueprint.</param>
        public static SpawnExclusionClass Remember(ObjectType type, string resref, uint spawn)
        {
            var spawnClass = Classify(spawn);
            _classesByResref[(type, resref)] = spawnClass;
            return spawnClass;
        }

        /// <summary>
        /// Returns true when a spawn of this class may stand at the position. Always true outside a player city.
        /// </summary>
        /// <param name="area">The area being spawned into.</param>
        /// <param name="spawnClass">The spawn's class.</param>
        /// <param name="position">The position to test.</param>
        public static bool IsLegal(uint area, SpawnExclusionClass spawnClass, Vector3 position)
        {
            return SpawnExclusionRules.IsLegal(GetZone(area), spawnClass, position);
        }

        /// <summary>
        /// Draws a random position for a new spawn that is legal for its class. In an area that is not a player city,
        /// and for Other spawns, every position is legal. For placeables, pass the positions of the placeable spawns
        /// already in the area so the existing spacing applies among the legal candidates.
        /// </summary>
        /// <param name="area">The area being spawned into.</param>
        /// <param name="spawnClass">The spawn's class.</param>
        /// <param name="nextCandidate">Supplies a new random candidate position on each call.</param>
        /// <param name="attempts">Maximum number of candidates to draw.</param>
        /// <param name="position">The chosen position when this returns true.</param>
        /// <param name="occupiedPlaceablePositions">Positions of placeable spawns already in the area, or null for creatures.</param>
        /// <returns>False when no candidate is legal, and the caller skips the spawn until its next respawn.</returns>
        public static bool TryPickLegalPosition(
            uint area,
            SpawnExclusionClass spawnClass,
            Func<Vector3> nextCandidate,
            int attempts,
            out Vector3 position,
            IReadOnlyCollection<Vector3> occupiedPlaceablePositions = null)
        {
            return SpawnExclusionRules.TryPickLegalPosition(
                GetZone(area),
                spawnClass,
                nextCandidate,
                attempts,
                out position,
                occupiedPlaceablePositions);
        }

        /// <summary>
        /// Decides where a spawn with a fixed point stands. Outside the zones it keeps its point. A Hostile or
        /// ResourceNode spawn inside them is skipped, unless a quest depends on it: then it moves to the nearest
        /// walkable legal spot within 40 m, or keeps its point when there is none. Other spawns never move.
        /// </summary>
        /// <param name="area">The area being spawned into.</param>
        /// <param name="spawnClass">The spawn's class.</param>
        /// <param name="original">The spawn's own point.</param>
        /// <param name="isQuestCritical">True when a quest depends on this spawn.</param>
        /// <param name="position">Where to spawn when this returns true.</param>
        /// <returns>False when the spawn is skipped.</returns>
        public static bool TryResolveFixedPosition(
            uint area,
            SpawnExclusionClass spawnClass,
            Vector3 original,
            bool isQuestCritical,
            out Vector3 position)
        {
            return SpawnExclusionRules.TryResolveFixedPosition(
                GetZone(area),
                spawnClass,
                original,
                isQuestCritical,
                () => Walkmesh.GetWalkablePoints(area),
                out position);
        }

        private static SpawnExclusionZone GetZone(uint area)
        {
            var cityId = Property.GetPropertyId(area);
            if (string.IsNullOrWhiteSpace(cityId))
                return SpawnExclusionZone.None;

            if (!_zonesByCityId.TryGetValue(cityId, out var zone))
            {
                zone = BuildZone(cityId, area);
                _zonesByCityId[cityId] = zone;
            }

            return zone;
        }

        private static SpawnExclusionZone BuildZone(string cityId, uint area)
        {
            var city = DB.Get<WorldProperty>(cityId);

            // Stations have no hostile spawns, and anything but a city has no buildings to keep clear of.
            if (city == null || city.PropertyType != PropertyType.City || Station.IsStation(city))
                return SpawnExclusionZone.None;

            var structureQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), cityId, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure)
                .AddFieldSearch(nameof(WorldProperty.StructureType), SpawnExclusionRules.QualifyingStructures.Select(s => (int)s))
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            var structureCount = (int)DB.SearchCount(structureQuery);
            var structures = structureCount > 0
                ? DB.Search(structureQuery.AddPaging(structureCount, 0)).ToList()
                : new List<WorldProperty>();

            var positions = new List<Vector2>();
            Vector2? cityHall = null;
            foreach (var structure in structures)
            {
                if (!structure.Positions.TryGetValue(PropertyLocationType.StaticPosition, out var location))
                    continue;

                var position = new Vector2(location.X, location.Y);
                positions.Add(position);

                if (structure.StructureType == StructureType.CityHall)
                    cityHall = position;
            }

            var level = city.Upgrades.TryGetValue(PropertyUpgradeType.CityLevel, out var cityLevel) ? cityLevel : 1;
            var shorterSide = SpawnExclusionRules.GetShorterSideMeters(
                GetAreaSize(Dimension.Width, area),
                GetAreaSize(Dimension.Height, area));

            return new SpawnExclusionZone(
                positions,
                cityHall,
                SpawnExclusionRules.GetSettledRadius(level, shorterSide));
        }
    }
}
