using System;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// The flooding of one commodity at one exchange. Player sales add pressure and lower the price; purchases remove
    /// it and raise the price. Pressure decays toward 0 from <see cref="UpdatedAt"/>.
    /// </summary>
    public class TradeMarketState
    {
        public double Pressure { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
