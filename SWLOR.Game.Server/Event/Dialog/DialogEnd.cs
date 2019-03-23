using NWN;
using SWLOR.Game.Server.Conversation.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Event.Dialog
{
    public class DialogEnd: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer player = (_.GetPCSpeaker());
            if (!DialogService.HasPlayerDialog(player.GlobalID)) return false;

            PlayerDialog dialog = DialogService.LoadPlayerDialog(player.GlobalID);
            
            App.ResolveByInterface<IConversation>("Conversation." + dialog.ActiveDialogName, convo =>
            {
                convo.EndDialog();
                DialogService.RemovePlayerDialog(player.GlobalID);
                player.DeleteLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN");
            });
            
            return true;
        }
    }
}
