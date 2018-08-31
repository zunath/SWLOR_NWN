using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable
{
    public class GenericConversation: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDialogService _dialog;

        public GenericConversation(
            INWScript script,
            IDialogService dialog)
        {
            _ = script;
            _dialog = dialog;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable placeable = NWPlaceable.Wrap(Object.OBJECT_SELF);
            NWPlayer user = NWPlayer.Wrap(_.GetLastUsedBy());
            if (!user.IsPlayer && !user.IsDM) return false;

            string conversation = placeable.GetLocalString("CONVERSATION");
            _dialog.StartConversation(user, placeable, conversation);

            return true;
        }
    }
}
