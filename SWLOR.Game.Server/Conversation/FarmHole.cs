using SWLOR.Game.Server.GameObject;

using NWN;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class FarmHole: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "There is a small hole here. What would you like to do?",
                "Plant a seed",
                "Cover up the hole"
            );

            DialogPage coverUpConfirm = new DialogPage(
                "Are you sure you want to cover up this hole? You'll need a shovel to dig it up again!",
                "Yes, cover up the hole"
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("CoverUpConfirm", coverUpConfirm);
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
                    HandleMainPageResponse(responseID);
                    break;
                case "CoverUpConfirm":
                    HandleCoverUpConfirmResponse(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        private void HandleMainPageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Plant a seed
                    Location location = GetPC().Location;
                    NWPlaceable planter = (_.CreateObject(_.OBJECT_TYPE_PLACEABLE, "farm_plant_seed", location));
                    planter.SetLocalObject("FARM_SMALL_HOLE", GetDialogTarget().Object);
                    GetPC().AssignCommand(() => _.ActionInteractObject(planter.Object));
                    break;
                case 2: // Cover up the hole
                    ChangePage("CoverUpConfirm");
                    break;
            }
        }

        private void HandleCoverUpConfirmResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Confirm
                    GetDialogTarget().Destroy();
                    EndConversation();
                    break;
            }
        }
        public override void EndDialog()
        {
        }
    }
}
