﻿using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
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

                var area = GetArea(OBJECT_SELF);
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
                    var cityArea = Cache.GetAreaByResref(dbCity.ParentPropertyId);

                    planetType = Planet.GetPlanetType(cityArea);
                }

                if (planetType == PlanetType.Invalid)
                {
                    SendMessageToPC(player, "Unable to continue. The planet could not be determined. Notify an admin.");
                    Log.Write(LogGroup.Error, $"Unable to determine planet for NPC '{GetName(OBJECT_SELF)}' located in {GetName(area)} ({GetTag(area)} / {GetResRef(area)})");
                    return;
                }

                var spaceLocation = GetLocation(GetWaypointByTag(spaceWaypointTag));
                var landingLocation = string.IsNullOrWhiteSpace(landingWaypointTag) 
                    ? GetLocalLocation(OBJECT_SELF, "STARPORT_LANDING_WAYPOINT")
                    : GetLocation(GetWaypointByTag(landingWaypointTag));

                var payload = new ShipManagementPayload(planetType, spaceLocation, landingLocation);
                Gui.TogglePlayerWindow(player, GuiWindowType.ShipManagement, payload, OBJECT_SELF);
            });

        }
    }
}
