using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CityStoreService
{
    /// <summary>
    /// One city store's definition: the store blueprint that opens at each level, and the limited items on top of it.
    /// </summary>
    public sealed class CityStoreDetail
    {
        public CityStoreDetail(
            CityStoreKind kind,
            string name,
            IReadOnlyList<(int MinLevel, string Blueprint)> blueprints,
            IReadOnlyList<CityStoreLimitedItem> limitedItems)
        {
            Kind = kind;
            Name = name;
            Blueprints = blueprints;
            LimitedItems = limitedItems;
        }

        public CityStoreKind Kind { get; }

        /// <summary>What the store is called to a player.</summary>
        public string Name { get; }

        /// <summary>The blueprints in order of the level that opens them. The first opens at level 1.</summary>
        public IReadOnlyList<(int MinLevel, string Blueprint)> Blueprints { get; }

        /// <summary>The limited items, whatever level stocks them.</summary>
        public IReadOnlyList<CityStoreLimitedItem> LimitedItems { get; }
    }
}
