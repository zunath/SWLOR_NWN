#nullable disable

using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    public interface IDungeonLayoutProfileListDefinition
    {
        Dictionary<string, DungeonLayoutProfile> BuildLayoutProfiles();
    }

    public interface IDungeonTilesetProfileListDefinition
    {
        Dictionary<string, DungeonTilesetProfile> BuildTilesetProfiles();
    }

    public interface IDungeonListDefinition
    {
        Dictionary<string, DungeonDetail> BuildDungeons();
    }
}
