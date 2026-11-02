namespace SWLOR.Game.Server.Service.TravelerService
{
    /// <summary>An item a traveler may carry, and how many of it the traveler sells in a week.</summary>
    /// <param name="Resref">The item blueprint.</param>
    /// <param name="WeeklyLimit">How many the traveler sells a week, wherever it is. The count lives in <c>TravelerStockAllowance</c>.</param>
    public sealed record TravelerStockItem(string Resref, int WeeklyLimit);
}
