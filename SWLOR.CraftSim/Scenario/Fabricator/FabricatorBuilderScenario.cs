using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;

namespace SWLOR.CraftSim.Scenario.Fabricator
{
    /// <summary>
    /// The Fabricator played by a Builder with a chosen style and the whole line it can own at its rank, so the gates that need a
    /// perked crafter (rotation, length, material and XP parity, enhancement parity) show that the line does not break them. A recipe
    /// is played at its level's rank, so a rank-30 Builder owns the purchases up to rank 30 and a rank-5 crafter owns almost none.
    /// </summary>
    public abstract class FabricatorBuilderScenario : FabricatorScenario
    {
        private readonly FabricatorBuild _build;
        private readonly string _name;
        private readonly string _title;

        protected FabricatorBuilderScenario(string name, string title, FabricatorBuild build)
        {
            _name = name;
            _title = title;
            _build = build;
        }

        public override string Name => _name;

        public override string Title => _title;

        /// <summary>The style comparison belongs to the unperked scenario, which plays both styles.</summary>
        public override IReadOnlyList<CraftStylePair> StylePairs => Array.Empty<CraftStylePair>();

        public override IReadOnlyList<string> Limitations => base.Limitations.Concat(new[]
        {
            $"Every crafter plays as a {_build.Name} Builder: the style from rank 20 and every other purchase of the line the rank allows. The unperked rank-50 result for gate 3 is in the `fabricator` scenario."
        }).ToList();

        protected override FabricatorPerkStats PerkStatsFor(CraftEngineRequest request) =>
            request.Style?.Configuration is FabricatorBuild build ? build.StatsAt(request.Rank) : _build.StatsAt(request.Rank);
    }
}
