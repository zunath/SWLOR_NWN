using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Shared.Core.Models
{
    public class LootTable : List<LootTableItem>
    {
        private readonly IRandomService _random;
        public bool IsRare { get; set; }

        public LootTable(IRandomService random)
        {
            _random = random;
        }

        /// <summary>
        /// Retrieves a random item from the loot table.
        /// Throws an exception if there are no items in the loot table.
        /// </summary>
        /// <returns>A random item from the loot table.</returns>
        public LootTableItem GetRandomItem(int treasureHunterLevel)
        {
            if (Count <= 0)
                throw new Exception("No items are in this loot table.");

            var weights = new int[Count];
            for (var x = 0; x < Count; x++)
            {
                var item = this.ElementAt(x);
                var weight = item.Weight;

                // Treasure Hunter perk: Increases weight of rare items by 10 per level.
                if (treasureHunterLevel > 0 && item.IsRare)
                {
                    weight += treasureHunterLevel * 10;
                }

                weights[x] = weight;
            }

            var randomIndex = _random.GetRandomWeightedIndex(weights);
            return this.ElementAt(randomIndex);
        }

        /// <summary>
        /// Retrieves a random item from the loot table.
        /// Throws an exception if there are no items in the loot table.
        /// </summary>
        /// <returns>A random item from the loot table.</returns>
        public LootTableItem GetRandomItem()
        {
            return GetRandomItem(0);
        }
    }
}
