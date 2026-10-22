using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Scenario
{
    /// <summary>Everything a scenario needs to build one engine for one simulated attempt.</summary>
    public sealed class CraftEngineRequest
    {
        public CraftEngineRequest(RecipeEntry recipe, int rank, GearLevel gear, CraftStyle style, int seed, IMinigameRandom random)
        {
            Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
            Gear = gear ?? throw new ArgumentNullException(nameof(gear));
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Rank = rank;
            Style = style;
            Seed = seed;
        }

        /// <summary>The recipe being crafted. <see cref="RecipeEntry.Detail"/> holds its per-recipe minigame parameters when it came from the server's definitions.</summary>
        public RecipeEntry Recipe { get; }

        /// <summary>The crafter's rank in the recipe's skill.</summary>
        public int Rank { get; }

        /// <summary>The crafting stats worn: Control, Craftsmanship and CP bonus, none or a gear tier (<see cref="GearLevel.Tier"/> 0 means none).</summary>
        public GearLevel Gear { get; }

        /// <summary>The style or perk configuration to build with, or null for no perks.</summary>
        public CraftStyle Style { get; }

        /// <summary>The seed of the whole simulation (the same for every attempt), for a scenario that needs a number of its own. The attempt's own dice are <see cref="Random"/>.</summary>
        public int Seed { get; }

        /// <summary>The source of chance the engine must be built with, so the attempt repeats from its seed.</summary>
        public IMinigameRandom Random { get; }
    }
}
