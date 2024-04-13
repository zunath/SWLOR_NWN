using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.LootService
{
    public class LootTableBuilder
    {
        private readonly Dictionary<string, LootTable> _lootTables = new();

        private LootTable _activeTable;
        private LootTableItem _activeItem { get; set; }


        /// <summary>
        /// Creates a new loot table with the specified Id
        /// </summary>
        /// <param name="lootTableId">The loot table Id to create the table with</param>
        /// <returns>A loot table builder with the configured settings.</returns>
        public LootTableBuilder Create(string lootTableId)
        {
            _activeTable = new LootTable();
            _lootTables[lootTableId] = _activeTable;

            return this;
        }

        /// <summary>
        /// Marks the loot table as rare which will increase the chance the table is used if a player has tagged a creature with Treasure Hunter during combat.
        /// </summary>
        /// <returns>A loot table builder with the configured settings.</returns>
        public LootTableBuilder IsRare()
        {
            _activeTable.IsRare = true;

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
            _activeItem = new LootTableItem(resref, maxQuantity, frequency, isRare);
            _activeTable.Add(_activeItem);

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
            _activeItem = new LootTableItem(GoldResref, maxAmount, frequency, false);
            _activeTable.Add(_activeItem);

            return this;
        }

        /// <summary>
        /// Adds a custom spawn action to run when this item is created.
        /// This can be useful for applying random item properties, sending a message, etc.
        /// </summary>
        /// <param name="spawnAction">The action to run when the item is spawned.</param>
        /// <returns>A loot table builder with the configured settings.</returns>
        public LootTableBuilder AddSpawnAction(Action<uint> spawnAction)
        {
            _activeItem.OnSpawn = spawnAction;

            return this;
        }

        /// <summary>
        /// Builds a dictionary of loot tables.
        /// </summary>
        /// <returns>A dictionary of loot tables.</returns>
        public Dictionary<string, LootTable> Build()
        {
            return _lootTables;
        }
    }
}
