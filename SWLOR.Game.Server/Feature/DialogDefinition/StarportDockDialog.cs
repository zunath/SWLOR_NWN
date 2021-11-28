using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class StarportDockDialog: DialogBase
    {
        private class Model
        {
            public Location SpaceLocation { get; set; }
            public Location LandingLocation { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialize)
                .AddPage(MainPageId, MainPageInit);


            return builder.Build();
        }

        private void Initialize()
        {
            var self = OBJECT_SELF;
            var landingWaypointTag = GetLocalString(self, "STARPORT_LANDING_WAYPOINT");
            var spaceWaypointTag = GetLocalString(self, "STARPORT_TELEPORT_WAYPOINT");
            var player = GetPC();

            if (string.IsNullOrWhiteSpace(landingWaypointTag))
            {
                Log.Write(LogGroup.Error, $"{GetName(self)} is missing the local variable 'STARPORT_LANDING_WAYPOINT' and cannot be used by players to dock their ships.");
                SendMessageToPC(player, "This docking point is misconfigured. Notify an admin.");
                EndConversation();
                return;
            }

            if (string.IsNullOrWhiteSpace(spaceWaypointTag))
            {
                Log.Write(LogGroup.Error, $"{GetName(self)} is missing the local variable 'STARPORT_TELEPORT_WAYPOINT' and cannot be used by players to dock their ships.");
                SendMessageToPC(player, "This docking point is misconfigured. Notify an admin.");
                EndConversation();
                return;
            }

            var landingWaypoint = GetWaypointByTag(landingWaypointTag);
            var spaceWaypoint = GetWaypointByTag(spaceWaypointTag);

            if (!GetIsObjectValid(landingWaypoint))
            {
                Log.Write(LogGroup.Error, $"The waypoint associated with '{GetName(self)}' cannot be found. Did you place it in an area?");
                SendMessageToPC(player, "This docking point is misconfigured. Notify an admin.");
                EndConversation();
                return;
            }

            if (!GetIsObjectValid(spaceWaypoint))
            {
                Log.Write(LogGroup.Error, $"The waypoint associated with '{GetName(self)}' cannot be found. Did you place it in an area?");
                SendMessageToPC(player, "This docking point is misconfigured. Notify an admin.");
                EndConversation();
                return;
            }

            var model = GetDataModel<Model>();
            model.SpaceLocation = GetLocation(spaceWaypoint);
            model.LandingLocation = GetLocation(landingWaypoint);
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var model = GetDataModel<Model>();

            page.Header = "Would you like to dock your ship onto this location?";

            page.AddResponse("Dock Ship", () =>
            {
                var spaceArea = GetAreaFromLocation(model.SpaceLocation);
                var spaceAreaResref = GetResRef(spaceArea);
                var spacePosition = GetPositionFromLocation(model.SpaceLocation);
                var spaceOrientation = GetFacingFromLocation(model.SpaceLocation);

                var landingArea = GetAreaFromLocation(model.LandingLocation);
                var landingAreaResref = GetResRef(landingArea);
                var landingPosition = GetPositionFromLocation(model.LandingLocation);
                var landingOrientation = GetFacingFromLocation(model.LandingLocation);

                // Clear the ship property's space position and update its last docked position with the new destination.
                var dbPlayer = DB.Get<Player>(playerId);
                var dbShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
                var dbProperty = DB.Get<WorldProperty>(dbShip.PropertyId);
                dbProperty.Positions.Remove(PropertyLocationType.CurrentPosition);

                dbProperty.Positions[PropertyLocationType.DockPosition] = new PropertyLocation
                {
                    AreaResref = landingAreaResref,
                    X = landingPosition.X,
                    Y = landingPosition.Y,
                    Z = landingPosition.Z,
                    Orientation = landingOrientation
                };

                dbProperty.Positions[PropertyLocationType.SpacePosition] = new PropertyLocation
                {
                    AreaResref = spaceAreaResref,
                    X = spacePosition.X,
                    Y = spacePosition.Y,
                    Z = spacePosition.Z,
                    Orientation = spaceOrientation
                };

                DB.Set(dbProperty);

                AssignCommand(player, () =>
                {
                    ActionJumpToLocation(model.LandingLocation);
                });

                Space.ExitSpaceMode(player);
            });
        }
    }
}
