using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Tests.Play
{
    [TestFixture]
    public class CraftPlayerTests
    {
        private static BenchEngine Bench(IMinigameRandom random, BenchTuning tuning = null) => new(random, tuning ?? new BenchTuning());

        [Test]
        public void PlayReportsHowTheAttemptEnded()
        {
            var engine = new ToyEngine(SimMinigameRandom.ForRun(1));

            var result = CraftPlayer.PlayFixed(engine, new[] { ToyEngine.B, ToyEngine.B, ToyEngine.B, ToyEngine.A, ToyEngine.A, ToyEngine.A });

            result.Status.Should().Be(MinigameStatus.Succeeded);
            result.Succeeded.Should().BeTrue();
            result.Stalled.Should().BeFalse();
            result.QualityFraction.Should().Be(1.0);
            result.ReachedMaxQuality.Should().BeTrue();
            result.Actions.Should().Be(6);
            result.CpSpent.Should().Be(0);
            result.Score.Should().Be(1.0);
        }

        [Test]
        public void ABrokenOrAbandonedPieceScoresZeroWhateverItsQuality()
        {
            var finished = CraftPlayer.PlayFixed(new ToyEngine(SimMinigameRandom.ForRun(1)), new[] { ToyEngine.C });
            var abandoned = CraftPlayer.PlayFixed(new ToyEngine(SimMinigameRandom.ForRun(1)), new[] { ToyEngine.B });
            var empty = CraftPlayer.PlayFixed(new ToyEngine(SimMinigameRandom.ForRun(1)), Array.Empty<string>());

            finished.Succeeded.Should().BeTrue();
            finished.Score.Should().Be(0, "an item with no quality scores no quality");
            finished.ReachedMaxQuality.Should().BeFalse();
            abandoned.Stalled.Should().BeTrue("the last action repeats up to the action limit and the progress meter never moves");
            abandoned.Succeeded.Should().BeFalse();
            abandoned.Score.Should().Be(0);
            abandoned.QualityFraction.Should().Be(1.0, "the quality meter filled but no item was made");
            empty.Actions.Should().Be(0);
            empty.Stalled.Should().BeTrue();
        }

        [Test]
        public void SpentCpIsTheMeterTheAttemptStartedWithMinusWhatIsLeft()
        {
            var engine = new ToyEngine(SimMinigameRandom.ForRun(1), maxCp: 12);

            var result = CraftPlayer.PlayFixed(engine, new[] { ToyEngine.D, ToyEngine.D, ToyEngine.D, ToyEngine.A }, actionLimit: 4);

            result.CpSpent.Should().Be(10, "the third D could not be paid for and was skipped");
        }

        [Test]
        public void TheActionLimitStopsAnAttemptThatNeverEnds()
        {
            var engine = new ToyEngine(SimMinigameRandom.ForRun(1));

            var result = CraftPlayer.PlayFixed(engine, new[] { ToyEngine.B }, actionLimit: 7);

            result.Actions.Should().Be(7);
            result.Stalled.Should().BeTrue();
        }

        [Test]
        public void AnUnavailableActionIsSkippedAndSpendsNoTurn()
        {
            // Two CP pay for one Stoke; Polish costs 3 and the second Stoke has nothing left to be paid with.
            var random = new ScriptedRandom(new[] { 1 }.Concat(Enumerable.Repeat(100, 50)).ToArray());
            var engine = Bench(random, new BenchTuning { MaxCp = 2 });

            var result = CraftPlayer.PlayFixed(engine, new[] { BenchEngine.Polish, BenchEngine.Stoke, BenchEngine.Stoke, BenchEngine.Press });

            result.CpSpent.Should().Be(2, "only the Stoke was paid for");
            result.Actions.Should().Be(11, "one Stoke, then Press repeated until the integrity ran out; the skipped actions took no turn");
            result.Status.Should().Be(MinigameStatus.Failed);
        }

        [Test]
        public void WhenTheLastActionIsNoLongerAvailableTheAttemptIsAbandoned()
        {
            var random = new ScriptedRandom(new[] { 1 }.Concat(Enumerable.Repeat(100, 10)).ToArray());
            var engine = Bench(random, new BenchTuning { MaxCp = 2 });

            var result = CraftPlayer.PlayFixed(engine, new[] { BenchEngine.Stoke, BenchEngine.Stoke });

            result.Actions.Should().Be(1);
            result.Stalled.Should().BeTrue();
        }

        [Test]
        public void PlayFixedAndAFixedSequencePolicyPlayTheSameAttempt()
        {
            var sequences = new[]
            {
                new[] { "stoke", "stoke", "polish", "press", "press", "polish", "press" },
                new[] { "polish", "polish", "press" },
                new[] { "stoke", "mend", "stoke", "press", "polish", "stoke", "press" }
            };

            foreach (var sequence in sequences)
            {
                for (var seed = 1; seed <= 30; seed++)
                {
                    var viaPolicy = CraftPlayer.Play(Bench(SimMinigameRandom.ForRun(seed)), new FixedSequencePolicy(sequence));
                    var direct = CraftPlayer.PlayFixed(Bench(SimMinigameRandom.ForRun(seed)), sequence);

                    direct.Should().Be(viaPolicy, $"{string.Join(",", sequence)} with seed {seed}");
                }
            }
        }

        [Test]
        public void ARefusedActionEndsAnAttemptPlayedByAPolicy()
        {
            var engine = new ToyEngine(SimMinigameRandom.ForRun(1), maxCp: 0);

            var result = CraftPlayer.Play(engine, new FixedSequencePolicyWithAnUnpayableAction());

            result.Actions.Should().Be(0);
        }

        [Test]
        public void ThePlayersRejectMissingArguments()
        {
            var engine = new ToyEngine(SimMinigameRandom.ForRun(1));

            ((Action)(() => CraftPlayer.Play(null, new FixedSequencePolicy(Array.Empty<string>())))).Should().Throw<ArgumentNullException>();
            ((Action)(() => CraftPlayer.Play(engine, null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => CraftPlayer.PlayFixed(engine, null))).Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void FixedSequencePolicySkipsRepeatsTheLastActionAndStopsWhenItIsUnavailable()
        {
            var engine = new ToyEngine(SimMinigameRandom.ForRun(1), maxCp: 5);
            var policy = new FixedSequencePolicy(new[] { ToyEngine.A, ToyEngine.D, ToyEngine.D });

            policy.Choose(engine).Should().Be(ToyEngine.A);
            engine.Perform(ToyEngine.A);
            policy.Choose(engine).Should().Be(ToyEngine.D);
            engine.Perform(ToyEngine.D);
            policy.Choose(engine).Should().BeNull("the second D cannot be paid for, and it is also the last action");
            new FixedSequencePolicy(Array.Empty<string>()).Choose(engine).Should().BeNull();
        }

        [Test]
        public void ARotationIsDescribedWithItsRunsCollapsed()
        {
            FixedSequencePolicy.Describe(new[] { "stoke", "stoke", "press", "polish", "polish", "polish" }).Should().Be("stoke x2 > press > polish x3");
            FixedSequencePolicy.Describe(Array.Empty<string>()).Should().Be("(none)");
        }

        private sealed class FixedSequencePolicyWithAnUnpayableAction : ICraftPolicy
        {
            public string Name => "Unpayable";

            public string Choose(ICraftMinigameEngine engine) => ToyEngine.D;
        }
    }
}
