using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Feat
{
    public class OnUseCraftingFeat: IRegisteredEvent
    {
        private readonly IDialogService _dialog;

        public OnUseCraftingFeat(IDialogService dialog)
        {
            _dialog = dialog;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = Object.OBJECT_SELF;
            _dialog.StartConversation(player, player, "ModifyItemAppearance");
            return true;
        }
    }
}
