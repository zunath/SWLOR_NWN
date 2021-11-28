using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class PropertyExitDialog: DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);


            return builder.Build();
        }

        private void ReturnToLastDockedPosition(uint player, PropertyLocation propertyLocation)
        {
            var returningArea = Cache.GetAreaByResref(propertyLocation.AreaResref);
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
            var propertyId = Property.GetPropertyId(area);
            var property = DB.Get<WorldProperty>(propertyId);

            page.Header = $"What would you like to do?";

            // The existence of a current position means this is a starship currently in space.
            // Players should only have the "Emergency Exit" option.
            if (property.Positions.ContainsKey(PropertyLocationType.CurrentPosition))
            {
                page.AddResponse(ColorToken.Red("Emergency Exit"), () =>
                {
                    var propertyLocation = property.Positions[PropertyLocationType.DockPosition];
                    ReturnToLastDockedPosition(player, propertyLocation);

                    Space.PerformEmergencyExit(area);
                });
            }
            // The existence of a "Last Docked" position means this is a starship currently docked at a starport.
            else if (property.Positions.ContainsKey(PropertyLocationType.DockPosition))
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
                    Property.JumpToOriginalLocation(player);
                });
            }
        }
    }
}
