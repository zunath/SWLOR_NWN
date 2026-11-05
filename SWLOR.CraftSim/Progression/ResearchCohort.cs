using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>One Fabrication rank of the research cohort.</summary>
    /// <param name="FabricationRank">The researcher's Fabrication rank.</param>
    /// <param name="Tier">The highest research tier the rank opens: rank / 10.</param>
    /// <param name="RecipeLevel">Level of the best researchable recipe the tier allows (the one that pays the most XP).</param>
    /// <param name="BaseXp">The XP chart's pay for that recipe level against the rank (<c>ResearchXpRules.BaseXp</c>). A research award is a share of it.</param>
    /// <param name="ConcurrentJobs">Research jobs the rank may run at once: 1, 2 from Fabrication 25, 3 from 50.</param>
    /// <param name="FirstJobSeconds">Length of a blueprint's first job (research level 1), which is also how a recipe's blueprint is first acquired.</param>
    /// <param name="TenthJobSeconds">Length of the job that takes a blueprint to research level 10.</param>
    /// <param name="FirstJobCredits">Credits that first job costs (the same formula as its length).</param>
    /// <param name="ActiveSecondsPerJob">Seconds the player spends at the terminal per job: the experiment plus starting and collecting.</param>
    /// <param name="FastLoopJobsPerHour">Jobs per hour when every concurrent slot keeps running first jobs on new blueprints, with the Analyze time cut at its cap.</param>
    /// <param name="ClimbJobsPerHour">Jobs per hour when every concurrent slot climbs one blueprint from research level 1 to 10, with the time cut at its cap.</param>
    /// <param name="CraftingXpPerHour">XP per hour of at-level Fabrication crafting at this rank, gathering included (the hours-to-50 model).</param>
    /// <param name="CraftWindowXpPerHour">The same crafting XP per hour of time in the craft window alone, gathering left out (the stricter comparison).</param>
    /// <param name="StartingLevel1XpPerJob">XP a level-1 job pays under the Research workstream's starting values (<c>ResearchXpRules</c>): the experiment at full effort plus completion.</param>
    /// <param name="StartingXpPerHour">Research XP per elapsed hour under those starting values, for the better of the fast loop and the climb.</param>
    public sealed record ResearchRow(
        int FabricationRank,
        int Tier,
        int RecipeLevel,
        int BaseXp,
        int ConcurrentJobs,
        double FirstJobSeconds,
        double TenthJobSeconds,
        double FirstJobCredits,
        double ActiveSecondsPerJob,
        double FastLoopJobsPerHour,
        double ClimbJobsPerHour,
        double CraftingXpPerHour,
        double CraftWindowXpPerHour,
        int StartingLevel1XpPerJob,
        double StartingXpPerHour)
    {
        /// <summary>Research XP per hour at an award of one base XP per job (a share of 1.0), for the best loop.</summary>
        public double ResearchXpPerHourAtFullShare => BaseXp * Math.Max(FastLoopJobsPerHour, ClimbJobsPerHour);

        /// <summary>
        /// The largest award, as a share of the recipe's base XP per job, that keeps research XP per hour below at-level crafting XP per hour;
        /// null when the rank's base XP is zero, so any share is safe.
        /// </summary>
        public double? CeilingShare => BaseXp > 0 ? CraftingXpPerHour / ResearchXpPerHourAtFullShare : null;

        /// <summary>Research XP per hour of the player's own time at the terminal, at an award of one base XP per job.</summary>
        public double ResearchXpPerActiveHourAtFullShare => BaseXp * 3600.0 / ActiveSecondsPerJob;

        /// <summary>The largest award share that keeps research XP per active hour below crafting XP per hour of crafting; null when the base XP is zero.</summary>
        public double? ActiveCeilingShare => BaseXp > 0 ? CraftingXpPerHour / ResearchXpPerActiveHourAtFullShare : null;

        /// <summary>The largest award share that also stays below the craft window's own XP per hour, gathering left out; null when the base XP is zero.</summary>
        public double? StrictCeilingShare => BaseXp > 0 ? CraftWindowXpPerHour / ResearchXpPerActiveHourAtFullShare : null;

        /// <summary>The share of base XP a level-1 job pays under the starting values: experiment plus completion, each raised for the level.</summary>
        public double? StartingShare => BaseXp > 0 ? StartingLevel1XpPerJob / (double)BaseXp : null;

        /// <summary>Research XP per elapsed hour under the starting values, against at-level crafting XP per hour (gathering included); above 1 breaks the ceiling.</summary>
        public double? StartingToCraftingRatio => BaseXp > 0 ? StartingXpPerHour / CraftingXpPerHour : null;

        /// <summary>Research XP per hour of the player's own time under the starting values, against at-level crafting XP per hour.</summary>
        public double? StartingActiveToCraftingRatio =>
            BaseXp > 0 ? StartingLevel1XpPerJob * 3600.0 / ActiveSecondsPerJob / CraftingXpPerHour : null;
    }

    /// <summary>
    /// The research cohort: a Fabrication researcher with the rank-gated tier and job slots, who runs the job loop that pays the most XP per
    /// hour. Job length and credit cost are one formula, <c>200 x tier^2.2 x level^2</c> (<c>Craft.CalculateBlueprintResearchSeconds</c>); a
    /// blueprint is first acquired by starting its level-1 job with no blueprint (<c>ResearchViewModel.ClickCompleteJob</c>). The experiment
    /// cuts up to <see cref="ExperimentRules.TimeCutCapPercent"/> percent of a job's time, which the cohort takes as the best case; XP under
    /// the Research workstream's starting values comes from <see cref="ResearchXpRules"/> itself.
    /// </summary>
    public static class ResearchCohort
    {
        public const int LowestResearchRank = 10;

        /// <summary><c>Craft.CalculateResearchCost</c> with the research constant of 200: the job's seconds and its credits.</summary>
        public static int JobSeconds(int recipeLevel, int blueprintLevel, double costModifier)
        {
            var tier = Math.Min(recipeLevel / 10 + 1, CraftRankMilestones.MaxResearchTier);
            var price = 200 * (Math.Pow(tier, 2.2f) * Math.Pow(blueprintLevel, 2f));
            price += price * costModifier;
            return (int)price;
        }

        /// <summary>XP a job to research level <paramref name="level"/> pays under the starting values: the experiment at full effort plus completion.</summary>
        public static int StartingXpPerJob(int baseXp, int level) =>
            ResearchXpRules.ExperimentXp(baseXp, level, 100) + ResearchXpRules.CompletionXp(baseXp, level);

        public static IReadOnlyList<ResearchRow> Rows(ProgressionModel model, string fabricationGear, ProgressionAssumptions assumptions)
        {
            ServerXp.ForDelta(0);
            var fabrication = model.Path(SkillType.Fabrication, fabricationGear, assumptions);

            // The recipes the research window lists, by the server's own rule, that a researcher can make without learning them first
            // (the research job checks the recipe unlock).
            var researchable = model.Recipes.Recipes
                .Where(r => !r.RequiresLearning && ServerResearchRules.IsResearchable(r))
                .ToList();

            var timeCut = 1 - ExperimentRules.TimeCutCapPercent / 100.0;
            var rows = new List<ResearchRow>();
            for (var rank = LowestResearchRank; rank < ProgressionAssumptions.MaximumRank; rank++)
            {
                var tier = CraftRankMilestones.ResearchTierForRank(rank);
                var candidates = researchable
                    .Where(r => Math.Min(r.Level / 10 + 1, CraftRankMilestones.MaxResearchTier) <= tier)
                    .ToList();
                if (candidates.Count == 0)
                    continue;

                // The recipe that pays the most XP against this rank. Recipes that pay the same (the chart tops out at 1,200) are tied on
                // what the researcher gets, so they choose the shortest first job (the recipe's cost modifier lengthens it), then the higher
                // level, then the recipe type, so the pick is stable.
                var best = candidates
                    .OrderByDescending(r => ResearchXpRules.BaseXp(r.Level, rank))
                    .ThenBy(r => JobSeconds(r.Level, 1, r.Detail.ResearchCostModifier))
                    .ThenByDescending(r => r.Level)
                    .ThenBy(r => r.Type.ToString(), StringComparer.Ordinal)
                    .First();
                var baseXp = ResearchXpRules.BaseXp(best.Level, rank);
                var modifier = best.Detail.ResearchCostModifier;
                var concurrent = CraftRankMilestones.MaxConcurrentResearchJobsForRank(rank);
                var first = JobSeconds(best.Level, 1, modifier);
                var tenth = JobSeconds(best.Level, 10, modifier);
                var climbSeconds = Enumerable.Range(1, 10).Sum(level => JobSeconds(best.Level, level, modifier)) * timeCut;
                var climbXp = Enumerable.Range(1, 10).Sum(level => StartingXpPerJob(baseXp, level));
                var active = assumptions.ResearchTrials * assumptions.ResearchSecondsPerTrial + 2 * assumptions.ResearchVisitSeconds;

                // The player can run no more jobs than their own time at the terminal allows.
                var attentionLimit = 3600.0 / active;
                var fastLoop = Math.Min(concurrent * 3600.0 / (first * timeCut), attentionLimit);
                var climb = Math.Min(concurrent * 10 * 3600.0 / climbSeconds, attentionLimit);
                var level1Xp = StartingXpPerJob(baseXp, 1);
                var startingXpPerHour = Math.Max(fastLoop * level1Xp, climb * climbXp / 10.0);

                var step = fabrication.Steps[rank];
                rows.Add(new ResearchRow(
                    rank,
                    tier,
                    best.Level,
                    baseXp,
                    concurrent,
                    first,
                    tenth,
                    first,
                    active,
                    fastLoop,
                    climb,
                    step.RequiredXp / step.TotalHours,
                    step.RequiredXp / step.CraftHours,
                    level1Xp,
                    startingXpPerHour));
            }

            return rows;
        }
    }
}
