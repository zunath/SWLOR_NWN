using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.MarketTerminal
{
    public class OnUsed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer player = NWScript.GetLastUsedBy();
            NWPlaceable device = NWScript.OBJECT_SELF;

            if (player.IsBusy)
            {
                player.SendMessage("You are too busy to do that right now.");
                return;
            }

            DialogService.StartConversation(player, device, "MarketTerminal");
        }
    }
}
