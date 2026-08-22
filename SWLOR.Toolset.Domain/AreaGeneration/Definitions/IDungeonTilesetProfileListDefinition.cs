#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Implementations define tileset profiles (tileset + placeholder + lighting + accent terrain)
    /// discovered via reflection at module load, mirroring IDungeonListDefinition.
    /// </summary>
    public interface IDungeonTilesetProfileListDefinition
    {
        Dictionary<string, DungeonTilesetProfile> BuildTilesetProfiles();
    }
}
