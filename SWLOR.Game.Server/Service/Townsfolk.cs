using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.TownsfolkService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// A city's street life (Player City Revamp, "Ambient townsfolk"): wandering townsfolk, patrolling guards, and roaming
    /// critters, made from existing module creature templates. They are plot, in the Neutral faction, with every event
    /// script cleared, so they never fight, react, talk, drop loot, or give XP. A city's crowd is made when a player first
    /// enters its area, counted by the city's current level and the mayor's two switches, and removed when the area has
    /// been empty for 20 minutes, the same way the Spawn service handles areas with no players. The rules live in
    /// <see cref="TownsfolkRules"/>; this class reads the city and acts on the game.
    /// </summary>
    public static class Townsfolk
    {
        /// <summary>Local string on a street creature: the property id of the city it belongs to.</summary>
        public const string CityVariable = "CITY_STREET_LIFE_CITY_ID";

        /// <summary>Local int on a street creature: its <see cref="TownsfolkGroup"/>.</summary>
        public const string GroupVariable = "CITY_STREET_LIFE_GROUP";

        private const int FailureLimit = 3;

        private static readonly EventScript[] CreatureEventScripts =
        {
            EventScript.Creature_OnHeartbeat,
            EventScript.Creature_OnNotice,
            EventScript.Creature_OnSpellCastAt,
            EventScript.Creature_OnMeleeAttacked,
            EventScript.Creature_OnDamaged,
            EventScript.Creature_OnDisturbed,
            EventScript.Creature_OnEndCombatRound,
            EventScript.Creature_OnDialogue,
            EventScript.Creature_OnSpawnIn,
            EventScript.Creature_OnRested,
            EventScript.Creature_OnDeath,
            EventScript.Creature_OnUserDefined,
            EventScript.Creature_OnBlockedByDoor,
        };

        private static readonly TownsfolkGroup[] Groups =
        {
            TownsfolkGroup.Wanderer,
            TownsfolkGroup.Guard,
            TownsfolkGroup.Critter,
        };

        private static readonly Dictionary<string, Crowd> _crowds = new();
        private static readonly HashSet<string> _notCities = new();
        private static readonly HashSet<string> _loggedMissingTemplates = new();

        private sealed class Member
        {
            public uint Creature { get; init; }
            public TownsfolkGroup Group { get; init; }
            public int LookIndex { get; init; }

            /// <summary>The entrance a wanderer or critter last headed to, or the guard's stop on the patrol.</summary>
            public int StopIndex { get; set; } = -1;

            public Vector3 LastPosition { get; set; }
            public bool IsWalking { get; set; }
            public int StuckBeats { get; set; }
        }

        private sealed class Crowd
        {
            public Crowd(string cityId, uint area)
            {
                CityId = cityId;
                Area = area;
            }

            public string CityId { get; }
            public uint Area { get; set; }
            public TownsfolkTarget Target { get; set; } = TownsfolkTarget.None;
            public List<Member> Members { get; } = new();
            public List<Vector3> Entrances { get; } = new();
            public List<int> PatrolOrder { get; set; } = new();
            public bool IsDirty { get; set; } = true;
            public DateTime NextRefreshUtc { get; set; } = DateTime.MinValue;
            public DateTime? EmptySinceUtc { get; set; }
            public int Failures { get; set; }
        }

        #region Events

        /// <summary>
        /// When a player or DM enters a city's area, its crowd is made if it is not there yet. The first few creatures appear
        /// at once and the rest over the next heartbeats.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void EnterArea()
        {
            var entering = GetEnteringObject();
            if (!GetIsPC(entering) && !GetIsDM(entering))
                return;

            var area = OBJECT_SELF;
            var cityId = Property.GetPropertyId(area);
            if (string.IsNullOrWhiteSpace(cityId) || _notCities.Contains(cityId))
                return;

            try
            {
                if (!_crowds.TryGetValue(cityId, out var crowd))
                {
                    var city = DB.Get<WorldProperty>(cityId);
                    if (city == null || city.PropertyType != PropertyType.City)
                    {
                        // An apartment, building interior, or starship: it has no streets.
                        _notCities.Add(cityId);
                        return;
                    }

                    crowd = new Crowd(cityId, area);
                    _crowds[cityId] = crowd;
                }

                crowd.Area = area;
                ProcessCrowd(crowd, DateTime.UtcNow, true);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Street life for city '{cityId}' failed on area entry: {ex}");
            }
        }

        /// <summary>
        /// Every 6 seconds, each city crowd with a player in its area is kept at its target and its creatures are sent
        /// walking; a crowd whose area has been empty for 20 minutes is removed. An empty area costs nothing in between.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorHeartbeat)]
        public static void ProcessCrowds()
        {
            if (_crowds.Count == 0)
                return;

            var now = DateTime.UtcNow;
            foreach (var crowd in _crowds.Values.ToList())
            {
                try
                {
                    var hasPlayers = GetIsObjectValid(crowd.Area) && AreaPlugin.GetNumberOfPlayersInArea(crowd.Area) > 0;
                    ProcessCrowd(crowd, now, hasPlayers);
                }
                catch (Exception ex)
                {
                    crowd.Failures++;
                    Log.Write(LogGroup.Error, $"Street life for city '{crowd.CityId}' failed: {ex}");

                    // A crowd that keeps failing is removed; the next player to enter the area starts it again.
                    if (crowd.Failures >= FailureLimit)
                        Remove(crowd);
                }
            }
        }

        #endregion

        #region Changes from the rest of the game

        /// <summary>
        /// Call when something that sets a city's street life changed: its level, or a switch. The crowd is brought to
        /// the new target at once, so creatures that are no longer wanted leave in the same moment. New ones appear if
        /// players are in the area, and otherwise when the next player enters it.
        /// </summary>
        /// <param name="cityId">The city's property id.</param>
        public static void OnCityChanged(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId) || !_crowds.TryGetValue(cityId, out var crowd))
                return;

            try
            {
                var now = DateTime.UtcNow;
                crowd.IsDirty = true;
                if (!Refresh(crowd, now))
                    return;

                Prune(crowd);
                RemoveSurplus(crowd);

                if (GetIsObjectValid(crowd.Area) && AreaPlugin.GetNumberOfPlayersInArea(crowd.Area) > 0)
                    SpawnShortfall(crowd);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Street life for city '{cityId}' failed to update: {ex}");
            }
        }

        /// <summary>
        /// Call when a structure of a city was placed, moved, or retrieved. A building changes where the entrances are, so the
        /// crowd reads them again on its next beat.
        /// </summary>
        /// <param name="structure">The structure's property.</param>
        public static void OnStructureChanged(WorldProperty structure)
        {
            if (structure == null ||
                !TownsfolkRules.IsEntranceStructure(structure.StructureType) ||
                !_crowds.TryGetValue(structure.ParentPropertyId ?? string.Empty, out var crowd))
            {
                return;
            }

            crowd.IsDirty = true;
        }

        /// <summary>
        /// Call when a city (or a station) is deleted. Its street life leaves with it.
        /// </summary>
        /// <param name="cityId">The city's property id.</param>
        public static void OnCityDeleted(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return;

            if (_crowds.TryGetValue(cityId, out var crowd))
                Remove(crowd);

            _notCities.Remove(cityId);
        }

        #endregion

        #region Switches

        /// <summary>Whether the mayor switched the group off.</summary>
        public static bool IsHidden(WorldProperty city, TownsfolkSwitch which)
        {
            if (city == null)
                return false;

            return which == TownsfolkSwitch.Critters ? city.CrittersHidden : city.TownsfolkHidden;
        }

        /// <summary>
        /// Switches a group on or off for a city. The city is saved, the Gazette records the change under the name and title
        /// of the official, and the crowd is brought to its new target at once. Nothing happens when it is already so.
        /// The caller checks that the official may (see <see cref="TownsfolkRules.SwitchPower"/>).
        /// </summary>
        /// <param name="city">The city. It is saved.</param>
        /// <param name="which">Which switch.</param>
        /// <param name="hidden">True to hide the group, false to show it.</param>
        /// <param name="officialPlayerId">The official who switched it.</param>
        /// <param name="officialTitle">The official's title in the Gazette.</param>
        /// <returns>True if the switch changed.</returns>
        public static bool SetHidden(
            WorldProperty city,
            TownsfolkSwitch which,
            bool hidden,
            string officialPlayerId,
            string officialTitle)
        {
            if (city == null || which == TownsfolkSwitch.Invalid || IsHidden(city, which) == hidden)
                return false;

            if (which == TownsfolkSwitch.Critters)
                city.CrittersHidden = hidden;
            else
                city.TownsfolkHidden = hidden;

            DB.Set(city);

            CityGazette.Record(
                city.Id,
                CityGazetteEntryType.Notice,
                TownsfolkRules.BuildGazetteText(which, hidden),
                officialPlayerId,
                officialTitle);

            OnCityChanged(city.Id);
            return true;
        }

        #endregion

        #region Keeping a crowd

        private static void ProcessCrowd(Crowd crowd, DateTime now, bool hasPlayers)
        {
            if (!GetIsObjectValid(crowd.Area))
            {
                Remove(crowd);
                return;
            }

            if (!hasPlayers)
            {
                crowd.EmptySinceUtc ??= now;
                if (TownsfolkRules.ShouldDespawnEmptyArea(crowd.EmptySinceUtc, now))
                    Remove(crowd);

                return;
            }

            crowd.EmptySinceUtc = null;
            if ((crowd.IsDirty || TownsfolkRules.IsRefreshDue(crowd.NextRefreshUtc, now)) && !Refresh(crowd, now))
                return;

            Prune(crowd);
            RemoveSurplus(crowd);
            SpawnShortfall(crowd);
            Walk(crowd);
        }

        /// <summary>
        /// Reads the city again: its level, the two switches, and where its building entrances are. Removes the crowd and
        /// returns false when the city is gone.
        /// </summary>
        private static bool Refresh(Crowd crowd, DateTime now)
        {
            var city = DB.Get<WorldProperty>(crowd.CityId);
            if (city == null || city.IsQueuedForDeletion)
            {
                Remove(crowd);
                return false;
            }

            LoadEntrances(crowd, city);

            var level = city.Upgrades != null && city.Upgrades.TryGetValue(PropertyUpgradeType.CityLevel, out var cityLevel)
                ? cityLevel
                : 1;

            crowd.Target = TownsfolkRules.GetTarget(
                level,
                city.TownsfolkHidden,
                city.CrittersHidden,
                Station.IsStation(city),
                crowd.Entrances.Count);
            crowd.IsDirty = false;
            crowd.NextRefreshUtc = now.AddSeconds(TownsfolkRules.RefreshSeconds);
            return true;
        }

        /// <summary>
        /// Finds the city's building entrances: the door each building spawned, or the building's saved position when its
        /// door is not in the world yet.
        /// </summary>
        private static void LoadEntrances(Crowd crowd, WorldProperty city)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), city.Id, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure)
                .AddFieldSearch(nameof(WorldProperty.StructureType), TownsfolkRules.EntranceStructures.Select(s => (int)s))
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            var count = (int)DB.SearchCount(query);
            var structures = count > 0
                ? DB.Search(query.AddPaging(count, 0)).OrderBy(s => s.Id, StringComparer.Ordinal).ToList()
                : new List<WorldProperty>();

            crowd.Entrances.Clear();
            foreach (var structure in structures)
            {
                if (!structure.Positions.TryGetValue(PropertyLocationType.StaticPosition, out var location))
                    continue;

                var position = new Vector3(location.X, location.Y, location.Z);

                var building = Property.GetPlaceableByPropertyId(structure.Id);
                if (GetIsObjectValid(building))
                {
                    var door = GetLocalObject(building, StructureChangedAction.DoorVariable);
                    if (GetIsObjectValid(door) && GetArea(door) == crowd.Area)
                        position = GetPosition(door);
                }

                crowd.Entrances.Add(position);
            }

            crowd.PatrolOrder = TownsfolkRules.GetPatrolOrder(crowd.Entrances);
        }

        /// <summary>Forgets creatures that no longer exist, so the shortfall is made good.</summary>
        private static void Prune(Crowd crowd)
        {
            crowd.Members.RemoveAll(member => !GetIsObjectValid(member.Creature));
        }

        /// <summary>Removes the newest creatures of any group that has more than its target.</summary>
        private static void RemoveSurplus(Crowd crowd)
        {
            foreach (var group in Groups)
            {
                var surplus = -TownsfolkRules.GetChange(CountOf(crowd, group), crowd.Target.Get(group));
                for (var removed = 0; removed < surplus; removed++)
                {
                    var index = crowd.Members.FindLastIndex(member => member.Group == group);
                    if (index < 0)
                        break;

                    DestroyMember(crowd.Members[index]);
                    crowd.Members.RemoveAt(index);
                }
            }
        }

        /// <summary>Makes the creatures a group is short of, a few per beat.</summary>
        private static void SpawnShortfall(Crowd crowd)
        {
            var budget = TownsfolkRules.MaximumSpawnsPerBeat;
            foreach (var group in Groups)
            {
                var toAdd = TownsfolkRules.LimitSpawns(
                    TownsfolkRules.GetChange(CountOf(crowd, group), crowd.Target.Get(group)),
                    budget);

                for (var added = 0; added < toAdd; added++)
                {
                    if (!SpawnMember(crowd, group))
                        return;

                    budget--;
                }
            }
        }

        private static int CountOf(Crowd crowd, TownsfolkGroup group)
        {
            return crowd.Members.Count(member => member.Group == group);
        }

        private static bool SpawnMember(Crowd crowd, TownsfolkGroup group)
        {
            var looks = TownsfolkLooks.Get(group);
            var usedLooks = crowd.Members.Where(member => member.Group == group).Select(member => member.LookIndex).ToList();
            var lookIndex = TownsfolkRules.PickLookIndex(looks.Count, usedLooks, Random.Next);
            if (lookIndex < 0 || crowd.Entrances.Count == 0)
                return false;

            var look = looks[lookIndex];

            // A guard starts at its own stop on the patrol; the others at an entrance away from the players.
            var stopIndex = -1;
            int entranceIndex;
            if (group == TownsfolkGroup.Guard)
            {
                stopIndex = TownsfolkRules.GetPatrolStartIndex(CountOf(crowd, group), crowd.Target.Guards, crowd.PatrolOrder.Count);
                entranceIndex = stopIndex < 0 ? -1 : crowd.PatrolOrder[stopIndex];
            }
            else
            {
                var players = Area.GetPlayersInArea(crowd.Area).Select(player => GetPosition(player)).ToList();
                entranceIndex = TownsfolkRules.PickSpawnIndex(crowd.Entrances, players, Random.Next);
                stopIndex = entranceIndex;
            }

            if (entranceIndex < 0)
                return false;

            var position = crowd.Entrances[entranceIndex];
            var creature = CreateObject(ObjectType.Creature, look.Resref, Location(crowd.Area, position, Random.Next(360)));
            if (!GetIsObjectValid(creature))
            {
                if (_loggedMissingTemplates.Add(look.Resref))
                    Log.Write(LogGroup.Error, $"Street life could not create a creature from template '{look.Resref}'.");

                return false;
            }

            Prepare(creature, look, crowd.CityId, group);

            crowd.Members.Add(new Member
            {
                Creature = creature,
                Group = group,
                LookIndex = lookIndex,
                StopIndex = stopIndex,
                LastPosition = position,
            });
            return true;
        }

        /// <summary>
        /// Makes a new creature scenery: plot, Neutral, renamed, with no event scripts, so it never fights, reacts, talks,
        /// drops loot, or gives XP, and nothing in the Hostile faction hunts it.
        /// </summary>
        private static void Prepare(uint creature, TownsfolkLook look, string cityId, TownsfolkGroup group)
        {
            SetPlotFlag(creature, true);
            CreaturePlugin.SetFaction(creature, TownsfolkRules.NeutralFactionId);
            SetName(creature, look.Name);

            if (look.Appearance.HasValue)
                SetCreatureAppearanceType(creature, (AppearanceType)look.Appearance.Value);

            foreach (var handler in CreatureEventScripts)
            {
                SetEventScript(creature, handler, string.Empty);
            }

            SetLocalString(creature, CityVariable, cityId);
            SetLocalInt(creature, GroupVariable, (int)group);
        }

        #endregion

        #region Walking

        /// <summary>
        /// Sends every idle creature somewhere: wanderers to another entrance, guards to the next stop on their patrol,
        /// critters part of the way to an entrance. A creature stuck against something is sent somewhere else.
        /// </summary>
        private static void Walk(Crowd crowd)
        {
            foreach (var member in crowd.Members)
            {
                var creature = member.Creature;
                var action = GetCurrentAction(creature);
                var position = GetPosition(creature);
                var metersMoved = HorizontalDistance(position, member.LastPosition);

                member.StuckBeats = TownsfolkRules.GetStuckBeats(
                    member.IsWalking && action == ActionType.MoveToPoint,
                    metersMoved,
                    member.StuckBeats);
                member.LastPosition = position;

                if (action != ActionType.Invalid && !TownsfolkRules.IsStuck(member.StuckBeats))
                    continue;

                SendSomewhere(crowd, member, position);
            }
        }

        private static void SendSomewhere(Crowd crowd, Member member, Vector3 position)
        {
            Vector3 destination;
            switch (member.Group)
            {
                case TownsfolkGroup.Guard:
                    member.StopIndex = TownsfolkRules.GetNextPatrolIndex(member.StopIndex, crowd.PatrolOrder.Count);
                    if (member.StopIndex < 0)
                        return;

                    destination = crowd.Entrances[crowd.PatrolOrder[member.StopIndex]];
                    break;
                case TownsfolkGroup.Critter:
                    member.StopIndex = TownsfolkRules.PickDestinationIndex(crowd.Entrances.Count, member.StopIndex, Random.Next);
                    if (member.StopIndex < 0)
                        return;

                    destination = TownsfolkRules.PickCritterDestination(position, crowd.Entrances[member.StopIndex], Random.Next);
                    break;
                default:
                    member.StopIndex = TownsfolkRules.PickDestinationIndex(crowd.Entrances.Count, member.StopIndex, Random.Next);
                    if (member.StopIndex < 0)
                        return;

                    destination = crowd.Entrances[member.StopIndex];
                    break;
            }

            var location = Location(crowd.Area, destination, Random.Next(360));
            var pause = (float)TownsfolkRules.GetPauseSeconds(member.Group, Random.Next);

            AssignCommand(member.Creature, () =>
            {
                ClearAllActions();
                ActionMoveToLocation(location);
                ActionWait(pause);
            });

            member.IsWalking = true;
            member.StuckBeats = 0;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        #endregion

        #region Removing

        private static void Remove(Crowd crowd)
        {
            foreach (var member in crowd.Members)
            {
                DestroyMember(member);
            }

            crowd.Members.Clear();
            _crowds.Remove(crowd.CityId);
        }

        private static void DestroyMember(Member member)
        {
            if (GetIsObjectValid(member.Creature))
                DestroyObject(member.Creature);
        }

        #endregion
    }
}
