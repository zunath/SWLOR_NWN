using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class PlayerHousePropertyDialog : DialogBase
    {
        private class Model
        {
            public Model()
            {
                TargetObject = OBJECT_INVALID;
            }
            public uint TargetObject { get; set; }
            public Location TargetLocation { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string ManagePermissionsPageId = "MANAGE_PERMISSIONS_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(LoadDataModel)
                .AddPage(MainPageId, MainPageInit)
                .AddPage(ManagePermissionsPageId, ManagePermissionsPageInit);


            return builder.Build();
        }

        private void LoadDataModel()
        {
            var player = GetPC();
            var model = GetDataModel<Model>();

            model.TargetObject = GetLocalObject(player, "TEMP_PROPERTY_TOOL_OBJECT");
            model.TargetLocation = GetLocalLocation(player, "TEMP_PROPERTY_TOOL_LOCATION");

            DeleteLocalObject(player, "TEMP_PROPERTY_TOOL_OBJECT");
            DeleteLocalLocation(player, "TEMP_PROPERTY_TOOL_LOCATION");
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var area = GetArea(player);
            var ownerPlayerUUID = GetLocalString(area, "HOUSING_OWNER_PLAYER_UUID");
            var dbHouse = DB.Get<PlayerHouse>(ownerPlayerUUID);
            var permission = dbHouse.PlayerPermissions.ContainsKey(playerId)
                ? dbHouse.PlayerPermissions[playerId]
                : new PlayerHousePermission();
            var layoutDetail = Housing.GetHouseTypeDetail(dbHouse.HouseType);
            var model = GetDataModel<Model>();

            page.Header = ColorToken.Green("Property Management Menu") + "\n" +
                          ColorToken.Green("Furniture Limit: ") + dbHouse.Furnitures.Count + " / " + layoutDetail.FurnitureLimit + "\n\n" +
                          "Please select from the options below.";

            if (permission.CanAdjustPermissions)
            {
                // todo: Currently disabled. Plan to introduce permission management at a later date.
                //page.AddResponse(ColorToken.Green("Manage Permissions"), () => ChangePage(ManagePermissionsPageId));
            }

            // Load nearby furniture.

            if (GetIsObjectValid(model.TargetObject))
            {
                LoadFurniture(dbHouse, model.TargetObject, page);
            }

            const int MaxNumberOfFurniture = 10;
            var nth = 1;
            var nearby = GetNearestObjectToLocation(model.TargetLocation, ObjectType.Placeable, nth);
            while (GetIsObjectValid(nearby))
            {
                // Reached the max.
                if (nth > MaxNumberOfFurniture) break;

                // This placeable is the same as the targeted placeable. We don't want it to show up twice.
                if (nearby == model.TargetObject) continue;

                LoadFurniture(dbHouse, nearby, page);

                nth++;
                nearby = GetNearestObjectToLocation(model.TargetLocation, ObjectType.Placeable, nth);
            }
        }

        private void LoadFurniture(PlayerHouse dbHouse, uint placeable, DialogPage page)
        {
            var furnitureId = GetLocalString(placeable, "HOUSING_FURNITURE_ID");
            if (string.IsNullOrWhiteSpace(furnitureId)) return;

            var dbFurniture = dbHouse.Furnitures[furnitureId];
            var detail = Housing.GetFurnitureDetail(dbFurniture.FurnitureType);

            page.AddResponse(detail.Name, () =>
            {
                var player = GetPC();

                SetLocalInt(player, "TEMP_FURNITURE_TYPE_ID", (int)dbFurniture.FurnitureType);
                SetLocalLocation(player, "TEMP_FURNITURE_LOCATION", GetLocation(placeable));
                SetLocalObject(player, "TEMP_FURNITURE_PLACEABLE", placeable);

                SwitchConversation(nameof(PlayerHouseFurnitureDialog), false);
            });
        }

        private void ManagePermissionsPageInit(DialogPage page)
        {

        }
    }
}
