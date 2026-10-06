using System;
namespace SWLOR.Game.Server.Service.SpaceService
{
    public enum ShipSaleSourceState { Untouched, Consumed, Invalid }
    public static class ShipTradePolicy
    {
        public static ShipSaleSourceState Source(SpaceTradeTransaction trade,int currentQuantity)
        {
            if(trade.Purchase||trade.Quantity<=0||trade.InitialQuantity<trade.Quantity||currentQuantity<0)return ShipSaleSourceState.Invalid;
            if(currentQuantity==trade.InitialQuantity)return ShipSaleSourceState.Untouched;
            return currentQuantity==trade.InitialQuantity-trade.Quantity?ShipSaleSourceState.Consumed:ShipSaleSourceState.Invalid;
        }
    }
}
