namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>What an exchange does with a commodity: it exports it cheap, imports it at a premium, or neither.</summary>
    public enum CommodityRole
    {
        Neutral = 0,
        Producer = 1,
        Consumer = 2,
    }
}
