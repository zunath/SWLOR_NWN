using SWLOR.Component.World.Model;

namespace SWLOR.Component.World.Contracts
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
