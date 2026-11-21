using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.TravelerService
{
    /// <summary>
    /// A pool a traveler's weekly stock is drawn from. Each week the traveler carries <see cref="Picks"/> items of the pool,
    /// the same ones for everyone and wherever the traveler is, and a different draw the next week.
    /// </summary>
    public sealed class TravelerStockGroup
    {
        public TravelerStockGroup(string name, int picks)
        {
            Name = name;
            Picks = picks;
        }

        /// <summary>What the pool is, such as "Seeds". It also seeds the draw, so it never changes.</summary>
        public string Name { get; }

        /// <summary>How many different items of the pool the traveler carries in a week.</summary>
        public int Picks { get; }

        public List<TravelerStockItem> Items { get; } = new();
    }
}
