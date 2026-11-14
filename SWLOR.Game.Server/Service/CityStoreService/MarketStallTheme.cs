namespace SWLOR.Game.Server.Service.CityStoreService
{
    /// <summary>
    /// The stock theme a mayor picks for a market stall. Saved on the stall's structure record, so the numbers never
    /// change. A stall with no theme sells general goods.
    /// </summary>
    public enum MarketStallTheme
    {
        Invalid = 0,
        GeneralGoods = 1,
        CraftingSupplies = 2,
        Decor = 3
    }
}
