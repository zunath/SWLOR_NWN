namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>What a delivery pays: the fee, the deposit back when on time, and whether it was late.</summary>
    public class FreightDeliveryResult
    {
        public FreightRefusal Refusal { get; set; }
        public int Fee { get; set; }
        public int DepositReturned { get; set; }
        public bool IsLate { get; set; }

        public static FreightDeliveryResult Refused(FreightRefusal refusal) => new() { Refusal = refusal };
    }
}
