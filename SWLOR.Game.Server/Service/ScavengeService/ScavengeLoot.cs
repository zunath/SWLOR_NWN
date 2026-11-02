using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>
    /// How a scavenge site's loot table is rolled for a find. A Search find rolls the whole table exactly as scavenging always did:
    /// Treasure Hunter adds 10 weight per level to the rare-flagged entries (<see cref="LootTable.GetRandomItem(int)"/>), a quantity of 1
    /// to the entry's maximum, and Creditfinder raises gold. A Dig Deeper find rolls the same table with the rare entries
    /// (<see cref="RareEntries"/>) favoured: their weight is multiplied by <see cref="DeeperWeightMultiplier"/>, after Treasure Hunter's.
    /// It is much better odds of a rare item, not a certainty of one, so that digging adds finds without flooding the economy with the
    /// rare items. The roll takes its chance from the minigame's source of chance so a run can be repeated.
    /// </summary>
    public static class ScavengeLoot
    {
        public const string GoldResref = "nw_it_gold001";

        /// <summary>The weight Treasure Hunter adds to a rare-flagged entry for each level (the same 10 as <see cref="LootTable"/>).</summary>
        public const int TreasureHunterWeightPerLevel = 10;

        /// <summary>The percent each Creditfinder level adds to a gold find.</summary>
        public const float CreditFinderIncreasePerLevel = 0.2f;

        /// <summary>
        /// What a Dig Deeper multiplies the weight of the table's rare entries by. A starting value: CraftSim's rare-supply tables show what
        /// it does to the rare items real scavenge sites give.
        /// </summary>
        public const int DeeperWeightMultiplier = 3;

        /// <summary>
        /// The table's rare entries, the ones a Dig Deeper favours: the entries flagged rare, or when the table has none, its least likely
        /// entries (the lowest weight, gold aside), or the whole table when every entry is equally likely.
        /// </summary>
        public static IReadOnlyList<LootTableItem> RareEntries(LootTable table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            if (table.Count == 0)
                throw new ArgumentException("No items are in this loot table.", nameof(table));

            var flagged = table.Where(item => item.IsRare).ToList();
            if (flagged.Count > 0)
                return flagged;

            var candidates = table.Where(item => item.Resref != GoldResref).ToList();
            if (candidates.Count == 0)
                return table.ToList();

            var lowest = candidates.Min(item => item.Weight);
            var highest = candidates.Max(item => item.Weight);
            return lowest == highest ? candidates : candidates.Where(item => item.Weight == lowest).ToList();
        }

        /// <summary>An entry's weight with Treasure Hunter: rare-flagged entries gain 10 per level.</summary>
        public static int WeightOf(LootTableItem item, int treasureHunterLevel) =>
            item.Weight + (treasureHunterLevel > 0 && item.IsRare ? treasureHunterLevel * TreasureHunterWeightPerLevel : 0);

        /// <summary>
        /// The weights of the entries a roll chooses from. A Dig Deeper's roll multiplies the weight of the rare entries by
        /// <see cref="DeeperWeightMultiplier"/> and the dig bonus; an ordinary find multiplies them by 1 and the find bonus (0 unless a
        /// perk grants it, which leaves the roll exactly as scavenging always did).
        /// </summary>
        public static IReadOnlyList<(LootTableItem Item, int Weight)> Weights(
            LootTable table,
            int treasureHunterLevel,
            bool deeper,
            int deeperWeightBonus = 0,
            int findWeightBonus = 0)
        {
            var boosted = deeper || findWeightBonus > 0;
            var rare = boosted ? RareEntries(table) : (IReadOnlyList<LootTableItem>)Array.Empty<LootTableItem>();
            var multiplier = deeper ? DeeperWeightMultiplier + deeperWeightBonus : 1 + findWeightBonus;
            return table
                .Select(item => (item, WeightOf(item, treasureHunterLevel) * (boosted && rare.Contains(item) ? multiplier : 1)))
                .ToList();
        }

        /// <summary>
        /// The share of a roll's chances that land on the table's rare entries: their weight over the table's total (what Treasure Hunter
        /// raises), with a Dig Deeper's multiplier on the rare entries when <paramref name="deeper"/>.
        /// </summary>
        public static double RareShare(LootTable table, int treasureHunterLevel, bool deeper, int deeperWeightBonus = 0, int findWeightBonus = 0)
        {
            var rare = RareEntries(table);
            var weights = Weights(table, treasureHunterLevel, deeper, deeperWeightBonus, findWeightBonus);
            var total = weights.Sum(entry => (double)entry.Weight);
            return total <= 0 ? 0 : weights.Where(entry => rare.Contains(entry.Item)).Sum(entry => (double)entry.Weight) / total;
        }

        /// <summary>
        /// Rolls one find. The entry is chosen by weight, then the quantity is 1 to the entry's maximum, and gold gets the Creditfinder
        /// bonus as <c>quantity += (int)(quantity * 0.2 * level)</c>.
        /// </summary>
        public static ScavengeFind Roll(
            LootTable table,
            int treasureHunterLevel,
            int creditFinderLevel,
            bool deeper,
            IMinigameRandom random,
            int deeperWeightBonus = 0,
            int findWeightBonus = 0)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var weights = Weights(table, treasureHunterLevel, deeper, deeperWeightBonus, findWeightBonus);
            var total = weights.Sum(entry => Math.Max(0, entry.Weight));
            LootTableItem chosen;
            if (total <= 0)
            {
                chosen = weights[random.Next(0, weights.Count)].Item;
            }
            else
            {
                var roll = random.Next(0, total);
                var cumulative = 0;
                chosen = weights[^1].Item;
                foreach (var (item, weight) in weights)
                {
                    cumulative += Math.Max(0, weight);
                    if (roll < cumulative)
                    {
                        chosen = item;
                        break;
                    }
                }
            }

            var quantity = random.Next(1, Math.Max(1, chosen.MaxQuantity) + 1);
            if (chosen.Resref == GoldResref)
                quantity += (int)(quantity * (creditFinderLevel * CreditFinderIncreasePerLevel));

            return new ScavengeFind(chosen.Resref, quantity, deeper);
        }
    }
}
