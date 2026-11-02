using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum.Area;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Keeps new spawns off a player city's buildings. Random spawns avoid the buffer around city buildings and houses
    /// and the Settled zone around City Hall, and fixed spawns inside either are skipped unless a quest depends on them.
    /// A city's zone is cached and rebuilt on first use after its structures or level change.
    /// The rules live in <see cref="SpawnExclusionRules"/>.
    /// </summary>
    public static class SpawnExclusion
    {
        private static readonly Dictionary<string, SpawnExclusionZone> _zonesByCityId = new();

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
        /// Draws a random position for a new spawn that is legal for the area. In an area that is not a player city every
        /// position is legal. For placeables, pass the positions of the placeable spawns already in the area so the
        /// existing spacing applies among the legal candidates.
        /// </summary>
        /// <param name="area">The area being spawned into.</param>
        /// <param name="nextCandidate">Supplies a new random candidate position on each call.</param>
        /// <param name="attempts">Maximum number of candidates to draw.</param>
        /// <param name="position">The chosen position when this returns true.</param>
        /// <param name="occupiedPlaceablePositions">Positions of placeable spawns already in the area, or null for creatures.</param>
        /// <returns>False when no candidate is legal, and the caller skips the spawn until its next respawn.</returns>
        public static bool TryPickLegalPosition(
            uint area,
            Func<Vector3> nextCandidate,
            int attempts,
            out Vector3 position,
            IReadOnlyCollection<Vector3> occupiedPlaceablePositions = null)
        {
            return SpawnExclusionRules.TryPickLegalPosition(
                GetZone(area),
                nextCandidate,
                attempts,
                out position,
                occupiedPlaceablePositions);
        }

        /// <summary>
        /// Decides where a spawn with a fixed point stands. Outside the zones it keeps its point. Inside them it is
        /// skipped, unless a quest depends on it: then it moves to the nearest walkable legal spot within 40 m, or keeps
        /// its point when there is none.
        /// </summary>
        /// <param name="area">The area being spawned into.</param>
        /// <param name="original">The spawn's own point.</param>
        /// <param name="isQuestCritical">True when a quest depends on this spawn.</param>
        /// <param name="position">Where to spawn when this returns true.</param>
        /// <returns>False when the spawn is skipped.</returns>
        public static bool TryResolveFixedPosition(uint area, Vector3 original, bool isQuestCritical, out Vector3 position)
        {
            var zone = GetZone(area);
            if (SpawnExclusionRules.IsLegal(zone, original))
            {
                position = original;
                return true;
            }

            if (!isQuestCritical)
            {
                position = original;
                return false;
            }

            position = SpawnExclusionRules.GetQuestCriticalPosition(zone, original, Walkmesh.GetWalkablePoints(area));
            return true;
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
