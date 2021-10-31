using System;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
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
            var displayUndock = false;
            var waypointTag = GetLocalString(OBJECT_SELF, "STARPORT_TELEPORT_WAYPOINT");
            var waypoint = GetWaypointByTag(waypointTag);
            
            if (dbPlayer.SelectedShipId != Guid.Empty.ToString())
            {
                var selectedShip = DB.Get<PlayerShip>(dbPlayer.SelectedShipId);
                var shipDetail = Space.GetShipDetailByItemTag(selectedShip.Status.ItemTag);
                selectedShipInfo = ColorToken.Green("Active Ship: ") + selectedShip.Status.Name + $" [{shipDetail.Name}]\n";

                // Ensure the player has the necessary perks to use the ship and all modules.
                if (Space.CanPlayerUseShip(player, selectedShip.Status))
                {
                    displayUndock = true;
                }
                else
                {
                    selectedShipInfo += ColorToken.Red("You do not have the necessary perks to undock this ship.\n");
                }
            }

            page.Header = ColorToken.Green("Starport Menu") + "\n" +
                          selectedShipInfo + "\n" +
                          "What would you like to do?";

            page.AddResponse("Manage Ships", () =>
            {
                EndConversation();

                Gui.TogglePlayerWindow(player, GuiWindowType.ShipManagement, null, OBJECT_SELF);
            });

            // Undock (available if a waypoint is specified)
            if (displayUndock && GetIsObjectValid(waypoint))
            {
                page.AddResponse("Undock", () =>
                {
                    EndConversation();

                    Space.EnterSpaceMode(player, dbPlayer.SelectedShipId);
                    AssignCommand(player, () => ActionJumpToLocation(GetLocation(waypoint)));
                });
            }
        }
    }
}
