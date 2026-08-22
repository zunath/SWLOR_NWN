#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Implementations define layout profiles (style + tuning knobs) discovered via reflection
    /// at module load, mirroring IDungeonListDefinition.
    /// </summary>
    public interface IDungeonLayoutProfileListDefinition
    {
        Dictionary<string, DungeonLayoutProfile> BuildLayoutProfiles();
    }
}
