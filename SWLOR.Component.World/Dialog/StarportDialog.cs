using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.Properties.Contracts;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.UI.Payloads;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Domain.World.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.World.Dialog
{
    public class StarportDialog: DialogBase
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IKeyItemService KeyItemService => _serviceProvider.GetRequiredService<IKeyItemService>();
        private IPropertyService PropertyService => _serviceProvider.GetRequiredService<IPropertyService>();
        private IGuiService GuiService => _serviceProvider.GetRequiredService<IGuiService>();
        private IPlanetService PlanetService => _serviceProvider.GetRequiredService<IPlanetService>();
        private IAreaService AreaService => _serviceProvider.GetRequiredService<IAreaService>();
        private const string MainPageId = "MAIN_PAGE";

        public StarportDialog(
            ILogger logger, 
            IDatabaseService db, 
            IDialogService dialogService,
            IDialogBuilder dialogBuilder,
            IServiceProvider serviceProvider) : base(dialogService, dialogBuilder)
        {
            _logger = logger;
            _db = db;
            _serviceProvider = serviceProvider;
        }

        public override PlayerDialog SetUp(uint player)
        {
            var builder = DialogBuilder
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();

            // Must have the CZ-220 shuttle pass in order to use the ship management.
            if (!KeyItemService.HasKeyItem(player, KeyItemType.CZ220ShuttlePass) && !GetIsDM(player))
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
                    var propertyId = PropertyService.GetPropertyId(area);
                    var planetType = PlanetType.Invalid;

                    // NPC starports can retrieve the planet based on the name of the planet.
                    if (string.IsNullOrWhiteSpace(propertyId))
                    {
                        planetType = PlanetService.GetPlanetType(area);
                    }
                    // PC starports need to look at the city's area to determine this.
                    else
                    {
                        var dbProperty = _db.Get<WorldProperty>(propertyId);
                        var dbBuilding = _db.Get<WorldProperty>(dbProperty.ParentPropertyId);
                        var dbCity = _db.Get<WorldProperty>(dbBuilding.ParentPropertyId);
                        var cityArea = AreaService.GetAreaByResref(dbCity.ParentPropertyId);

                        planetType = PlanetService.GetPlanetType(cityArea);
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
                    GuiService.TogglePlayerWindow(player, GuiWindowType.ShipManagement, payload, OBJECT_SELF);
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
