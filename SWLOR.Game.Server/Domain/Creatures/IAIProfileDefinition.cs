using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Creatures
{
    public interface IAIProfileDefinition
    {
        Dictionary<AIProfileType, AIProfile> BuildProfiles();
    }
}
