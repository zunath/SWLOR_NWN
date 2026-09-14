using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AIService
{
    public interface IAIProfileDefinition
    {
        Dictionary<AIProfileType, AIProfile> BuildProfiles();
    }
}
