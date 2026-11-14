namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// A freight contract's sealed load in a ship's cargo bay (plan section 13). It fills cargo volume, cannot be sold
    /// and is not scanned by customs; the contract itself is on the owner's player record.
    /// </summary>
    public class SealedFreightLot
    {
        public string ContractId { get; set; }
        public int Volume { get; set; }
        public TradeExchangeType Destination { get; set; }
    }
}
