namespace SWLOR.Game.Server.Service.CityTreasuryService
{
    /// <summary>
    /// What a spend took from a city: how much from the restricted balance and how much from the ordinary treasury. A
    /// failed spend took nothing.
    /// </summary>
    public readonly record struct TreasurySpend(bool Succeeded, long Restricted, long Unrestricted)
    {
        public long Total => Restricted + Unrestricted;

        public static TreasurySpend Failed => new(false, 0, 0);
    }
}
