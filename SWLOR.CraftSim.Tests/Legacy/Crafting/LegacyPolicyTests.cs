using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Tests.Legacy.Crafting
{
    [TestFixture]
    public class LegacyPolicyTests
    {
        private static LegacyCraftContext Context(int level, int rank, int control = 0, int craftsmanship = 0, int cp = 0) =>
            new(level, rank, control, craftsmanship, cp);

        [Test]
        public void DominantRotation_OpensWithWasteNotThenBasicTouch()
        {
            var context = Context(40, 50, control: 29, craftsmanship: 30, cp: 37);
            var state = context.Start();
            var policy = new DominantRotationPolicy();
            var rng = new SimRandom(1);

            policy.Choose(state, context, rng).Should().Be(LegacyAction.WasteNot);
            LegacyCraftEngine.Apply(ref state, context, LegacyAction.WasteNot, rng);
            policy.Choose(state, context, rng).Should().Be(LegacyAction.BasicTouch);
        }

        [Test]
        public void DominantRotation_BelowRank8_HasNoWasteNotToOpenWith()
        {
            var context = Context(5, 5);

            new DominantRotationPolicy().Choose(context.Start(), context, new SimRandom(1)).Should().Be(LegacyAction.BasicTouch);
        }

        [Test]
        public void DominantRotation_BelowRank5_CanOnlyFinishWithBasicSynthesis()
        {
            var context = Context(4, 4);

            new DominantRotationPolicy().Choose(context.Start(), context, new SimRandom(1)).Should().Be(LegacyAction.BasicSynthesis);
        }

        [Test]
        public void DominantRotation_RepairsWithMastersMendWhenTheNextTouchWouldEndThePiece()
        {
            var context = Context(40, 50, control: 29, craftsmanship: 30, cp: 37);
            var state = context.Start();
            state.Durability = 5;
            state.WasteNotSteps = 4;

            new DominantRotationPolicy().Choose(state, context, new SimRandom(1)).Should().Be(LegacyAction.MastersMend);
        }

        [Test]
        public void DominantRotation_StopsTouchingOnceQualityIsFull()
        {
            var context = Context(40, 50, control: 29, craftsmanship: 30, cp: 37);
            var state = context.Start();
            state.Quality = context.MaxQuality;
            state.WasteNotSteps = 4;

            var action = new DominantRotationPolicy().Choose(state, context, new SimRandom(1));

            LegacyActionSpec.Of(action.Value).Kind.Should().Be(LegacyActionKind.Progress);
        }

        [Test]
        public void Lookahead_NeverTouchesWhenATouchWouldBreakThePieceShortOfFinishing()
        {
            var context = Context(10, 8);
            var state = context.Start();
            state.Durability = 10;
            state.Progress = context.MaxProgress - 1;

            new RolloutPolicy().Choose(state, context, new SimRandom(1)).Should().BeOneOf(LegacyAction.BasicSynthesis, LegacyAction.WasteNot);
        }

        [TestCase(0)]
        [TestCase(RolloutPolicy.MaximumRollouts + 1)]
        public void Lookahead_SamplesAtLeastOneAndAtMostTheMaximumNumberOfFutures(int futures)
        {
            var act = () => new RolloutPolicy(futures);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase(0)]
        [TestCase(1)]
        public void EveryPolicy_OnlyChoosesActionsThePlayerCanUse(int policyIndex)
        {
            ILegacyCraftPolicy policy = policyIndex == 0 ? new DominantRotationPolicy() : new RolloutPolicy();
            var rng = new SimRandom(42);

            for (var trial = 0; trial < 60; trial++)
            {
                var level = 1 + rng.Roll(50);
                var rank = Math.Clamp(level + rng.Roll(9) - 5, LegacyCraftRules.RequiredRank(level), 50);
                var context = Context(level, rank, control: rng.Roll(30), craftsmanship: rng.Roll(30), cp: rng.Roll(37));
                var state = context.Start();
                for (var step = 0; step < 40 && !state.IsFinished; step++)
                {
                    var action = policy.Choose(state, context, rng);
                    action.Should().NotBeNull();
                    LegacyCraftEngine.CanUse(state, context, action.Value).Should().BeTrue($"{action} at level {level}, rank {rank}, step {step}");
                    LegacyCraftEngine.Apply(ref state, context, action.Value, rng);
                }
            }
        }

        [Test]
        public void EveryPolicy_ReturnsNothingForAFinishedCraft()
        {
            var context = Context(5, 5);
            var state = context.Start();
            state.Outcome = LegacyCraftOutcome.Failed;

            new DominantRotationPolicy().Choose(state, context, new SimRandom(1)).Should().BeNull();
            new RolloutPolicy().Choose(state, context, new SimRandom(1)).Should().BeNull();
        }

        [Test]
        public void Lookahead_SucceedsFarMoreOftenThanTheRotationWhereTheRotationRunsOutOfCp()
        {
            var context = Context(50, 50);

            var rotation = SuccessRate(context, new DominantRotationPolicy(), 200);
            var lookahead = SuccessRate(context, new RolloutPolicy(), 200);

            lookahead.Should().BeGreaterThan(rotation + 0.2, "a crafter with no gear at level 50 needs more than the fixed rotation");
        }

        [Test]
        public void DominantRotation_WithBestGearAtLevel40To50_FinishesAlmostEveryCraftAtNearFullQuality()
        {
            foreach (var level in new[] { 40, 45, 50 })
            {
                var context = Context(level, level, control: 29, craftsmanship: 30, cp: 37);
                var results = Enumerable.Range(0, 200).Select(run => LegacyCraftRunner.Play(context, new DominantRotationPolicy(), new SimRandom(run))).ToList();

                results.Count(r => r.Succeeded).Should().BeGreaterThanOrEqualTo(180, $"level {level}");
                results.Where(r => r.Succeeded).Average(r => r.QualityFraction).Should().BeGreaterThan(0.85, $"level {level}");
            }
        }

        [Test]
        public void DominantRotation_BelowRank40_CannotReachMeaningfulQualityWithoutGear()
        {
            var context = Context(30, 30);

            var results = Enumerable.Range(0, 200).Select(run => LegacyCraftRunner.Play(context, new DominantRotationPolicy(), new SimRandom(run))).ToList();

            results.Where(r => r.Succeeded).Average(r => r.QualityFraction).Should().BeLessThan(0.15);
        }

        private static double SuccessRate(LegacyCraftContext context, ILegacyCraftPolicy policy, int runs) =>
            Enumerable.Range(0, runs).Count(run => LegacyCraftRunner.Play(context, policy, new SimRandom(run)).Succeeded) / (double)runs;
    }
}
