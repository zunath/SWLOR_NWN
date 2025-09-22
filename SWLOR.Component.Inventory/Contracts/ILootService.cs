using SWLOR.Component.Inventory.Model;

namespace SWLOR.Component.Inventory.Contracts
{
    public interface ILootService
    {
        void RegisterLootTables();

        /// <summary>
        /// When a creature spawns, items which can be stolen are spawned and marked as undroppable.
        /// These items are only available with the Thief ability "Steal" and related perks.
        /// </summary>
        void SpawnStealLoot();

        /// <summary>
        /// Attempts to spawn items found in the associated loot table based on the configured variables.
        /// </summary>
        /// <param name="source">The source of the items (must contain the local variables)</param>
        /// <param name="receiver">The receiver of the items</param>
        /// <param name="lootTablePrefix">The prefix of the loot tables. In the toolset this should be numeric starting at 1.</param>
        void SpawnLoot(uint source, uint receiver, string lootTablePrefix);

        /// <summary>
        /// When a creature dies, loot tables are spawned based on local variables.
        /// </summary>
        void SpawnLootOnCreatureDeath();

        /// <summary>
        /// Retrieves a loot table by its unique name.
        /// If name is not registered, an exception will be raised.
        /// </summary>
        /// <param name="name">The name of the loot table to retrieve.</param>
        /// <returns>A loot table matching the specified name.</returns>
        LootTable GetLootTableByName(string name);

        /// <summary>
        /// Checks whether a loot table is defined by the specified name.
        /// </summary>
        /// <param name="name">The name to search by.</param>
        /// <returns>true if the loot table exists, false otherwise</returns>
        bool LootTableExists(string name);

        /// <summary>
        /// When a creature is hit by another creature with the Creditfinder or Treasure Hunter perk,
        /// a local variable is set on the creature which will be picked up when spawning items.
        /// These will be checked later when the creature dies and loot is spawned.
        /// </summary>
        void MarkCreditfinderAndTreasureHunterOnTarget();

        /// <summary>
        /// Handles creating a corpse placeable on a creature's death, copying its inventory to the placeable,
        /// and changing the name of the placeable to match the creature.
        /// </summary>
        void ProcessCorpse();

        /// <summary>
        /// When the loot corpse is closed, either spawn an "Extract" placeable to be used with Beast Mastery DNA extraction
        /// or remove the dead creature from the game.
        /// </summary>
        void CloseCorpseContainer();

        /// <summary>
        /// When a player adds an item to a corpse, return it to them.
        /// When a player removes an item from the corpse, update the connected creature's appearance if needed.
        /// </summary>
        void DisturbCorpseContainer();
    }
}