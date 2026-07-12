using System.Collections.Generic;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Feature.DungeonDefinition
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
