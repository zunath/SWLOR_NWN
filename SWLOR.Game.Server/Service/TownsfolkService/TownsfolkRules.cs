using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service.TownsfolkService
{
    /// <summary>
    /// The rules for a city's street life (Player City Revamp, "Ambient townsfolk"): how many wanderers, guards, and critters
    /// a city has at each level and under each Manage City switch, what to add or remove when that changes, and where they
    /// walk. Pure, with no NWN calls; <see cref="Townsfolk"/> feeds it the city's data and makes the creatures.
    /// </summary>
    public static class TownsfolkRules
    {
        /// <summary>The first city level (Village) that gets wandering townsfolk.</summary>
        public const int MinimumTownsfolkLevel = 2;

        /// <summary>Wandering townsfolk for each city level, from <see cref="MinimumTownsfolkLevel"/>.</summary>
        public const int TownsfolkPerLevel = 2;

        /// <summary>The first city level (Township) that gets guards.</summary>
        public const int MinimumGuardLevel = 3;

        /// <summary>Guards at <see cref="MinimumGuardLevel"/>.</summary>
        public const int GuardsAtMinimumLevel = 2;

        /// <summary>Guards added for each city level above <see cref="MinimumGuardLevel"/>.</summary>
        public const int GuardsPerLevelAboveMinimum = 1;

        /// <summary>The first city level that gets street critters.</summary>
        public const int MinimumCritterLevel = 2;

        /// <summary>Street critters for each city level, from <see cref="MinimumCritterLevel"/>.</summary>
        public const int CrittersPerLevel = 1;

        /// <summary>The highest city level. A level read from older or damaged data never counts above it.</summary>
        public const int MaximumCityLevel = 5;

        /// <summary>
        /// The faction every street creature belongs to: Neutral (id 6 in Module/fac/repute.fac.json). Hostile creatures
        /// treat it as neutral (reputation 50), so nothing in the Hostile faction hunts them, and it is not the Hostile faction
        /// (<see cref="SpawnService.SpawnExclusionRules.HostileFactionId"/>), so they never count as hostile spawns.
        /// </summary>
        public const int NeutralFactionId = 6;

        /// <summary>
        /// The city power that gates the two switches. Switching the streets on or off is a routine Manage City setting like
        /// taxes, so it needs the same power: the mayor, an Acting Mayor, and a holder of Edit Taxes.
        /// </summary>
        public const CityPower SwitchPower = CityPower.ChangeTaxes;

        /// <summary>Minutes an area stays empty before its street life is removed. The same wait the Spawn service uses.</summary>
        public const int EmptyAreaDespawnMinutes = 20;

        /// <summary>The most creatures one city makes in one 6-second heartbeat, so a crowd arrives over a few beats.</summary>
        public const int MaximumSpawnsPerBeat = 4;

        /// <summary>A new creature appears at an entrance at least this far from every player, when one is.</summary>
        public const float MinimumSpawnDistanceFromPlayersMeters = 15.0f;

        /// <summary>A walking creature that moves less than this in a beat has not moved.</summary>
        public const float StuckDistanceMeters = 0.5f;

        /// <summary>Beats a walking creature may stand still before it is given a new destination.</summary>
        public const int StuckBeatLimit = 3;

        /// <summary>Seconds between full refreshes of a city's level, switches, and entrances while players are in it.</summary>
        public const int RefreshSeconds = 600;

        private const float CritterNearestFraction = 0.35f;
        private const int CritterFractionSteps = 51;

        private static readonly HashSet<StructureType> _entranceStructures = new()
        {
            StructureType.CityHall,
            StructureType.StationCore,
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

        #region Counts

        /// <summary>
        /// Structures that have an entrance door, which is where street life walks: the city's buildings and houses and a
        /// station's core. Working structures with no door, such as plots and the Dueling Ring, are not walked to.
        /// </summary>
        public static IReadOnlyCollection<StructureType> EntranceStructures => _entranceStructures;

        /// <summary>Whether a structure has an entrance door street life walks to.</summary>
        public static bool IsEntranceStructure(StructureType structureType)
        {
            return _entranceStructures.Contains(structureType);
        }

        /// <summary>Wandering townsfolk at a city level: 2 per level from level 2, none below.</summary>
        public static int GetWandererCount(int cityLevel)
        {
            var level = ClampLevel(cityLevel);
            return level < MinimumTownsfolkLevel ? 0 : level * TownsfolkPerLevel;
        }

        /// <summary>Guards at a city level: 2 at level 3 and 1 more for each level above, none below level 3.</summary>
        public static int GetGuardCount(int cityLevel)
        {
            var level = ClampLevel(cityLevel);
            return level < MinimumGuardLevel
                ? 0
                : GuardsAtMinimumLevel + (level - MinimumGuardLevel) * GuardsPerLevelAboveMinimum;
        }

        /// <summary>Street critters at a city level: 1 per level from level 2, none below.</summary>
        public static int GetCritterCount(int cityLevel)
        {
            var level = ClampLevel(cityLevel);
            return level < MinimumCritterLevel ? 0 : level * CrittersPerLevel;
        }

        /// <summary>Whether a city gets critters at all. Stations do not: the Plan lists only townsfolk for a station deck.</summary>
        public static bool HasCritters(bool isStation)
        {
            return !isStation;
        }

        /// <summary>
        /// How many of each group a city should have. Nothing when there is no entrance to walk to. The townsfolk switch
        /// removes wanderers and guards together; the critters switch removes critters.
        /// </summary>
        /// <param name="cityLevel">The city's current level.</param>
        /// <param name="townsfolkHidden">The mayor's townsfolk switch.</param>
        /// <param name="crittersHidden">The mayor's critters switch.</param>
        /// <param name="isStation">Whether the city is a player station.</param>
        /// <param name="entranceCount">How many building entrances the city has.</param>
        public static TownsfolkTarget GetTarget(
            int cityLevel,
            bool townsfolkHidden,
            bool crittersHidden,
            bool isStation,
            int entranceCount)
        {
            if (entranceCount <= 0)
                return TownsfolkTarget.None;

            return new TownsfolkTarget(
                townsfolkHidden ? 0 : GetWandererCount(cityLevel),
                townsfolkHidden ? 0 : GetGuardCount(cityLevel),
                crittersHidden || !HasCritters(isStation) ? 0 : GetCritterCount(cityLevel));
        }

        /// <summary>
        /// What to do about one group: a positive number is how many to add, a negative number how many to remove, and 0
        /// means the group is right. A level change, a switch, or a lost creature all show up here.
        /// </summary>
        public static int GetChange(int current, int target)
        {
            return Math.Max(0, target) - Math.Max(0, current);
        }

        /// <summary>How many of the creatures still wanted may be made now, given what is left of this beat's budget.</summary>
        public static int LimitSpawns(int wanted, int remainingBudget)
        {
            return Math.Max(0, Math.Min(wanted, remainingBudget));
        }

        /// <summary>The structure power check: whether a role plus its permissions may use the switches.</summary>
        public static bool CanUseSwitches(
            CityRole role,
            IReadOnlyDictionary<PropertyPermissionType, bool> permissions)
        {
            return CityPowerRules.HasPower(role, SwitchPower, permissions);
        }

        private static int ClampLevel(int cityLevel)
        {
            return Math.Clamp(cityLevel, 0, MaximumCityLevel);
        }

        #endregion

        #region Choosing

        /// <summary>
        /// Picks which look a new creature gets. Looks the city does not already show come first, so the streets stay varied
        /// until every look is out.
        /// </summary>
        /// <param name="poolSize">How many looks there are.</param>
        /// <param name="usedIndexes">The looks the city's creatures of this group already wear.</param>
        /// <param name="nextInt">Returns a number from 0 up to, but not including, the argument.</param>
        /// <returns>An index into the pool, or -1 when the pool is empty.</returns>
        public static int PickLookIndex(int poolSize, IReadOnlyCollection<int> usedIndexes, Func<int, int> nextInt)
        {
            if (poolSize <= 0)
                return -1;

            var free = new List<int>();
            for (var index = 0; index < poolSize; index++)
            {
                if (usedIndexes == null || !usedIndexes.Contains(index))
                    free.Add(index);
            }

            return free.Count > 0 ? free[nextInt(free.Count)] : nextInt(poolSize);
        }

        /// <summary>
        /// Picks the entrance a new creature appears at: a random one that is at least
        /// <see cref="MinimumSpawnDistanceFromPlayersMeters"/> from every player, or any when each is closer than that, so
        /// a crowd does not pop into view in front of someone.
        /// </summary>
        /// <param name="entrances">The entrance positions.</param>
        /// <param name="players">The positions of the players in the area.</param>
        /// <param name="nextInt">Returns a number from 0 up to, but not including, the argument.</param>
        /// <returns>An index into <paramref name="entrances"/>, or -1 when there are none.</returns>
        public static int PickSpawnIndex(IReadOnlyList<Vector3> entrances, IReadOnlyList<Vector3> players, Func<int, int> nextInt)
        {
            if (entrances == null || entrances.Count == 0)
                return -1;

            var farEnough = new List<int>();
            for (var index = 0; index < entrances.Count; index++)
            {
                if (IsFarFromAll(entrances[index], players, MinimumSpawnDistanceFromPlayersMeters))
                    farEnough.Add(index);
            }

            return farEnough.Count > 0 ? farEnough[nextInt(farEnough.Count)] : nextInt(entrances.Count);
        }

        /// <summary>
        /// Picks the entrance a wanderer walks to next: any one but the one it is at. With a single entrance that is the
        /// only choice.
        /// </summary>
        /// <param name="entranceCount">How many entrances there are.</param>
        /// <param name="currentIndex">The entrance it is at, or -1 when none.</param>
        /// <param name="nextInt">Returns a number from 0 up to, but not including, the argument.</param>
        /// <returns>An entrance index, or -1 when there are no entrances.</returns>
        public static int PickDestinationIndex(int entranceCount, int currentIndex, Func<int, int> nextInt)
        {
            if (entranceCount <= 0)
                return -1;

            if (entranceCount == 1)
                return 0;

            if (currentIndex < 0 || currentIndex >= entranceCount)
                return nextInt(entranceCount);

            var pick = nextInt(entranceCount - 1);
            return pick >= currentIndex ? pick + 1 : pick;
        }

        /// <summary>
        /// Puts the entrances in a loop order for guards: by angle around their centre, so a patrol walks around the city
        /// instead of crossing it.
        /// </summary>
        /// <param name="entrances">The entrance positions.</param>
        /// <returns>The entrance indexes in patrol order.</returns>
        public static List<int> GetPatrolOrder(IReadOnlyList<Vector3> entrances)
        {
            var order = new List<int>();
            if (entrances == null || entrances.Count == 0)
                return order;

            var centreX = 0.0f;
            var centreY = 0.0f;
            foreach (var entrance in entrances)
            {
                centreX += entrance.X;
                centreY += entrance.Y;
            }

            centreX /= entrances.Count;
            centreY /= entrances.Count;

            for (var index = 0; index < entrances.Count; index++)
                order.Add(index);

            order.Sort((a, b) =>
            {
                var angleA = MathF.Atan2(entrances[a].Y - centreY, entrances[a].X - centreX);
                var angleB = MathF.Atan2(entrances[b].Y - centreY, entrances[b].X - centreX);
                var byAngle = angleA.CompareTo(angleB);
                return byAngle != 0 ? byAngle : a.CompareTo(b);
            });

            return order;
        }

        /// <summary>
        /// Where along a patrol the Nth guard starts, so guards spread around it instead of bunching at one door.
        /// </summary>
        /// <param name="ordinal">Which guard it is, from 0.</param>
        /// <param name="guardCount">How many guards the city has.</param>
        /// <param name="routeLength">How many stops the patrol has.</param>
        public static int GetPatrolStartIndex(int ordinal, int guardCount, int routeLength)
        {
            if (routeLength <= 0)
                return -1;

            var start = Math.Max(0, ordinal) * routeLength / Math.Max(1, guardCount);
            return start % routeLength;
        }

        /// <summary>The next stop on a patrol, wrapping round. -1 when there is no patrol.</summary>
        public static int GetNextPatrolIndex(int currentIndex, int routeLength)
        {
            if (routeLength <= 0)
                return -1;

            return currentIndex < 0 ? 0 : (currentIndex + 1) % routeLength;
        }

        /// <summary>
        /// Where a critter ambles to: a point part of the way from where it stands towards an entrance, so it roams the
        /// streets between buildings and never stands on top of one.
        /// </summary>
        /// <param name="from">Where the critter is.</param>
        /// <param name="entrance">The entrance it heads towards.</param>
        /// <param name="nextInt">Returns a number from 0 up to, but not including, the argument.</param>
        public static Vector3 PickCritterDestination(Vector3 from, Vector3 entrance, Func<int, int> nextInt)
        {
            var fraction = CritterNearestFraction + nextInt(CritterFractionSteps) / 100.0f;
            return Vector3.Lerp(from, entrance, fraction);
        }

        /// <summary>How long a creature of the group stands still on reaching a destination, in seconds.</summary>
        public static int GetPauseSeconds(TownsfolkGroup group, Func<int, int> nextInt)
        {
            switch (group)
            {
                case TownsfolkGroup.Guard:
                    return 8 + nextInt(9);
                case TownsfolkGroup.Critter:
                    return 6 + nextInt(15);
                default:
                    return 4 + nextInt(9);
            }
        }

        private static bool IsFarFromAll(Vector3 point, IReadOnlyList<Vector3> players, float minimumMeters)
        {
            if (players == null)
                return true;

            foreach (var player in players)
            {
                var dx = point.X - player.X;
                var dy = point.Y - player.Y;
                if (dx * dx + dy * dy < minimumMeters * minimumMeters)
                    return false;
            }

            return true;
        }

        #endregion

        #region Walking and lifetime

        /// <summary>
        /// The number of beats a creature has been stuck after this one: it is stuck when it was told to walk and moved less
        /// than <see cref="StuckDistanceMeters"/>. Anything else resets the count.
        /// </summary>
        /// <param name="isWalking">Whether the creature is on a walk.</param>
        /// <param name="metersMoved">How far it moved since the last beat.</param>
        /// <param name="stuckBeats">The count before this beat.</param>
        public static int GetStuckBeats(bool isWalking, float metersMoved, int stuckBeats)
        {
            return isWalking && metersMoved < StuckDistanceMeters ? stuckBeats + 1 : 0;
        }

        /// <summary>Whether a creature has stood still long enough to be sent somewhere else.</summary>
        public static bool IsStuck(int stuckBeats)
        {
            return stuckBeats >= StuckBeatLimit;
        }

        /// <summary>
        /// Whether a city's street life should go: its area has been empty for
        /// <see cref="EmptyAreaDespawnMinutes"/> minutes, so an empty city costs nothing.
        /// </summary>
        /// <param name="emptySinceUtc">When the area became empty, or null while a player is in it.</param>
        /// <param name="nowUtc">The current time.</param>
        public static bool ShouldDespawnEmptyArea(DateTime? emptySinceUtc, DateTime nowUtc)
        {
            return emptySinceUtc.HasValue && nowUtc - emptySinceUtc.Value >= TimeSpan.FromMinutes(EmptyAreaDespawnMinutes);
        }

        /// <summary>Whether a city's data is due a full refresh.</summary>
        public static bool IsRefreshDue(DateTime nextRefreshUtc, DateTime nowUtc)
        {
            return nowUtc >= nextRefreshUtc;
        }

        #endregion

        #region Text

        /// <summary>The Gazette line for a switch change.</summary>
        public static string BuildGazetteText(TownsfolkSwitch which, bool hidden)
        {
            switch (which)
            {
                case TownsfolkSwitch.Townsfolk:
                    return hidden
                        ? "The streets were cleared of wandering townsfolk and guards."
                        : "Wandering townsfolk and guards returned to the streets.";
                case TownsfolkSwitch.Critters:
                    return hidden
                        ? "The streets were cleared of critters."
                        : "Critters returned to the streets.";
                default:
                    return string.Empty;
            }
        }

        /// <summary>The Manage City button text: what the group is doing now.</summary>
        public static string BuildButtonText(TownsfolkSwitch which, bool hidden)
        {
            var name = which == TownsfolkSwitch.Critters ? "Critters" : "Townsfolk";
            return $"{name}: {(hidden ? "Hidden" : "Shown")}";
        }

        /// <summary>The Manage City button tooltip.</summary>
        public static string BuildTooltip(TownsfolkSwitch which)
        {
            switch (which)
            {
                case TownsfolkSwitch.Townsfolk:
                    return "Wandering townsfolk and guards. Hiding them removes them at once and keeps them away until you show them again. Staff, stall vendors, and travelers always stay at their posts.";
                case TownsfolkSwitch.Critters:
                    return "Harmless animals roaming the streets. Hiding them removes them at once and keeps them away until you show them again.";
                default:
                    return string.Empty;
            }
        }

        /// <summary>The line Manage City shows after a switch is flipped.</summary>
        public static string BuildResultText(TownsfolkSwitch which, bool hidden)
        {
            var name = which == TownsfolkSwitch.Critters ? "Critters" : "Townsfolk";
            return hidden
                ? $"{name} are now hidden."
                : $"{name} are now shown.";
        }

        #endregion
    }
}
