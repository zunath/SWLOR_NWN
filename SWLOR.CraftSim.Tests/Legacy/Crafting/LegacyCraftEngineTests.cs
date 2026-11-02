using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Tests.Legacy.Crafting
{
    [TestFixture]
    public class LegacyCraftEngineTests
    {
        private static LegacyCraftContext Context(int level = 40, int rank = 50, int control = 0, int craftsmanship = 0, int cp = 0) =>
            new(level, rank, control, craftsmanship, cp);

        /// <summary>A seed whose first d100 satisfies the test, so a roll's outcome is fixed without a fake generator.</summary>
        private static SimRandom SeedWhereFirstRoll(Func<int, bool> accept)
        {
            for (var seed = 0; seed < 10_000; seed++)
            {
                if (accept(new SimRandom(seed).D100()))
                    return new SimRandom(seed);
            }

            throw new InvalidOperationException("No seed found.");
        }

        private static SimRandom Succeeds(int chance) => SeedWhereFirstRoll(roll => roll <= chance);
        private static SimRandom Fails(int chance) => SeedWhereFirstRoll(roll => roll > chance);

        [Test]
        public void ActionTable_MatchesTheSource()
        {
            var expected = new (LegacyAction Action, string Name, int Rank, int Chance, int Cp, int Label, int Durability, LegacyActionKind Kind, int Amount)[]
            {
                (LegacyAction.BasicSynthesis, "Basic Synthesis", 0, 90, 0, 0, 10, LegacyActionKind.Progress, 10),
                (LegacyAction.RapidSynthesis, "Rapid Synthesis", 10, 75, 6, 6, 10, LegacyActionKind.Progress, 30),
                (LegacyAction.CarefulSynthesis, "Careful Synthesis", 30, 50, 10, 15, 10, LegacyActionKind.Progress, 80),
                (LegacyAction.BasicTouch, "Basic Touch", 5, 90, 3, 3, 10, LegacyActionKind.Quality, 10),
                (LegacyAction.StandardTouch, "Standard Touch", 15, 75, 6, 6, 10, LegacyActionKind.Quality, 30),
                (LegacyAction.PreciseTouch, "Precise Touch", 35, 50, 10, 15, 10, LegacyActionKind.Quality, 80),
                (LegacyAction.MastersMend, "Master's Mend", 10, 100, 10, 10, 0, LegacyActionKind.Repair, 30),
                (LegacyAction.SteadyHand, "Steady Hand", 20, 100, 12, 12, 0, LegacyActionKind.Buff, 0),
                (LegacyAction.MuscleMemory, "Muscle Memory", 40, 100, 12, 12, 0, LegacyActionKind.Buff, 0),
                (LegacyAction.Veneration, "Veneration", 25, 100, 8, 8, 10, LegacyActionKind.Buff, 4),
                (LegacyAction.WasteNot, "Waste Not", 8, 100, 4, 4, 0, LegacyActionKind.Buff, 4),
            };

            LegacyActionSpec.All.Should().HaveCount(11);
            foreach (var row in expected)
            {
                var spec = LegacyActionSpec.Of(row.Action);
                (spec.Name, spec.UnlockRank, spec.BaseChance, spec.CpCost, spec.LabelledCpCost, spec.DurabilityLoss, spec.Kind, spec.Amount)
                    .Should().Be((row.Name, row.Rank, row.Chance, row.Cp, row.Label, row.Durability, row.Kind, row.Amount), row.Name);
            }
        }

        [Test]
        public void CarefulSynthesisAndPreciseTouch_SpendTenCpWhileTheirButtonsShowFifteen()
        {
            var context = Context(cp: 30);
            var state = context.Start();
            var before = state.Cp;

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.CarefulSynthesis, new SimRandom(1));
            before.Should().Be(state.Cp + 10);

            before = state.Cp;
            LegacyCraftEngine.Apply(ref state, context, LegacyAction.PreciseTouch, new SimRandom(1));
            before.Should().Be(state.Cp + 10);
            LegacyActionSpec.Of(LegacyAction.CarefulSynthesis).LabelledCpCost.Should().Be(15);
            LegacyActionSpec.Of(LegacyAction.PreciseTouch).LabelledCpCost.Should().Be(15);
        }

        [Test]
        public void SynthesesAreSteadyHandActionsAndTouchesAreMuscleMemoryActions()
        {
            foreach (var action in new[] { LegacyAction.BasicSynthesis, LegacyAction.RapidSynthesis, LegacyAction.CarefulSynthesis })
                LegacyActionSpec.Of(action).Boost.Should().Be(LegacyChanceBoost.SteadyHand);

            foreach (var action in new[] { LegacyAction.BasicTouch, LegacyAction.StandardTouch, LegacyAction.PreciseTouch })
                LegacyActionSpec.Of(action).Boost.Should().Be(LegacyChanceBoost.MuscleMemory);
        }

        [Test]
        public void Start_BeginsWithFullCpAndDurabilityAndNothingActive()
        {
            var context = Context(level: 20, rank: 25, cp: 10);
            var state = context.Start();

            state.Cp.Should().Be(context.MaxCp);
            state.Durability.Should().Be(context.MaxDurability);
            (state.Progress, state.Quality, state.Actions).Should().Be((0, 0, 0));
            (state.SteadyHandActive, state.MuscleMemoryActive, state.VenerationSteps, state.WasteNotSteps).Should().Be((false, false, 0, 0));
            state.Outcome.Should().Be(LegacyCraftOutcome.InProgress);
        }

        [Test]
        public void LockedAction_IsRefusedAndChangesNothing()
        {
            var context = Context(rank: 4);
            var state = context.Start();
            var before = state;

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.BasicTouch, new SimRandom(1)).Should().Be(LegacyActionResult.Refused);

            state.Should().Be(before);
        }

        [Test]
        public void UnaffordableAction_IsRefusedAndChangesNothing()
        {
            var context = Context(rank: 10);
            var state = context.Start();
            state.Cp = 5;
            var before = state;

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.RapidSynthesis, new SimRandom(1)).Should().Be(LegacyActionResult.Refused);

            state.Should().Be(before);
        }

        [Test]
        public void RefusedRapidSynthesis_StillSpendsAVenerationStep()
        {
            var context = Context(rank: 30);
            var state = context.Start();
            state.Cp = 2;
            state.VenerationSteps = 4;

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.RapidSynthesis, new SimRandom(1)).Should().Be(LegacyActionResult.Refused);

            (state.Cp, state.VenerationSteps, state.Durability).Should().Be((2, 3, context.MaxDurability));
        }

        [Test]
        public void Veneration_HalvesTheCpOfTheNextFourRapidOrCarefulSynthesesOnly()
        {
            var context = Context(level: 60, rank: 50, cp: 200);
            var state = context.Start();
            LegacyCraftEngine.Apply(ref state, context, LegacyAction.Veneration, new SimRandom(1));
            state.VenerationSteps.Should().Be(4);

            LegacyCraftEngine.CpCost(state, LegacyAction.RapidSynthesis).Should().Be(3);
            LegacyCraftEngine.CpCost(state, LegacyAction.CarefulSynthesis).Should().Be(5);
            LegacyCraftEngine.CpCost(state, LegacyAction.BasicTouch).Should().Be(3);

            for (var use = 0; use < 4; use++)
            {
                var cp = state.Cp;
                LegacyCraftEngine.Apply(ref state, context, LegacyAction.RapidSynthesis, new SimRandom(use));
                (cp - state.Cp).Should().Be(3, $"use {use + 1} is halved");
            }

            LegacyCraftEngine.CpCost(state, LegacyAction.RapidSynthesis).Should().Be(6);
        }

        [Test]
        public void WasteNot_HalvesTheDurabilityOfTheNextFourLosingActionsOnly()
        {
            var context = Context(rank: 40, cp: 200);
            var state = context.Start();
            LegacyCraftEngine.Apply(ref state, context, LegacyAction.WasteNot, new SimRandom(1));
            state.WasteNotSteps.Should().Be(4);
            var durability = state.Durability;

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.SteadyHand, new SimRandom(1));
            LegacyCraftEngine.Apply(ref state, context, LegacyAction.MuscleMemory, new SimRandom(1));
            state.Durability.Should().Be(durability, "actions that lose no durability keep the steps");
            state.WasteNotSteps.Should().Be(4);

            for (var use = 0; use < 4; use++)
            {
                var before = state.Durability;
                LegacyCraftEngine.Apply(ref state, context, LegacyAction.BasicTouch, new SimRandom(use));
                (before - state.Durability).Should().Be(5, $"use {use + 1} is halved");
            }

            var last = state.Durability;
            LegacyCraftEngine.Apply(ref state, context, LegacyAction.BasicTouch, new SimRandom(9));
            (last - state.Durability).Should().Be(10);
        }

        [Test]
        public void VenerationItself_LosesDurabilityAndSpendsAWasteNotStep()
        {
            var context = Context(rank: 40, cp: 200);
            var state = context.Start();
            state.WasteNotSteps = 4;
            var durability = state.Durability;

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.Veneration, new SimRandom(1));

            (durability - state.Durability, state.WasteNotSteps).Should().Be((5, 3));
        }

        [Test]
        public void AFailedRoll_StillSpendsCpDurabilityAndAWasteNotStep()
        {
            var context = Context(rank: 40, cp: 200);
            var state = context.Start();
            state.WasteNotSteps = 2;
            var (cp, durability) = (state.Cp, state.Durability);

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.StandardTouch, Fails(75)).Should().Be(LegacyActionResult.RollFailed);

            (cp - state.Cp, durability - state.Durability, state.WasteNotSteps, state.Quality).Should().Be((6, 5, 1, 0));
        }

        [Test]
        public void SteadyHand_GuaranteesTheNextSynthesisAndIsSpentOnlyByASynthesis()
        {
            var context = Context(rank: 40, cp: 200, craftsmanship: 10);
            var state = context.Start();
            LegacyCraftEngine.Apply(ref state, context, LegacyAction.SteadyHand, new SimRandom(1));
            LegacyCraftEngine.ChancePercent(state, LegacyAction.CarefulSynthesis).Should().Be(100);
            LegacyCraftEngine.ChancePercent(state, LegacyAction.BasicTouch).Should().Be(90);

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.BasicTouch, new SimRandom(2));
            state.SteadyHandActive.Should().BeTrue("a touch does not spend Steady Hand");

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.CarefulSynthesis, Fails(50)).Should().Be(LegacyActionResult.RollSucceeded);

            state.SteadyHandActive.Should().BeFalse();
            state.Progress.Should().Be(context.ProgressOf(LegacyAction.CarefulSynthesis));
        }

        [Test]
        public void MuscleMemory_GuaranteesTheNextTouchAndIsSpentOnlyByATouch()
        {
            var context = Context(rank: 40, cp: 200, control: 10);
            var state = context.Start();
            LegacyCraftEngine.Apply(ref state, context, LegacyAction.MuscleMemory, new SimRandom(1));
            LegacyCraftEngine.ChancePercent(state, LegacyAction.PreciseTouch).Should().Be(100);
            LegacyCraftEngine.ChancePercent(state, LegacyAction.BasicSynthesis).Should().Be(90);

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.BasicSynthesis, new SimRandom(2));
            state.MuscleMemoryActive.Should().BeTrue();

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.PreciseTouch, Fails(50)).Should().Be(LegacyActionResult.RollSucceeded);

            state.MuscleMemoryActive.Should().BeFalse();
            state.Quality.Should().Be(context.QualityOf(LegacyAction.PreciseTouch));
        }

        [Test]
        public void MastersMend_RestoresThirtyDurabilityUpToTheMaximum()
        {
            var context = Context(rank: 40, cp: 50);
            var state = context.Start();
            state.Durability = 20;

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.MastersMend, new SimRandom(1));
            state.Durability.Should().Be(50);

            state.Durability = context.MaxDurability - 10;
            LegacyCraftEngine.Apply(ref state, context, LegacyAction.MastersMend, new SimRandom(1));
            state.Durability.Should().Be(context.MaxDurability);
        }

        [Test]
        public void Durability_NeverGoesBelowZero()
        {
            var context = Context(level: 40, rank: 40);
            var state = context.Start();
            state.Durability = 4;
            state.Progress = 0;

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.BasicSynthesis, Fails(90));

            state.Durability.Should().Be(0);
        }

        [Test]
        public void ProgressAndQuality_StopAtTheirMaximums()
        {
            var context = Context(level: 1, rank: 50, control: 100, craftsmanship: 100, cp: 100);
            var state = context.Start();
            state.Quality = context.MaxQuality - 1;
            state.Progress = context.MaxProgress - 1;

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.BasicTouch, Succeeds(90));
            state.Quality.Should().Be(context.MaxQuality);

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.BasicSynthesis, Succeeds(90));
            state.Progress.Should().Be(context.MaxProgress);
        }

        [Test]
        public void CompletingProgressWinsOverTheDurabilityThatActionSpent()
        {
            var context = Context(level: 40, rank: 40, craftsmanship: 100);
            var state = context.Start();
            state.Durability = 10;
            state.Progress = context.MaxProgress - 1;

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.BasicSynthesis, Succeeds(90)).Should().Be(LegacyActionResult.RollSucceeded);

            (state.Durability, state.Outcome).Should().Be((0, LegacyCraftOutcome.Succeeded));
        }

        [Test]
        public void ReachingZeroDurabilityShortOfTheProgress_FailsTheCraft()
        {
            var context = Context(level: 40, rank: 40);
            var state = context.Start();
            state.Durability = 10;

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.BasicSynthesis, Succeeds(90));

            (state.Durability, state.Outcome).Should().Be((0, LegacyCraftOutcome.Failed));
        }

        [Test]
        public void AFinishedCraft_RefusesEveryAction()
        {
            var context = Context(level: 1, rank: 50, craftsmanship: 100);
            var state = context.Start();
            state.Progress = context.MaxProgress - 1;
            LegacyCraftEngine.Apply(ref state, context, LegacyAction.BasicSynthesis, Succeeds(90));
            state.Outcome.Should().Be(LegacyCraftOutcome.Succeeded);
            var finished = state;

            LegacyCraftEngine.Apply(ref state, context, LegacyAction.BasicTouch, new SimRandom(1)).Should().Be(LegacyActionResult.Refused);

            state.Should().Be(finished);
        }

        [Test]
        public void CanUse_RequiresTheRankTheCpAndAnUnfinishedCraft()
        {
            var context = Context(rank: 10);
            var state = context.Start();
            state.Cp = 5;

            LegacyCraftEngine.CanUse(state, context, LegacyAction.BasicSynthesis).Should().BeTrue();
            LegacyCraftEngine.CanUse(state, context, LegacyAction.RapidSynthesis).Should().BeFalse("it costs 6 CP");
            LegacyCraftEngine.CanUse(state, context, LegacyAction.CarefulSynthesis).Should().BeFalse("it unlocks at rank 30");
            state.Outcome = LegacyCraftOutcome.Failed;
            LegacyCraftEngine.CanUse(state, context, LegacyAction.BasicSynthesis).Should().BeFalse();
        }
    }
}
