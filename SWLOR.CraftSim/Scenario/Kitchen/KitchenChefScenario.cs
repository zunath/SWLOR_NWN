namespace SWLOR.CraftSim.Scenario.Kitchen
{
    /// <summary>
    /// The Kitchen played by a Chef with a chosen style and the whole line it can own at its rank, so the gates that need a perked cook
    /// (rotation, length, material and XP parity, enhancement parity) show that the line does not break them. A recipe is played at its
    /// level's rank, so a rank-30 Chef owns the purchases up to rank 30 and a rank-5 cook owns almost none.
    /// </summary>
    public abstract class KitchenChefScenario : KitchenScenario
    {
        private readonly KitchenBuild _build;
        private readonly string _name;
        private readonly string _title;

        protected KitchenChefScenario(string name, string title, KitchenBuild build)
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
            $"Every cook plays as a {_build.Name} Chef: the style from rank 20 and every other purchase of the line the rank allows. The unperked rank-50 result for gate 3 is in the `kitchen` scenario."
        }).ToList();

        protected override SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen.KitchenPerkStats PerkStatsFor(CraftEngineRequest request) =>
            request.Style?.Configuration is KitchenBuild build ? build.StatsAt(request.Rank) : _build.StatsAt(request.Rank);
    }

    /// <summary>The Kitchen played by a Slow Cooking Chef with the rest of the line its rank allows, Seasoned Palate off.</summary>
    public sealed class KitchenSlowCookingScenario : KitchenChefScenario
    {
        public KitchenSlowCookingScenario()
            : base("kitchen-slow-cooking", "Kitchen (Slow Cooking Chef)", KitchenBuild.SlowCookingLine)
        {
        }
    }

    /// <summary>The Kitchen played by a Flash Cooking Chef with the rest of the line its rank allows, Seasoned Palate off.</summary>
    public sealed class KitchenFlashCookingScenario : KitchenChefScenario
    {
        public KitchenFlashCookingScenario()
            : base("kitchen-flash-cooking", "Kitchen (Flash Cooking Chef)", KitchenBuild.FlashCookingLine)
        {
        }
    }

    /// <summary>The Kitchen played by a Slow Cooking Chef with the rest of the line its rank allows, Seasoned Palate on for every dish.</summary>
    public sealed class KitchenSlowCookingSeasonedScenario : KitchenChefScenario
    {
        public KitchenSlowCookingSeasonedScenario()
            : base("kitchen-slow-cooking-seasoned", "Kitchen (Slow Cooking Chef, Seasoned Palate on)", KitchenBuild.SlowCookingLineSeasoned)
        {
        }
    }

    /// <summary>The Kitchen played by a Flash Cooking Chef with the rest of the line its rank allows, Seasoned Palate on for every dish.</summary>
    public sealed class KitchenFlashCookingSeasonedScenario : KitchenChefScenario
    {
        public KitchenFlashCookingSeasonedScenario()
            : base("kitchen-flash-cooking-seasoned", "Kitchen (Flash Cooking Chef, Seasoned Palate on)", KitchenBuild.FlashCookingLineSeasoned)
        {
        }
    }
}
