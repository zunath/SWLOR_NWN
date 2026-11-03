using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of loot: loot tables, the loot a creature carries and drops, and its corpse. The logic lives in
    /// <see cref="ILootService"/>; every member here forwards to the container's instance.
    /// </summary>
    public static class Loot
    {
        private static ILootService Instance => FacadeAccessor.Resolve<ILootService>();

        public const string CorpseBodyVariable = Domain.Items.LootService.CorpseBodyVariable;

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void RegisterLootTables()
        {
            Instance.RegisterLootTables();
        }

        /// <summary>
        /// When a creature spawns, items which can be stolen are spawned and marked as undroppable.
        /// These items are only available with the Thief ability "Steal" and related perks.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureSpawnBefore)]
        public static void SpawnStealLoot()
        {
            Instance.SpawnStealLoot();
        }

        /// <summary>
        /// Attempts to spawn items found in the associated loot table based on the configured variables.
        /// </summary>
        /// <param name="source">The source of the items (must contain the local variables)</param>
        /// <param name="receiver">The receiver of the items</param>
        /// <param name="lootTablePrefix">The prefix of the loot tables. In the toolset this should be numeric starting at 1.</param>
        public static void SpawnLoot(uint source, uint receiver, string lootTablePrefix)
        {
            Instance.SpawnLoot(source, receiver, lootTablePrefix);
        }

        /// <summary>
        /// When a creature dies, loot tables are spawned based on local variables.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void SpawnLootOnCreatureDeath()
        {
            Instance.SpawnLootOnCreatureDeath();
        }

        /// <summary>
        /// Retrieves a loot table by its unique name.
        /// If name is not registered, an exception will be raised.
        /// </summary>
        /// <param name="name">The name of the loot table to retrieve.</param>
        /// <returns>A loot table matching the specified name.</returns>
        public static LootTable GetLootTableByName(string name)
        {
            return Instance.GetLootTableByName(name);
        }

        /// <summary>
        /// Checks whether a loot table is defined by the specified name.
        /// </summary>
        /// <param name="name">The name to search by.</param>
        /// <returns>true if the loot table exists, false otherwise</returns>
        public static bool LootTableExists(string name)
        {
            return Instance.LootTableExists(name);
        }

        /// <summary>
        /// When a creature is hit by another creature with the Creditfinder or Treasure Hunter perk,
        /// a local variable is set on the creature which will be picked up when spawning items.
        /// These will be checked later when the creature dies and loot is spawned.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemHit)]
        public static void MarkCreditfinderAndTreasureHunterOnTarget()
        {
            Instance.MarkCreditfinderAndTreasureHunterOnTarget();
        }

        public static void RecordWeaponHitFindBonuses(uint attacker, uint target)
        {
            Instance.RecordWeaponHitFindBonuses(attacker, target);
        }

        /// <summary>
        /// Handles creating a corpse placeable on a creature's death, copying its inventory to the placeable,
        /// and changing the name of the placeable to match the creature.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void ProcessCorpse()
        {
            Instance.ProcessCorpse();
        }

        /// <summary>
        /// When the loot corpse is closed, either spawn an "Extract" placeable to be used with Beast Mastery DNA extraction
        /// or remove the dead creature from the game.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCorpseClosed)]
        public static void CloseCorpseContainer()
        {
            Instance.CloseCorpseContainer();
        }

        /// <summary>
        /// When a player adds an item to a corpse, return it to them.
        /// When a player removes an item from the corpse, update the connected creature's appearance if needed.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCorpseDisturbed)]
        public static void DisturbCorpseContainer()
        {
            Instance.DisturbCorpseContainer();
        }
    }
}
