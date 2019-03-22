using NWN;
using SWLOR.Game.Server.Conversation.Contracts;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Event.Dialog
{
    public class DialogEnd: IRegisteredEvent
    {
        
        private readonly IDialogService _dialog;

        public DialogEnd( IDialogService dialog)
        {
            
            _dialog = dialog;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = (_.GetPCSpeaker());
            if (!_dialog.HasPlayerDialog(player.GlobalID)) return false;

            PlayerDialog dialog = _dialog.LoadPlayerDialog(player.GlobalID);
            
            App.ResolveByInterface<IConversation>("Conversation." + dialog.ActiveDialogName, convo =>
            {
                convo.EndDialog();
                _dialog.RemovePlayerDialog(player.GlobalID);
                player.DeleteLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN");
            });
            
            return true;
        }
    }
}
