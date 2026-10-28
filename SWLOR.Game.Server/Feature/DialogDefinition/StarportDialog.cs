using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.TradeService;

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

                AddTradeExchangeResponse(page, player, landingWaypointTag);
            }

            page.AddResponse("View Shop", () =>
            {
                var store = GetNearestObjectByTag("dockhand_store", Owner);
                OpenStore(store, player);
                Close();
            });
        }

        /// <summary>
        /// The dock's Trade Exchange (plan section 13). An NPC starport trades through its planet's exchange; a player
        /// city's starport trades through the same exchange once the city has upgraded it, and charges the city's tariff.
        /// Ship-only locations such as the Abandoned Station have none.
        /// </summary>
        private void AddTradeExchangeResponse(ConversationMenuPage page, uint player, string landingWaypointTag)
        {
            var area = GetArea(Owner);
            var starportId = Property.GetPropertyId(area);
            if (!string.IsNullOrWhiteSpace(starportId))
            {
                AddCityTradeExchangeResponse(page, player, starportId);
                return;
            }

            var exchange = TradeLookup.GetExchangeForPlanet(Planet.GetPlanetType(area));
            var landingLocation = string.IsNullOrWhiteSpace(landingWaypointTag)
                ? GetLocalLocation(Owner, "STARPORT_LANDING_WAYPOINT")
                : GetLocation(GetWaypointByTag(landingWaypointTag));
            var landingArea = GetAreaFromLocation(landingLocation);
            if (exchange == TradeExchangeType.Invalid ||
                !GetIsObjectValid(landingArea) ||
                GetLocalBool(landingArea, StarshipDockReturn.ShipOnlyLocationVariable))
            {
                return;
            }

            var landingAreaResref = GetResRef(landingArea);
            page.AddResponse("Trade Exchange", () =>
            {
                Close();
                var payload = new TradeExchangePayload(exchange, landingAreaResref);
                Gui.TogglePlayerWindow(player, GuiWindowType.TradeExchange, payload, Owner);
            });
        }

        /// <summary>
        /// A player city's Trade Exchange. Cities whose starport is below the opening level (and cities on no NPC planet)
        /// see no exchange; a city starport that only needs an upgrade tells the player so.
        /// </summary>
        private void AddCityTradeExchangeResponse(ConversationMenuPage page, uint player, string starportId)
        {
            var dock = Trade.TryGetCityDock(starportId);
            if (dock == null || dock.Exchange == TradeExchangeType.Invalid)
                return;

            page.AddResponse("Trade Exchange", () =>
            {
                Close();
                if (!dock.HasExchange)
                {
                    SendMessageToPC(player, ColorToken.Red(
                        $"This starport needs to be upgraded to level {TradeTuning.CityExchangeStarportLevel} before it has a Trade Exchange. Ask the city's leadership."));
                    return;
                }

                var payload = new TradeExchangePayload(dock.Exchange, starportId);
                Gui.TogglePlayerWindow(player, GuiWindowType.TradeExchange, payload, Owner);
            });
        }
    }
}
