using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.TownsfolkService;
using SWLOR.Game.Server.Service.TravelerService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The traveling NPCs (Player City Revamp, "Travelers"). Each week, at the first server start after the weekly boundary
    /// (<see cref="CityWeek"/>), every traveler moves to a city with a Traveler's Lodge. A traveler is made at the Lodge
    /// when a player is in its host city's area and removed after the area has been empty for 20 minutes, the way street
    /// life is (<see cref="Townsfolk"/>). A traveler no city qualifies for, or whose host lost its Lodge, waits at a fixed
    /// public hub. Sales go through the host city's store path (<see cref="CityStores"/>), so the host collects its sales
    /// tax. The rules live in <see cref="TravelerRules"/> and the roster in <see cref="TravelerCatalog"/>.
    /// </summary>
    public static class Travelers
    {
        /// <summary>The conversation menu a traveler opens, by class name. <c>TravelerDialog</c> in the dialog definitions.</summary>
        public const string ConversationName = "TravelerDialog";

        private const string TypeVariable = "TRAVELER_TYPE";
        private const string StoreVariable = "TRAVELER_STORE";
        private const string ConversationVariable = "CONVERSATION";

        /// <summary>How long to wait before trying again when a traveler could not be made where players are.</summary>
        private const int RetrySeconds = 30;

        // Every event script but the dialogue one, which the template keeps: it opens the conversation.
        private static readonly EventScript[] SilencedEventScripts =
        {
            EventScript.Creature_OnHeartbeat,
            EventScript.Creature_OnNotice,
            EventScript.Creature_OnSpellCastAt,
            EventScript.Creature_OnMeleeAttacked,
            EventScript.Creature_OnDamaged,
            EventScript.Creature_OnDisturbed,
            EventScript.Creature_OnEndCombatRound,
            EventScript.Creature_OnSpawnIn,
            EventScript.Creature_OnRested,
            EventScript.Creature_OnDeath,
            EventScript.Creature_OnUserDefined,
            EventScript.Creature_OnBlockedByDoor,
        };

        private static readonly Dictionary<TravelerType, TravelerState> _states = new();
        private static readonly Dictionary<TravelerType, Presence> _present = new();
        private static readonly Dictionary<TravelerType, DateTime> _retryAfterUtc = new();
        private static uint _hubWaypoint = OBJECT_INVALID;

        /// <summary>A traveler standing in the world, with the store it carries.</summary>
        private sealed class Presence
        {
            public TravelerType Type { get; init; }
            public string HostCityId { get; init; }
            public uint Area { get; init; }
            public uint Creature { get; init; }
            public uint Store { get; init; }
            public DateTime? EmptySinceUtc { get; set; }
        }

        #region Asking

        /// <summary>Which traveler this creature is, or Invalid.</summary>
        public static TravelerType GetTravelerType(uint npc)
        {
            return GetIsObjectValid(npc) ? (TravelerType)GetLocalInt(npc, TypeVariable) : TravelerType.Invalid;
        }

        /// <summary>The travelers hosted by a city this week.</summary>
        public static List<TravelerType> GetHostedBy(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return new List<TravelerType>();

            return TravelerCatalog.All.Keys
                .Where(type => string.Equals(GetState(type)?.HostCityId, cityId, StringComparison.Ordinal))
                .ToList();
        }

        /// <summary>Where every traveler is this week, for the login notice and the information broker.</summary>
        public static List<TravelerLocation> GetLocations()
        {
            var locations = new List<TravelerLocation>();
            foreach (var type in TravelerCatalog.All.Keys.OrderBy(type => (int)type))
            {
                var state = GetState(type);
                if (state == null || state.RotatedWeekStartUtc == default)
                    continue;

                if (string.IsNullOrWhiteSpace(state.HostCityId))
                {
                    var hub = GetHubWaypoint();
                    locations.Add(new TravelerLocation(type, string.Empty, GetIsObjectValid(hub) ? GetName(GetArea(hub)) : string.Empty));
                    continue;
                }

                var city = DB.Get<WorldProperty>(state.HostCityId);
                if (city == null || city.IsQueuedForDeletion)
                    continue;

                var area = GetCityArea(city);
                var areaName = GetIsObjectValid(area) && !Station.IsStation(city) ? GetName(area) : string.Empty;
                locations.Add(new TravelerLocation(type, city.CustomName, areaName));
            }

            return locations;
        }

        /// <summary>Opens the store a traveler carries for a player.</summary>
        public static bool OpenStore(uint player, uint npc)
        {
            var store = GetIsObjectValid(npc) ? GetLocalObject(npc, StoreVariable) : OBJECT_INVALID;
            if (!GetIsObjectValid(store))
            {
                SendMessageToPC(player, "This traveler has nothing for sale right now.");
                return false;
            }

            return CityStores.Open(player, store);
        }

        private static TravelerState GetState(TravelerType type)
        {
            if (_states.TryGetValue(type, out var cached))
                return cached;

            var state = DB.Get<TravelerState>(type.ToString());
            if (state != null)
                _states[type] = state;

            return state;
        }

        private static void SaveState(TravelerState state)
        {
            DB.Set(state);
            if (Enum.TryParse<TravelerType>(state.Id, false, out var type))
                _states[type] = state;
        }

        #endregion

        #region Weekly rotation

        /// <summary>
        /// Moves every traveler whose week has ended to a new host, and any whose host lost its Lodge to the hub. Called once
        /// at server start from city processing. Nothing here stops the rest of city processing if it fails.
        /// </summary>
        /// <param name="nowUtc">The current time.</param>
        public static void Rotate(DateTime nowUtc)
        {
            try
            {
                RotateTravelers(nowUtc);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Traveler rotation failed: {ex}");
            }
        }

        private static void RotateTravelers(DateTime nowUtc)
        {
            var weekStart = CityWeek.GetWeekStart(nowUtc);
            var hosts = GetLodgeCities();
            var lodgeCityIds = hosts.Keys.ToList();

            var due = new List<TravelerRotationInput>();
            var occupied = new List<string>();
            var states = new Dictionary<TravelerType, TravelerState>();

            foreach (var type in TravelerCatalog.All.Keys.OrderBy(type => (int)type))
            {
                var state = DB.Get<TravelerState>(type.ToString()) ?? new TravelerState(type);
                states[type] = state;

                if (TravelerRules.ShouldRotate(state.RotatedWeekStartUtc, weekStart))
                {
                    due.Add(new TravelerRotationInput(type, state.History.ToList()));
                }
                else if (TravelerRules.MustMoveToHub(state.HostCityId, lodgeCityIds))
                {
                    // Its host lost the Lodge, or the city is gone, since the last start.
                    state.HostCityId = TravelerRules.HubHostId;
                    SaveState(state);
                }
                else if (!string.IsNullOrWhiteSpace(state.HostCityId))
                {
                    occupied.Add(state.HostCityId);
                }
            }

            if (due.Count > 0)
            {
                var candidates = hosts.Values
                    .Select(city => new TravelerHostCandidate(city.Id, Edict.IsActive(city, EdictType.OpenGates, nowUtc)))
                    .ToList();
                var plan = TravelerRules.PlanRotation(due, candidates, occupied, weekStart, Random.Next);

                foreach (var (type, hostId) in plan)
                {
                    var state = states[type];
                    state.History = TravelerRules.AppendVisit(state.History, hostId, weekStart);
                    state.HostCityId = hostId;
                    state.RotatedWeekStartUtc = weekStart;
                    SaveState(state);

                    Announce(type, hostId, hosts);
                }
            }

            // Whatever stands in the world is taken down; the next beat with players around makes it again.
            foreach (var presence in _present.Values.ToList())
                RemovePresence(presence);

            foreach (var (type, state) in states)
                _states[type] = state;
        }

        /// <summary>The Gazette of the new host city, which also posts to Discord.</summary>
        private static void Announce(TravelerType type, string hostId, Dictionary<string, WorldProperty> hosts)
        {
            var detail = TravelerCatalog.Get(type);
            if (detail == null || string.IsNullOrWhiteSpace(hostId) || !hosts.TryGetValue(hostId, out var city))
                return;

            CityGazette.Post(city.Id, CityGazetteEntryType.Notice, TravelerText.BuildArrival(detail, city.CustomName));
            Log.Write(LogGroup.Property, $"TRAVELER - {detail.Key} is hosted by city '{city.CustomName}' ({city.Id}) this week.");
        }

        /// <summary>The cities (and stations) that have a Traveler's Lodge, by id.</summary>
        private static Dictionary<string, WorldProperty> GetLodgeCities()
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure)
                .AddFieldSearch(nameof(WorldProperty.StructureType), (int)StructureType.TravelersLodge)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            var count = checked((int)DB.SearchCount(query));
            var lodges = count <= 0 ? new List<WorldProperty>() : DB.Search(query.AddPaging(count, 0)).ToList();

            var cities = new Dictionary<string, WorldProperty>(StringComparer.Ordinal);
            foreach (var lodge in lodges)
            {
                if (string.IsNullOrWhiteSpace(lodge.ParentPropertyId) || cities.ContainsKey(lodge.ParentPropertyId))
                    continue;

                var city = DB.Get<WorldProperty>(lodge.ParentPropertyId);
                if (city != null && city.PropertyType == PropertyType.City && !city.IsQueuedForDeletion)
                    cities[city.Id] = city;
            }

            return cities;
        }

        #endregion

        #region Changes from the rest of the game

        /// <summary>
        /// A Traveler's Lodge was placed, moved or loaded. The traveler it hosts is taken down, and appears again at the
        /// Lodge's new place the next time a player is in the area.
        /// </summary>
        public static void OnLodgePlaced(WorldProperty structure, uint placeable)
        {
            if (structure == null)
                return;

            foreach (var presence in _present.Values.Where(x => x.HostCityId == structure.ParentPropertyId).ToList())
                RemovePresence(presence);
        }

        /// <summary>
        /// A Traveler's Lodge was taken down (picked up, or deleted with its city). The traveler it hosted waits at the hub
        /// until next week, and the city's Gazette says so.
        /// </summary>
        public static void OnLodgeDeleted(WorldProperty structure)
        {
            if (structure == null || string.IsNullOrWhiteSpace(structure.ParentPropertyId))
                return;

            var cityId = structure.ParentPropertyId;
            var city = DB.Get<WorldProperty>(cityId);
            var announce = city != null && !city.IsQueuedForDeletion;

            // A city has one Lodge, but a second would keep the traveler where it is.
            var otherLodges = GetLodges(cityId).Where(x => x.Id != structure.Id).ToList();
            if (otherLodges.Count > 0)
                return;

            MoveHostedToHub(cityId, announce);
        }

        /// <summary>A city (or station) was deleted. Whoever it hosted waits at the hub until next week.</summary>
        public static void OnCityDeleted(string cityId)
        {
            MoveHostedToHub(cityId, false);
        }

        private static void MoveHostedToHub(string cityId, bool announce)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return;

            foreach (var type in GetHostedBy(cityId))
            {
                var state = GetState(type);
                state.HostCityId = TravelerRules.HubHostId;
                SaveState(state);

                if (_present.TryGetValue(type, out var presence))
                    RemovePresence(presence);

                var detail = TravelerCatalog.Get(type);
                if (announce && detail != null)
                    CityGazette.Record(cityId, CityGazetteEntryType.Notice, TravelerText.BuildLodgeLost(detail));

                Log.Write(LogGroup.Property, $"TRAVELER - {type} moved to the hub because city '{cityId}' lost its Traveler's Lodge.");
            }
        }

        private static List<WorldProperty> GetLodges(string cityId)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), cityId, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure)
                .AddFieldSearch(nameof(WorldProperty.StructureType), (int)StructureType.TravelersLodge);
            var count = checked((int)DB.SearchCount(query));

            return count <= 0
                ? new List<WorldProperty>()
                : DB.Search(query.AddPaging(count, 0)).Where(x => !x.IsQueuedForDeletion).ToList();
        }

        #endregion

        #region In the world

        /// <summary>
        /// When a player or DM enters the area a traveler is hosted in, the traveler is made at once if it is not there yet.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void EnterArea()
        {
            var entering = GetEnteringObject();
            if (!GetIsPC(entering) && !GetIsDM(entering))
                return;

            var area = OBJECT_SELF;
            var now = DateTime.UtcNow;
            foreach (var type in TravelerCatalog.All.Keys.ToList())
            {
                try
                {
                    var state = GetState(type);
                    if (state != null && IsHostArea(state, area))
                        TryAppear(type, now, area);
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"Traveler '{type}' failed to appear on area entry: {ex}");
                }
            }
        }

        /// <summary>
        /// Every 6 seconds, a traveler with a player in its area is made if it is not there, and one whose area has been empty
        /// for 20 minutes is removed. An area nobody is in costs nothing.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorHeartbeat)]
        public static void ProcessTravelers()
        {
            var now = DateTime.UtcNow;
            foreach (var type in TravelerCatalog.All.Keys.ToList())
            {
                try
                {
                    if (_present.TryGetValue(type, out var presence))
                        Maintain(presence, now);
                    else
                        TryAppear(type, now, OBJECT_INVALID);
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"Traveler '{type}' failed: {ex}");
                    _retryAfterUtc[type] = now.AddSeconds(RetrySeconds);
                }
            }
        }

        /// <summary>Whether this is the area a traveler is hosted in: the host city's own area, or the hub's.</summary>
        private static bool IsHostArea(TravelerState state, uint area)
        {
            if (string.IsNullOrWhiteSpace(state.HostCityId))
            {
                var hub = GetHubWaypoint();
                return GetIsObjectValid(hub) && GetArea(hub) == area;
            }

            return string.Equals(Property.GetPropertyId(area), state.HostCityId, StringComparison.Ordinal);
        }

        private static void Maintain(Presence presence, DateTime now)
        {
            var state = GetState(presence.Type);
            if (!GetIsObjectValid(presence.Creature) ||
                !GetIsObjectValid(presence.Area) ||
                state == null ||
                !string.Equals(state.HostCityId, presence.HostCityId, StringComparison.Ordinal))
            {
                RemovePresence(presence);
                return;
            }

            if (AreaPlugin.GetNumberOfPlayersInArea(presence.Area) > 0)
            {
                presence.EmptySinceUtc = null;
                return;
            }

            presence.EmptySinceUtc ??= now;
            if (TownsfolkRules.ShouldDespawnEmptyArea(presence.EmptySinceUtc, now))
                RemovePresence(presence);
        }

        /// <summary>
        /// Makes a traveler where it is hosted when a player is in that area. <paramref name="areaWithPlayer"/> names an area a
        /// player just entered, which may not count its newest player yet.
        /// </summary>
        private static void TryAppear(TravelerType type, DateTime now, uint areaWithPlayer)
        {
            if (_present.ContainsKey(type))
                return;

            if (_retryAfterUtc.TryGetValue(type, out var retryAfter) && now < retryAfter)
                return;

            var detail = TravelerCatalog.Get(type);
            var state = GetState(type);
            if (detail == null || state == null || state.RotatedWeekStartUtc == default)
                return;

            WorldProperty city = null;
            uint area;
            if (string.IsNullOrWhiteSpace(state.HostCityId))
            {
                var hub = GetHubWaypoint();
                if (!GetIsObjectValid(hub))
                    return;

                area = GetArea(hub);
            }
            else
            {
                city = DB.Get<WorldProperty>(state.HostCityId);
                if (city == null || city.IsQueuedForDeletion)
                    return;

                area = GetCityArea(city);
            }

            if (!GetIsObjectValid(area))
                return;

            if (area != areaWithPlayer && AreaPlugin.GetNumberOfPlayersInArea(area) <= 0)
                return;

            if (!TryGetLocation(city, out var location))
            {
                // The Lodge is not in the world yet. Try again shortly instead of every beat.
                _retryAfterUtc[type] = now.AddSeconds(RetrySeconds);
                return;
            }

            Appear(detail, city, area, location);
        }

        private static void Appear(TravelerDetail detail, WorldProperty city, uint area, Location location)
        {
            var creature = CreateObject(ObjectType.Creature, detail.LookResref, location);
            if (!GetIsObjectValid(creature))
            {
                _retryAfterUtc[detail.Type] = DateTime.UtcNow.AddSeconds(RetrySeconds);
                Log.Write(LogGroup.Error, $"Traveler '{detail.Key}' could not be created from template '{detail.LookResref}'.", true);
                return;
            }

            var store = CityStores.CreateTraveler(city, detail, location);

            SetPlotFlag(creature, true);
            CreaturePlugin.SetFaction(creature, TownsfolkRules.NeutralFactionId);
            SetName(creature, detail.Name);
            SetDescription(creature, detail.Story);
            foreach (var handler in SilencedEventScripts)
                SetEventScript(creature, handler, string.Empty);

            SetLocalInt(creature, TypeVariable, (int)detail.Type);
            SetLocalString(creature, ConversationVariable, ConversationName);
            if (GetIsObjectValid(store))
                SetLocalObject(creature, StoreVariable, store);

            _present[detail.Type] = new Presence
            {
                Type = detail.Type,
                HostCityId = city?.Id ?? string.Empty,
                Area = area,
                Creature = creature,
                Store = store,
            };
        }

        /// <summary>
        /// Where the traveler stands: in front of the host city's Lodge, or at the hub waypoint. False when the Lodge is not
        /// in the world.
        /// </summary>
        private static bool TryGetLocation(WorldProperty city, out Location location)
        {
            location = default;
            if (city == null)
            {
                var hub = GetHubWaypoint();
                if (!GetIsObjectValid(hub))
                    return false;

                location = GetLocation(hub);
                return true;
            }

            var lodge = GetLodges(city.Id).FirstOrDefault();
            var placeable = lodge == null ? OBJECT_INVALID : Property.GetPlaceableByPropertyId(lodge.Id);
            if (!GetIsObjectValid(placeable))
                return false;

            var position = GetPosition(placeable);
            var placement = TravelerRules.GetLodgePlacement(position.X, position.Y, GetFacing(placeable));
            location = Location(GetArea(placeable), Vector3(placement.X, placement.Y, position.Z), placement.FacingDegrees);
            return true;
        }

        private static void RemovePresence(Presence presence)
        {
            CityStores.Remove(presence.Store);
            if (GetIsObjectValid(presence.Creature))
                DestroyObject(presence.Creature);

            _present.Remove(presence.Type);
        }

        /// <summary>The area a city stands in: a planet city's own area, or a station's deck once it is loaded.</summary>
        private static uint GetCityArea(WorldProperty city)
        {
            if (Station.IsStation(city))
                return Property.TryGetLoadedInstance(city.Id, out var deck) ? deck.Area : OBJECT_INVALID;

            return Area.GetAreaByResref(city.ParentPropertyId);
        }

        private static uint GetHubWaypoint()
        {
            if (!GetIsObjectValid(_hubWaypoint))
                _hubWaypoint = GetWaypointByTag(TravelerRules.HubWaypointTag);

            return _hubWaypoint;
        }

        #endregion
    }
}
