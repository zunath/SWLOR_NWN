using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The station-only buildings in the world (plan section 25): their consoles and scenery, their saved state, the
    /// Shipyard gate and fee, the Asteroid Drill's ore, droids assigned to them, and Jump Beacon jumps.
    /// Rules: <see cref="StationBuildingRules"/>.
    /// </summary>
    public static class StationBuildings
    {
        /// <summary>The tag every Shipyard console carries, so a crafter's nearest one is found by tag.</summary>
        public const string ShipyardTag = "STATION_SHIPYARD";

        private const string ModelVariable = "STATION_BUILDING_MODEL";

        /// <summary>
        /// A droid controller's marks, saved in its owner's character file while it moves to or from a building:
        /// "building id|assignment id". The next login settles a move a restart interrupted.
        /// </summary>
        private const string AssigningVariable = "STATION_DROID_ASSIGNING";
        private const string RecalledVariable = "STATION_DROID_RECALLED";

        // State

        /// <summary>A building's saved state; a building without one gets the default settings, unsaved.</summary>
        public static StationBuildingState GetState(WorldProperty building)
        {
            return DB.Get<StationBuildingState>(StationBuildingState.KeyFor(building.Id)) ?? new StationBuildingState
            {
                Id = StationBuildingState.KeyFor(building.Id),
                BuildingId = building.Id,
                StationId = building.ParentPropertyId,
                StructureType = building.StructureType,
            };
        }

        /// <summary>The structure property behind a building's console, or null.</summary>
        public static WorldProperty GetBuildingOfConsole(uint console)
        {
            var propertyId = Property.GetPropertyId(console);
            return string.IsNullOrWhiteSpace(propertyId) ? null : DB.Get<WorldProperty>(propertyId);
        }

        /// <summary>The station a building stands on, or null.</summary>
        public static WorldProperty GetStation(WorldProperty building)
        {
            var station = building == null || string.IsNullOrWhiteSpace(building.ParentPropertyId)
                ? null
                : DB.Get<WorldProperty>(building.ParentPropertyId);
            return Station.IsStation(station) ? station : null;
        }

        // Spawning and removal

        /// <summary>
        /// A building's console was placed or moved: it gets the building's name and use script, and the building's scenery
        /// model stands behind it. The first spawn saves the building's state with its default settings.
        /// </summary>
        public static void OnPositionChanged(WorldProperty building, uint console)
        {
            var detail = StationBuildingRules.GetBuilding(building.StructureType);
            if (detail == null)
                return;

            SetName(console, detail.Name);
            SetUseableFlag(console, true);
            SetEventScript(console, EventScript.Placeable_OnUsed, ScriptName.OnStationBuildingUsed);
            if (building.StructureType == StructureType.Shipyard)
                SetTag(console, ShipyardTag);

            DestroyModel(console);
            var location = GetLocation(console);
            var position = GetPositionFromLocation(location);
            var modelLocation = Location(GetAreaFromLocation(location),
                Vector3(position.X, position.Y + StationTuning.BuildingModelOffset, position.Z),
                GetFacingFromLocation(location));
            var model = CreateObject(ObjectType.Placeable, detail.ModelResref, modelLocation);
            SetName(model, detail.Name);
            SetPlotFlag(model, true);
            SetUseableFlag(model, false);
            SetLocalObject(console, ModelVariable, model);

            if (!DB.Exists<StationBuildingState>(StationBuildingState.KeyFor(building.Id)))
            {
                // A new drill starts mining at once.
                var state = GetState(building);
                state.DrillLastAccrual = DateTime.UtcNow;
                DB.Set(state);
            }
        }

        /// <summary>A building was retrieved: its scenery goes with it. Its state is cleared when the structure is deleted.</summary>
        public static void OnRetrieved(WorldProperty building, uint console)
        {
            DestroyModel(console);
        }

        /// <summary>
        /// A building's structure is being deleted (retrieved, or its station lost): an assigned droid goes back to its owner
        /// through the overflow store (/overflow), and the stored ore and settings go with the building.
        /// </summary>
        public static void OnDeleted(WorldProperty building)
        {
            var state = DB.Get<StationBuildingState>(StationBuildingState.KeyFor(building.Id));
            if (state == null)
                return;

            if (!string.IsNullOrWhiteSpace(state.SerializedDroid))
                ReturnDroidToOverflow(state);

            DB.Delete<StationBuildingState>(state.Id);
        }

        private static void DestroyModel(uint console)
        {
            var model = GetLocalObject(console, ModelVariable);
            if (GetIsObjectValid(model))
                DestroyObject(model);
            DeleteLocalObject(console, ModelVariable);
        }

        /// <summary>A building's console was used: open its menu.</summary>
        [NWNEventHandler(ScriptName.OnStationBuildingUsed)]
        public static void UseConsole()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDMPossessed(player))
                return;

            ConversationMenu.Start(player, OBJECT_SELF, nameof(StationBuildingDialog));
        }

        // Permissions and citizenship

        public static bool IsCitizen(uint player, WorldProperty station)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            return dbPlayer != null && station != null && dbPlayer.CitizenPropertyId == station.Id;
        }

        /// <summary>Whether the player is a citizen of one of the stations the beacon lists as allies.</summary>
        public static bool IsAllyCitizen(uint player, StationBuildingState beacon)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            return dbPlayer != null &&
                   !string.IsNullOrWhiteSpace(dbPlayer.CitizenPropertyId) &&
                   beacon.BeaconAllyStationIds.Contains(dbPlayer.CitizenPropertyId);
        }

        // The Shipyard

        /// <summary>The nearest Shipyard console within range of the player, or OBJECT_INVALID.</summary>
        public static uint GetNearbyShipyard(uint player)
        {
            var console = GetNearestObjectByTag(ShipyardTag, player);
            if (!GetIsObjectValid(console) ||
                GetArea(console) != GetArea(player) ||
                GetDistanceBetween(console, player) > StationTuning.ShipyardRange)
                return OBJECT_INVALID;

            return console;
        }

        /// <summary>Why the player cannot build a Capital hull where they stand, or an empty string.</summary>
        public static string GetShipyardError(uint player)
        {
            var console = GetNearbyShipyard(player);
            var building = GetIsObjectValid(console) ? GetBuildingOfConsole(console) : null;
            var station = GetStation(building);
            if (station == null)
                return StationBuildingRules.DescribeShipyardRefusal(ShipyardRefusal.NotAtShipyard);

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            var refusal = StationBuildingRules.GetShipyardRefusal(
                true,
                GetState(building).ShipyardAccess,
                IsCitizen(player, station),
                dbPlayer?.PropertyOwedTaxes ?? 0);
            return StationBuildingRules.DescribeShipyardRefusal(refusal);
        }

        /// <summary>The station whose Shipyard the player stands at, or null.</summary>
        public static WorldProperty GetShipyardStation(uint player)
        {
            var console = GetNearbyShipyard(player);
            return GetIsObjectValid(console) ? GetStation(GetBuildingOfConsole(console)) : null;
        }

        /// <summary>The station's Shipyard fee per Capital hull.</summary>
        public static int GetShipyardFee(WorldProperty station)
        {
            return station != null && station.Taxes.TryGetValue(PropertyTaxType.Shipyard, out var fee)
                ? StationBuildingRules.ClampShipyardFee(fee)
                : 0;
        }

        /// <summary>The build speed bonus of the droid assigned to the Shipyard the player stands at, in percent.</summary>
        public static int GetShipyardSpeedPercent(uint player)
        {
            var console = GetNearbyShipyard(player);
            var building = GetIsObjectValid(console) ? GetBuildingOfConsole(console) : null;
            if (building == null)
                return 0;

            return GetDroidPercent(GetState(building));
        }

        /// <summary>
        /// What the building's assigned droid adds, in percent (<see cref="StationDroidRules"/>): the Shipyard's build speed
        /// or the Asteroid Drill's output; 0 without a droid.
        /// </summary>
        public static int GetDroidPercent(StationBuildingState state)
        {
            if (string.IsNullOrWhiteSpace(state.SerializedDroid))
                return 0;

            return state.StructureType == StructureType.Shipyard
                ? StationDroidRules.ShipyardBuildSpeedPercent(state.DroidIndustry)
                : StationDroidRules.DrillOutputPercent(state.DroidIndustry, state.DroidEfficiencyPercent);
        }

        /// <summary>
        /// Takes the station's Shipyard fee from the player. The treasury is credited later (<see cref="CreditShipyardFee"/>), once the
        /// character file holds the payment. Returns the fee taken: 0 when there is none or the player cannot pay.
        /// </summary>
        public static int TakeShipyardFee(uint player, WorldProperty station)
        {
            var fee = GetShipyardFee(station);
            if (fee <= 0 || GetGold(player) < fee)
                return 0;

            AssignCommand(player, () => TakeGoldFromCreature(fee, player, true));
            SendMessageToPC(player, $"Paid the {station.CustomName} Shipyard fee of {fee} credits.");
            return fee;
        }

        /// <summary>
        /// Credits a Shipyard fee to the station's treasury once: the fee's id is recorded in the same save as the credit, so a second
        /// call (a retry after a crash) does nothing. A station that no longer exists takes nothing.
        /// </summary>
        public static void CreditShipyardFee(string propertyId, string feeId, int fee, string payer)
        {
            var dbStation = DB.Get<WorldProperty>(propertyId);
            if (dbStation == null || fee <= 0)
                return;

            dbStation.PaidCraftFees ??= new HashSet<string>();
            if (!dbStation.PaidCraftFees.Add(feeId))
                return;

            dbStation.Treasury += fee;
            DB.Set(dbStation);
            Log.Write(LogGroup.Property, $"{payer} paid a Shipyard fee of {fee} credits to station '{dbStation.CustomName}' ({dbStation.Id}).");
        }

        // The Asteroid Drill

        /// <summary>
        /// Adds the ore the drill mined since it was last read, rolled from its anchor area's asteroid tables, and saves it.
        /// </summary>
        public static void AccrueDrill(WorldProperty station, StationBuildingState state)
        {
            var anchor = Station.GetAnchor(station);
            if (anchor == null)
                return;

            var stored = state.DrillStorage.Values.Sum();
            var (units, next) = StationBuildingRules.AccrueDrill(stored, state.DrillLastAccrual, DateTime.UtcNow, GetDroidPercent(state));
            state.DrillLastAccrual = next;

            var totalWeight = StationBuildingRules.GetDrillTotalWeight(anchor.AreaResref);
            for (var count = 0; count < units && totalWeight > 0; count++)
            {
                var tableName = StationBuildingRules.PickDrillTable(anchor.AreaResref, Random.Next(totalWeight));
                var table = string.IsNullOrWhiteSpace(tableName) ? null : Loot.GetLootTableByName(tableName);
                var resref = table?.GetRandomItem()?.Resref;
                if (string.IsNullOrWhiteSpace(resref))
                    continue;

                state.DrillStorage[resref] = state.DrillStorage.GetValueOrDefault(resref) + 1;
            }

            DB.Set(state);
        }

        /// <summary>Gives the player the drill's stored ore. Stops when their inventory is full and keeps the rest.</summary>
        public static void CollectDrill(uint player, StationBuildingState state)
        {
            var collected = new List<string>();
            foreach (var (resref, units) in state.DrillStorage.ToList())
            {
                var given = 0;
                for (; given < units; given++)
                {
                    var item = CreateItemOnObject(resref, player);
                    if (!GetIsObjectValid(item))
                        break;
                }

                if (given > 0)
                    collected.Add($"{given}x {Cache.GetItemNameByResref(resref)}");

                if (given >= units)
                    state.DrillStorage.Remove(resref);
                else
                {
                    state.DrillStorage[resref] = units - given;
                    break;
                }
            }

            // The ore is saved with the character before the drill's stock is, so the window in which a restart could
            // let it be collected twice is as short as it can be.
            if (collected.Count > 0)
                ExportSingleCharacter(player);

            DB.Set(state);
            SendMessageToPC(player, collected.Count > 0
                ? ColorToken.Green($"Collected from the Asteroid Drill: {string.Join(", ", collected)}.")
                : ColorToken.Red("The Asteroid Drill has no ore to collect."));
        }

        // Droids

        /// <summary>A droid controller's Industry rating and Efficiency Protocol percent (plan section 27).</summary>
        public static (int Industry, int EfficiencyPercent) GetDroidIndustry(uint controller)
        {
            var details = Droid.LoadDroidItemPropertyDetails(controller);
            var protocols = DroidIndustryRules.SelectProtocols(details.Perks);
            return (Math.Max(0, details.Industry), DroidIndustryRules.GetProtocolValue(protocols, PerkType.EfficiencyProtocol));
        }

        /// <summary>Assigns one of the player's droids to the building: its controller is kept with the building.</summary>
        public static bool AssignDroid(uint player, WorldProperty building, uint controller)
        {
            var state = GetState(building);
            var detail = StationBuildingRules.GetBuilding(building.StructureType);
            if (detail == null || !StationDroidRules.TakesDroid(detail.Slot))
                return false;

            if (!string.IsNullOrWhiteSpace(state.SerializedDroid))
            {
                SendMessageToPC(player, ColorToken.Red("A droid is already assigned here."));
                return false;
            }

            if (!GetIsObjectValid(controller) || GetItemPossessor(controller) != player || !Space.GetPostableDroids(player).Contains(controller))
            {
                SendMessageToPC(player, ColorToken.Red("You no longer carry that droid, or it is busy."));
                return false;
            }

            // The controller is marked and saved with the character before the building takes it, so a restart in between
            // leaves a marked controller the next login settles: it stays if the building never saved the droid, and goes
            // if the building did.
            var serialized = ObjectPlugin.Serialize(controller);
            var assignmentId = Guid.NewGuid().ToString();
            SetLocalString(controller, AssigningVariable, $"{building.Id}|{assignmentId}");
            ExportSingleCharacter(player);

            state.DroidName = Droid.GetDroidName(controller);
            (state.DroidIndustry, state.DroidEfficiencyPercent) = GetDroidIndustry(controller);
            state.DroidOwnerPlayerId = GetObjectUUID(player);
            state.SerializedDroid = serialized;
            state.DroidAssignmentId = assignmentId;
            DB.Set(state);
            DestroyObject(controller);

            SendMessageToPC(player, ColorToken.Green($"{state.DroidName} now works the {detail.Name}."));
            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} assigned droid '{state.DroidName}' to building {building.Id} on station {building.ParentPropertyId}.");
            return true;
        }

        /// <summary>
        /// Recalls the building's droid. Its owner gets the controller back at once; anyone else who may manage the station's
        /// structures sends it to its owner's overflow store.
        /// </summary>
        public static void RecallDroid(uint player, WorldProperty building)
        {
            var state = GetState(building);
            if (string.IsNullOrWhiteSpace(state.SerializedDroid))
                return;

            if (GetObjectUUID(player) != state.DroidOwnerPlayerId)
            {
                ReturnDroidToOverflow(state);
                DB.Set(state);
                SendMessageToPC(player, $"{state.DroidName} was sent back to its owner.");
                return;
            }

            var item = ObjectPlugin.Deserialize(state.SerializedDroid);
            if (!Item.TryAcquireItem(player, item))
            {
                SendMessageToPC(player, ColorToken.Red(Item.InventoryFullShortMessage));
                return;
            }

            // The returned controller is marked and saved with the character before the building lets the droid go, so
            // a restart in between leaves a marked controller the next login settles by clearing the building.
            if (!string.IsNullOrEmpty(state.DroidAssignmentId))
                SetLocalString(item, RecalledVariable, $"{building.Id}|{state.DroidAssignmentId}");
            ExportSingleCharacter(player);

            SendMessageToPC(player, ColorToken.Green($"{state.DroidName} returns to you."));
            ClearDroid(state);
            DB.Set(state);
            DeleteLocalString(item, RecalledVariable);
        }

        /// <summary>
        /// Settles a droid move a restart interrupted, from the marks on the player's controllers: a controller being
        /// assigned goes if its building saved the droid and stays otherwise; a recalled controller's building lets the
        /// droid go if it still holds it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void SettleDroidMoves()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            foreach (var controller in Droid.GetControllers(player))
            {
                if (TryReadMark(controller, AssigningVariable, out var state, out var assignmentId))
                {
                    DeleteLocalString(controller, AssigningVariable);
                    if (state != null && state.DroidAssignmentId == assignmentId && !string.IsNullOrWhiteSpace(state.SerializedDroid))
                    {
                        DestroyObject(controller);
                        Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} - removed droid controller already assigned to building {state.BuildingId} after a restart.");
                    }
                }

                if (TryReadMark(controller, RecalledVariable, out state, out assignmentId))
                {
                    DeleteLocalString(controller, RecalledVariable);
                    if (state != null && state.DroidAssignmentId == assignmentId && !string.IsNullOrWhiteSpace(state.SerializedDroid))
                    {
                        ClearDroid(state);
                        DB.Set(state);
                        Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} - cleared recalled droid from building {state.BuildingId} after a restart.");
                    }
                }
            }
        }

        private static bool TryReadMark(uint controller, string variable, out StationBuildingState state, out string assignmentId)
        {
            state = null;
            assignmentId = string.Empty;
            var mark = GetLocalString(controller, variable).Split('|');
            if (mark.Length != 2)
                return false;

            state = DB.Get<StationBuildingState>(StationBuildingState.KeyFor(mark[0]));
            assignmentId = mark[1];
            return true;
        }

        /// <summary>
        /// Sends the building's droid to its owner's overflow store. The overflow record's id comes from the assignment,
        /// so a return repeated after a restart writes the same record, and the next module load clears a building whose
        /// droid's record is already written.
        /// </summary>
        private static void ReturnDroidToOverflow(StationBuildingState state)
        {
            if (string.IsNullOrEmpty(state.DroidAssignmentId))
            {
                state.DroidAssignmentId = Guid.NewGuid().ToString();
                DB.Set(state);
            }

            DB.Set(new OverflowItem
            {
                Id = OverflowDroidId(state.DroidAssignmentId),
                PlayerId = state.DroidOwnerPlayerId,
                Name = state.DroidName,
                Resref = Droid.DroidControlItemResref,
                Tag = Droid.DroidControlItemResref,
                Quantity = 1,
                Data = state.SerializedDroid,
            });
            Log.Write(LogGroup.Property, $"Droid '{state.DroidName}' from station building {state.Id} was returned to player {state.DroidOwnerPlayerId}'s overflow store.");
            ClearDroid(state);
        }

        private static string OverflowDroidId(string assignmentId) => "station-droid:" + assignmentId;

        /// <summary>Clears any building whose droid a restart left both there and in its owner's overflow store.</summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void FinishDroidReturns()
        {
            var count = (int)DB.SearchCount(new DBQuery<StationBuildingState>());
            if (count <= 0)
                return;

            foreach (var state in DB.Search(new DBQuery<StationBuildingState>().AddPaging(count, 0)))
            {
                if (string.IsNullOrWhiteSpace(state.SerializedDroid) || string.IsNullOrEmpty(state.DroidAssignmentId) ||
                    DB.Get<OverflowItem>(OverflowDroidId(state.DroidAssignmentId)) == null)
                    continue;

                ClearDroid(state);
                DB.Set(state);
                Log.Write(LogGroup.Property, $"Cleared the droid of station building {state.BuildingId}, already in its owner's overflow store, after a restart.");
            }
        }

        private static void ClearDroid(StationBuildingState state)
        {
            state.SerializedDroid = string.Empty;
            state.DroidOwnerPlayerId = string.Empty;
            state.DroidName = string.Empty;
            state.DroidIndustry = 0;
            state.DroidEfficiencyPercent = 0;
            state.DroidAssignmentId = string.Empty;
        }

        // The Jump Beacon

        /// <summary>A station's Jump Beacon level (1-3), limited by its city level like every city upgrade.</summary>
        public static int GetBeaconLevel(WorldProperty station)
        {
            if (station == null)
                return 1;

            var level = station.Upgrades.GetValueOrDefault(PropertyUpgradeType.JumpBeaconLevel, 1);
            var cityLevel = station.Upgrades.GetValueOrDefault(PropertyUpgradeType.CityLevel, 1);
            return StationBuildingRules.ClampBeaconLevel(Math.Min(level, cityLevel));
        }

        /// <summary>The station's Jump Beacon structure, or null.</summary>
        public static WorldProperty GetBeaconBuilding(string stationId)
        {
            return DB.Search(new DBQuery<WorldProperty>()
                    .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), stationId, false)
                    .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure)
                    .AddFieldSearch(nameof(WorldProperty.StructureType), (int)StructureType.JumpBeacon))
                .FirstOrDefault(x => !x.IsQueuedForDeletion);
        }

        /// <summary>The interiors of a station's Starports: where its ships are docked.</summary>
        public static List<string> GetStarportInteriorIds(string stationId)
        {
            return DB.Search(new DBQuery<WorldProperty>()
                    .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), stationId, false)
                    .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure)
                    .AddFieldSearch(nameof(WorldProperty.StructureType), (int)StructureType.Starport))
                .SelectMany(x => x.ChildPropertyIds.GetValueOrDefault(PropertyChildType.Interior) ?? new List<string>())
                .ToList();
        }

        private static PlayerShip GetShipRecord(string shipPropertyId)
        {
            return DB.Search(new DBQuery<PlayerShip>()
                    .AddFieldSearch(nameof(PlayerShip.PropertyId), shipPropertyId, false))
                .FirstOrDefault();
        }

        /// <summary>Whether the ship is docked at one of these Starports, with nobody flying it.</summary>
        private static bool IsDockedAt(WorldProperty ship, PlayerShip record, ICollection<string> starportInteriorIds)
        {
            return record != null &&
                   string.IsNullOrWhiteSpace(record.DockedToShipId) &&
                   !ship.Positions.ContainsKey(PropertyLocationType.CurrentPosition) &&
                   ship.Positions.TryGetValue(PropertyLocationType.DockPosition, out var dock) &&
                   string.IsNullOrWhiteSpace(dock.AreaResref) &&
                   starportInteriorIds.Contains(dock.InstancePropertyId);
        }

        /// <summary>The ships docked at the station that the player owns or may pilot.</summary>
        public static List<(WorldProperty Ship, PlayerShip Record)> GetJumpableShips(uint player, WorldProperty station)
        {
            var interiors = GetStarportInteriorIds(station.Id);
            var ships = new List<(WorldProperty Ship, PlayerShip Record)>();
            foreach (var interiorId in interiors)
            {
                var interior = DB.Get<WorldProperty>(interiorId);
                foreach (var shipId in interior?.ChildPropertyIds.GetValueOrDefault(PropertyChildType.Starship) ?? new List<string>())
                {
                    var ship = DB.Get<WorldProperty>(shipId);
                    var record = ship == null ? null : GetShipRecord(ship.Id);
                    if (ship == null || !IsDockedAt(ship, record, interiors) ||
                        !Property.HasPropertyPermission(player, ship.Id, PropertyPermissionType.PilotShip))
                        continue;

                    ships.Add((ship, record));
                }
            }

            return ships;
        }

        /// <summary>
        /// Every other station with a Jump Beacon the player may jump to: never in Wild Space, and only while its Starport is
        /// loaded to take the ship.
        /// </summary>
        public static List<WorldProperty> GetDestinations(uint player, WorldProperty origin)
        {
            var beacons = DB.Search(new DBQuery<StationBuildingState>()
                .AddFieldSearch(nameof(StationBuildingState.StructureType), (int)StructureType.JumpBeacon)
                .AddPaging(500, 0));

            var destinations = new List<WorldProperty>();
            foreach (var beacon in beacons)
            {
                if (beacon.StationId == origin.Id || !DB.Exists<WorldProperty>(beacon.BuildingId))
                    continue;

                var station = DB.Get<WorldProperty>(beacon.StationId);
                var anchor = Station.GetAnchor(station);
                if (anchor == null || anchor.IsWildSpace || station.IsQueuedForDeletion ||
                    Station.GetDockPoints(station.Id).Count == 0 ||
                    !StationBuildingRules.CanUseBeacon(beacon.BeaconAccess, IsCitizen(player, station), IsAllyCitizen(player, beacon)))
                    continue;

                destinations.Add(station);
            }

            return destinations.OrderBy(x => x.CustomName).ToList();
        }

        /// <summary>What the jump rules need to know about a jump of this ship from one station's beacon to another's.</summary>
        public static JumpRequest BuildJumpRequest(uint player, WorldProperty origin, WorldProperty destination, WorldProperty ship, PlayerShip record)
        {
            var originBeacon = GetBeaconBuilding(origin.Id);
            var destinationBeacon = destination == null ? null : GetBeaconBuilding(destination.Id);
            var originState = originBeacon == null ? null : GetState(originBeacon);
            var destinationState = destinationBeacon == null ? null : GetState(destinationBeacon);
            var originAnchor = Station.GetAnchor(origin);
            var destinationAnchor = destination == null ? null : Station.GetAnchor(destination);
            return new JumpRequest
            {
                ShipDocked = ship != null && IsDockedAt(ship, record, GetStarportInteriorIds(origin.Id)),
                CanPilot = ship != null && Property.HasPropertyPermission(player, ship.Id, PropertyPermissionType.PilotShip),
                OriginInWildSpace = Station.GetAnchor(origin)?.IsWildSpace ?? true,
                DestinationInWildSpace = Station.GetAnchor(destination)?.IsWildSpace ?? true,
                SameStation = destination != null && destination.Id == origin.Id,
                OriginPermits = originState != null &&
                                StationBuildingRules.CanUseBeacon(originState.BeaconAccess, IsCitizen(player, origin), IsAllyCitizen(player, originState)),
                DestinationPermits = destinationState != null &&
                                     StationBuildingRules.CanUseBeacon(destinationState.BeaconAccess, IsCitizen(player, destination), IsAllyCitizen(player, destinationState)),
                OriginBeaconLevel = GetBeaconLevel(origin),
                DestinationBeaconLevel = GetBeaconLevel(destination),
                ShipSize = record == null ? ShipSize.Invalid : Space.GetShipSize(record),
                HullTag = record?.Status?.ItemTag ?? string.Empty,
                FuelAboard = record == null ? 0 : CargoHoldRules.GetUnits(record.Cargo, CommodityType.StationFuel),
                CargoValue = record == null ? 0 : StationBuildingRules.GetJumpCargoValue(record, Space.GetDockedShips(record.Id)),
                Hops = originAnchor == null || destinationAnchor == null
                    ? 0
                    : TradeLookup.GetHops(originAnchor.Exchange, destinationAnchor.Exchange),
                DestinationBlockaded = destination != null && Space.IsStationBlockaded(destination.Id),
            };
        }

        /// <summary>
        /// Jumps a ship docked at <paramref name="origin"/> to a Starport of <paramref name="destination"/>: it burns the
        /// Station Fuel, moves the ship's dock and launch point to the destination, and takes the player along to its Starport.
        /// </summary>
        public static bool Jump(uint player, WorldProperty origin, WorldProperty destination, string shipPropertyId)
        {
            var ship = DB.Get<WorldProperty>(shipPropertyId);
            var record = ship == null ? null : GetShipRecord(ship.Id);
            var request = BuildJumpRequest(player, origin, destination, ship, record);
            var refusal = StationBuildingRules.GetJumpRefusal(request);
            if (refusal != JumpRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(StationBuildingRules.DescribeJumpRefusal(refusal, request)));
                return false;
            }

            // The size rules of the destination's Starports (a Capital hull needs a level 2 starport) still apply.
            ShipDockPoint dock = null;
            var starportRefusal = StarportDockRefusal.None;
            foreach (var (starportId, point) in Station.GetDockPoints(destination.Id))
            {
                var landingArea = GetAreaFromLocation(point.Location);
                starportRefusal = Space.GetStarportRefusal(record, landingArea, starportId);
                if (starportRefusal == StarportDockRefusal.None &&
                    Property.GetPropertyLoadState(starportId) == PropertyLoadState.Loaded)
                {
                    dock = point;
                    break;
                }
            }

            var anchor = Station.GetAnchor(destination);
            var anchorWaypoint = GetWaypointByTag(anchor.Tag);
            if (dock == null || !GetIsObjectValid(anchorWaypoint))
            {
                var message = starportRefusal != StarportDockRefusal.None
                    ? ShipAccessRules.DescribeStarportRefusal(starportRefusal, ShipSize.Invalid)
                    : "That station's Starport is not ready to take ships. Please try again shortly.";
                SendMessageToPC(player, ColorToken.Red(message));
                return false;
            }

            // The jump's destination is saved with the fuel it burns, so a restart before the ship moves finishes the jump
            // at the next module load rather than losing the fuel.
            var fuel = StationBuildingRules.GetJumpFuel(request);
            var dockPosition = GetPositionFromLocation(dock.Location);
            var spacePosition = GetPosition(anchorWaypoint);
            record.PendingJumpDock = new PropertyLocation
            {
                InstancePropertyId = dock.PropertyId,
                X = dockPosition.X,
                Y = dockPosition.Y,
                Z = dockPosition.Z,
                Orientation = GetFacingFromLocation(dock.Location),
            };
            record.PendingJumpSpace = new PropertyLocation
            {
                AreaResref = GetResRef(GetArea(anchorWaypoint)),
                X = spacePosition.X,
                Y = spacePosition.Y,
                Z = spacePosition.Z,
                Orientation = GetFacing(anchorWaypoint),
            };
            CargoHoldRules.RemoveUnits(record.Cargo, CommodityType.StationFuel, fuel);
            DB.Set(record);

            FinishJump(ship, record);

            var landing = dock.Location;
            AssignCommand(player, () =>
            {
                ClearAllActions();
                ActionJumpToLocation(landing);
            });
            SendMessageToPC(player, ColorToken.Green($"{ship.CustomName} jumped to {destination.CustomName}, burning {fuel} Station Fuel."));
            Log.Write(LogGroup.Space, $"JUMP BEACON - {PlayerName.GetAuditName(player)} jumped ship '{ship.CustomName}' ({ship.Id}) from station '{origin.CustomName}' ({origin.Id}) to '{destination.CustomName}' ({destination.Id}) for {fuel} Station Fuel.");
            return true;
        }

        /// <summary>Finishes any station jump a restart interrupted after its fuel was burned.</summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void FinishInterruptedJumps()
        {
            var count = (int)DB.SearchCount(new DBQuery<PlayerShip>());
            if (count <= 0)
                return;

            foreach (var record in DB.Search(new DBQuery<PlayerShip>().AddPaging(count, 0)).Where(x => x.PendingJumpDock != null))
            {
                var starportId = record.PendingJumpDock.InstancePropertyId;
                FinishJump(DB.Get<WorldProperty>(record.PropertyId), record);
                Log.Write(LogGroup.Space, $"JUMP BEACON - finished the jump of ship {record.PropertyId} to starport {starportId} after a restart.");
            }
        }

        /// <summary>Moves the ship to its pending jump's dock, then clears the jump. Each step is safe to repeat.</summary>
        private static void FinishJump(WorldProperty ship, PlayerShip record)
        {
            if (ship != null && DB.Get<WorldProperty>(record.PendingJumpDock.InstancePropertyId) != null)
                MoveDock(ship, record.PendingJumpDock, record.PendingJumpSpace);

            record.PendingJumpDock = null;
            record.PendingJumpSpace = null;
            DB.Set(record);
        }

        /// <summary>
        /// Re-registers the ship from its old Starport to the destination's and moves its dock and launch point. The ship
        /// joins the destination first, then moves, then leaves the old Starport, so a restart part-way through leaves it
        /// listed at both, never at none.
        /// </summary>
        private static void MoveDock(WorldProperty ship, PropertyLocation dock, PropertyLocation space)
        {
            var starportId = dock.InstancePropertyId;
            if (!ship.ChildPropertyIds.ContainsKey(PropertyChildType.RegisteredStarport))
                ship.ChildPropertyIds[PropertyChildType.RegisteredStarport] = new List<string>();

            var oldIds = ship.ChildPropertyIds[PropertyChildType.RegisteredStarport].Where(x => x != starportId).ToList();
            ship.ChildPropertyIds[PropertyChildType.RegisteredStarport].Clear();
            ship.ChildPropertyIds[PropertyChildType.RegisteredStarport].Add(starportId);

            var starport = DB.Get<WorldProperty>(starportId);
            if (!starport.ChildPropertyIds.ContainsKey(PropertyChildType.Starship))
                starport.ChildPropertyIds[PropertyChildType.Starship] = new List<string>();
            if (!starport.ChildPropertyIds[PropertyChildType.Starship].Contains(ship.Id))
                starport.ChildPropertyIds[PropertyChildType.Starship].Add(ship.Id);
            DB.Set(starport);

            ship.Positions[PropertyLocationType.DockPosition] = dock;
            ship.Positions[PropertyLocationType.SpacePosition] = space;
            DB.Set(ship);

            foreach (var oldId in oldIds)
            {
                var oldStarport = DB.Get<WorldProperty>(oldId);
                if (oldStarport != null && oldStarport.ChildPropertyIds.TryGetValue(PropertyChildType.Starship, out var oldShips) && oldShips.Remove(ship.Id))
                    DB.Set(oldStarport);
            }
        }
    }
}
