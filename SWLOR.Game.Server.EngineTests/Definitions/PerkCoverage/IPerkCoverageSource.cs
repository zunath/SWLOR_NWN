using System.Collections.Generic;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    /// <summary>
    /// A per-tree collection of perk coverage cases. Implementations are discovered by
    /// reflection: the NUnit ratchet (PerkCoverageTests) asserts that every registered
    /// perk has exactly one case and that each case matches the perk its definition
    /// actually builds.
    /// </summary>
    public interface IPerkCoverageSource
    {
        List<PerkCoverageCase> BuildCases();
    }
}
