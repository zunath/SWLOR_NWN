using System.Collections.Generic;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// A per-tree collection of ability behavior cases. Implementations are discovered by
    /// reflection: the engine-side executor runs every source's cases inside the live server,
    /// and the NUnit coverage ratchet (AbilityBehaviorCoverageTests) asserts that every
    /// registered ability feat has exactly one case across all sources.
    /// </summary>
    public interface IAbilityBehaviorSource
    {
        List<AbilityBehaviorCase> BuildCases();
    }
}
