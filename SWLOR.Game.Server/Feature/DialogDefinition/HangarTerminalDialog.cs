using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.Engine;
using System.Numerics;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class HangarTerminalDialog : DialogBase
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
            var area = GetArea(OBJECT_SELF);
            var hostPropertyId = Property.GetPropertyId(area);

            page.Header = "Hangar Terminal\n\nSelect a docked ship to board.";

            if (string.IsNullOrWhiteSpace(hostPropertyId))
            {
                page.Header += "\n\n" + ColorToken.Red("This terminal is not inside a valid ship property.");
                return;
            }

            var dockedShips = Space.GetShipsPresentInHangar(hostPropertyId);
            if (dockedShips.Count == 0)
            {
                page.Header += "\n\nNo ships are currently docked in this hangar.";
                return;
            }

            foreach (var (ship, property) in dockedShips)
            {
                var shipName = string.IsNullOrWhiteSpace(property.CustomName)
                    ? "Unnamed Ship"
                    : property.CustomName;

                page.AddResponse(shipName, () =>
                {
                    if (!Property.HasPropertyPermission(player, ship.PropertyId, PropertyPermissionType.EnterProperty) &&
                        property.OwnerPlayerId != GetObjectUUID(player))
                    {
                        SendMessageToPC(player, ColorToken.Red("You do not have permission to board that ship."));
                        return;
                    }

                    // Re-validate the ship is still present in the hangar.
                    var stillDocked = Space.GetShipsPresentInHangar(hostPropertyId);
                    if (!stillDocked.Exists(s => s.Ship.Id == ship.Id))
                    {
                        SendMessageToPC(player, ColorToken.Red("That ship is no longer in the hangar."));
                        return;
                    }

                    var shipDetail = Space.GetShipDetailByItemTag(ship.Status.ItemTag);
                    var instance = Property.GetRegisteredInstance(ship.PropertyId);
                    if (instance == null || !GetIsObjectValid(instance.Area))
                    {
                        SendMessageToPC(player, ColorToken.Red("ERROR: Could not locate that ship's interior."));
                        return;
                    }

                    var entrance = Property.GetEntrancePosition(shipDetail.Layout);
                    var location = Location(instance.Area, new Vector3(entrance.X, entrance.Y, entrance.Z), entrance.W);

                    AssignCommand(player, () =>
                    {
                        ClearAllActions();
                        ActionJumpToLocation(location);
                    });
                });
            }
        }
    }
}
