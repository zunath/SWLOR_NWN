using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Enums;
using SWLOR.Component.World.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Model;
using SWLOR.Shared.Dialog.Service;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model.Payload;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.World.Dialog
{
    public class StarportDialog: DialogBase
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _db;
        private readonly IKeyItemService _keyItemService;
        private readonly IPropertyService _propertyService;
        private readonly IGuiService _guiService;
        private readonly IPlanetService _planetService;
        private const string MainPageId = "MAIN_PAGE";

        public StarportDialog(ILogger logger, IDatabaseService db, IKeyItemService keyItemService, IPropertyService propertyService, IGuiService guiService, IPlanetService planetService, IDialogService dialogService) 
            : base(dialogService)
        {
            _logger = logger;
            _db = db;
            _keyItemService = keyItemService;
            _propertyService = propertyService;
            _guiService = guiService;
            _planetService = planetService;
        }

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();

            // Must have the CZ-220 shuttle pass in order to use the ship management.
            if (!_keyItemService.HasKeyItem(player, KeyItemType.CZ220ShuttlePass) && !GetIsDM(player))
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

            if (!GetIsDM(player) && !GetIsDMPossessed(player))
            {
                page.AddResponse("Manage Ships", () =>
                {
                    EndConversation();

                    var area = GetArea(OBJECT_SELF);
                    var propertyId = _propertyService.GetPropertyId(area);
                    var planetType = PlanetType.Invalid;

                    // NPC starports can retrieve the planet based on the name of the planet.
                    if (string.IsNullOrWhiteSpace(propertyId))
                    {
                        planetType = _planetService.GetPlanetType(area);
                    }
                    // PC starports need to look at the city's area to determine this.
                    else
                    {
                        var dbProperty = _db.Get<WorldProperty>(propertyId);
                        var dbBuilding = _db.Get<WorldProperty>(dbProperty.ParentPropertyId);
                        var dbCity = _db.Get<WorldProperty>(dbBuilding.ParentPropertyId);
                        var cityArea = Area.GetAreaByResref(dbCity.ParentPropertyId);

                        planetType = _planetService.GetPlanetType(cityArea);
                    }

                    if (planetType == PlanetType.Invalid)
                    {
                        SendMessageToPC(player, "Unable to continue. The planet could not be determined. Notify an admin.");
                        _logger.Write<ErrorLogGroup>($"Unable to determine planet for NPC '{GetName(OBJECT_SELF)}' located in {GetName(area)} ({GetTag(area)} / {GetResRef(area)})");
                        return;
                    }

                    var spaceLocation = GetLocation(GetWaypointByTag(spaceWaypointTag));
                    var landingLocation = string.IsNullOrWhiteSpace(landingWaypointTag)
                        ? GetLocalLocation(OBJECT_SELF, "STARPORT_LANDING_WAYPOINT")
                        : GetLocation(GetWaypointByTag(landingWaypointTag));

                    var payload = new ShipManagementPayload(planetType, spaceLocation, landingLocation);
                    _guiService.TogglePlayerWindow(player, GuiWindowType.ShipManagement, payload, OBJECT_SELF);
                });

            }

            page.AddResponse("View Shop", () =>
            {
                var store = GetNearestObjectByTag("dockhand_store", OBJECT_SELF);
                OpenStore(store, player);
                EndConversation();
            });
        }
    }
}
