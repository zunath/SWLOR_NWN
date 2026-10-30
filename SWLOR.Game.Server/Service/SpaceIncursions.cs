using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Incursions (plan section 30). Once a week a Sith or Mandalorian armada invades a region (an orbit with a dock and its
    /// starlane neighbours) for up to 48 hours: waves in every area, then each area's command group, and a blockade at
    /// every planet dock that refuses docking and closes the dock's exchange while it holds. The region's exchanges price
    /// everything higher while it lasts. Clearing every area and blockade pays each player by the contribution tracked
    /// (damage dealt to destroyed incursion ships, killing blows and repairs) plus a region reward. The schedule and the
    /// running incursion are saved in <see cref="IncursionState"/>: a restart respawns its ships and keeps its progress.
    /// </summary>
    public static partial class Space
    {
        private const string IncursionRoleVariable = "INCURSION_ROLE";
        private const string IncursionTierVariable = "INCURSION_TIER";
        private const string IncursionRoleWave = "wave";
        private const string IncursionRoleCommand = "command";
        private const string IncursionRoleBlockade = "blockade";
        private const string DockTeleportWaypointVariable = "STARPORT_TELEPORT_WAYPOINT";
        private const string DockLandingWaypointVariable = "STARPORT_LANDING_WAYPOINT_TAG";
        private const string DockPlanetVariable = "PLANET_TYPE_ID";
        private const string DockConversationVariable = "CONVERSATION";
        private const string DockConversation = "StarportDockDialog";

        private enum IncursionAreaPhase
        {
            Idle = 0,
            Wave = 1,
            Command = 2,
        }

        private static IncursionState _incursionState;
        private static bool _incursionDirty;
        private static bool _incursionSpawned;
        private static DateTime _incursionSavedAt;
        private static DateTime _incursionStationsCheckedAt;
        private static readonly Dictionary<string, List<uint>> _incursionAreaShips = new();
        private static readonly Dictionary<string, IncursionAreaPhase> _incursionAreaPhases = new();
        private static readonly Dictionary<string, DateTime> _incursionNextSpawn = new();
        private static readonly Dictionary<string, List<uint>> _incursionBlockadeShips = new();

        /// <summary>Blockades none of whose ships could be created, by dock key, with how to spawn them again.</summary>
        private static readonly Dictionary<string, Func<List<uint>>> _incursionBlockadeRetries = new();
        // Damage is keyed by the persistent player id of the pilot, not the temporary creature handle, so a pilot who
        // logs out before the kill is still credited.
        private static readonly Dictionary<uint, Dictionary<string, double>> _incursionDamage = new();
        private static readonly Dictionary<uint, string> _incursionLastHit = new();
        private static readonly Dictionary<string, string> _incursionPilotNames = new();

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ScheduleIncursions()
        {
            Scheduler.ScheduleRepeating(ProcessIncursions, TimeSpan.FromSeconds(IncursionTuning.TickSeconds), TimeSpan.FromSeconds(30));
        }

        private static IncursionState IncursionRecord =>
            _incursionState ??= DB.Get<IncursionState>(IncursionState.RecordId) ?? new IncursionState();

        /// <summary>The running incursion, or null.</summary>
        public static ActiveIncursion GetActiveIncursion() => IncursionRecord.Active;

        private static void SaveIncursions()
        {
            DB.Set(IncursionRecord);
            _incursionDirty = false;
            _incursionSavedAt = DateTime.UtcNow;
        }

        private static void ProcessIncursions()
        {
            try
            {
                var state = IncursionRecord;
                var now = DateTime.UtcNow;
                if (state.NextStartAt == default)
                {
                    state.NextStartAt = IncursionRules.NextStart(now);
                    _incursionDirty = true;
                }

                if (state.Active == null)
                {
                    if (IncursionRules.IsDue(state.NextStartAt, now))
                    {
                        var missed = IncursionRules.IsMissed(state.NextStartAt, now);
                        state.NextStartAt = IncursionRules.NextStart(now);
                        _incursionDirty = true;
                        if (!missed)
                        {
                            var region = IncursionRules.PickRegion(IncursionTuning.Regions, state.LastCenter, max => Random.Next(max));
                            if (region != null)
                                StartIncursion(region, IncursionRules.PickInvader(max => Random.Next(max)), "weekly schedule");
                        }
                    }
                }
                else if (now >= state.Active.EndsAt)
                {
                    EndIncursion(false);
                }
                else
                {
                    if (!_incursionSpawned)
                        SpawnIncursion();
                    UpdateIncursion(now);
                }

                if (_incursionDirty || now - _incursionSavedAt >= TimeSpan.FromMinutes(1) && state.Active != null)
                    SaveIncursions();

                ProcessCapitalBattle(now);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Incursion processing failed: {ex}");
            }
        }

        /// <summary>Starts an incursion in a region now. Refused while another runs.</summary>
        public static bool StartIncursion(IncursionRegion region, IncursionInvader invader, string source)
        {
            var state = IncursionRecord;
            if (state.Active != null || region == null)
                return false;

            var docks = new Dictionary<string, int>();
            var stationDocks = new Dictionary<string, string>();
            foreach (var area in region.Areas)
            {
                foreach (var (_, key, planet) in GetIncursionDocks(area.Resref))
                    docks[key] = planet;
                foreach (var (_, key, stationId) in GetIncursionStationDocks(area.Resref))
                    stationDocks[key] = stationId;
            }

            state.Active = IncursionRules.Create(region, invader, DateTime.UtcNow, docks, stationDocks);
            state.LastCenter = region.Center;
            _incursionSpawned = false;
            SpawnIncursion();
            SaveIncursions();

            var name = IncursionRules.GetInvader(invader)?.Name ?? "An armada";
            BroadcastIncursion(ColorToken.Red(
                $"INCURSION: the {name} invades the {region.Name} region ({string.Join(", ", region.Areas.Select(x => x.Name))}) for up to " +
                $"{IncursionTuning.DurationHours:0} hours. Its blockades have shut the region's docks and prices there have spiked. " +
                "Drive it out for a share of the spoils: /incursion for details."));
            Log.Write(LogGroup.Space, $"INCURSION STARTED - {invader} in {region.Center} ({source}), {docks.Count} blockades");
            return true;
        }

        /// <summary>Starts an incursion centred on the DM's orbit (DM tools). Returns the status line.</summary>
        public static (bool Started, string Message) StartIncursionAt(uint dm)
        {
            var area = GetArea(dm);
            if (!IsSpaceArea(area))
                return (false, "You must be in a space area.");

            if (IncursionRecord.Active != null)
                return (false, $"An incursion is already running in the {IncursionRules.FindRegion(IncursionRecord.Active.Center)?.Name} region.");

            var region = IncursionRules.FindRegion(GetResRef(area));
            if (region == null)
                return (false, "Stand in an orbit with a dock to start an incursion centred on it.");

            var invader = IncursionRules.PickInvader(max => Random.Next(max));
            var started = StartIncursion(region, invader, $"DM {GetName(dm)}");
            return (started, started
                ? $"Started a {IncursionRules.GetInvader(invader)?.Name} incursion in the {region.Name} region."
                : "The incursion could not be started.");
        }

        /// <summary>The planet docks of an area that a blockade can hold: (placeable, key, planet type).</summary>
        private static List<(uint Dock, string Key, int Planet)> GetIncursionDocks(string areaResref)
        {
            var docks = new List<(uint Dock, string Key, int Planet)>();
            var area = Area.GetAreaByResref(areaResref);
            if (!GetIsObjectValid(area))
                return docks;

            for (var obj = GetFirstObjectInArea(area, ObjectType.Placeable); GetIsObjectValid(obj); obj = GetNextObjectInArea(area, ObjectType.Placeable))
            {
                var waypoint = GetLocalString(obj, DockTeleportWaypointVariable);
                if (string.IsNullOrWhiteSpace(waypoint) ||
                    GetLocalString(obj, DockConversationVariable) != DockConversation ||
                    !string.IsNullOrWhiteSpace(GetLocalString(obj, DockLandingWaypointVariable)) ||
                    !string.IsNullOrWhiteSpace(GetLocalString(obj, Station.StationCityVariable)))
                {
                    continue;
                }

                docks.Add((obj, IncursionRules.DockKey(areaResref, waypoint), GetLocalInt(obj, DockPlanetVariable)));
            }

            return docks;
        }

        /// <summary>
        /// The player stations standing in an area (W11o): (anchor waypoint, dock key, station id). A station's docking point
        /// stands at its anchor, so the blockade forms there; the anchor waypoint is module data, there before any station loads.
        /// </summary>
        private static List<(uint Anchor, string Key, string StationId)> GetIncursionStationDocks(string areaResref)
        {
            var docks = new List<(uint Anchor, string Key, string StationId)>();
            foreach (var anchor in StationRules.GetAnchorsInArea(areaResref))
            {
                var station = Station.GetStationAtAnchor(anchor.Id);
                var waypoint = GetWaypointByTag(anchor.Tag);
                if (station == null || !GetIsObjectValid(waypoint))
                    continue;

                docks.Add((waypoint, IncursionRules.StationDockKey(anchor), station.Id));
            }

            return docks;
        }

        /// <summary>Spawns what the running incursion has left: each area's current wave or command group and the unbroken blockades.</summary>
        private static void SpawnIncursion()
        {
            var incursion = IncursionRecord.Active;
            if (incursion == null)
                return;

            DespawnIncursionShips();
            var region = IncursionRules.FindRegion(incursion.Center);
            if (region == null)
                return;

            foreach (var area in region.Areas)
            {
                _incursionAreaPhases[area.Resref] = IncursionAreaPhase.Idle;
                _incursionNextSpawn[area.Resref] = DateTime.UtcNow;
                if (incursion.Areas.TryGetValue(area.Resref, out var progress) && !IncursionRules.IsAreaLiberated(progress))
                    SpawnIncursionAreaGroup(incursion, area, progress);
            }

            foreach (var area in region.Areas)
            {
                foreach (var (dock, key, _) in GetIncursionDocks(area.Resref))
                {
                    if (!IncursionRules.IsBlockadeHolding(incursion, key))
                        continue;

                    var tier = area.Tier;
                    SpawnBlockade(key, () => SpawnIncursionGroup(incursion, GetArea(dock), GetPosition(dock),
                        IncursionRules.BlockadeShips(tier, incursion.Invader), IncursionTuning.BlockadeRingMeters, IncursionRoleBlockade, tier));
                }
            }

            // Player station docks (W11o) hold the same blockade at their anchor.
            ReleaseLostStationBlockades(incursion);
            foreach (var area in region.Areas)
            {
                foreach (var anchor in StationRules.GetAnchorsInArea(area.Resref))
                {
                    var key = IncursionRules.StationDockKey(anchor);
                    var waypoint = GetWaypointByTag(anchor.Tag);
                    if (!IncursionRules.IsBlockadeHolding(incursion, key) || !GetIsObjectValid(waypoint))
                        continue;

                    var tier = area.Tier;
                    SpawnBlockade(key, () => SpawnIncursionGroup(incursion, GetArea(waypoint), GetPosition(waypoint),
                        IncursionRules.BlockadeShips(tier, incursion.Invader), IncursionTuning.BlockadeRingMeters, IncursionRoleBlockade, tier));
                }
            }

            _incursionSpawned = true;
        }

        /// <summary>Spawns an area's next wave, or its command group after the last wave.</summary>
        private static void SpawnIncursionAreaGroup(ActiveIncursion incursion, IncursionArea area, IncursionAreaProgress progress)
        {
            var areaObject = Area.GetAreaByResref(area.Resref);
            if (!GetIsObjectValid(areaObject))
                return;

            var location = Walkmesh.GetRandomLocation(areaObject);
            var center = GetPositionFromLocation(location);
            var command = progress.WavesCleared >= IncursionTuning.WavesPerArea;
            var ships = command
                ? IncursionRules.CommandShips(area.Tier, incursion.Invader)
                : IncursionRules.WaveShips(area.Tier, incursion.Invader);
            var spawned = SpawnIncursionGroup(incursion, areaObject, center, ships, IncursionTuning.WaveRingMeters,
                command ? IncursionRoleCommand : IncursionRoleWave, area.Tier);

            // A group none of whose ships could be created would count as destroyed: it is tried again after the wave delay.
            if (spawned.Count == 0)
            {
                _incursionAreaShips.Remove(area.Resref);
                _incursionAreaPhases[area.Resref] = IncursionAreaPhase.Idle;
                _incursionNextSpawn[area.Resref] = DateTime.UtcNow.AddSeconds(IncursionTuning.WaveDelaySeconds);
                Log.Write(LogGroup.Error, $"Incursion: no ship of the {(command ? "command group" : "wave")} in {area.Resref} could be created; trying again.");
                return;
            }

            _incursionAreaShips[area.Resref] = spawned;
            _incursionAreaPhases[area.Resref] = command ? IncursionAreaPhase.Command : IncursionAreaPhase.Wave;

            if (command)
            {
                var name = IncursionRules.GetInvader(incursion.Invader)?.Name;
                BroadcastIncursion(ColorToken.Red($"INCURSION: the {name} command group has arrived in {area.Name}. Destroy it to liberate the system."));
            }
        }

        /// <summary>
        /// Spawns a dock's blockade. One none of whose ships could be created is tried again on later updates, so an empty
        /// blockade never counts as broken.
        /// </summary>
        private static void SpawnBlockade(string key, Func<List<uint>> spawn)
        {
            var ships = spawn();
            if (ships.Count > 0)
            {
                _incursionBlockadeShips[key] = ships;
                _incursionBlockadeRetries.Remove(key);
                return;
            }

            if (_incursionBlockadeRetries.TryAdd(key, spawn))
                Log.Write(LogGroup.Error, $"Incursion: no ship of the blockade at {key} could be created; trying again.");
        }

        private static List<uint> SpawnIncursionGroup(ActiveIncursion incursion, uint area, System.Numerics.Vector3 center,
            IReadOnlyList<(string Tag, string Hull)> ships, float radius, string role, int tier)
        {
            var spawned = new List<uint>();
            for (var index = 0; index < ships.Count; index++)
            {
                var (x, y) = SignatureSiteRules.RingOffset(index, ships.Count, radius);
                var ship = CreateObject(ObjectType.Creature, ships[index].Tag, Location(area, Vector3(center.X + x, center.Y + y, center.Z), Random.Next(360)));
                if (!GetIsObjectValid(ship))
                    continue;

                SetLocalString(ship, IncursionRoleVariable, role);
                SetLocalInt(ship, IncursionTierVariable, tier);

                spawned.Add(ship);
            }

            return spawned;
        }

        /// <summary>Moves each area on: a destroyed wave brings the next after the delay, a destroyed command group liberates the area.</summary>
        private static void UpdateIncursion(DateTime now)
        {
            var incursion = IncursionRecord.Active;
            var region = IncursionRules.FindRegion(incursion.Center);
            if (region == null)
            {
                EndIncursion(false);
                return;
            }

            var name = IncursionRules.GetInvader(incursion.Invader)?.Name;
            foreach (var area in region.Areas)
            {
                if (!incursion.Areas.TryGetValue(area.Resref, out var progress) || IncursionRules.IsAreaLiberated(progress))
                    continue;

                var alive = PruneIncursionShips(_incursionAreaShips, area.Resref);
                _incursionAreaPhases.TryGetValue(area.Resref, out var phase);
                if (alive > 0)
                    continue;

                if (phase == IncursionAreaPhase.Wave)
                {
                    progress.WavesCleared = Math.Min(IncursionTuning.WavesPerArea, progress.WavesCleared + 1);
                    _incursionAreaPhases[area.Resref] = IncursionAreaPhase.Idle;
                    _incursionNextSpawn[area.Resref] = now.AddSeconds(IncursionTuning.WaveDelaySeconds);
                    _incursionDirty = true;
                    MessageIncursionArea(area.Resref, ColorToken.Cyan(
                        $"Wave {progress.WavesCleared} of {IncursionTuning.WavesPerArea} destroyed. More of the {name} is inbound."));
                    continue;
                }

                if (phase == IncursionAreaPhase.Command)
                {
                    progress.CommandDestroyed = true;
                    _incursionAreaPhases[area.Resref] = IncursionAreaPhase.Idle;
                    _incursionDirty = true;
                    BroadcastIncursion(ColorToken.Green($"INCURSION: {area.Name} is liberated from the {name}."));
                    continue;
                }

                if (_incursionNextSpawn.TryGetValue(area.Resref, out var at) && now < at)
                    continue;

                SpawnIncursionAreaGroup(incursion, area, progress);
            }

            if (now - _incursionStationsCheckedAt >= TimeSpan.FromMinutes(1))
                ReleaseLostStationBlockades(incursion);

            foreach (var (key, spawn) in _incursionBlockadeRetries.ToList())
            {
                if (IncursionRules.IsBlockadeHolding(incursion, key))
                    SpawnBlockade(key, spawn);
                else
                    _incursionBlockadeRetries.Remove(key);
            }

            foreach (var key in _incursionBlockadeShips.Keys.ToList())
            {
                if (PruneIncursionShips(_incursionBlockadeShips, key) > 0)
                    continue;

                _incursionBlockadeShips.Remove(key);
                if (!incursion.BrokenBlockades.Contains(key))
                {
                    incursion.BrokenBlockades.Add(key);
                    _incursionDirty = true;
                    BroadcastIncursion(ColorToken.Green($"INCURSION: the blockade at {DescribeDock(key)} is broken. Its docking services are back online."));
                }
            }

            if (IncursionRules.IsCleared(incursion))
                CompleteIncursion(region);
        }

        private static int PruneIncursionShips(Dictionary<string, List<uint>> groups, string key)
        {
            if (!groups.TryGetValue(key, out var ships))
                return 0;

            ships.RemoveAll(x => !GetIsObjectValid(x) || GetIsDead(x));
            return ships.Count;
        }

        /// <summary>
        /// Lifts the blockade of a station that is gone (retrieved, lost to upkeep) so the region can still be cleared:
        /// its ships leave and its dock counts as broken, without a server-wide message.
        /// </summary>
        private static void ReleaseLostStationBlockades(ActiveIncursion incursion)
        {
            _incursionStationsCheckedAt = DateTime.UtcNow;
            foreach (var (key, stationId) in incursion.StationBlockades)
            {
                if (!IncursionRules.IsBlockadeHolding(incursion, key))
                    continue;

                var station = DB.Get<WorldProperty>(stationId);
                if (station != null && !station.IsQueuedForDeletion)
                    continue;

                if (_incursionBlockadeShips.TryGetValue(key, out var ships))
                {
                    foreach (var ship in ships.Where(GetIsObjectValid))
                        DestroyObject(ship);
                    _incursionBlockadeShips.Remove(key);
                }

                incursion.BrokenBlockades.Add(key);
                _incursionDirty = true;
                Log.Write(LogGroup.Space, $"INCURSION - station {stationId} is gone; its blockade at {key} is lifted");
            }
        }

        private static string DescribeDock(string key)
        {
            var incursion = IncursionRecord.Active;
            if (incursion != null && incursion.StationBlockades.TryGetValue(key, out var stationId))
            {
                var station = DB.Get<WorldProperty>(stationId);
                if (station != null)
                    return station.CustomName;
            }

            var waypoint = key.Contains(':') ? key[(key.IndexOf(':') + 1)..] : key;
            return waypoint.Replace("_Orbit", string.Empty).Replace("_", " ");
        }

        /// <summary>
        /// Pays every contributor and ends the incursion. Every payout is saved as pending in the same record that ends
        /// the incursion, and the players online then collect theirs: a restart in between cannot clear the incursion
        /// twice, and leaves the spoils for the next login.
        /// </summary>
        private static void CompleteIncursion(IncursionRegion region)
        {
            var state = IncursionRecord;
            var incursion = state.Active;
            var online = GetOnlinePlayersById();
            var collecting = new List<(string PlayerId, uint Player)>();
            var paid = 0;
            foreach (var (playerId, contribution) in incursion.Contributions)
            {
                if (contribution.TotalPoints() <= 0)
                    continue;

                var payout = IncursionRules.BuildPayout(contribution, region, incursion.Invader, max => Random.Next(max));
                paid++;
                CapitalBattleRules.AddPending(state.PendingPayouts, playerId, RollPulls(payout));
                if (online.TryGetValue(playerId, out var player))
                    collecting.Add((playerId, player));
            }

            var name = IncursionRules.GetInvader(incursion.Invader)?.Name;
            BroadcastIncursion(ColorToken.Green($"INCURSION: the {region.Name} region is cleared and the {name} is in retreat. {CapitalBattleRules.DescribeSpoilsShare(paid)}"));
            Log.Write(LogGroup.Space, $"INCURSION CLEARED - {incursion.Invader} in {incursion.Center}, {paid} contributors paid");
            EndIncursion(true);

            foreach (var (playerId, player) in collecting)
                CollectPendingIncursionPayout(player, playerId);
        }

        /// <summary>Ends the running incursion: cleared, or the armada withdraws when time runs out.</summary>
        private static void EndIncursion(bool cleared)
        {
            var state = IncursionRecord;
            var incursion = state.Active;
            DespawnIncursionShips();
            state.Active = null;
            SaveIncursions();

            if (!cleared && incursion != null)
            {
                var region = IncursionRules.FindRegion(incursion.Center);
                BroadcastIncursion(ColorToken.Orange(
                    $"INCURSION: the {IncursionRules.GetInvader(incursion.Invader)?.Name} withdraws from the {region?.Name} region before it could be cleared. No spoils are paid."));
                Log.Write(LogGroup.Space, $"INCURSION ENDED - {incursion.Invader} in {incursion.Center} timed out");
            }
        }

        private static void DespawnIncursionShips()
        {
            foreach (var ship in _incursionAreaShips.Values.Concat(_incursionBlockadeShips.Values).SelectMany(x => x).Where(GetIsObjectValid).ToList())
                DestroyObject(ship);

            _incursionAreaShips.Clear();
            _incursionAreaPhases.Clear();
            _incursionNextSpawn.Clear();
            _incursionBlockadeShips.Clear();
            _incursionBlockadeRetries.Clear();
            _incursionDamage.Clear();
            _incursionLastHit.Clear();
            _incursionPilotNames.Clear();
            _incursionSpawned = false;
        }

        /// <summary>The players online now (DMs left out), by persistent player id.</summary>
        private static Dictionary<string, uint> GetOnlinePlayersById()
        {
            var online = new Dictionary<string, uint>();
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!GetIsDM(player))
                    online[GetObjectUUID(player)] = player;
            }

            return online;
        }

        /// <summary>Gives a payout's credits and items; <paramref name="heading"/> names the event with the area in {0}.</summary>
        /// <summary>
        /// Gives what of the payout the character does not hold yet: the credits, then the items in order until the
        /// inventory is full. They are saved in one character save with a mark of what was given, and the number of items
        /// the character now holds is returned.
        /// </summary>
        private static int DeliverPayout(uint player, IncursionPayout payout, int creditsGiven, int itemsGiven, string variable, string heading, string logLabel)
        {
            var credits = Math.Max(0, payout.Credits - creditsGiven);
            if (credits > 0)
                GiveGoldToCreature(player, credits);

            var given = new List<string>();
            foreach (var resref in payout.Items.Skip(itemsGiven))
            {
                if (!string.IsNullOrWhiteSpace(resref) && !GetIsObjectValid(CreateItemOnObject(resref, player)))
                    break;

                given.Add(resref);
            }

            itemsGiven += given.Count;
            ObjectPlugin.SetString(player, variable, $"{Math.Max(payout.Credits, creditsGiven)}|{itemsGiven}", true);
            ExportSingleCharacter(player);

            var points = payout.Points > 0 ? $" for {payout.Points:0.#} contribution points" : string.Empty;
            if (credits > 0 || given.Count > 0)
                SendMessageToPC(player, ColorToken.Green(
                    $"{string.Format(heading, payout.RegionName)}: {credits} credits and {given.Count(x => !string.IsNullOrWhiteSpace(x))} item(s){points}."));

            var left = payout.Items.Count - itemsGiven;
            if (left > 0)
                SendMessageToPC(player, ColorToken.Red($"Your inventory is full. {left} more item(s) from these spoils wait for you: make room, and they are given at your next login."));
            Log.Write(LogGroup.Space, $"{logLabel} - {PlayerName.GetAuditName(player)} - {payout.Id} - {credits} credits, {string.Join(", ", given)}" +
                                      (left > 0 ? $" ({left} item(s) still pending)" : string.Empty));
            return itemsGiven;
        }

        /// <summary>A player who was offline when a region was cleared collects the spoils at login.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void DeliverPendingIncursionPayout()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            if (!IncursionRecord.PendingPayouts.ContainsKey(playerId))
                return;

            // The payout stays pending until delivery: a player who disconnects within the delay collects it at the next login.
            DelayCommand(6f, () => CollectPendingIncursionPayout(player, playerId));
        }

        private static void CollectPendingIncursionPayout(uint player, string playerId) =>
            CollectPendingPayout(player, IncursionRecord.PendingPayouts, playerId, "Incursion spoils for the {0} region", "INCURSION PAYOUT");

        /// <summary>
        /// Rolls a payout's loot pulls into items once, before it is saved as pending, so a payout collected again after
        /// a restart gives the same items rather than new rolls.
        /// </summary>
        private static IncursionPayout RollPulls(IncursionPayout payout)
        {
            foreach (var (table, pulls) in payout.Pulls)
            {
                var lootTable = Loot.GetLootTableByName(table);
                for (var pull = 0; pull < pulls; pull++)
                    payout.Items.Add(lootTable.GetRandomItem().Resref);
            }

            payout.Pulls.Clear();
            return payout;
        }

        /// <summary>
        /// The character-file local prefix, before a payout's id, recording what of that payout the character holds:
        /// "credits|items". It is saved with them, so a payout a restart left pending gives only what is still owed.
        /// </summary>
        private const string CollectedPayoutPrefix = "SPACE_PAYOUT_";

        /// <summary>
        /// Gives a player online what their pending payout still owes and saves the character, then clears the payout
        /// once every item is given. A restart at any point gives only what the character file does not hold yet, and
        /// items that did not fit wait in the payout for the next login.
        /// </summary>
        private static void CollectPendingPayout(uint player, Dictionary<string, IncursionPayout> pending, string playerId, string heading, string logLabel)
        {
            if (pending == null || !GetIsObjectValid(player) || !pending.TryGetValue(playerId, out var payout))
                return;

            // The id is set, and an older record's pulls rolled, and saved before anything is given, so a retry gives the same.
            if (string.IsNullOrEmpty(payout.Id) || payout.Pulls.Count > 0)
            {
                if (string.IsNullOrEmpty(payout.Id))
                    payout.Id = Guid.NewGuid().ToString();
                RollPulls(payout);
                SaveIncursions();
            }

            var variable = CollectedPayoutPrefix + payout.Id;
            var given = ObjectPlugin.GetString(player, variable).Split('|');
            var creditsGiven = given.Length == 2 && int.TryParse(given[0], out var credits) ? credits : 0;
            var itemsGiven = given.Length == 2 && int.TryParse(given[1], out var items) ? items : 0;
            itemsGiven = DeliverPayout(player, payout, creditsGiven, itemsGiven, variable, heading, logLabel);
            if (itemsGiven < payout.Items.Count)
                return;

            IncursionRules.ClaimPending(pending, playerId, true);
            SaveIncursions();
            ObjectPlugin.DeleteString(player, variable);
        }

        // Contribution

        /// <summary>The players aboard a ship: its pilot and the station crew (viewport watchers left out).</summary>
        private static List<uint> GetShipCrewPlayers(uint pilot)
        {
            var players = new List<uint> { pilot };
            players.AddRange(_crewAssignments.Where(x => x.Value.Pilot == pilot && !x.Value.IsViewer && GetIsPC(x.Key)).Select(x => x.Key));
            return players.Where(GetIsObjectValid).Distinct().ToList();
        }

        /// <summary>The pilot of the ship a player fires from: themselves, or the pilot of the ship whose station they hold.</summary>
        private static uint GetFiringShip(uint player) =>
            _crewAssignments.TryGetValue(player, out var assignment) ? assignment.Pilot : player;

        /// <summary>The players sharing a pilot's ship now (the crew while the pilot is online), or just the pilot by id and last known name.</summary>
        private static IReadOnlyList<(string Id, string Name)> GetPilotCrew(string pilotId, IReadOnlyDictionary<string, string> names)
        {
            var pilot = GetObjectByUUID(pilotId);
            if (GetIsObjectValid(pilot) && GetIsPC(pilot))
                return GetShipCrewPlayers(pilot).Select(x => (GetObjectUUID(x), GetName(x))).ToList();

            names.TryGetValue(pilotId, out var name);
            return new List<(string Id, string Name)> { (pilotId, name ?? string.Empty) };
        }

        private static IncursionContribution GetContribution(ActiveIncursion incursion, uint player)
        {
            var playerId = GetObjectUUID(player);
            if (!incursion.Contributions.TryGetValue(playerId, out var contribution))
                incursion.Contributions[playerId] = contribution = new IncursionContribution();

            contribution.Name = GetName(player);
            return contribution;
        }

        /// <summary>Called by both ship damage paths: a player ship's damage to an incursion ship counts toward its kill.</summary>
        private static void RecordIncursionDamage(uint attacker, uint target, int amount)
        {
            if (amount <= 0 || !GetIsPC(attacker))
                return;

            // A capital battle ship (W11p) is tracked apart from the incursion's ships, so neither event clears the other's book.
            if (_capitalBattleArea != null && GetLocalInt(target, CapitalBattleVariable) == 1)
            {
                TrackShipDamage(_capitalBattleDamage, _capitalBattleLastHit, _capitalBattlePilotNames, attacker, target, amount);
                return;
            }

            if (IncursionRecord.Active == null || string.IsNullOrEmpty(GetLocalString(target, IncursionRoleVariable)))
                return;

            TrackShipDamage(_incursionDamage, _incursionLastHit, _incursionPilotNames, attacker, target, amount);
        }

        private static void TrackShipDamage(Dictionary<uint, Dictionary<string, double>> damageByShip, Dictionary<uint, string> lastHit,
            Dictionary<string, string> pilotNames, uint attacker, uint target, int amount)
        {
            var ship = GetFiringShip(attacker);
            var pilotId = GetObjectUUID(ship);
            pilotNames[pilotId] = GetName(ship);
            if (!damageByShip.TryGetValue(target, out var damage))
                damageByShip[target] = damage = new Dictionary<string, double>();

            damage.TryGetValue(pilotId, out var dealt);
            damage[pilotId] = dealt + amount;
            lastHit[target] = pilotId;
        }

        /// <summary>A destroyed incursion ship's points go to the ships that damaged it, shared by the players aboard each.</summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void CreditIncursionKill()
        {
            var ship = OBJECT_SELF;
            if (GetLocalInt(ship, CapitalBattleVariable) == 1)
            {
                CreditCapitalBattleKill(ship);
                return;
            }

            var incursion = IncursionRecord.Active;
            if (incursion == null || string.IsNullOrEmpty(GetLocalString(ship, IncursionRoleVariable)))
                return;

            _incursionDamage.Remove(ship, out var damage);
            _incursionLastHit.Remove(ship, out var killer);
            if (damage == null)
                return;

            var tier = GetLocalInt(ship, IncursionTierVariable);
            var points = IncursionRules.KillPoints(_npcPresets.TryGetValue(GetTag(ship), out var preset) ? preset.Difficulty : "Normal");
            // A pilot still online shares the points with the crew aboard; one who logged out is credited by id and
            // collects a pending payout when the region clears.
            IncursionRules.CreditKill(incursion, tier, damage, points, killer, pilotId => GetPilotCrew(pilotId, _incursionPilotNames));

            _incursionDirty = true;
        }

        /// <summary>Called when a module repairs a ship: repairing an ally in an incursion area counts.</summary>
        private static void RecordIncursionRepair(uint source, uint repaired, int restored)
        {
            var incursion = IncursionRecord.Active;
            if (incursion == null || restored <= 0 || !GetIsPC(source) || !GetIsPC(repaired))
                return;

            var ship = GetFiringShip(source);
            if (ship == repaired)
                return;

            var area = IncursionRules.FindArea(incursion, GetResRef(GetArea(ship)));
            if (area == null)
                return;

            var aboard = GetShipCrewPlayers(ship);
            var points = IncursionRules.RepairPoints(restored, area.Tier);
            foreach (var player in aboard)
            {
                var contribution = GetContribution(incursion, player);
                IncursionRules.AddPoints(contribution, area.Tier, points / aboard.Count);
                contribution.Repairs += restored / aboard.Count;
            }

            _incursionDirty = true;
        }

        // Docks, trade and status

        /// <summary>Why a dock refuses docking, or empty: a blockade holding at it.</summary>
        public static string GetIncursionDockRefusal(uint dock)
        {
            var incursion = IncursionRecord.Active;
            if (incursion == null)
                return string.Empty;

            var key = IncursionRules.DockKey(GetResRef(GetArea(dock)), GetLocalString(dock, DockTeleportWaypointVariable));
            return IncursionRules.IsBlockadeHolding(incursion, key) ? IncursionRules.DockRefusal(incursion.Invader) : string.Empty;
        }

        /// <summary>Why an exchange is closed, or empty.</summary>
        public static string GetIncursionExchangeRefusal(TradeExchangeType exchange)
        {
            var incursion = IncursionRecord.Active;
            return IncursionRules.IsExchangeClosed(incursion, exchange) ? IncursionRules.ExchangeRefusal(incursion.Invader) : string.Empty;
        }

        /// <summary>
        /// Why a dock's Trade Exchange is closed, or empty: its exchange's planet dock is blockaded, or (W11o) the dock is a
        /// player station's Starport and a blockade holds at the station.
        /// </summary>
        public static string GetIncursionExchangeRefusal(TradeDock dock)
        {
            if (dock == null)
                return string.Empty;

            var incursion = IncursionRecord.Active;
            if (dock.IsCity && IncursionRules.IsStationBlockaded(incursion, dock.CityId))
                return IncursionRules.ExchangeRefusal(incursion.Invader);

            return GetIncursionExchangeRefusal(dock.Exchange);
        }

        /// <summary>Whether a blockade holds at a player station's dock (W11o).</summary>
        public static bool IsStationBlockaded(string stationId) =>
            IncursionRules.IsStationBlockaded(IncursionRecord.Active, stationId);

        /// <summary>The incursion price multiplier of an exchange (1 outside a running incursion's region).</summary>
        public static double GetIncursionPriceMultiplier(TradeExchangeType exchange) =>
            IncursionRules.GetPriceMultiplier(IncursionRecord.Active, exchange);

        /// <summary>The Ship HUD's incursion line for a pilot in an incursion area, or null.</summary>
        public static string GetIncursionStatusText(uint player)
        {
            var incursion = IncursionRecord.Active;
            var area = IncursionRules.FindArea(incursion, GetResRef(GetArea(player)));
            if (area == null)
                return null;

            incursion.Contributions.TryGetValue(GetObjectUUID(player), out var contribution);
            var progress = incursion.Areas.TryGetValue(area.Resref, out var p) ? p : new IncursionAreaProgress();
            var stage = IncursionRules.IsAreaLiberated(progress)
                ? "liberated"
                : progress.WavesCleared >= IncursionTuning.WavesPerArea ? "command group" : $"wave {progress.WavesCleared + 1}/{IncursionTuning.WavesPerArea}";
            return $"{stage}, {IncursionRules.FormatRemaining(incursion.EndsAt - DateTime.UtcNow)} left, {contribution?.TotalPoints() ?? 0:0.#} pts";
        }

        /// <summary>The /incursion report.</summary>
        public static string DescribeIncursion(uint player)
        {
            var state = IncursionRecord;
            var incursion = state.Active;
            if (incursion == null)
                return $"No incursion is under way. The next armada is expected {state.NextStartAt:dddd d MMMM, HH:mm} UTC.";

            var region = IncursionRules.FindRegion(incursion.Center);
            var lines = new List<string>
            {
                $"The {IncursionRules.GetInvader(incursion.Invader)?.Name} holds the {region?.Name} region: {IncursionRules.FormatRemaining(incursion.EndsAt - DateTime.UtcNow)} left. " +
                $"Prices at the region's exchanges are x{IncursionTuning.PriceMultiplier:0.##}.",
            };
            foreach (var area in region?.Areas ?? Array.Empty<IncursionArea>())
            {
                var progress = incursion.Areas.TryGetValue(area.Resref, out var p) ? p : new IncursionAreaProgress();
                var stage = IncursionRules.IsAreaLiberated(progress)
                    ? "liberated"
                    : progress.WavesCleared >= IncursionTuning.WavesPerArea ? "command group" : $"wave {progress.WavesCleared + 1} of {IncursionTuning.WavesPerArea}";
                lines.Add($"  {area.Name} (tier {area.Tier}): {stage}");
            }

            var held = incursion.Blockades.Keys.Count(x => !incursion.BrokenBlockades.Contains(x));
            lines.Add($"  Blockades holding: {held} of {incursion.Blockades.Count}");
            incursion.Contributions.TryGetValue(GetObjectUUID(player), out var contribution);
            lines.Add(contribution == null
                ? "  You have not contributed yet."
                : $"  Your contribution: {contribution.TotalPoints():0.#} points ({contribution.Kills} killing blows, {contribution.Damage} damage, {contribution.Repairs} repaired).");
            return string.Join("\n", lines);
        }

        private static void BroadcastIncursion(string message)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                SendMessageToPC(player, message);
        }

        private static void MessageIncursionArea(string areaResref, string message)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetResRef(GetArea(player)) == areaResref)
                    SendMessageToPC(player, message);
            }
        }
    }
}
