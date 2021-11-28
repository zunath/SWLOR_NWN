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
            var waypointTag = GetLocalString(self, "DOCKING_WAYPOINT");
            var player = GetPC();

            if (string.IsNullOrWhiteSpace(waypointTag))
            {
                Log.Write(LogGroup.Error, $"{GetName(self)} is missing the local variable 'DOCKING_WAYPOINT' and cannot be used by players to dock their ships.");
                SendMessageToPC(player, "This docking point is misconfigured. Notify an admin.");
                EndConversation();
                return;
            }

            var waypoint = GetWaypointByTag(waypointTag);

            if (!GetIsObjectValid(waypoint))
            {
                Log.Write(LogGroup.Error, $"The waypoint associated with '{GetName(self)}' cannot be found. Did you place it in an area?");
                SendMessageToPC(player, "This docking point is misconfigured. Notify an admin.");
                EndConversation();
                return;
            }

            var model = GetDataModel<Model>();
            var location = GetLocation(waypoint);
            model.LandingLocation = location;
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var model = GetDataModel<Model>();

            page.Header = "Would you like to dock your ship onto this location?";

            page.AddResponse("Dock Ship", () =>
            {
                var area = GetAreaFromLocation(model.LandingLocation);
                var areaResref = GetResRef(area);
                var position = GetPositionFromLocation(model.LandingLocation);
                var orientation = GetFacingFromLocation(model.LandingLocation);

                // Clear the ship property's space position and update its last docked position with the new destination.
                var dbPlayer = DB.Get<Player>(playerId);
                var dbShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
                var dbProperty = DB.Get<WorldProperty>(dbShip.PropertyId);
                dbProperty.Positions.Remove(PropertyLocationType.SpacePosition);

                dbProperty.Positions[PropertyLocationType.DockPosition] = new PropertyLocation
                {
                    AreaResref = areaResref,
                    X = position.X,
                    Y = position.Y,
                    Z = position.Z,
                    Orientation = orientation
                };

                dbProperty.Positions.Remove(PropertyLocationType.CurrentPosition);

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
