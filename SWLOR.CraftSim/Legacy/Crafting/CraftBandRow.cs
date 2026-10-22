using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>One recipe-level band's results for a skill, gear level and policy, weighted by the number of recipes at each level.</summary>
    public sealed class CraftBandRow
    {
        public SkillType Skill { get; init; }
        public string Band { get; init; }
        public string Gear { get; init; }
        public string Policy { get; init; }
        public int RankOffset { get; init; }
        public int Recipes { get; init; }
        public int Runs { get; init; }

        public double SuccessRate { get; init; }

        /// <summary>Mean final quality fraction of the successful crafts.</summary>
        public double QualityFraction { get; init; }

        /// <summary>The share of successful crafts that reached the recipe's maximum quality.</summary>
        public double FullQualityShare { get; init; }

        public double ActionsPerCraft { get; init; }

        /// <summary>Component stacks consumed per successful item when the band's craftable recipes are attempted equally often; null when none ever succeeded.</summary>
        public double? StacksPerSuccess { get; init; }

        /// <summary>The share of the band's recipes that never succeeded in the simulated runs; they are left out of the material figure.</summary>
        public double NeverCraftedShare { get; init; }
    }
}
