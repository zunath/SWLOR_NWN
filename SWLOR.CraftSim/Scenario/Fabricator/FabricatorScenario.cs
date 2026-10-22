using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Scenario.Fabricator
{
    /// <summary>
    /// Fabrication's item-craft minigame, the Fabricator, for the balance gates. It builds the real engine from the real recipe
    /// definition through <see cref="FabricatorSetup.ForRecipe"/>, the same call the crafting window makes, so the gates play the
    /// rules the game ships and not a copy of them.
    /// </summary>
    public sealed class FabricatorScenario : ICraftScenario
    {
        public string Name => "fabricator";

        public string Title => "Fabricator";

        public SkillType Skill => SkillType.Fabrication;

        public CraftMinigameType Minigame => CraftMinigameType.Fabricator;

        /// <summary>
        /// 8 seconds an action: the Fabricator window shows nine actions and the previews of a three-head game take longer to read than
        /// the old window's eleven, which the legacy default sizes at 6 seconds. An estimate until client timing replaces it.
        /// </summary>
        public double SecondsPerAction => 8.0;

        /// <summary>
        /// The default search with a heuristic that knows the item is made by Assemble and not by a full progress meter
        /// (<see cref="FabricatorLeafHeuristic"/>).
        /// </summary>
        public AdaptivePolicyOptions AdaptiveOptions => AdaptivePolicyOptions.Default with { Heuristic = new FabricatorLeafHeuristic() };

        public IReadOnlyList<string> Limitations => new[]
        {
            "No enhancements are slotted: the item is made at its base size, the way the gates' cohort plays every minigame.",
            "Prime Feedstock is the only chance in the Fabricator, so the adaptive player's edge over a fixed rotation comes from it and from the recipe's state, not from luck at the actions themselves."
        };

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request)
        {
            var recipe = request.Recipe;
            var gear = request.Gear;
            var setup = recipe.Detail != null
                ? FabricatorSetup.ForRecipe(recipe.Detail, request.Rank, gear.Craftsmanship, gear.Control, gear.CpBonus)
                : new FabricatorSetup(
                    null,
                    recipe.Level,
                    SWLOR.Game.Server.Service.Craft.GetRecipeLevelDetail(recipe.Level).Progress,
                    request.Rank,
                    gear.Craftsmanship,
                    gear.Control,
                    gear.CpBonus);

            return new FabricatorEngine(request.Random, setup);
        }
    }
}
