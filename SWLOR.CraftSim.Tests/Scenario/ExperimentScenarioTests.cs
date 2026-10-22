using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Cli;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Scenario.Experiment;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    [TestFixture]
    public class ExperimentScenarioTests
    {
        /// <summary>A source of chance whose every Test finds evidence.</summary>
        private sealed class AlwaysFindsEvidence : IMinigameRandom
        {
            public int D100() => 1;

            public int Next(int minInclusive, int maxExclusive) => minInclusive;
        }

        private static readonly GearLevel None = GearLevel.Fixed("no gear", SkillType.Fabrication, 0, 0, 0);
        private static readonly GearLevel Mid = GearLevel.Fixed("mid gear", SkillType.Fabrication, 13, 12, 26);
        private static readonly GearLevel Best = GearLevel.Fixed("best gear", SkillType.Fabrication, 29, 30, 37);

        [Test]
        public void TheRegistryFindsTheExperimentScenarioWithTheResearchLengthTarget()
        {
            var scenario = ScenarioRegistry.Discover().Find("experiment");

            scenario.Skill.Should().Be(SkillType.Fabrication);
            scenario.Minigame.Should().Be(CraftMinigameType.Experiment);
            scenario.LengthTarget.Should().Be(LengthTarget.ResearchExperiment);
            scenario.LengthTarget.MinimumActions.Should().Be(5);
            scenario.LengthTarget.MaximumActions.Should().Be(8);
            scenario.StylePairs.Should().BeEmpty();
            scenario.Limitations.Should().NotBeEmpty();
        }

        [Test]
        public void TheCohortIsTheResearchableRecipesAtTheirTiersFirstRank()
        {
            var scenario = new ExperimentScenario();
            var recipes = scenario.Cohort.Recipes(SharedData.Recipes);

            recipes.Should().NotBeEmpty();
            recipes.Should().OnlyContain(recipe => ExperimentScenario.IsResearchable(recipe.Detail));
            foreach (var recipe in recipes)
            {
                var tier = Math.Min(5, recipe.Level / 10 + 1);
                scenario.Cohort.RankFor(recipe).Should().Be(CraftRankMilestones.RequiredFabricationRankForResearchTier(tier));
            }

            scenario.Cohort.Bands.Select(band => band.Name).Should().Equal(ExperimentScenario.TierBands.Select(band => band.Name));
        }

        [Test]
        public void TheScenarioBuildsAnExperimentEngineForARecipeAtItsGearAndRank()
        {
            var scenario = new ExperimentScenario();
            var recipe = scenario.Cohort.Recipes(SharedData.Recipes).First();
            var request = new CraftEngineRequest(recipe, scenario.Cohort.RankFor(recipe), Mid, null, 1, new SeededMinigameRandom(1));

            var engine = scenario.CreateEngine(request).Should().BeOfType<ExperimentEngine>().Subject;

            engine.Type.Should().Be(CraftMinigameType.Experiment);
            engine.Setup.TargetLevel.Should().Be(ExperimentScenario.ResearchLevelFor(recipe));
            engine.Meters.Cp.Max.Should().Be((int)(26 + request.Rank * 0.75));
        }

        [Test]
        public void ConsecutiveRecipesCoverAllTenResearchLevels()
        {
            var recipes = new ExperimentScenario().Cohort.Recipes(SharedData.Recipes).Take(40).ToList();

            recipes.Select(ExperimentScenario.ResearchLevelFor).Distinct().OrderBy(level => level)
                .Should().Equal(Enumerable.Range(1, 10));
        }

        [Test]
        public void TheLengthGateRunsTheScenarioAndPassesItsTarget()
        {
            var writer = new StringWriter();
            var directory = Directory.CreateTempSubdirectory("craftsim-experiment-").FullName;
            try
            {
                var code = GatesCommand.Run(
                    new[] { "--scenario", "experiment", "--runs", "6", "--recipes-per-band", "2", "--gates", "length", "--out", Path.Combine(directory, "gates.md") },
                    writer,
                    new ScenarioRegistry(new ICraftScenario[] { new ExperimentScenario() }),
                    SharedData.Recipes,
                    SharedData.Gear,
                    SharedData.Enhancements);

                code.Should().Be(0);
                writer.ToString().Should().Contain("Length: Passed");
                File.ReadAllText(Path.Combine(directory, "gates.md")).Should().Contain("Research Experiment");
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void TheFocusedPlayerTestsOnlyItsCandidateAtARandomLevelAndAnalyzesWithTheLastTrialAtAFixedLevel()
        {
            var random = new ExperimentCase(3, 3, 30, None);
            var randomEngine = new ExperimentEngine(new SeededMinigameRandom(3), random.CreateSetup());
            var randomPolicy = new ExperimentFocusedPolicy(ResearchCandidate.TimeReduction);
            var randomActions = new List<string>();
            while (randomEngine.Status == MinigameStatus.InProgress)
            {
                var action = randomPolicy.Choose(randomEngine);
                randomActions.Add(action);
                randomEngine.Perform(action);
            }

            randomActions.Should().OnlyContain(action => action == ExperimentActionIds.TestTimeReduction);
            randomActions.Count.Should().Be(6);

            var fixedCase = new ExperimentCase(1, 3, 30, None);
            var fixedEngine = new ExperimentEngine(new AlwaysFindsEvidence(), fixedCase.CreateSetup());
            var fixedPolicy = new ExperimentFocusedPolicy(ResearchCandidate.FixedGrant);
            var fixedActions = new List<string>();
            while (fixedEngine.Status == MinigameStatus.InProgress)
            {
                var action = fixedPolicy.Choose(fixedEngine);
                fixedActions.Add(action);
                fixedEngine.Perform(action);
            }

            fixedActions.Should().Equal(Enumerable.Repeat(ExperimentActionIds.TestFixed, 5).Append(ExperimentActionIds.Analyze));
            fixedEngine.TimeCutPercent.Should().Be(25);
        }

        [Test]
        public void TheFocusedPlayerRefinesBeforeATestWhenItCanAffordIt()
        {
            var experimentCase = new ExperimentCase(1, 1, 10, Mid);
            var engine = new ExperimentEngine(new SeededMinigameRandom(1), experimentCase.CreateSetup());

            new ExperimentFocusedPolicy(ResearchCandidate.FixedGrant).Choose(engine).Should().Be(ExperimentActionIds.RefineMethod);
            new ExperimentFocusedPolicy(ResearchCandidate.FixedGrant, useRefine: false).Choose(engine).Should().Be(ExperimentActionIds.TestFixed);
        }

        [Test]
        public void PlayingTheSameSeedGivesTheSameSample()
        {
            var experimentCase = new ExperimentCase(3, 2, 20, Mid);

            var first = ExperimentStudy.Play(experimentCase, () => new ExperimentFocusedPolicy(ResearchCandidate.CreditReduction), ResearchCandidate.CreditReduction, 50, 9);
            var second = ExperimentStudy.Play(experimentCase, () => new ExperimentFocusedPolicy(ResearchCandidate.CreditReduction), ResearchCandidate.CreditReduction, 50, 9);

            second.Should().BeEquivalentTo(first);
            first.MeanActions.Should().BeInRange(5, 8);
            first.MeanTargetMagnitude.Should().BeInRange(1, ResearchGrantRules.MaximumAmount(ResearchCandidate.CreditReduction));
        }

        [Test]
        public void ABaseResearcherWithAFocusedPlanAveragesAboutTodaysDiceForTheChosenBonus()
        {
            foreach (var candidate in new[] { ResearchCandidate.CreditReduction, ResearchCandidate.TimeReduction })
            {
                var sample = ExperimentStudy.Play(
                    new ExperimentCase(3, 1, 10, None), () => new ExperimentFocusedPolicy(candidate), candidate, 4000, 5);

                (sample.MeanMagnitudeWithEvidence / ResearchGrantRules.AverageDice(candidate)).Should().BeInRange(0.9, 1.1, candidate.ToString());
            }
        }

        [Test]
        public void NoOutcomeEverExceedsTodaysDiceMaximum()
        {
            foreach (var candidate in new[] { ResearchCandidate.CreditReduction, ResearchCandidate.TimeReduction })
            {
                var sample = ExperimentStudy.Play(
                    new ExperimentCase(5, 1, 50, Best), () => new ExperimentFocusedPolicy(candidate), candidate, 500, 5);

                sample.MeanTargetMagnitude.Should().BeLessThanOrEqualTo(ResearchGrantRules.MaximumAmount(candidate));
            }
        }

        [Test]
        public void TheJobLengthIsTheGamesResearchFormula()
        {
            ExperimentStudy.JobSeconds(0, 1).Should().Be(200);
            ExperimentStudy.JobSeconds(9, 10).Should().Be(20000);
            ExperimentStudy.JobSeconds(20, 2).Should().Be((int)(200 * Math.Pow(3, 2.2f) * 4));
            ExperimentStudy.JobSeconds(52, 10).Should().Be((int)(200 * Math.Pow(5, 2.2f) * 100));

            var source = File.ReadAllText(Path.Combine(SharedData.Root, "SWLOR.Game.Server", "Service", "Craft.cs"));
            source.Should().Contain("var price = baseConstant * (Math.Pow(perkLevel, 2.2f) * Math.Pow(blueprintLevel, 2f));");
            source.Should().Contain("return CalculateResearchCost(recipe, blueprintLevel, 200, reductionBonus * 0.01f);");
        }

        [Test]
        public void ShortJobsAreLimitedByThePlayersTimeNotTheJobs()
        {
            // Three jobs of 60 s each with 90 s of active time per job: the player needs 270 s to start them, not 150 s.
            var row = new ResearchXpRow
            {
                ConcurrentJobs = 3, ExperimentXp = 100, CompletionXp = 50, JobSeconds = 60, ActiveSeconds = 90
            };

            row.XpPerJob.Should().Be(150);
            row.XpPerHour.Should().BeApproximately(3 * 150 / 270.0 * 3600, 1e-9);
        }

        [Test]
        public void LongJobsAreLimitedByTheirLength()
        {
            var row = new ResearchXpRow
            {
                ConcurrentJobs = 2, ExperimentXp = 100, CompletionXp = 50, JobSeconds = 3510, ActiveSeconds = 90
            };

            row.XpPerHour.Should().BeApproximately(2 * 150 / 3510.0 * 3600, 1e-9);
        }

        [Test]
        public void TheExperimentRunsInsideTheJobsTimerSoItAddsNothingToALongJobsCycle()
        {
            var row = new ResearchXpRow
            {
                ConcurrentJobs = 1, ExperimentXp = 100, CompletionXp = 50, JobSeconds = 170, ActiveSeconds = 72
            };

            row.XpPerHour.Should().BeApproximately(150 / 170.0 * 3600, 1e-9);
        }

        [Test]
        public void NoResearchRowPaysMoreXpPerRealHourThanAtLevelCraftingAtItsLowestQuality()
        {
            Skill.CacheXPChartData();
            // 15 actions at 6 s, 0% quality, no blueprint, chart XP 600 at level: the low end of at-level crafting.
            var crafting = CraftOutcomeRules.CalculateXp(Skill.GetDeltaXP(0), 0, false, 0f) / (15 * 6.0) * 3600;
            var worst = 0.0;
            foreach (var rank in new[] { 10, 20, 30, 40, 50 })
            {
                var recipeLevel = rank == 50 ? 52 : rank - 1;
                var concurrent = CraftRankMilestones.MaxConcurrentResearchJobsForRank(rank);
                for (var researchLevel = 1; researchLevel <= 10; researchLevel++)
                {
                    // Generous inputs: 6 actions, the 25% cap always reached, a 0 s overhead.
                    var row = ExperimentStudy.XpRow(rank, recipeLevel, researchLevel, concurrent, 6, 25, 100, 6, 0, Skill.GetDeltaXP);
                    worst = Math.Max(worst, row.XpPerHour);
                }
            }

            worst.Should().BeLessThan(crafting, "research XP per real hour must stay under at-level crafting's");
        }

        [Test]
        public void TheResearchCommandWritesTheReportWithEverySection()
        {
            var directory = Directory.CreateTempSubdirectory("craftsim-research-").FullName;
            try
            {
                var path = Path.Combine(directory, "research.md");
                var writer = new StringWriter();

                var code = ResearchCommand.Run(
                    new[] { "--runs", "30", "--adaptive-runs", "3", "--seed", "4", "--out", path },
                    writer,
                    new[] { None, Mid, Best });

                code.Should().Be(0);
                writer.ToString().Should().Contain("Wrote");
                var report = File.ReadAllText(path);
                report.Should().Contain("# Crafting revamp: the research Experiment");
                report.Should().Contain("Seed 4; 30 experiments per case");
                report.Should().Contain("## Length").And.Contain("## Reduction size against today's dice").And.Contain("### Expected supply per random level");
                report.Should().Contain("## Time cut (fixed levels)").And.Contain("## Fabrication XP per real hour").And.Contain("## Limitations");
                report.Should().Contain("Verdict: every case takes 5-8 actions");
                report.Should().MatchRegex("^[\\x20-\\x7E\\r\\n]*$");
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void TheCraftingComparatorUsesTheSameSecondsPerActionAsTheResearchRows()
        {
            var directory = Directory.CreateTempSubdirectory("craftsim-research-").FullName;
            try
            {
                var path = Path.Combine(directory, "research.md");

                ResearchCommand.Run(
                    new[] { "--runs", "5", "--adaptive-runs", "1", "--seconds-per-action", "12", "--out", path },
                    new StringWriter(),
                    new[] { None });

                var report = File.ReadAllText(path);
                report.Should().Contain("at 12.0 s each takes 180.0 s and pays 12000 XP per hour at 0% quality");
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void TheResearchCommandRejectsAnUnknownOption()
        {
            var act = () => ResearchCommand.Run(new[] { "--bogus", "1" }, new StringWriter(), new[] { None });

            act.Should().Throw<ArgumentException>();
        }
    }
}
