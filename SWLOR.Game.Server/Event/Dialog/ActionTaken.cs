using NWN;
using SWLOR.Game.Server.Conversation.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Event.Dialog
{
    public class ActionTaken: IRegisteredEvent
    {

        public bool Run(params object[] args)
        {
            int nodeID = (int)args[0];
            NWPlayer player = (_.GetPCSpeaker());
            PlayerDialog dialog = DialogService.LoadPlayerDialog(player.GlobalID);
            int selectionNumber = nodeID + 1;
            int responseID = nodeID + (DialogService.NumberOfResponsesPerPage * dialog.PageOffset);

            if (selectionNumber == DialogService.NumberOfResponsesPerPage + 1) // Next page
            {
                dialog.PageOffset = dialog.PageOffset + 1;
            }
            else if (selectionNumber == DialogService.NumberOfResponsesPerPage + 2) // Previous page
            {
                dialog.PageOffset = dialog.PageOffset - 1;
            }
            else if (selectionNumber == DialogService.NumberOfResponsesPerPage + 3) // Back
            {
                string currentPageName = dialog.CurrentPageName;
                var previous = dialog.NavigationStack.Pop();
                
                // This might be a little confusing but we're passing the active page as the "old page" to the Back() method.
                // This is because we need to run any dialog-specific clean up prior to moving the conversation backwards.
                App.ResolveByInterface<IConversation>("Conversation." + dialog.ActiveDialogName, convo =>
                {
                    convo.Back(player, currentPageName, previous.PageName);
                });

                // Previous page was in a different conversation. Switch to it.
                if (previous.DialogName != dialog.ActiveDialogName)
                {
                    DialogService.LoadConversation(player, dialog.DialogTarget, previous.DialogName, dialog.DialogNumber);
                    dialog = DialogService.LoadPlayerDialog(player.GlobalID);
                    dialog.ResetPage();

                    dialog.CurrentPageName = previous.PageName;
                    dialog.PageOffset = 0;

                    App.ResolveByInterface<IConversation>("Conversation." + dialog.ActiveDialogName, convo =>
                    {
                        convo.Initialize();
                        player.SetLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN", 1);
                    });
                }
                // Otherwise it's in the same conversation. Switch to that.
                else
                {
                    dialog.CurrentPageName = previous.PageName;
                    dialog.PageOffset = 0;
                }
            }
            else if (selectionNumber != DialogService.NumberOfResponsesPerPage + 4) // End
            {
                App.ResolveByInterface<IConversation>("Conversation." + dialog.ActiveDialogName, convo =>
                {
                    convo.DoAction(player, dialog.CurrentPageName, responseID + 1);
                });
            }

            return true;
        }
    }
}
