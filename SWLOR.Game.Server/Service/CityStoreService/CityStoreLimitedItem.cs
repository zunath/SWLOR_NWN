namespace SWLOR.Game.Server.Service.CityStoreService
{
    /// <summary>
    /// An item a city store sells only in limited numbers: it has a weekly allowance that persists outside the store,
    /// so recreating the store never refills it.
    /// </summary>
    public sealed class CityStoreLimitedItem
    {
        public CityStoreLimitedItem(string resref, int minLevel, int weeklyLimit)
        {
            Resref = resref;
            MinLevel = minLevel;
            WeeklyLimit = weeklyLimit;
        }

        /// <summary>The item blueprint. Limited items are kept out of the store blueprints and put on the shelf from the allowance.</summary>
        public string Resref { get; }

        /// <summary>The store level from which the item is stocked.</summary>
        public int MinLevel { get; }

        /// <summary>How many the store sells a week.</summary>
        public int WeeklyLimit { get; }
    }
}
