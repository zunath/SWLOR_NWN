using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.MarketTerminal
{
    public class OnOpened: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable device = NWScript.OBJECT_SELF;
            NWPlayer player = NWScript.GetLastOpenedBy();
            var model = MarketService.GetPlayerMarketData(player);

            if (model.IsSellingItem)
            {
                player.FloatingText("Please place an item you wish to sell inside of the terminal.");
            }

            device.IsLocked = true;
        }
    }
}
