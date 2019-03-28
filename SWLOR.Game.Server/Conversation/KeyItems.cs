using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class KeyItems: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {

            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "Select a key item category.",
                "Maps",
                "Quest Items",
                "Documents",
                "Keys"
            );

            DialogPage keyItemListPage = new DialogPage(
                "Select a key item."
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("KeyItemsListPage", keyItemListPage);
            return dialog;
        }

        public override void Initialize()
        {
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    switch (responseID)
                    {
                        case 1: // "Maps"
                        case 2: // "Quest Items"
                        case 3: // "Documents"
                            GetPC().SetLocalInt("TEMP_MENU_KEY_ITEM_CATEGORY_ID", responseID);
                            LoadKeyItemsOptions(responseID);
                            break;
                        case 4:
                            GetPC().SetLocalInt("TEMP_MENU_KEY_ITEM_CATEGORY_ID", 5);
                            LoadKeyItemsOptions(5);
                            break;
                    }
                    break;
                case "KeyItemsListPage":
                    HandleKeyItemSelection(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
            ClearTempVariables();
        }

        private void ClearTempVariables()
        {
            GetPC().DeleteLocalString("TEMP_MENU_KEY_ITEM_CATEGORY_ID");
            SetPageHeader("KeyItemsListPage", "Select a key item.");
        }

        private void LoadKeyItemsOptions(int categoryID)
        {
            List<PCKeyItem> items = KeyItemService.GetPlayerKeyItemsByCategory(GetPC(), categoryID).ToList();

            ClearPageResponses("KeyItemsListPage");
            foreach (PCKeyItem item in items)
            {
                var keyItem = KeyItemService.GetKeyItemByID(item.KeyItemID);
                AddResponseToPage("KeyItemsListPage", keyItem.Name, true, item.KeyItemID);
            }
            ChangePage("KeyItemsListPage");
        }

        private void HandleKeyItemSelection(int responseID)
        {
            DialogResponse response = GetResponseByID(GetCurrentPageName(), responseID);
            int keyItemID = (int)response.CustomData;

            if (keyItemID <= 0)
            {
                ClearTempVariables();
                ClearNavigationStack();
                ChangePage("MainPage", false);
            }
            else
            {
                SetPageHeader("KeyItemsListPage", BuildKeyItemHeader(responseID));
            }
        }

        private string BuildKeyItemHeader(int responseID)
        {
            DialogResponse response = GetResponseByID(GetCurrentPageName(), responseID);
            int keyItemID = (int)response.CustomData;
            KeyItem entity = KeyItemService.GetKeyItemByID(keyItemID);

            string header = ColorTokenService.Green("Key Item: ") + entity.Name + "\n\n";
            header += ColorTokenService.Green("Description: ") + entity.Description + "\n";

            return header;
        }

    }
}
