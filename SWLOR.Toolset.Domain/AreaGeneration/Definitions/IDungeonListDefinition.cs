#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Implemented by classes which define one or more procedural dungeon themes.
    /// Discovered via reflection by DungeonContentPlacer at module load, mirroring
    /// ISpawnListDefinition/ILootTableDefinition/IAbilityListDefinition.
    /// </summary>
    public interface IDungeonListDefinition
    {
        /// <summary>
        /// Creates a dictionary of dungeon theme definitions to be stored in the cache.
        /// </summary>
        /// <returns>A dictionary of dungeon definitions, keyed by theme key.</returns>
        public Dictionary<string, DungeonDetail> BuildDungeons();
    }
}
