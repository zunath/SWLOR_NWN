using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Placeable.MarketTerminal
{
    public class OnUsed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer player = _.GetLastUsedBy();
            NWPlaceable device = NWGameObject.OBJECT_SELF;

            if (player.IsBusy)
            {
                player.SendMessage("You are too busy to do that right now.");
                return false;
            }

            DialogService.StartConversation(player, device, "MarketTerminal");
            return true;
        }
    }
}
