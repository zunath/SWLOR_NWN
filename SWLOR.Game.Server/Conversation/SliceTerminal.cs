using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class SliceTerminal: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");

            var mainPage = new DialogPage("You can slice this terminal. What would you like to do?",
                "Slice the terminal");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (responseID)
            {
                case 1:
                    DoSlice();
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        private void DoSlice()
        {
            NWPlaceable self = NWScript.OBJECT_SELF;
            var keyItemID = self.GetLocalInt("KEY_ITEM_ID");

            if (keyItemID <= 0)
            {
                GetPC().SendMessage("ERROR: Improperly configured key item. ID is not set. Notify an admin.");
                return;
            }

            KeyItemService.GivePlayerKeyItem(GetPC(), keyItemID);

            var visibilityObjectID = self.GetLocalString("VISIBILITY_OBJECT_ID");

            if (!string.IsNullOrWhiteSpace(visibilityObjectID))
            {
                ObjectVisibilityService.AdjustVisibility(GetPC(), self, false);
            }

            EndConversation();
        }

        public override void EndDialog()
        {
        }
    }
}
