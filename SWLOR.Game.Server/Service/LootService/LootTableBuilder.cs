using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.LootService
{
    public class LootTableBuilder
    {
        private readonly Dictionary<string, LootTable> LootTables = new Dictionary<string, LootTable>();

        private LootTable ActiveTable { get; set; }


        /// <summary>
        /// Creates a new loot table with the specified Id
        /// </summary>
        /// <param name="lootTableId">The loot table Id to create the table with</param>
        /// <returns>A loot table builder with the configured settings.</returns>
        public LootTableBuilder Create(string lootTableId)
        {
            ActiveTable = new LootTable();
            LootTables[lootTableId] = ActiveTable;

            return this;
        }

        /// <summary>
        /// Marks the loot table as rare which will increase the chance the table is used if a player has tagged a creature with Treasure Hunter during combat.
        /// </summary>
        /// <returns>A loot table builder with the configured settings.</returns>
        public LootTableBuilder IsRare()
        {
            ActiveTable.IsRare = true;

            return this;
        }

        /// <summary>
        /// Adds an item to this loot table.
        /// </summary>
        /// <param name="resref">The resref of the item</param>
        /// <param name="frequency">The weighted frequency chance of the item to drop</param>
        /// <param name="maxQuantity">The max quantity of this item to drop. A random value is selected, not to exceed this value.</param>
        /// <param name="isRare">If true, item frequency will be adjusted by the Treasure Hunter perk.</param>
        /// <returns>A loot table builder with the configured settings.</returns>
        public LootTableBuilder AddItem(string resref, int frequency, int maxQuantity = 1, bool isRare = false)
        {
            ActiveTable.Add(new LootTableItem(resref, maxQuantity, frequency, isRare));

            return this;
        }

        /// <summary>
        /// Adds gold to this loot table.
        /// </summary>
        /// <param name="maxAmount">The max amount of gold to drop. A random value is selected, not to exceed this value.</param>
        /// <param name="frequency">The weighted chance of the gold to drop</param>
        /// <returns>A loot table builder with the configured settings.</returns>
        public LootTableBuilder AddGold(int maxAmount, int frequency)
        {
            const string GoldResref = "nw_it_gold001";
            ActiveTable.Add(new LootTableItem(GoldResref, maxAmount, frequency, false));

            return this;
        }

        /// <summary>
        /// Builds a dictionary of loot tables.
        /// </summary>
        /// <returns>A dictionary of loot tables.</returns>
        public Dictionary<string, LootTable> Build()
        {
            return LootTables;
        }
    }
}
