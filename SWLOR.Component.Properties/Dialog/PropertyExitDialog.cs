using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Model;
using SWLOR.Shared.Dialog.Service;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Entity;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Properties.Dialog
{
    public class PropertyExitDialog: DialogBase
    {
        private readonly IDatabaseService _db;
        private readonly IAreaService _areaService;
        private readonly IPropertyService _property;
        private readonly ISpaceService _spaceService;

        public PropertyExitDialog(
            IDatabaseService db, 
            IPropertyService property, 
            IDialogService dialogService,
            IAreaService areaService,
            ISpaceService spaceService) 
            : base(dialogService)
        {
            _db = db;
            _property = property;
            _areaService = areaService;
            _spaceService = spaceService;
        }
        
        private const string MainPageId = "MAIN_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);


            return builder.Build();
        }

        private void ReturnToLastDockedPosition(uint player, PropertyLocation propertyLocation)
        {
            var returningArea = string.IsNullOrWhiteSpace(propertyLocation.AreaResref)
                ? _property.GetRegisteredInstance(propertyLocation.InstancePropertyId).Area
                : _areaService.GetAreaByResref(propertyLocation.AreaResref);
            
            var location = Location(
                returningArea,
                Vector3(propertyLocation.X, propertyLocation.Y, propertyLocation.Z),
                propertyLocation.Orientation);

            AssignCommand(player, () => ActionJumpToLocation(location));
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var area = GetArea(player);
            var propertyId = _property.GetPropertyId(area);
            var property = _db.Get<WorldProperty>(propertyId);

            page.Header = $"What would you like to do?";

            // The existence of a current position means this is a starship currently in space.
            // Players should only have the "Emergency Exit" option.
            if (property != null &&
                property.Positions.ContainsKey(PropertyLocationType.CurrentPosition))
            {
                page.Header += "\nYou may perform an emergency exit to return to the last dock at which this ship landed. If you are the last person on board, the ship will be towed back, damaging the ship's shields and hull.";

                page.AddResponse(ColorToken.Red("Emergency Exit"), () =>
                {
                    var propertyLocation = property.Positions[PropertyLocationType.DockPosition];
                    ReturnToLastDockedPosition(player, propertyLocation);

                    _spaceService.PerformEmergencyExit(area);
                });
            }
            // The existence of a "Last Docked" position means this is a starship currently docked at a starport.
            else if (property != null && 
                     property.Positions.ContainsKey(PropertyLocationType.DockPosition))
            {
                page.AddResponse("Exit", () =>
                {
                    var propertyLocation = property.Positions[PropertyLocationType.DockPosition];
                    ReturnToLastDockedPosition(player, propertyLocation);
                });
            }
            // For all other scenarios, the player should be jumped to their original location.
            else
            {
                page.AddResponse("Exit", () =>
                {
                    // Building interiors will have a location set identifying where their doors are located.
                    // Jump to this location if it's set.
                    if (GetLocalBool(area, "BUILDING_EXIT_SET"))
                    {
                        var location = GetLocalLocation(area, "BUILDING_EXIT_LOCATION");
                        AssignCommand(player, () => ActionJumpToLocation(location));
                    }
                    // Otherwise jump the player to their original location.
                    else
                    {
                        _property.JumpToOriginalLocation(player);
                    }
                });
            }
        }
    }
}
