using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>Which skill's hours an XP target is set on.</summary>
    public enum XpTargetMetric
    {
        /// <summary>The slowest crafting skill.</summary>
        SlowestSkill,

        /// <summary>The median crafting skill: the middle one, or the mean of the middle two.</summary>
        MedianSkill
    }

    /// <summary>
    /// Finds the crafting-only XP multiplier, or the per-rank XP cost cap, that brings the hours to rank 50 down to a target. Hours always
    /// include the gathering that supplies the crafts and the guild task award the solver is given.
    /// </summary>
    public sealed class XpTargetSolver
    {
        private const double MaximumMultiplier = 64;
        private const int MinimumCap = 250;
        private const int MaximumCap = 20_000;
        private const int Iterations = 60;

        private readonly ProgressionModel _model;
        private readonly double _guildShare;
        private readonly string _gear;

        /// <param name="guildShare">The guild task award as a share of base XP; zero or less makes no hand-ins.</param>
        public XpTargetSolver(ProgressionModel model, double guildShare, string gear = CraftingGearCatalog.MidGearName)
        {
            _model = model;
            _guildShare = guildShare;
            _gear = gear;
        }

        public IReadOnlyList<SkillType> Skills => _model.Skills;

        public SkillPath Path(SkillType skill, ProgressionAssumptions assumptions)
        {
            var plan = _guildShare > 0 ? GuildHandInPlan.For(_model.Guilds, GuildTaskCatalog.GuildOf(skill), _guildShare) : null;
            return _model.Path(skill, _gear, assumptions, plan);
        }

        /// <summary>The hours of the slowest skill under the assumptions.</summary>
        public double SlowestHours(ProgressionAssumptions assumptions) => Hours(assumptions, XpTargetMetric.SlowestSkill);

        /// <summary>The hours of the median skill under the assumptions.</summary>
        public double MedianHours(ProgressionAssumptions assumptions) => Hours(assumptions, XpTargetMetric.MedianSkill);

        public double Hours(ProgressionAssumptions assumptions, XpTargetMetric metric)
        {
            var hours = Skills.Select(skill => Path(skill, assumptions).TotalHours).OrderBy(h => h).ToList();
            return metric == XpTargetMetric.SlowestSkill
                ? hours[^1]
                : (hours[(hours.Count - 1) / 2] + hours[hours.Count / 2]) / 2;
        }

        /// <summary>
        /// The smallest multiplier on crafting XP (crafts and hand-ins) that brings the metric to the target, or null when no multiplier up to
        /// 64 does. Hours only fall as the multiplier rises.
        /// </summary>
        public double? SolveMultiplier(ProgressionAssumptions assumptions, double targetHours, XpTargetMetric metric = XpTargetMetric.SlowestSkill)
        {
            if (Hours(assumptions with { CraftXpMultiplier = 1.0 }, metric) <= targetHours)
                return 1.0;

            if (Hours(assumptions with { CraftXpMultiplier = MaximumMultiplier }, metric) > targetHours)
                return null;

            double low = 1.0, high = MaximumMultiplier;
            for (var i = 0; i < Iterations; i++)
            {
                var middle = (low + high) / 2;
                if (Hours(assumptions with { CraftXpMultiplier = middle }, metric) > targetHours)
                    low = middle;
                else
                    high = middle;
            }

            return high;
        }

        /// <summary>
        /// The largest cap on the XP any rank costs that brings the metric to the target, or null when even the lowest cap does not.
        /// Hours only rise as the cap rises.
        /// </summary>
        public int? SolveCap(ProgressionAssumptions assumptions, double targetHours, XpTargetMetric metric = XpTargetMetric.SlowestSkill)
        {
            if (Hours(assumptions with { RequiredXpCap = MaximumCap }, metric) <= targetHours)
                return MaximumCap;

            if (Hours(assumptions with { RequiredXpCap = MinimumCap }, metric) > targetHours)
                return null;

            int low = MinimumCap, high = MaximumCap;
            while (high - low > 1)
            {
                var middle = (low + high) / 2;
                if (Hours(assumptions with { RequiredXpCap = middle }, metric) > targetHours)
                    high = middle;
                else
                    low = middle;
            }

            return low;
        }
    }
}
