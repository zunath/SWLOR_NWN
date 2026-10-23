namespace SWLOR.CraftSim.Scenario.Fabricator
{
    /// <summary>The Fabricator played by a Fine Tolerance Builder with the rest of the line its rank allows.</summary>
    public sealed class FabricatorFineToleranceScenario : FabricatorBuilderScenario
    {
        public FabricatorFineToleranceScenario()
            : base("fabricator-fine-tolerance", "Fabricator (Fine Tolerance Builder)", FabricatorBuild.FineToleranceLine)
        {
        }
    }
}
