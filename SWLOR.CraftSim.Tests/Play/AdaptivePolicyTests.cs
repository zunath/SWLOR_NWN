using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Tests.Play
{
    [TestFixture]
    public class AdaptivePolicyTests
    {
        private static readonly AdaptivePolicyOptions Quick = new() { Depth = 3, ForecastSamples = 1, SampleBudget = 2000 };

        private static BenchEngine Bench(IMinigameRandom random, BenchTuning tuning = null) => new(random, tuning ?? new BenchTuning { MaxCp = 40 });

        private static List<string> Line(IMinigameRandom random, int policySeed, AdaptivePolicyOptions options = null, BenchTuning tuning = null)
        {
            var engine = Bench(random, tuning);
            var policy = new AdaptivePolicy(options ?? Quick, policySeed);
            var line = new List<string>();
            while (engine.Status == MinigameStatus.InProgress && line.Count < 60)
            {
                var action = policy.Choose(engine);
                if (action == null)
                    break;

                line.Add(action);
                engine.Perform(action);
            }

            return line;
        }

        [Test]
        public void ChoosingNeverTouchesTheRealEngineOrItsSourceOfChance()
        {
            var random = SimMinigameRandom.ForRun(1);
            var engine = Bench(random);
            var meters = engine.DescribeMeters();

            new AdaptivePolicy(Quick, 5).Choose(engine);

            random.Calls.Should().Be(0, "the search plays copies on chance of its own");
            engine.Turn.Should().Be(0);
            engine.Status.Should().Be(MinigameStatus.InProgress);
            engine.DescribeMeters().Should().Equal(meters);
        }

        [Test]
        public void ASeedRepeatsTheSameAttemptActionForAction()
        {
            Line(SimMinigameRandom.ForRun(3), 11).Should().Equal(Line(SimMinigameRandom.ForRun(3), 11));
        }

        [Test]
        public void ThePolicyAdaptsToTheStateItIsShown()
        {
            // Heat 0: nothing works yet, so a skilled player sets up with Stoke. Two Surges later Heat is in the window and Stoke is wasted.
            var cold = Bench(SimMinigameRandom.ForRun(1));
            var warm = Bench(new ScriptedRandom(1, 100, 1, 100));
            warm.Perform(BenchEngine.Stoke);
            warm.Perform(BenchEngine.Stoke);
            warm.Heat.Should().BeInRange(BenchEngine.WindowFrom, BenchEngine.WindowTo);

            new AdaptivePolicy(Quick, 1).Choose(cold).Should().Be(BenchEngine.Stoke);
            new AdaptivePolicy(Quick, 1).Choose(warm).Should().BeOneOf(BenchEngine.Press, BenchEngine.Polish);
        }

        [Test]
        public void AShallowSearchNeverSetsUpAndPlaysFarWorse()
        {
            double Mean(int depth)
            {
                var options = new AdaptivePolicyOptions { Depth = depth, ForecastSamples = 1, SampleBudget = 2000 };
                return Enumerable.Range(1, 16).Average(run =>
                    CraftPlayer.Play(Bench(SimMinigameRandom.ForRun(9, run)), new AdaptivePolicy(options, run)).Score);
            }

            Mean(3).Should().BeGreaterThan(Mean(1) + 0.3, "only a lookahead of three sees the payoff of two Stokes");
        }

        [Test]
        public void ADeterministicEngineIsPlayedThroughToItsBestOutcome()
        {
            var engine = new ToyEngine(SimMinigameRandom.ForRun(1));

            var result = CraftPlayer.Play(engine, new AdaptivePolicy(Quick, 1));

            result.Succeeded.Should().BeTrue();
            result.ReachedMaxQuality.Should().BeTrue("the player builds quality before finishing, and does not take the instant finish with none");
            result.Actions.Should().BeLessThanOrEqualTo(7, "it does not dither once quality is full");
        }

        [Test]
        public void ThePlayerPrefersFewerActionsWhenTheResultIsTheSame()
        {
            var patient = new AdaptivePolicyOptions { Depth = 3, ForecastSamples = 1, TimePreference = 0 };
            var hurried = new AdaptivePolicyOptions { Depth = 3, ForecastSamples = 1, TimePreference = 0.05 };

            var patientLine = Line(SimMinigameRandom.ForRun(1), 1, patient, new BenchTuning { HasPerfect = true });
            var hurriedLine = Line(SimMinigameRandom.ForRun(1), 1, hurried, new BenchTuning { HasPerfect = true });

            hurriedLine.Count.Should().BeLessThanOrEqualTo(patientLine.Count);
            hurriedLine.Count.Should().BeLessThanOrEqualTo(6, "five Perfect actions finish a full-quality item");
        }

        [Test]
        public void ASearchOutOfBudgetStillChoosesAnAvailableAction()
        {
            var engine = Bench(SimMinigameRandom.ForRun(1));
            var budgeted = new AdaptivePolicyOptions { Depth = 4, ForecastSamples = 4, SampleBudget = 1 };

            var action = new AdaptivePolicy(budgeted, 1).Choose(engine);

            engine.PreviewAll().Where(preview => preview.Enabled).Select(preview => preview.Action.Id).Should().Contain(action);
        }

        [Test]
        public void ABiggerBudgetPlaysNoWorseThanASmallOne()
        {
            double Mean(int budget)
            {
                var options = new AdaptivePolicyOptions { Depth = 3, ForecastSamples = 1, SampleBudget = budget };
                return Enumerable.Range(1, 12).Average(run =>
                    CraftPlayer.Play(Bench(SimMinigameRandom.ForRun(4, run)), new AdaptivePolicy(options, run)).Score);
            }

            Mean(2000).Should().BeGreaterThanOrEqualTo(Mean(2) - 0.05);
        }

        [Test]
        public void ThereIsNothingToChooseOnceTheAttemptIsOver()
        {
            var finished = new ToyEngine(SimMinigameRandom.ForRun(1));
            finished.Perform(ToyEngine.C);

            new AdaptivePolicy(Quick, 1).Choose(finished).Should().BeNull();
        }

        [Test]
        public void AnActionThatEndsTheAttemptIsValuedFromItsPreviewWithoutPlayingIt()
        {
            // A lookahead of one chooses from the previews alone, with no copy of the engine; both finishing actions leave a full-quality item.
            var engine = new ToyEngine(SimMinigameRandom.ForRun(1));
            for (var i = 0; i < 3; i++)
                engine.Perform(ToyEngine.B);
            engine.Perform(ToyEngine.A);
            engine.Perform(ToyEngine.A);

            var action = new AdaptivePolicy(new AdaptivePolicyOptions { Depth = 1 }, 1).Choose(engine);

            action.Should().BeOneOf(ToyEngine.A, ToyEngine.C);
        }

        [Test]
        public void OptionsOutsideTheirRangesAreRejected()
        {
            ((Action)(() => new AdaptivePolicy(new AdaptivePolicyOptions { Depth = 0 }))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new AdaptivePolicy(new AdaptivePolicyOptions { Depth = 7 }))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new AdaptivePolicy(new AdaptivePolicyOptions { ForecastSamples = 0 }))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new AdaptivePolicy(new AdaptivePolicyOptions { SampleBudget = 0 }))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new AdaptivePolicy(new AdaptivePolicyOptions { TimePreference = 1 }))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new AdaptivePolicy(new AdaptivePolicyOptions { Heuristic = null }))).Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void TheDefaultOptionsLookThreeActionsAheadAndDescribeThemselves()
        {
            AdaptivePolicyOptions.Default.Depth.Should().Be(3);
            AdaptivePolicyOptions.Default.Describe().Should().Contain("depth 3").And.Contain("DefaultLeafHeuristic");
        }
    }
}
