namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>The units of one commodity in a ship's cargo bay, and what they cost in total.</summary>
    public class CargoLot
    {
        public int Units { get; set; }

        /// <summary>The credits these units cost; a sale's profit (and its Piloting XP) is measured from it.</summary>
        public int CostBasis { get; set; }
    }
}
