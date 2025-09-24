using SWLOR.Component.Inventory.Model;
using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Component.Inventory.Service
{
    public class LootTableBuilder : ILootTableBuilder
    {
        private readonly Dictionary<string, LootTable> _lootTables = new();
        private readonly IRandomService _random;

        private LootTable _activeTable;
        private LootTableItem ActiveItem { get; set; }

        public LootTableBuilder(IRandomService random)
        {
            _random = random;
        }


        /// <summary>
        /// Creates a new loot table with the specified Id
        /// </summary>
        /// <param name="lootTableId">The loot table Id to create the table with</param>
        /// <returns>A loot table builder with the configured settings.</returns>
        public ILootTableBuilder Create(string lootTableId)
        {
            _activeTable = new LootTable(_random);
            _lootTables[lootTableId] = _activeTable;

            return this;
        }

        /// <summary>
        /// Marks the loot table as rare which will increase the chance the table is used if a player has tagged a creature with Treasure Hunter during combat.
        /// </summary>
        /// <returns>A loot table builder with the configured settings.</returns>
        public ILootTableBuilder IsRare()
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
        public ILootTableBuilder AddItem(string resref, int frequency, int maxQuantity = 1, bool isRare = false)
        {
            ActiveItem = new LootTableItem(resref, maxQuantity, frequency, isRare);
            _activeTable.Add(ActiveItem);

            return this;
        }

        /// <summary>
        /// Adds gold to this loot table.
        /// </summary>
        /// <param name="maxAmount">The max amount of gold to drop. A random value is selected, not to exceed this value.</param>
        /// <param name="frequency">The weighted chance of the gold to drop</param>
        /// <returns>A loot table builder with the configured settings.</returns>
        public ILootTableBuilder AddGold(int maxAmount, int frequency)
        {
            const string GoldResref = "nw_it_gold001";
            ActiveItem = new LootTableItem(GoldResref, maxAmount, frequency, false);
            _activeTable.Add(ActiveItem);

            return this;
        }

        /// <summary>
        /// Adds a custom spawn action to run when this item is created.
        /// This can be useful for applying random item properties, sending a message, etc.
        /// </summary>
        /// <param name="spawnAction">The action to run when the item is spawned.</param>
        /// <returns>A loot table builder with the configured settings.</returns>
        public ILootTableBuilder AddSpawnAction(Action<uint> spawnAction)
        {
            ActiveItem.OnSpawn = spawnAction;

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
