using SWLOR.Component.Inventory.Model;

namespace SWLOR.Component.Inventory.Service;

public interface ILootTableBuilder
{
    /// <summary>
    /// Creates a new loot table with the specified Id
    /// </summary>
    /// <param name="lootTableId">The loot table Id to create the table with</param>
    /// <returns>A loot table builder with the configured settings.</returns>
    ILootTableBuilder Create(string lootTableId);

    /// <summary>
    /// Marks the loot table as rare which will increase the chance the table is used if a player has tagged a creature with Treasure Hunter during combat.
    /// </summary>
    /// <returns>A loot table builder with the configured settings.</returns>
    ILootTableBuilder IsRare();

    /// <summary>
    /// Adds an item to this loot table.
    /// </summary>
    /// <param name="resref">The resref of the item</param>
    /// <param name="frequency">The weighted frequency chance of the item to drop</param>
    /// <param name="maxQuantity">The max quantity of this item to drop. A random value is selected, not to exceed this value.</param>
    /// <param name="isRare">If true, item frequency will be adjusted by the Treasure Hunter perk.</param>
    /// <returns>A loot table builder with the configured settings.</returns>
    ILootTableBuilder AddItem(string resref, int frequency, int maxQuantity = 1, bool isRare = false);

    /// <summary>
    /// Adds gold to this loot table.
    /// </summary>
    /// <param name="maxAmount">The max amount of gold to drop. A random value is selected, not to exceed this value.</param>
    /// <param name="frequency">The weighted chance of the gold to drop</param>
    /// <returns>A loot table builder with the configured settings.</returns>
    ILootTableBuilder AddGold(int maxAmount, int frequency);

    /// <summary>
    /// Adds a custom spawn action to run when this item is created.
    /// This can be useful for applying random item properties, sending a message, etc.
    /// </summary>
    /// <param name="spawnAction">The action to run when the item is spawned.</param>
    /// <returns>A loot table builder with the configured settings.</returns>
    ILootTableBuilder AddSpawnAction(Action<uint> spawnAction);

    /// <summary>
    /// Builds a dictionary of loot tables.
    /// </summary>
    /// <returns>A dictionary of loot tables.</returns>
    Dictionary<string, LootTable> Build();
}