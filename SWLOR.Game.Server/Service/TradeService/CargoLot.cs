namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>The units of one commodity in a ship's cargo bay, and what they cost in total.</summary>
    public class CargoLot
    {
        public int Units { get; set; }

        /// <summary>The credits these units cost; a sale's profit (and its Piloting XP) is measured from it.</summary>
        public int CostBasis { get; set; }

        /// <summary>
        /// The Piloting XP sales from this lot already gave. The sale cap is the lot's, so selling it in many small orders
        /// gives no more than one order would. It goes with the lot when its last unit is sold.
        /// </summary>
        public int XpEarned { get; set; }
    }
}
