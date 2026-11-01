using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// The console of a station-only building (plan section 25): the Shipyard (build Capital hulls, who may build), the
    /// Asteroid Drill (stored ore and collecting it), the Jump Beacon (jumping a docked ship, who may jump, allies), and the
    /// droid assigned to the Shipyard or the Drill.
    /// </summary>
    public class StationBuildingDialog : ConversationMenuDefinition
    {
        private class Model
        {
            public string BuildingId { get; set; }
            public string ShipId { get; set; }
            public string DestinationId { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string DroidPageId = "DROID_PAGE";
        private const string DroidPickPageId = "DROID_PICK_PAGE";
        private const string ShipPageId = "SHIP_PAGE";
        private const string DestinationPageId = "DESTINATION_PAGE";
        private const string ConfirmJumpPageId = "CONFIRM_JUMP_PAGE";
        private const string AllyPageId = "ALLY_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialize)
                .AddPage(MainPageId, MainPageInit)
                .AddPage(DroidPageId, DroidPageInit)
                .AddPage(DroidPickPageId, DroidPickPageInit)
                .AddPage(ShipPageId, ShipPageInit)
                .AddPage(DestinationPageId, DestinationPageInit)
                .AddPage(ConfirmJumpPageId, ConfirmJumpPageInit)
                .AddPage(AllyPageId, AllyPageInit);

            return builder.Build();
        }

        private void Initialize()
        {
            Data<Model>().BuildingId = StationBuildings.GetBuildingOfConsole(Owner)?.Id ?? string.Empty;
        }

        private bool TryGetContext(ConversationMenuPage page, out WorldProperty building, out WorldProperty station, out StationBuildingState state)
        {
            building = DB.Get<WorldProperty>(Data<Model>().BuildingId ?? string.Empty);
            station = StationBuildings.GetStation(building);
            state = building == null ? null : StationBuildings.GetState(building);
            if (station != null)
                return true;

            page.Header = "This console is not connected to a station.";
            return false;
        }

        private bool CanSetPolicy(WorldProperty station) =>
            Property.HasPropertyPermission(Player, station.Id, PropertyPermissionType.EditTaxes);

        private bool CanManageStructures(WorldProperty station) =>
            Property.HasPropertyPermission(Player, station.Id, PropertyPermissionType.EditStructures);

        private void MainPageInit(ConversationMenuPage page)
        {
            if (!TryGetContext(page, out var building, out var station, out var state))
                return;

            switch (building.StructureType)
            {
                case StructureType.Shipyard:
                    ShipyardPage(page, station, state);
                    break;
                case StructureType.AsteroidDrill:
                    DrillPage(page, station, state);
                    break;
                case StructureType.JumpBeacon:
                    BeaconPage(page, station, state);
                    break;
            }
        }

        // The Shipyard

        private void ShipyardPage(ConversationMenuPage page, WorldProperty station, StationBuildingState state)
        {
            var player = Player;
            var fee = StationBuildings.GetShipyardFee(station);
            page.Header = $"{station.CustomName} Shipyard\n\n" +
                          "Capital hulls can only be built at a station Shipyard. Ships already built are not affected.\n\n" +
                          $"Open to: {(state.ShipyardAccess == ShipyardAccessType.Anyone ? "anyone" : "citizens only")}\n" +
                          $"Fee per Capital hull: {fee} credits\n" +
                          DescribeDroid(state, $"build speed +{StationBuildings.GetDroidPercent(state)}%");

            page.AddResponse("Build ships (Engineering)", () =>
            {
                var error = StationBuildings.GetShipyardError(player);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    SendMessageToPC(player, ColorToken.Red(error));
                    return;
                }

                Close();
                var payload = new RecipesPayload(RecipesUIMode.Crafting, SkillType.Engineering);
                Gui.TogglePlayerWindow(player, GuiWindowType.Recipes, payload, Owner);
            });

            if (CanSetPolicy(station))
            {
                var next = state.ShipyardAccess == ShipyardAccessType.Anyone ? ShipyardAccessType.Citizens : ShipyardAccessType.Anyone;
                var buildingId = Data<Model>().BuildingId;
                page.AddResponse(next == ShipyardAccessType.Anyone ? "Open the Shipyard to anyone" : "Open the Shipyard to citizens only", () =>
                {
                    if (CanSetPolicy(station))
                        StationBuildings.UpdateState(buildingId, current => current.ShipyardAccess = next);
                });
            }

            page.AddResponse("Assigned droid", () => GoToPage(DroidPageId));
        }

        // The Asteroid Drill

        private void DrillPage(ConversationMenuPage page, WorldProperty station, StationBuildingState state)
        {
            var player = Player;
            StationBuildings.AccrueDrill(station, state);
            var anchor = Station.GetAnchor(station);
            var droidPercent = StationBuildings.GetDroidPercent(state);
            var stored = state.DrillStorage.Values.Sum();
            var capacity = StationBuildingRules.DrillCapacity(droidPercent);
            var ore = state.DrillStorage.Count == 0
                ? "none"
                : string.Join(", ", state.DrillStorage.OrderByDescending(x => x.Value).Select(x => $"{x.Value}x {Cache.GetItemNameByResref(x.Key)}"));

            page.Header = $"{station.CustomName} Asteroid Drill\n\n" +
                          $"The drill mines the asteroids of {anchor?.SystemName} around the clock.\n\n" +
                          $"Output: {StationBuildingRules.DrillUnitsPerDay(droidPercent):0} ore a day\n" +
                          $"Stored: {stored} / {capacity}{(stored >= capacity ? " (full: the drill has stopped)" : string.Empty)}\n" +
                          $"Ore: {ore}\n" +
                          DescribeDroid(state, $"output +{droidPercent}%");

            if (Property.HasPropertyPermission(player, station.Id, PropertyPermissionType.AccessTreasury))
            {
                page.AddResponse("Collect the ore", () =>
                {
                    StationBuildings.CollectDrill(player, station, state.BuildingId);
                });
            }

            page.AddResponse("Assigned droid", () => GoToPage(DroidPageId));
        }

        // Droids

        private static string DescribeDroid(StationBuildingState state, string effect) =>
            string.IsNullOrWhiteSpace(state.SerializedDroid)
                ? "Droid: none assigned"
                : $"Droid: {state.DroidName} (Industry {state.DroidIndustry}, {effect})";

        private void DroidPageInit(ConversationMenuPage page)
        {
            if (!TryGetContext(page, out var building, out var station, out var state))
                return;

            var player = Player;
            page.Header = "An industrial droid assigned here works the building around the clock. Its Industry rating sets how much it helps, " +
                          "and it never takes your follower slot.\n\n" +
                          DescribeDroid(state, building.StructureType == StructureType.Shipyard
                              ? $"build speed +{StationBuildings.GetDroidPercent(state)}%"
                              : $"output +{StationBuildings.GetDroidPercent(state)}%");

            if (!string.IsNullOrWhiteSpace(state.SerializedDroid))
            {
                if (state.DroidOwnerPlayerId == GetObjectUUID(player) || CanManageStructures(station))
                {
                    page.AddResponse($"Recall {state.DroidName}", () => StationBuildings.RecallDroid(player, building));
                }

                return;
            }

            if (CanManageStructures(station))
                page.AddResponse("Assign one of your droids", () => GoToPage(DroidPickPageId));
        }

        private void DroidPickPageInit(ConversationMenuPage page)
        {
            if (!TryGetContext(page, out var building, out var station, out var state))
                return;

            var player = Player;
            var controllers = Space.GetPostableDroids(player);
            if (controllers.Count == 0)
            {
                page.Header = "You carry no droid you can assign. A droid that follows you must be dismissed first.";
                return;
            }

            page.Header = "Choose a droid to assign. Its controller stays with the building until you recall it.";
            foreach (var controller in controllers)
            {
                var chosen = controller;
                page.AddResponse($"{Droid.GetDroidName(controller)} (Industry {StationBuildings.GetDroidIndustry(controller).Industry})", () =>
                {
                    if (StationBuildings.AssignDroid(player, building, chosen))
                        GoToPage(DroidPageId, false);
                });
            }
        }

        // The Jump Beacon

        private void BeaconPage(ConversationMenuPage page, WorldProperty station, StationBuildingState state)
        {
            var level = StationBuildings.GetBeaconLevel(station);
            page.Header = $"{station.CustomName} Jump Beacon\n\n" +
                          "The beacon jumps a ship docked at this station to another station's beacon. Beacons never reach into or out of Wild Space.\n\n" +
                          $"Beacon level: {level} (hulls up to {StationBuildingRules.GetMaxHull(level)}; both beacons must take the hull)\n" +
                          "Station Fuel per jump, from the ship's cargo bay: " +
                          string.Join(", ", StationTuning.JumpFuel.Select(x => $"{x.Key} {x.Value}")) +
                          $", plus 1 per {StationTuning.JumpFuelCargoValuePerHop} credits of cargo value (ships docked aboard count too) for each starlane hop the jump skips\n" +
                          $"Who may jump here: {DescribeAccess(state.BeaconAccess)}";

            page.AddResponse("Jump a docked ship", () => GoToPage(ShipPageId));

            if (CanSetPolicy(station))
            {
                var next = state.BeaconAccess switch
                {
                    JumpBeaconAccessType.Citizens => JumpBeaconAccessType.Allies,
                    JumpBeaconAccessType.Allies => JumpBeaconAccessType.Anyone,
                    _ => JumpBeaconAccessType.Citizens,
                };
                var buildingId = Data<Model>().BuildingId;
                page.AddResponse($"Open the beacon to {DescribeAccess(next)}", () =>
                {
                    if (CanSetPolicy(station))
                        StationBuildings.UpdateState(buildingId, current => current.BeaconAccess = next);
                });
                page.AddResponse($"Allied stations ({state.BeaconAllyStationIds.Count})", () => GoToPage(AllyPageId));
            }
        }

        private static string DescribeAccess(JumpBeaconAccessType access) => access switch
        {
            JumpBeaconAccessType.Anyone => "anyone",
            JumpBeaconAccessType.Allies => "citizens and allies",
            _ => "citizens only",
        };

        private void ShipPageInit(ConversationMenuPage page)
        {
            if (!TryGetContext(page, out _, out var station, out _))
                return;

            var ships = StationBuildings.GetJumpableShips(Player, station);
            if (ships.Count == 0)
            {
                page.Header = "You have no ship docked at this station that you may pilot.";
                return;
            }

            page.Header = "Choose the ship to jump.";
            foreach (var (ship, record) in ships)
            {
                var fuel = CargoHoldRules.GetUnits(record.Cargo, CommodityType.StationFuel);
                var shipId = ship.Id;
                page.AddResponse($"{ship.CustomName} ({Space.GetShipSize(record)}, {fuel} Station Fuel aboard)", () =>
                {
                    Data<Model>().ShipId = shipId;
                    GoToPage(DestinationPageId);
                });
            }
        }

        private void DestinationPageInit(ConversationMenuPage page)
        {
            if (!TryGetContext(page, out _, out var station, out _))
                return;

            var destinations = StationBuildings.GetDestinations(Player, station);
            if (destinations.Count == 0)
            {
                page.Header = "No other station's Jump Beacon accepts you right now.";
                return;
            }

            page.Header = "Choose the station to jump to.";
            foreach (var destination in destinations)
            {
                var destinationId = destination.Id;
                var anchor = Station.GetAnchor(destination);
                page.AddResponse($"{destination.CustomName} ({anchor?.SystemName}, beacon level {StationBuildings.GetBeaconLevel(destination)})", () =>
                {
                    Data<Model>().DestinationId = destinationId;
                    GoToPage(ConfirmJumpPageId);
                });
            }
        }

        private void ConfirmJumpPageInit(ConversationMenuPage page)
        {
            if (!TryGetContext(page, out _, out var station, out _))
                return;

            var player = Player;
            var model = Data<Model>();
            var ship = DB.Get<WorldProperty>(model.ShipId ?? string.Empty);
            var destination = DB.Get<WorldProperty>(model.DestinationId ?? string.Empty);
            if (ship == null || destination == null)
            {
                page.Header = "That ship or station is no longer available.";
                return;
            }

            var record = DB.Search(new DBQuery<PlayerShip>().AddFieldSearch(nameof(PlayerShip.PropertyId), ship.Id, false)).FirstOrDefault();
            var request = StationBuildings.BuildJumpRequest(player, station, destination, ship, record);
            var refusal = StationBuildingRules.GetJumpRefusal(request);
            if (refusal != JumpRefusal.None)
            {
                page.Header = StationBuildingRules.DescribeJumpRefusal(refusal, request);
                return;
            }

            var fuel = StationBuildingRules.GetJumpFuel(request);
            page.Header = $"Jump {ship.CustomName} to {destination.CustomName}? The jump burns {fuel} Station Fuel from its cargo bay, " +
                          "docks the ship at that station's Starport and takes you there with it.";
            page.AddResponse("CONFIRM JUMP", () =>
            {
                if (StationBuildings.Jump(player, station, destination, ship.Id))
                    Close();
            });
        }

        private void AllyPageInit(ConversationMenuPage page)
        {
            if (!TryGetContext(page, out _, out var station, out var state))
                return;

            if (!CanSetPolicy(station))
            {
                page.Header = "You need the Edit Taxes permission to choose allied stations.";
                return;
            }

            page.Header = "Citizens of allied stations may use this beacon while it is open to citizens and allies. Select a station to add or remove it.";
            var stations = DB.Search(new DBQuery<WorldProperty>()
                    .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City)
                    .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false)
                    .AddPaging(100, 0))
                .Where(x => Station.IsStation(x) && x.Id != station.Id)
                .OrderBy(x => x.CustomName);

            var buildingId = Data<Model>().BuildingId;
            foreach (var other in stations)
            {
                var otherId = other.Id;
                var allied = state.BeaconAllyStationIds.Contains(otherId);
                page.AddResponse($"{(allied ? "[Allied] " : string.Empty)}{other.CustomName}", () =>
                {
                    if (!CanSetPolicy(station))
                        return;

                    // The choice means what the page showed: remove an ally it listed, add one it did not.
                    StationBuildings.UpdateState(buildingId, current =>
                    {
                        current.BeaconAllyStationIds.Remove(otherId);
                        if (!allied)
                            current.BeaconAllyStationIds.Add(otherId);
                    });
                });
            }
        }
    }
}
