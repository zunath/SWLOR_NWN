using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpawnService
{
    public interface ISpawnListDefinition
    {
        /// <summary>
        /// Creates a dictionary of spawn tables to be stored in the cache.
        /// </summary>
        /// <returns>A dictionary of spawn tables.</returns>
        public Dictionary<string, SpawnTable> BuildSpawnTables();
    }
}
