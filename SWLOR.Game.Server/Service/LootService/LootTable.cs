using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.LootService
{
    public class LootTable : List<LootTableItem>
    {
        /// <summary>
        /// The rare find bonus granted by Treasure Hunter III.
        /// </summary>
        public const int FullRareChanceBonus = 30;

        public bool IsRare { get; set; }

        /// <summary>
        /// Percentage points a full rare find bonus adds to this table's drop chance.
        /// Null means the bonus is added unscaled.
        /// </summary>
        public int? MaxRareChanceBonus { get; set; }

        /// <summary>
        /// Converts a creature's rare find bonus into the percentage points added to this table's drop chance.
        /// </summary>
        /// <param name="rareBonusChance">The rare find bonus marked on the creature.</param>
        /// <returns>The percentage points to add to the drop chance.</returns>
        public int GetRareChanceBonus(int rareBonusChance)
        {
            if (MaxRareChanceBonus == null)
                return rareBonusChance;

            return rareBonusChance * MaxRareChanceBonus.Value / FullRareChanceBonus;
        }

        /// <summary>
        /// Retrieves a random item from the loot table.
        /// Throws an exception if there are no items in the loot table.
        /// </summary>
        /// <returns>A random item from the loot table.</returns>
        public LootTableItem GetRandomItem(int treasureHunterLevel)
        {
            return GetRandomItemWithRareBonus(treasureHunterLevel * 10);
        }

        /// <summary>
        /// Retrieves a random item, adding a rare find bonus (already in Treasure Hunter points: 10 per level)
        /// to the weight of each rare item.
        /// </summary>
        /// <param name="rareBonusChance">The rare find bonus, such as the value marked on a creature.</param>
        /// <returns>A random item from the loot table.</returns>
        public LootTableItem GetRandomItemWithRareBonus(int rareBonusChance)
        {
            if (Count <= 0)
                throw new Exception("No items are in this loot table.");

            var weights = new int[Count];
            for (var x = 0; x < Count; x++)
            {
                var item = this.ElementAt(x);
                var weight = item.Weight;

                // Treasure Hunter perk: Increases weight of rare items by 10 per level.
                if (rareBonusChance > 0 && item.IsRare)
                {
                    weight += rareBonusChance;
                }

                weights[x] = weight;
            }

            var randomIndex = Random.GetRandomWeightedIndex(weights);
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
