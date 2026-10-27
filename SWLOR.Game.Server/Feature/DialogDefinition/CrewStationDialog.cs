using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// The crew console of a ship in flight (plan section 10): take an open Gunner, Engineer or Sensors station, or
    /// watch from a viewport. A crew console placeable opens it, and so does the ship's computer while someone else
    /// flies the ship.
    /// </summary>
    public class CrewStationDialog : ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";

        public override ConversationMenuSpec Build()
        {
            return new ConversationMenuBuilder()
                .AddPage(MainPageId, MainPageInit)
                .Build();
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var player = Player;
            var propertyId = Property.GetPropertyId(GetArea(player));
            var dbShip = string.IsNullOrWhiteSpace(propertyId)
                ? null
                : DB.Search(new DBQuery<PlayerShip>()
                    .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyId, false)).FirstOrDefault();
            var pilot = dbShip == null ? OBJECT_INVALID : Space.GetShipPilot(dbShip.Id);

            if (!GetIsObjectValid(pilot))
            {
                page.Header = "Crew stations work only while the ship is in flight with a pilot at the controls.";
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
                    var occupant = SpaceCrew.GetSeatOccupant(pilot, seat);
                    var who = occupant == OBJECT_INVALID || !GetIsObjectValid(occupant)
                        ? ColorToken.Green("open")
                        : PlayerName.GetDisplayName(player, occupant);
                    var skill = Skill.GetSkillDetails(ShipCrewSeats.GetSeatSkill(seat)).Name;
                    header += $"\n{ShipCrewSeats.GetSeatName(seat)} ({skill}): {who}";
                }

                header += "\nYour rank in the station's skill decides which of its modules you can switch on.";
            }

            page.Header = header;

            foreach (var seat in stations)
            {
                if (!string.IsNullOrEmpty(Space.GetCrewStationBoardingError(player, seat)))
                    continue;

                var chosen = seat;
                page.AddResponse($"Take the {ShipCrewSeats.GetSeatName(seat)} station", () =>
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
        }
    }
}
