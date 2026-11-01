namespace SWLOR.CraftSim.Scenario.Fabricator
{
    /// <summary>The Fabricator played by a Production Run Builder with the rest of the line its rank allows.</summary>
    public sealed class FabricatorProductionRunScenario : FabricatorBuilderScenario
    {
        public FabricatorProductionRunScenario()
            : base("fabricator-production-run", "Fabricator (Production Run Builder)", FabricatorBuild.ProductionRunLine)
        {
        }
    }
}
