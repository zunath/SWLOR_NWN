using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
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
            var model = GetDataModel<Model>();

            page.Header = "Would you like to dock your ship onto this location?";

            page.AddResponse("Dock Ship", () =>
            {
                AssignCommand(player, () =>
                {
                    ActionJumpToLocation(model.LandingLocation);
                });

                Space.ExitSpaceMode(player);
            });
        }
    }
}
