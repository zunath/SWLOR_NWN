namespace SWLOR.Game.Server.Service.HoldingService
{
    /// <summary>What a holding's pending payment pays for (<see cref="HoldingPaymentRules"/>).</summary>
    public enum HoldingPaymentType
    {
        Invalid = 0,
        Purchase = 1,
        Upgrade = 2,
        Plant = 3,
        Upkeep = 4,
        Fine = 5,
    }
}
