using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class StarportDialog: ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var player = Player;

            // Must have the CZ-220 shuttle pass in order to use the ship management.
            if (!KeyItem.HasKeyItem(player, KeyItemType.CZ220ShuttlePass) && !GetIsDM(player))
            {
                page.Header = "Greetings. I am still setting up here. In the meantime, you should speak to Selan Flembek. Thank you for your patience.";
                return;
            }

            var spaceWaypointTag = GetLocalString(Owner, "STARPORT_TELEPORT_WAYPOINT");
            var landingWaypointTag = GetLocalString(Owner, "STARPORT_LANDING_WAYPOINT");

            page.Header = "Starport Menu\n\nWhat would you like to do?";

            if (!GetIsDM(player) && !GetIsDMPossessed(player))
            {
                page.AddResponse("Manage Ships", () =>
                {
                    Close();

                    var area = GetArea(Owner);
                    var propertyId = Property.GetPropertyId(area);
                    var planetType = PlanetType.Invalid;

                    // NPC starports can retrieve the planet based on the name of the planet.
                    if (string.IsNullOrWhiteSpace(propertyId))
                    {
                        planetType = Planet.GetPlanetType(area);
                    }
                    // PC starports need to look at the city's area to determine this.
                    else
                    {
                        var dbProperty = DB.Get<WorldProperty>(propertyId);
                        var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
                        var dbCity = DB.Get<WorldProperty>(dbBuilding.ParentPropertyId);
                        var cityArea = Area.GetAreaByResref(dbCity.ParentPropertyId);

                        planetType = Planet.GetPlanetType(cityArea);
                    }

                    if (planetType == PlanetType.Invalid)
                    {
                        SendMessageToPC(player, "Unable to continue. The planet could not be determined. Notify an admin.");
                        Log.Write(LogGroup.Error, $"Unable to determine planet for NPC '{GetName(Owner)}' located in {GetName(area)} ({GetTag(area)} / {GetResRef(area)})");
                        return;
                    }

                    var spaceLocation = GetLocation(GetWaypointByTag(spaceWaypointTag));
                    var landingLocation = string.IsNullOrWhiteSpace(landingWaypointTag)
                        ? GetLocalLocation(Owner, "STARPORT_LANDING_WAYPOINT")
                        : GetLocation(GetWaypointByTag(landingWaypointTag));

                    var payload = new ShipManagementPayload(planetType, spaceLocation, landingLocation);
                    Gui.TogglePlayerWindow(player, GuiWindowType.ShipManagement, payload, Owner);
                });

            }

            page.AddResponse("View Shop", () =>
            {
                var store = GetNearestObjectByTag("dockhand_store", Owner);
                OpenStore(store, player);
                Close();
            });
        }
    }
}
