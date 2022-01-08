using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class RenameItemDialog : DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";
        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit)
                .AddEndAction(()=> { CleanUp(player); });

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            SetLocalInt(player, "ITEM_RENAMING_LISTENING", 1);

            var item = GetLocalObject(player, "ITEM_BEING_RENAMED");
            string originalName = GetLocalString(item, "RENAMED_ITEM_ORIGINAL_NAME");
            string currentName = GetName(item);
            if (string.IsNullOrWhiteSpace(originalName))
                originalName = currentName;

            string header = "You are renaming an item.\n\n";
            header += ColorToken.Green("Original Name: ") + originalName + "\n";
            header += ColorToken.Green("Current Name: ") + currentName + "\n";
            header += "Type in a new name, and then select 'Change Name' to make the changes. Click 'Reset Name' to switch back to the item's original name.";

            page.Header = header;

            page.AddResponse("Change Name", ChangeName);

            page.AddResponse("Reset", ResetName);
        }

        private void ChangeName()
        {
            var player = GetPC();

            // Player hasn't specified a new name for this item.
            string newName = GetLocalString(player, "RENAMED_ITEM_NEW_NAME");
            if (string.IsNullOrWhiteSpace(newName))
            {
                FloatingTextStringOnCreature("Enter a new name into the chat box, select 'Refresh' then select 'Change Name'.", player);
                return;
            }
            var item = GetLocalObject(player, "ITEM_BEING_RENAMED");

            // Item isn't in player's inventory.
            if (GetItemPossessor(item) != player)
            {
                FloatingTextStringOnCreature("Item must be in your inventory in order to rename it.", player);
                return;
            }

            // Item's original name isn't being stored. Do that now.
            if (string.IsNullOrWhiteSpace(GetLocalString(item, "RENAMED_ITEM_ORIGINAL_NAME")))
                SetLocalString(item, "RENAMED_ITEM_ORIGINAL_NAME", GetName(item));

            SetName(item, newName);

            FloatingTextStringOnCreature("Item renamed to '" + newName + "'.", player);
            EndConversation();
        }

        private void ResetName()
        {
            var player = GetPC();
            var item = GetLocalObject(player, "ITEM_BEING_RENAMED");
            if (string.IsNullOrWhiteSpace(GetLocalString(item, "RENAMED_ITEM_ORIGINAL_NAME")))
                SetLocalString(item, "RENAMED_ITEM_ORIGINAL_NAME", GetName(item));

            SetName(item, GetLocalString(item, "RENAMED_ITEM_ORIGINAL_NAME"));
        }

        public void CleanUp(uint player)
        {
            DeleteLocalObject(player, "ITEM_BEING_RENAMED");
            DeleteLocalInt(player, "ITEM_RENAMING_LISTENING");
            DeleteLocalString(player, "RENAMED_ITEM_NEW_NAME");
        }

        [NWNEventHandler("on_nwnx_chat")]
        public static void HandleChatMessage()
        {
            var sender = OBJECT_SELF;

            if (GetLocalInt(sender, "ITEM_RENAMING_LISTENING") == 0) return;

            var originalMessage = ChatPlugin.GetMessage().Trim();
            SetLocalString(sender, "RENAMED_ITEM_NEW_NAME", originalMessage);

        }
    }
}