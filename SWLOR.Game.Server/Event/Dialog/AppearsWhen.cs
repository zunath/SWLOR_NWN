using NWN;
using SWLOR.Game.Server.Conversation.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Event.Dialog
{
    public class AppearsWhen: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            int nodeType = (int)args[0];
            int nodeID = (int)args[1];
            
            NWPlayer player = (_.GetPCSpeaker());
            bool hasDialog = DialogService.HasPlayerDialog(player.GlobalID);
            if (!hasDialog) return false;

            PlayerDialog dialog = DialogService.LoadPlayerDialog(player.GlobalID);
            DialogPage page = dialog.CurrentPage;
            int currentSelectionNumber = nodeID + 1;
            bool displayNode = false;
            string newNodeText = string.Empty;
            int dialogOffset = (DialogService.NumberOfResponsesPerPage + 1) * (dialog.DialogNumber - 1);

            if (currentSelectionNumber == DialogService.NumberOfResponsesPerPage + 1) // Next page
            {
                int displayCount = page.NumberOfResponses - (DialogService.NumberOfResponsesPerPage * dialog.PageOffset);

                if (displayCount > DialogService.NumberOfResponsesPerPage)
                {
                    displayNode = true;
                }
            }
            else if (currentSelectionNumber == DialogService.NumberOfResponsesPerPage + 2) // Previous Page
            {
                if (dialog.PageOffset > 0)
                {
                    displayNode = true;
                }
            }
            else if (currentSelectionNumber == DialogService.NumberOfResponsesPerPage + 3) // Back
            {
                if (dialog.NavigationStack.Count > 0 && dialog.EnableBackButton)
                {
                    displayNode = true;
                }
            }
            else if (nodeType == 2)
            {
                int responseID = (dialog.PageOffset * DialogService.NumberOfResponsesPerPage) + nodeID;
                if (responseID + 1 <= page.NumberOfResponses)
                {
                    DialogResponse response = page.Responses[responseID];

                    if (response != null)
                    {
                        newNodeText = response.Text;
                        displayNode = response.IsActive;
                    }
                }
            }
            else if (nodeType == 1)
            {
                return App.ResolveByInterface<IConversation, bool>("Conversation." + dialog.ActiveDialogName, convo =>
                {
                    if (player.GetLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN") != 1)
                    {
                        convo.Initialize();
                        player.SetLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN", 1);
                    }

                    if (dialog.IsEnding)
                    {
                        convo.EndDialog();
                        DialogService.RemovePlayerDialog(player.GlobalID);
                        player.DeleteLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN");
                        return false;
                    }

                    page = dialog.CurrentPage;
                    newNodeText = page.Header;

                    _.SetCustomToken(90000 + dialogOffset, newNodeText);
                    return true;
                });
            }

            _.SetCustomToken(90001 + nodeID + dialogOffset, newNodeText);
            return displayNode;

        }
    }
}
