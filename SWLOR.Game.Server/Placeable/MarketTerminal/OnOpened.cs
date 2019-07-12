using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Placeable.MarketTerminal
{
    public class OnOpened: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable device = NWGameObject.OBJECT_SELF;
            NWPlayer player = _.GetLastOpenedBy();
            var model = MarketService.GetPlayerMarketData(player);

            if (model.IsSellingItem)
            {
                player.FloatingText("Please place an item you wish to sell inside of the terminal.");
            }

            device.IsLocked = true;
            return true;
        }
    }
}
