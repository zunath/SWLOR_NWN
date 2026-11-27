namespace SWLOR.Game.Server.Service.CityStoreService
{
    /// <summary>How many of a limited item a store puts on its shelf right now.</summary>
    public readonly record struct CityStoreShelfEntry(string Resref, int Quantity);
}
