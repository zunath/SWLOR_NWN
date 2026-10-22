using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.CraftSim.Scenario.Experiment
{
    /// <summary>One experiment to play: the research level, the researcher's Fabrication rank and gear, and the recipe's tier.</summary>
    public sealed record ExperimentCase(int ResearchLevel, int Tier, int Rank, GearLevel Gear)
    {
        /// <summary>The lowest Fabrication rank that can research the tier.</summary>
        public int RequiredRank => CraftRankMilestones.RequiredFabricationRankForResearchTier(Tier);

        public ExperimentSetup CreateSetup(ExperimentTuning tuning = null) =>
            new(ResearchLevel, Rank, RequiredRank, Gear.CpBonus, extraBonus: false, tuning);
    }

    /// <summary>What many experiments played by one policy at one case came to.</summary>
    public sealed class ExperimentSample
    {
        public int Runs { get; init; }

        /// <summary>Mean actions per experiment: Tests, Analyzes and Refine Methods.</summary>
        public double MeanActions { get; init; }

        public double MeanTrialsUsed { get; init; }
        public double MeanRefines { get; init; }

        /// <summary>The mean share of the job's time cut, in percent.</summary>
        public double MeanTimeCutPercent { get; init; }

        /// <summary>The mean evidence on the chosen candidate when the experiment ended.</summary>
        public double MeanTargetEvidence { get; init; }

        /// <summary>The share of experiments that ended with no evidence on any candidate, which fall back to today's roll.</summary>
        public double NoEvidenceShare { get; init; }

        /// <summary>The mean size of the chosen candidate's bonus, an experiment with none counting 0 (licensed runs, or percent).</summary>
        public double MeanTargetMagnitude { get; init; }

        /// <summary>The mean size of the chosen candidate's bonus over the experiments that gathered evidence.</summary>
        public double MeanMagnitudeWithEvidence { get; init; }

        /// <summary>The share of experiments whose bonus reached today's dice maximum.</summary>
        public double MaximumShare { get; init; }
    }

    /// <summary>The XP and time of research at one rank, recipe level and research level.</summary>
    public sealed class ResearchXpRow
    {
        public int Rank { get; init; }
        public int RecipeLevel { get; init; }
        public int ResearchLevel { get; init; }
        public int ConcurrentJobs { get; init; }
        public int BaseXp { get; init; }
        public int ExperimentXp { get; init; }
        public int CompletionXp { get; init; }
        public double JobSeconds { get; init; }
        public double ActiveSeconds { get; init; }

        public int XpPerJob => ExperimentXp + CompletionXp;

        /// <summary>
        /// XP per real hour of a researcher who keeps every job slot busy: each job runs its (shortened) length plus the time the
        /// researcher spends in its experiment, and the researcher can run only one experiment at a time.
        /// </summary>
        public double XpPerHour
        {
            get
            {
                var cycle = Math.Max(JobSeconds + ActiveSeconds, ConcurrentJobs * ActiveSeconds);
                return cycle <= 0 ? 0 : ConcurrentJobs * XpPerJob / cycle * 3600;
            }
        }
    }

    /// <summary>The numbers the research report is built from. Everything is repeatable from the seed.</summary>
    public static class ExperimentStudy
    {
        /// <summary>The stream that separates the study's dice from every gate's.</summary>
        public const int StudyStream = 131;

        /// <summary>
        /// Plays <paramref name="runs"/> experiments of a case with a policy and summarizes them. The chosen candidate is the policy's
        /// own; a fixed level has only its grant.
        /// </summary>
        public static ExperimentSample Play(
            ExperimentCase experimentCase,
            Func<ICraftPolicy> policyFactory,
            ResearchCandidate target,
            int runs,
            int seed,
            ExperimentTuning tuning = null)
        {
            if (runs < 1)
                throw new ArgumentOutOfRangeException(nameof(runs));

            double actions = 0, trials = 0, refines = 0, cut = 0, evidence = 0, magnitude = 0, withEvidenceSum = 0;
            var noEvidence = 0;
            var maximum = 0;
            var withEvidenceRuns = 0;
            var setup = experimentCase.CreateSetup(tuning);
            var isRandom = setup.IsRandomLevel;
            var tracked = isRandom ? target : ResearchCandidate.FixedGrant;

            for (var run = 0; run < runs; run++)
            {
                var random = SimMinigameRandom.ForRun(
                    seed, StudyStream, experimentCase.ResearchLevel, experimentCase.Tier, experimentCase.Rank, experimentCase.Gear.Tier, run);
                var engine = new ExperimentEngine(random, setup);
                var result = CraftPlayer.Play(engine, policyFactory());

                actions += result.Actions;
                trials += engine.TrialsUsed;
                refines += result.CpSpent / (double)Math.Max(1, setup.RefineCp);
                cut += engine.TimeCutPercent;

                var snapshot = engine.Snapshot();
                var targetEvidence = snapshot.Evidence[tracked];
                evidence += targetEvidence;

                if (!isRandom)
                    continue;

                var (winner, size) = ResearchOutcomeRules.Resolve(experimentCase.ResearchLevel, snapshot.Evidence, random);
                if (winner == ResearchCandidate.None)
                {
                    noEvidence++;
                    continue;
                }

                withEvidenceRuns++;
                var won = winner == target ? size : 0;
                magnitude += won;
                withEvidenceSum += won;
                if (won >= ResearchGrantRules.MaximumAmount(target))
                    maximum++;
            }

            return new ExperimentSample
            {
                Runs = runs,
                MeanActions = actions / runs,
                MeanTrialsUsed = trials / runs,
                MeanRefines = refines / runs,
                MeanTimeCutPercent = cut / runs,
                MeanTargetEvidence = evidence / runs,
                NoEvidenceShare = noEvidence / (double)runs,
                MeanTargetMagnitude = magnitude / runs,
                MeanMagnitudeWithEvidence = withEvidenceRuns == 0 ? 0 : withEvidenceSum / withEvidenceRuns,
                MaximumShare = maximum / (double)runs
            };
        }

        /// <summary>
        /// The job length of researching a recipe to a research level, in seconds, before any reduction: the same formula the game
        /// charges (<c>Craft.CalculateResearchCost</c> with base constant 200 and no recipe cost modifier).
        /// </summary>
        public static double JobSeconds(int recipeLevel, int researchLevel)
        {
            var tier = Math.Min(CraftRankMilestones.MaxResearchTier, recipeLevel / CraftRankMilestones.RanksPerTier + 1);
            return (int)(200 * (Math.Pow(tier, 2.2f) * Math.Pow(researchLevel, 2f)));
        }

        /// <summary>The XP and time of research at a rank, with the experiment's mean actions and time cut from a sample.</summary>
        public static ResearchXpRow XpRow(
            int rank,
            int recipeLevel,
            int researchLevel,
            int concurrentJobs,
            double meanActions,
            double meanTimeCutPercent,
            double effortPercent,
            double secondsPerAction,
            double overheadSeconds,
            Func<int, int> chartXp)
        {
            var baseXp = chartXp(recipeLevel - rank);
            var fullSeconds = JobSeconds(recipeLevel, researchLevel);
            return new ResearchXpRow
            {
                Rank = rank,
                RecipeLevel = recipeLevel,
                ResearchLevel = researchLevel,
                ConcurrentJobs = concurrentJobs,
                BaseXp = baseXp,
                ExperimentXp = ResearchXpRules.ExperimentXp(baseXp, researchLevel, (int)Math.Round(effortPercent)),
                CompletionXp = ResearchXpRules.CompletionXp(baseXp, researchLevel),
                JobSeconds = fullSeconds * (100 - meanTimeCutPercent) / 100.0,
                ActiveSeconds = meanActions * secondsPerAction + overheadSeconds
            };
        }
    }
}
