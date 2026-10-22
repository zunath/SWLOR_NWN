using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>The results of many crafts by one crafter at one recipe level.</summary>
    public sealed class CraftCell
    {
        public SkillType Skill { get; init; }
        public int RecipeLevel { get; init; }
        public int Rank { get; init; }
        public int RankOffset { get; init; }
        public string Gear { get; init; }
        public string Policy { get; init; }
        public int Runs { get; init; }
        public int Successes { get; set; }
        public double QualitySum { get; set; }
        public int FullQualitySuccesses { get; set; }
        public long Actions { get; set; }
        public long CpSpent { get; set; }

        public double SuccessRate => Runs == 0 ? 0 : Successes / (double)Runs;

        /// <summary>Mean final quality fraction of the crafts that succeeded.</summary>
        public double QualityFraction => Successes == 0 ? 0 : QualitySum / Successes;

        public double FullQualityShare => Successes == 0 ? 0 : FullQualitySuccesses / (double)Successes;
        public double ActionsPerCraft => Runs == 0 ? 0 : Actions / (double)Runs;
    }
}
