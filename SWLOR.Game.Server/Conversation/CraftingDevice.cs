using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class CraftingDevice: ConversationBase
    {
        private readonly IColorTokenService _color;
        private readonly ICraftService _craft;

        public CraftingDevice(
            INWScript script, 
            IDialogService dialog,
            IColorTokenService color,
            ICraftService craft) 
            : base(script, dialog)
        {
            _color = color;
            _craft = craft;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "Please select a blueprint. Only blueprints you've learned will be displayed here. Learn more blueprints by purchasing crafting perks!"
            );
            DialogPage blueprintListPage = new DialogPage(
                "Please select a blueprint. Only blueprints you've learned will be displayed here. Learn more blueprints by purchasing crafting perks!",
                "Back"
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("BlueprintListPage", blueprintListPage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadCategoryResponses();
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleCategoryResponse(responseID);
                    break;
                case "BlueprintListPage":
                    HandleBlueprintListResponse(responseID);
                    break;
            }
        }

        public override void EndDialog()
        {
        }


        private void LoadCategoryResponses()
        {
            NWPlaceable device = (NWPlaceable)GetDialogTarget();
            int deviceID = device.GetLocalInt("CRAFT_DEVICE_ID");
            List<CraftBlueprintCategory> categories = _craft.GetCategoriesAvailableToPCByDeviceID(GetPC().GlobalID, deviceID);

            ClearPageResponses("MainPage");

            foreach (CraftBlueprintCategory category in categories)
            {
                AddResponseToPage("MainPage", category.Name, category.IsActive, category.CraftBlueprintCategoryID);
            }
        }

        private void LoadBlueprintListPage(long categoryID)
        {
            NWObject device = GetDialogTarget();
            int deviceID = device.GetLocalInt("CRAFT_DEVICE_ID");

            List<CraftBlueprint> blueprints = _craft.GetPCBlueprintsByDeviceAndCategoryID(GetPC().GlobalID, deviceID, categoryID);

            ClearPageResponses("BlueprintListPage");
            foreach (CraftBlueprint bp in blueprints)
            {
                AddResponseToPage("BlueprintListPage", bp.ItemName, bp.IsActive, bp.CraftBlueprintID);
            }

            AddResponseToPage("BlueprintListPage", "Back", true, (long)-1);
        }
        
        private void HandleCategoryResponse(int responseID)
        {
            DialogResponse response = GetResponseByID("MainPage", responseID);
            long categoryID = (long)response.CustomData;
            LoadBlueprintListPage(categoryID);
            ChangePage("BlueprintListPage");
        }

        private void HandleBlueprintListResponse(int responseID)
        {
            DialogResponse response = GetResponseByID("BlueprintListPage", responseID);
            if ((long)response.CustomData == -1)
            {
                ChangePage("MainPage");
                return;
            }

            long blueprintID = (long)response.CustomData;
            var model = _craft.GetPlayerCraftingData(GetPC());
            model.BlueprintID = blueprintID;
            SwitchConversation("CraftItem");
        }
        
    }
}
