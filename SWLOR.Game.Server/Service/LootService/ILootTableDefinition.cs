using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.LootService
{
    public interface ILootTableDefinition
    {
        /// <summary>
        /// Creates a dictionary of loot tables to be stored in the cache.
        /// </summary>
        /// <returns>A dictionary of loot tables.</returns>
        public Dictionary<string, LootTable> BuildLootTables();
    }
}
