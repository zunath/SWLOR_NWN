using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// The crew console of a ship (plan section 10). In flight: take an open Gunner, Engineer or Sensors station, or
    /// watch from a viewport. In flight or docked: post droids at the stations and recall them (the captain only),
    /// and undock a ship docked aboard (its pilots). A crew console placeable opens it, and so does the ship's
    /// computer while someone else flies the ship, or while it is docked for a captain with droids or a pilot with
    /// a ship docked aboard (its first choice then takes the controls).
    /// </summary>
    public class CrewStationDialog : ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";
        private const string DroidPageId = "DROID_PAGE";
        private const string DroidPickPageId = "DROID_PICK_PAGE";

        private class Model
        {
            public ShipCrewSeat Seat { get; set; }
            public string Result { get; set; } = string.Empty;
        }

        public override ConversationMenuSpec Build()
        {
            return new ConversationMenuBuilder()
                .WithDataModel(new Model())
                .AddPage(MainPageId, MainPageInit)
                .AddPage(DroidPageId, DroidPageInit)
                .AddPage(DroidPickPageId, DroidPickPageInit)
                .Build();
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var player = Player;
            var dbShip = Space.GetInteriorShipRecord(player);
            var pilot = dbShip == null ? OBJECT_INVALID : Space.GetShipPilot(dbShip.Id);
            var canManageDroids = Space.CanManageDroidCrew(player);

            if (!GetIsObjectValid(pilot))
            {
                page.Header = "Crew stations work only while the ship is in flight with a pilot at the controls.";
                if (dbShip != null && Space.IsShipComputer(Owner))
                {
                    var computer = Owner;
                    page.AddResponse("Take the controls", () =>
                    {
                        Close();
                        Space.TakeShipControls(player, computer);
                    });
                }

                if (canManageDroids)
                {
                    page.Header += "\nDroids can be posted at the stations now; they work them once the ship flies.";
                    page.AddResponse("Droid crew", () => GoToPage(DroidPageId));
                }

                AddUndockResponses(page, player, dbShip);
                return;
            }

            var status = Space.GetShipStatus(pilot);
            var stations = ShipCrewSeats.GetStationSeats(status?.Size ?? ShipSize.Small);
            var header = $"Captain: {PlayerName.GetDisplayName(player, pilot)}";

            if (stations.Count == 0)
            {
                header += "\nThis ship has no crew stations, but you can watch from a viewport.";
            }
            else
            {
                header += "\nCrew stations (the captain's party only):";
                foreach (var seat in stations)
                {
                    var skill = Skill.GetSkillDetails(ShipCrewSeats.GetSeatSkill(seat)).Name;
                    header += $"\n{ShipCrewSeats.GetSeatName(seat)} ({skill}): {DescribeOccupant(player, pilot, seat)}";
                }

                header += "\nYour rank in the station's skill decides which of its modules you can switch on. A player who takes a droid's station sends the droid back to its owner.";
            }

            page.Header = header;

            foreach (var seat in stations)
            {
                if (!string.IsNullOrEmpty(Space.GetCrewStationBoardingError(player, seat)))
                    continue;

                var chosen = seat;
                var droid = SpaceCrew.GetSeatDroid(pilot, seat);
                var text = droid == null
                    ? $"Take the {ShipCrewSeats.GetSeatName(seat)} station"
                    : $"Take the {ShipCrewSeats.GetSeatName(seat)} station from {droid.Name}";
                page.AddResponse(text, () =>
                {
                    Close();
                    Space.BoardCrewStation(player, chosen);
                });
            }

            page.AddResponse("Watch from a viewport", () =>
            {
                Close();
                Space.BoardCrewStation(player, ShipCrewSeat.Invalid);
            });

            if (canManageDroids)
                page.AddResponse("Droid crew", () => GoToPage(DroidPageId));

            AddUndockResponses(page, player, dbShip);
        }

        /// <summary>One response per ship docked aboard that the player may pilot (plan section 10, "Ship-to-ship docking").</summary>
        private void AddUndockResponses(ConversationMenuPage page, uint player, PlayerShip host)
        {
            foreach (var docked in Space.GetUndockableShips(player, host))
            {
                var shipId = docked.Id;
                var name = DB.Get<WorldProperty>(docked.PropertyId)?.CustomName ?? "your ship";
                page.AddResponse($"Undock {name}", () =>
                {
                    Close();
                    Space.UndockShip(player, shipId);
                });
            }
        }

        private static string DescribeOccupant(uint player, uint pilot, ShipCrewSeat seat)
        {
            var occupant = SpaceCrew.GetSeatOccupant(pilot, seat);
            if (occupant != OBJECT_INVALID && GetIsObjectValid(occupant))
                return PlayerName.GetDisplayName(player, occupant);

            var droid = SpaceCrew.GetSeatDroid(pilot, seat);
            return droid == null
                ? ColorToken.Green("open")
                : $"{droid.Name} (droid, {SpaceCrew.GetDroidEffectiveness(pilot, droid):P0})";
        }

        private void DroidPageInit(ConversationMenuPage page)
        {
            var player = Player;
            var dbShip = Space.GetInteriorShipRecord(player);
            if (dbShip?.Status == null || !Space.CanManageDroidCrew(player))
            {
                page.Header = "Only the captain can post droids at this ship's stations.";
                return;
            }

            var pilot = Space.GetShipPilot(dbShip.Id);
            var posted = Space.GetPostedDroids(dbShip);
            var header = "Droid crew. A posted droid stays at its post and does not follow you. At a station it works " +
                         "while someone flies the ship: weapons at the pilot's target, repairers and shields when they run " +
                         "low. Its Industry sets how well it works and its tier the module ranks it can switch on. A droid " +
                         "in a droid bay adds its Industry to the ship's yields or repairs.\n";

            foreach (var seat in Space.GetDroidPosts(dbShip.Status))
            {
                var droid = posted.FirstOrDefault(x => x.Seat == seat);
                var crewMember = GetIsObjectValid(pilot) ? SpaceCrew.GetSeatOccupant(pilot, seat) : OBJECT_INVALID;
                var who = droid != null
                    ? $"{droid.Name} (tier {droid.Tier}, Industry {droid.Industry})"
                    : crewMember != OBJECT_INVALID && GetIsObjectValid(crewMember)
                        ? PlayerName.GetDisplayName(player, crewMember)
                        : ColorToken.Green("open");
                header += $"\n{ShipCrewSeats.GetSeatName(seat)}: {who}";
                if (droid != null)
                    header += $"\n  {Space.DescribeDroidWork(seat, droid, dbShip.Status)}";

                var chosen = seat;
                var postName = ShipCrewSeats.GetPostName(seat);
                if (droid != null)
                {
                    page.AddResponse($"Recall {droid.Name} from {postName}",
                        () => Space.RecallDroid(player, chosen));
                }
                else if (crewMember == OBJECT_INVALID || !GetIsObjectValid(crewMember))
                {
                    page.AddResponse($"Post a droid at {postName}", () =>
                    {
                        var model = Data<Model>();
                        model.Seat = chosen;
                        model.Result = string.Empty;
                        GoToPage(DroidPickPageId);
                    });
                }
            }

            page.Header = header;
        }

        private void DroidPickPageInit(ConversationMenuPage page)
        {
            var player = Player;
            var model = Data<Model>();
            var postName = ShipCrewSeats.GetPostName(model.Seat);

            if (!string.IsNullOrEmpty(model.Result))
            {
                page.Header = model.Result;
                return;
            }

            var controllers = Space.GetPostableDroids(player);
            if (controllers.Count == 0)
            {
                page.Header = "You carry no droid you can post. A droid that follows you must be dismissed first.";
                return;
            }

            page.Header = model.Seat == ShipCrewSeat.DroidBay
                ? "Choose a droid for the droid bay. Its Industry sets what it adds to the ship's yields or repairs."
                : $"Choose a droid for {postName}. The percent is how well it works the station before Crew Morale " +
                  "and Leadership; it comes from the droid's Industry.";

            foreach (var controller in controllers)
            {
                var details = Droid.LoadDroidItemPropertyDetails(controller);
                var tier = DroidCrewRules.ClampTier(details.Tier);
                var name = Droid.GetDroidName(controller);
                var chosen = controller;
                var text = model.Seat == ShipCrewSeat.DroidBay
                    ? $"{name} (tier {tier}, Industry {details.Industry})"
                    : $"{name} (tier {tier}, Industry {details.Industry}): {DroidCrewRules.BaseEffectiveness(model.Seat, details.Industry):P0}, " +
                      $"modules up to rank {DroidCrewRules.MaximumModuleRank(tier)}";
                page.AddResponse(text,
                    () =>
                    {
                        if (Space.PostDroid(player, model.Seat, chosen))
                            model.Result = $"{name} now works {postName}.";
                    });
            }
        }
    }
}
