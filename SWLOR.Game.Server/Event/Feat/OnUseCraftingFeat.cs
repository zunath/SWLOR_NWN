using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Feat
{
    public class OnUseCraftingFeat: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer player = Object.OBJECT_SELF;
            DialogService.StartConversation(player, player, "ModifyItemAppearance");
            return true;
        }
    }
}
