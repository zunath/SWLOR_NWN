namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>What a journaled credit payment (<see cref="CreditPaymentMark"/>) pays for.</summary>
    public enum CreditPaymentKind
    {
        Invalid = 0,
        StationFounding = 1,
        HullRefit = 2,
        CustomsFine = 3,
    }
}
