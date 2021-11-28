using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.KeyItemService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class StarportDialog: DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            // Must have the CZ-220 shuttle pass in order to use the ship management.
            if (!dbPlayer.KeyItems.ContainsKey(KeyItemType.CZ220ShuttlePass))
            {
                page.Header = "Greetings. I am still setting up here. In the meantime, you should speak to Selan Flembek. Thank you for your patience.";
                return;
            }

            var selectedShipInfo = string.Empty;
            var spaceWaypointTag = GetLocalString(OBJECT_SELF, "STARPORT_TELEPORT_WAYPOINT");
            var landingWaypointTag = GetLocalString(OBJECT_SELF, "STARPORT_LANDING_WAYPOINT");
            
            page.Header = ColorToken.Green("Starport Menu") + "\n" +
                          selectedShipInfo + "\n" +
                          "What would you like to do?";

            page.AddResponse("Manage Ships", () =>
            {
                EndConversation();

                var spaceLocation = GetLocation(GetWaypointByTag(spaceWaypointTag));
                var landingLocation = GetLocation(GetWaypointByTag(landingWaypointTag));

                var payload = new ShipManagementPayload(spaceLocation, landingLocation);
                Gui.TogglePlayerWindow(player, GuiWindowType.ShipManagement, payload, OBJECT_SELF);
            });

        }
    }
}
