using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Service.PropertyService;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service.SpawnService
{
    /// <summary>
    /// Where new spawns may and may not appear inside a player city. Hostile creatures keep 15 m from buildings, houses
    /// and other working structures and stay out of the Settled zone around City Hall. Harvest and scavenge nodes keep
    /// the 15 m but may stand in the Settled zone. Every other spawn is never excluded. Pure rules with no NWN calls;
    /// <see cref="SpawnExclusion"/> feeds them the city's data.
    /// </summary>
    public static class SpawnExclusionRules
    {
        /// <summary>
        /// The Hostile faction in Module/fac/repute.fac.json, numbered as <c>CreaturePlugin.GetFaction</c> reports it.
        /// It is the only faction that dislikes the player faction (reputation 0, where the rest sit at 50).
        /// </summary>
        public const int HostileFactionId = 1;

        /// <summary>
        /// How far spawns keep, horizontally in meters, from every qualifying structure.
        /// </summary>
        public const float BuildingBufferMeters = 15.0f;

        /// <summary>
        /// Radius of the Settled zone, in meters, for each city level.
        /// </summary>
        public const float SettledMetersPerLevel = 10.0f;

        /// <summary>
        /// The Settled zone never reaches past this fraction of the area's shorter side.
        /// </summary>
        public const float SettledMaximumSideFraction = 0.25f;

        /// <summary>
        /// How far from its original point a quest-critical spawn may move to leave a zone.
        /// </summary>
        public const float QuestCriticalSearchMeters = 40.0f;

        /// <summary>
        /// The width of one area tile in meters.
        /// </summary>
        public const float TileMeters = 10.0f;

        /// <summary>
        /// The structures spawns keep clear of: city buildings and houses. Decorations, garden plots and every other
        /// structure do not count, so cheap structures cannot be used to clear the land.
        /// The city structures still to come (Market Stall, Corral, Dueling Ring and Traveler's Lodge) are added here
        /// when their StructureType values exist. A new building must be listed here too: the tests fail until it is.
        /// </summary>
        private static readonly HashSet<StructureType> _qualifyingStructures = new()
        {
            StructureType.CityHall,
            StructureType.Bank,
            StructureType.MedicalCenter,
            StructureType.Starport,
            StructureType.Cantina,
            StructureType.LabStyle1,

            StructureType.SmallHouseStyle1,
            StructureType.SmallHouseStyle2,
            StructureType.SmallHouseStyle3,
            StructureType.SmallHouseStyle4,
            StructureType.MediumHouseStyle1,
            StructureType.MediumHouseStyle2,
            StructureType.MediumHouseStyle3,
            StructureType.MediumHouseStyle4,
            StructureType.LargeHouseStyle1,
            StructureType.LargeHouseStyle2,
            StructureType.LargeHouseStyle3,
            StructureType.LargeHouseStyle4,
        };

        /// <summary>
        /// Every structure type that spawns keep clear of.
        /// </summary>
        public static IReadOnlyCollection<StructureType> QualifyingStructures => _qualifyingStructures;

        /// <summary>
        /// Returns true when spawns keep clear of this type of structure.
        /// </summary>
        /// <param name="structureType">The structure type.</param>
        public static bool IsQualifyingStructure(StructureType structureType)
        {
            return _qualifyingStructures.Contains(structureType);
        }

        /// <summary>
        /// The length of an area's shorter side in meters.
        /// </summary>
        /// <param name="widthTiles">The area width in tiles.</param>
        /// <param name="heightTiles">The area height in tiles.</param>
        public static float GetShorterSideMeters(int widthTiles, int heightTiles)
        {
            return Math.Min(widthTiles, heightTiles) * TileMeters;
        }

        /// <summary>
        /// The radius of the Settled zone around City Hall: 10 m per city level, but never more than a quarter of the
        /// area's shorter side.
        /// </summary>
        /// <param name="cityLevel">The city level.</param>
        /// <param name="shorterSideMeters">The length of the area's shorter side in meters.</param>
        public static float GetSettledRadius(int cityLevel, float shorterSideMeters)
        {
            var byLevel = Math.Max(0, cityLevel) * SettledMetersPerLevel;
            var byArea = Math.Max(0f, shorterSideMeters) * SettledMaximumSideFraction;
            return Math.Min(byLevel, byArea);
        }

        /// <summary>
        /// The class of a creature spawn: Hostile when it belongs to the Hostile faction, otherwise Other.
        /// </summary>
        /// <param name="factionId">The creature's faction, as <c>CreaturePlugin.GetFaction</c> reports it.</param>
        public static SpawnExclusionClass GetCreatureClass(int factionId)
        {
            return factionId == HostileFactionId ? SpawnExclusionClass.Hostile : SpawnExclusionClass.Other;
        }

        /// <summary>
        /// The class of a placeable spawn: ResourceNode when it carries a harvesting or scavenging loot table (the
        /// <c>HARVESTING_LOOT_TABLE</c> and <c>SCAVENGE_POINT_LOOT_TABLE_NAME</c> locals), otherwise Other.
        /// Fishing points, terminals and the like are not resource nodes.
        /// </summary>
        /// <param name="carriesHarvestingLootTable">True when the placeable has a harvesting loot table.</param>
        /// <param name="carriesScavengeLootTable">True when the placeable has a scavenging loot table.</param>
        public static SpawnExclusionClass GetPlaceableClass(bool carriesHarvestingLootTable, bool carriesScavengeLootTable)
        {
            return carriesHarvestingLootTable || carriesScavengeLootTable
                ? SpawnExclusionClass.ResourceNode
                : SpawnExclusionClass.Other;
        }

        /// <summary>
        /// Returns true when a spawn of this class may appear at the position. A position exactly on the edge of a
        /// zone is legal. Height is ignored.
        /// Hostile: at least 15 m from every qualifying structure and outside the Settled zone.
        /// ResourceNode: at least 15 m from every qualifying structure; the Settled zone does not apply.
        /// Other: always legal.
        /// </summary>
        /// <param name="zone">The city's zone.</param>
        /// <param name="spawnClass">The spawn's class.</param>
        /// <param name="position">The position to test.</param>
        public static bool IsLegal(SpawnExclusionZone zone, SpawnExclusionClass spawnClass, Vector3 position)
        {
            if (spawnClass == SpawnExclusionClass.Other)
                return true;

            var point = new Vector2(position.X, position.Y);

            if (spawnClass == SpawnExclusionClass.Hostile &&
                zone.SettledCenter.HasValue &&
                Vector2.DistanceSquared(point, zone.SettledCenter.Value) < zone.SettledRadius * zone.SettledRadius)
            {
                return false;
            }

            const float bufferSquared = BuildingBufferMeters * BuildingBufferMeters;
            foreach (var structure in zone.StructurePositions)
            {
                if (Vector2.DistanceSquared(point, structure) < bufferSquared)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Draws candidates and returns the first legal one for the spawn's class. When
        /// <paramref name="occupiedPositions"/> is given (random placeables), the existing <see cref="SpawnSpacing"/>
        /// applies among the legal candidates: the first with room wins, and when none has room the most open legal
        /// candidate is used. Returns false when no candidate within <paramref name="attempts"/> is legal, so the
        /// caller skips the spawn.
        /// </summary>
        /// <param name="zone">The city's zone.</param>
        /// <param name="spawnClass">The spawn's class.</param>
        /// <param name="nextCandidate">Supplies a new random candidate position on each call.</param>
        /// <param name="attempts">Maximum number of candidates to draw.</param>
        /// <param name="position">The chosen position when this returns true.</param>
        /// <param name="occupiedPositions">Positions of placeable spawns already in the area, or null for creatures.</param>
        public static bool TryPickLegalPosition(
            SpawnExclusionZone zone,
            SpawnExclusionClass spawnClass,
            Func<Vector3> nextCandidate,
            int attempts,
            out Vector3 position,
            IReadOnlyCollection<Vector3> occupiedPositions = null)
        {
            var legal = new List<Vector3>();

            for (var attempt = 0; attempt < Math.Max(1, attempts); attempt++)
            {
                var candidate = nextCandidate();
                if (!IsLegal(zone, spawnClass, candidate))
                    continue;

                if (occupiedPositions == null || SpawnSpacing.HasRoom(candidate, occupiedPositions))
                {
                    position = candidate;
                    return true;
                }

                legal.Add(candidate);
            }

            if (legal.Count == 0)
            {
                position = Vector3.Zero;
                return false;
            }

            var remaining = new Queue<Vector3>(legal);
            position = SpawnSpacing.PickSpacedPosition(remaining.Dequeue, occupiedPositions, maximumAttempts: legal.Count);
            return true;
        }

        /// <summary>
        /// Decides where a spawn with a fixed point stands. A spawn that is legal at its point keeps it. Otherwise
        /// it is skipped (false), unless a quest depends on it: then it moves as <see cref="GetQuestCriticalPosition"/>
        /// says and the result is true.
        /// </summary>
        /// <param name="zone">The city's zone.</param>
        /// <param name="spawnClass">The spawn's class.</param>
        /// <param name="original">The spawn's own point.</param>
        /// <param name="isQuestCritical">True when a quest depends on this spawn.</param>
        /// <param name="walkablePoints">Supplies the walkable points in the area. Only called when a quest-critical spawn must move.</param>
        /// <param name="position">Where to spawn when this returns true.</param>
        public static bool TryResolveFixedPosition(
            SpawnExclusionZone zone,
            SpawnExclusionClass spawnClass,
            Vector3 original,
            bool isQuestCritical,
            Func<IEnumerable<Vector3>> walkablePoints,
            out Vector3 position)
        {
            position = original;
            if (IsLegal(zone, spawnClass, original))
                return true;

            if (!isQuestCritical)
                return false;

            position = GetQuestCriticalPosition(zone, spawnClass, original, walkablePoints());
            return true;
        }

        /// <summary>
        /// Where a quest-critical spawn stands. A spawn whose point is legal stays there. One that is not moves to the
        /// nearest walkable point that is legal for its class and within <paramref name="maximumDistance"/>
        /// (horizontal), or keeps its original point when there is none, so the quest never breaks.
        /// </summary>
        /// <param name="zone">The city's zone.</param>
        /// <param name="spawnClass">The spawn's class.</param>
        /// <param name="original">The spawn's own point.</param>
        /// <param name="walkablePoints">Walkable points in the area, the same baked points random spawns use.</param>
        /// <param name="maximumDistance">How far the spawn may move.</param>
        public static Vector3 GetQuestCriticalPosition(
            SpawnExclusionZone zone,
            SpawnExclusionClass spawnClass,
            Vector3 original,
            IEnumerable<Vector3> walkablePoints,
            float maximumDistance = QuestCriticalSearchMeters)
        {
            if (IsLegal(zone, spawnClass, original))
                return original;

            var origin = new Vector2(original.X, original.Y);
            var nearest = original;
            var nearestDistanceSquared = maximumDistance * maximumDistance;
            var found = false;

            foreach (var point in walkablePoints)
            {
                var distanceSquared = Vector2.DistanceSquared(origin, new Vector2(point.X, point.Y));
                if (distanceSquared > nearestDistanceSquared)
                    continue;

                if (found && distanceSquared >= nearestDistanceSquared)
                    continue;

                if (!IsLegal(zone, spawnClass, point))
                    continue;

                nearest = point;
                nearestDistanceSquared = distanceSquared;
                found = true;
            }

            return found ? nearest : original;
        }
    }
}
