using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;


using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class DestroyPlayerGuide: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");
            var mainPage = new DialogPage(
                "Are you sure you want to destroy your player guide? This action is irreversible!",
                "Destroy Player Guide"
            );

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
                    NWItem item = (NWScript.GetItemPossessedBy(player.Object, "player_guide"));
                    NWScript.DestroyObject(item.Object);
                    EndConversation();
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
