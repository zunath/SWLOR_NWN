using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.CraftingDevice
{
    public class OnUsed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDialogService _dialog;

        public OnUsed(INWScript script,
            IDialogService dialog)
        {
            _ = script;
            _dialog = dialog;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = (_.GetLastUsedBy());

            if (player.IsBusy)
            {
                player.SendMessage("You are too busy to do that right now.");
                return false;
            }
            NWPlaceable device = (Object.OBJECT_SELF);
            _dialog.StartConversation(player, device, "CraftingDevice");
            return true;
        }
    }
}
