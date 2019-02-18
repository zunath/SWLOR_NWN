using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.MarketTerminal
{
    public class OnOpened: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IMarketService _market;

        public OnOpened(INWScript script, IMarketService market)
        {
            _ = script;
            _market = market;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable device = Object.OBJECT_SELF;
            NWPlayer player = _.GetLastOpenedBy();
            var model = _market.GetPlayerMarketData(player);



            device.IsLocked = true;
            return true;
        }
    }
}
