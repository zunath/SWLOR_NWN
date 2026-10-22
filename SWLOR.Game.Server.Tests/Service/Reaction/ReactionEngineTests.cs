using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

namespace SWLOR.Game.Server.Tests.Service.Reaction;

/// <summary>
/// Covers the Reaction engine: every action in every phase, the cycle, previews that equal results, finishing, breaking, and the
/// one random choice. The default recipe is a Brew cycle of six turns, Calm Calm Rising Peak Settling Settling.
/// </summary>
public class ReactionEngineTests
{
    private const int CalmStart = 0;
    private const int RisingStart = 2;
    private const int PeakStart = 3;
    private const int SettlingStart = 4;

    private static readonly string[] AllActions =
    {
        ReactionEngine.Measure, ReactionEngine.Charge, ReactionEngine.Refine, ReactionEngine.Dampen, ReactionEngine.Stabilize
    };

    private static ReactionSettings Settings(
        int rank = 30,
        int level = 30,
        int intensity = 2,
        ReactionCyclePattern pattern = ReactionCyclePattern.Brew,
        int length = 6) =>
        new(new ReactionParameters(pattern, length, intensity), rank, level);

    private static ReactionEngine EngineAt(int start, ReactionSettings settings = null) =>
        new(new SeededMinigameRandom(1), settings ?? Settings(), start);

    private static MinigameOutcomePreview OnlyOutcome(ReactionEngine engine, string action) =>
        engine.Preview(action).Outcomes.Should().ContainSingle().Subject;

    [Test]
    public void TheEngineIsTheReactionWithItsFiveActions()
    {
        var engine = EngineAt(CalmStart);

        engine.Type.Should().Be(CraftMinigameType.Reaction);
        engine.Actions.Select(action => action.Id).Should().Equal(AllActions);
        engine.Actions.Select(action => action.Name).Should().Equal("Measure", "Charge", "Refine", "Dampen", "Stabilize");
        engine.Actions.Should().OnlyContain(action => !string.IsNullOrWhiteSpace(action.Description));
    }

    [Test]
    public void ANewAttemptStartsEmptyWithFullStabilityAndCp()
    {
        var settings = Settings();
        var engine = EngineAt(CalmStart, settings);

        engine.Status.Should().Be(MinigameStatus.InProgress);
        engine.Turn.Should().Be(0);
        engine.NextOpportunity.Should().BeNull();
        engine.Meters.Progress.Should().Be(new MinigameMeter(0, ReactionRules.MeterMaximum));
        engine.Meters.Quality.Should().Be(new MinigameMeter(0, ReactionRules.MeterMaximum));
        engine.Meters.Integrity.Should().Be(new MinigameMeter(ReactionRules.MeterMaximum, ReactionRules.MeterMaximum));
        engine.Meters.Cp.Should().Be(new MinigameMeter(settings.MaxCp, settings.MaxCp));
    }

    [Test]
    public void TheMetersAreNamedForTheReactionAndTheCycleMeterMarksThePeak()
    {
        var engine = EngineAt(CalmStart);

        var meters = engine.DescribeMeters();

        meters.Select(meter => meter.Id).Should().Equal(
            MinigameMeterIds.Progress, MinigameMeterIds.Quality, MinigameMeterIds.Integrity, MinigameMeterIds.Cp, ReactionEngine.CycleMeterId);
        meters.Select(meter => meter.Label).Should().Equal("Potency", "Subtlety", "Stability", "CP", "Reaction");
        var cycle = meters.Single(meter => meter.Id == ReactionEngine.CycleMeterId);
        cycle.Min.Should().Be(0);
        cycle.Max.Should().Be(5);
        cycle.Mark.Should().Be(MinigameMeterMark.Range(3, 3, "Peak"));
        cycle.Mark!.IsLine.Should().BeTrue();
    }

    [Test]
    public void ALongPeakIsMarkedAsARange()
    {
        var engine = EngineAt(0, Settings(pattern: ReactionCyclePattern.Surge, length: 8));

        var cycle = engine.DescribeMeters().Single(meter => meter.Id == ReactionEngine.CycleMeterId);

        cycle.Mark.Should().Be(MinigameMeterMark.Range(4, 6, "Peak"));
        cycle.Mark!.IsLine.Should().BeFalse();
    }

    [Test]
    public void Measure_AddsALittlePotencyAndCostsOnlyTheActionStability_InEveryPhase(
        [Values(CalmStart, RisingStart, PeakStart, SettlingStart)] int start)
    {
        var engine = EngineAt(start);

        var outcome = OnlyOutcome(engine, ReactionEngine.Measure);

        outcome.Delta.Should().Be(new MinigameMeterDelta(
            Progress: engine.Settings.MeasurePotency, Integrity: -ReactionRules.ActionStability));
        engine.Settings.MeasurePotency.Should().BeLessThan(engine.Settings.ChargePotency);
    }

    [Test]
    public void Charge_InCalmIsSafe()
    {
        var engine = EngineAt(CalmStart);

        var preview = engine.Preview(ReactionEngine.Charge);

        preview.IntegrityCost.Should().Be(ReactionRules.ActionStability);
        preview.CpCost.Should().Be(0);
        preview.Outcomes.Should().ContainSingle().Which.Delta.Should().Be(new MinigameMeterDelta(
            Progress: engine.Settings.ChargePotency, Integrity: -ReactionRules.ActionStability));
    }

    [Test]
    public void Charge_InRisingAndSettlingCostsHeavyStability_ForTheNormalPotency(
        [Values(RisingStart, SettlingStart)] int start)
    {
        var engine = EngineAt(start);

        var preview = engine.Preview(ReactionEngine.Charge);

        preview.IntegrityCost.Should().Be(ReactionRules.ActionStability + ReactionRules.HeavyChargeStability);
        preview.Outcomes.Single().Delta.Progress.Should().Be(engine.Settings.ChargePotency);
    }

    [Test]
    public void Charge_AtPeakIsDoubled_AndCostsMoreStabilityAtHigherIntensity()
    {
        var previousCost = 0;
        for (var intensity = 1; intensity <= 5; intensity++)
        {
            var engine = EngineAt(PeakStart, Settings(intensity: intensity));

            var preview = engine.Preview(ReactionEngine.Charge);

            preview.Outcomes.Single().Delta.Progress.Should().Be(
                engine.Settings.ChargePotency * ReactionRules.PeakChargeMultiplier, $"intensity {intensity}");
            preview.IntegrityCost.Should().Be(
                ReactionRules.ActionStability + ReactionRules.PeakChargeStability(intensity), $"intensity {intensity}");
            preview.IntegrityCost.Should().BeGreaterThan(previousCost);
            previousCost = preview.IntegrityCost;
        }
    }

    [Test]
    public void Charge_AtPeakCostsMoreStabilityThanInAnyOtherPhase()
    {
        var peak = EngineAt(PeakStart).Preview(ReactionEngine.Charge).IntegrityCost;

        foreach (var start in new[] { CalmStart, RisingStart, SettlingStart })
            EngineAt(start).Preview(ReactionEngine.Charge).IntegrityCost.Should().BeLessThan(peak);
    }

    [Test]
    public void Refine_AddsSubtlety_DoubledInSettlingAndHalvedAtPeak()
    {
        var settings = Settings();
        var plain = settings.RefineSubtlety;

        OnlyOutcome(EngineAt(CalmStart, settings), ReactionEngine.Refine).Delta.Quality.Should().Be(plain);
        OnlyOutcome(EngineAt(RisingStart, settings), ReactionEngine.Refine).Delta.Quality.Should().Be(plain);
        OnlyOutcome(EngineAt(SettlingStart, settings), ReactionEngine.Refine).Delta.Quality.Should().Be(plain * 2);
        OnlyOutcome(EngineAt(PeakStart, settings), ReactionEngine.Refine).Delta.Quality.Should().Be(plain / 2);
    }

    [Test]
    public void Refine_CostsOnlyTheActionStabilityAndNoCp([Values(CalmStart, RisingStart, PeakStart, SettlingStart)] int start)
    {
        var preview = EngineAt(start).Preview(ReactionEngine.Refine);

        preview.IntegrityCost.Should().Be(ReactionRules.ActionStability);
        preview.CpCost.Should().Be(0);
        preview.Outcomes.Single().Delta.Progress.Should().Be(0);
    }

    [Test]
    public void Refine_AtPeakStillAddsAtLeastOne()
    {
        var engine = EngineAt(PeakStart, Settings(rank: 0, level: 40));

        OnlyOutcome(engine, ReactionEngine.Refine).Delta.Quality.Should().BeGreaterThanOrEqualTo(1);
    }

    [Test]
    public void Dampen_HoldsThePhaseForOneMoreTurn_ThenTheCycleMovesOnAgain()
    {
        var engine = EngineAt(PeakStart);

        var result = engine.Perform(ReactionEngine.Dampen);

        result.Performed.Should().BeTrue();
        engine.CycleIndex.Should().Be(PeakStart);
        engine.CurrentPhase.Should().Be(ReactionPhase.Peak, "the phase was held");

        engine.Perform(ReactionEngine.Measure);

        engine.CycleIndex.Should().Be(PeakStart + 1, "the hold lasts one turn only");
        engine.CurrentPhase.Should().Be(ReactionPhase.Settling);
    }

    [Test]
    public void Dampen_CostsCpAndTheActionStabilityAndGainsNothing()
    {
        var engine = EngineAt(RisingStart);

        var outcome = OnlyOutcome(engine, ReactionEngine.Dampen);

        outcome.Delta.Should().Be(new MinigameMeterDelta(Integrity: -ReactionRules.ActionStability, Cp: -ReactionRules.DampenCp));
        outcome.Change(ReactionEngine.CycleMeterId)!.Delta.Should().Be(0, "the preview shows the phase held");
    }

    [Test]
    public void EveryOtherActionShowsTheCycleMovingOnByOneTurn()
    {
        var engine = EngineAt(CalmStart);

        foreach (var action in new[] { ReactionEngine.Measure, ReactionEngine.Charge, ReactionEngine.Refine })
            OnlyOutcome(engine, action).Change(ReactionEngine.CycleMeterId)!.Delta.Should().Be(1, action);
    }

    [Test]
    public void Dampen_CannotBeUsedWithoutEnoughCp()
    {
        var engine = EngineAt(CalmStart, Settings(rank: 0, level: 3));

        var preview = engine.Preview(ReactionEngine.Dampen);
        var result = engine.Perform(ReactionEngine.Dampen);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Not enough CP.");
        result.Performed.Should().BeFalse();
        engine.Turn.Should().Be(0);
        engine.CycleIndex.Should().Be(CalmStart);
    }

    [Test]
    public void Stabilize_RestoresStabilityForCp_ButNeverPastTheMaximum()
    {
        var engine = EngineAt(PeakStart);
        engine.Perform(ReactionEngine.Charge);
        var missing = ReactionRules.MeterMaximum - engine.Meters.Integrity.Value;
        missing.Should().BeGreaterThan(0);

        var outcome = OnlyOutcome(engine, ReactionEngine.Stabilize);

        outcome.Delta.Integrity.Should().Be(Math.Min(ReactionRules.StabilizeRestore, missing));
        outcome.Delta.Cp.Should().Be(-ReactionRules.StabilizeCp);
        outcome.Delta.Progress.Should().Be(0);
        engine.Preview(ReactionEngine.Stabilize).IntegrityCost.Should().Be(0);

        engine.Perform(ReactionEngine.Stabilize);

        engine.Meters.Integrity.Value.Should().BeLessThanOrEqualTo(ReactionRules.MeterMaximum);
        engine.Meters.Integrity.Value.Should().Be(ReactionRules.MeterMaximum - missing + Math.Min(ReactionRules.StabilizeRestore, missing));
    }

    [Test]
    public void Stabilize_IsRefusedWhileStabilityIsFull_AndChangesNothing()
    {
        var engine = EngineAt(CalmStart);
        var before = engine.DescribeMeters();

        var preview = engine.Preview(ReactionEngine.Stabilize);
        var result = engine.Perform(ReactionEngine.Stabilize);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Stability is already full.");
        result.Performed.Should().BeFalse();
        engine.Turn.Should().Be(0);
        engine.DescribeMeters().Should().Equal(before);
    }

    [Test]
    public void Stabilize_CannotBeUsedWithoutEnoughCp()
    {
        var engine = EngineAt(CalmStart, Settings(rank: 0, level: 3));
        engine.Perform(ReactionEngine.Measure);

        var preview = engine.Preview(ReactionEngine.Stabilize);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Not enough CP.");
    }

    [Test]
    public void EveryAction_AdvancesTheCycleByOneTurn_AndWrapsAtTheEnd()
    {
        var engine = EngineAt(CalmStart);
        var seen = new List<int>();

        for (var turn = 0; turn < 8; turn++)
        {
            seen.Add(engine.CycleIndex);
            engine.Perform(ReactionEngine.Measure).Performed.Should().BeTrue();
        }

        seen.Should().Equal(0, 1, 2, 3, 4, 5, 0, 1);
        engine.Turn.Should().Be(8);
    }

    [Test]
    public void TheWindowSeesTheCurrentPhaseAndTheNextTwo_WrappingAroundTheCycle()
    {
        EngineAt(CalmStart).UpcomingPhases.Should().Equal(ReactionPhase.Calm, ReactionPhase.Calm, ReactionPhase.Rising);
        EngineAt(RisingStart).UpcomingPhases.Should().Equal(ReactionPhase.Rising, ReactionPhase.Peak, ReactionPhase.Settling);
        EngineAt(SettlingStart).UpcomingPhases.Should().Equal(ReactionPhase.Settling, ReactionPhase.Settling, ReactionPhase.Calm);
        EngineAt(5).UpcomingPhases.Should().Equal(ReactionPhase.Settling, ReactionPhase.Calm, ReactionPhase.Calm);
    }

    [Test]
    public void ThePhaseAfterAPreviewIsReadFromTheCycleMeter()
    {
        var engine = EngineAt(RisingStart);

        var after = OnlyOutcome(engine, ReactionEngine.Measure).Change(ReactionEngine.CycleMeterId)!.After;

        engine.PhaseAt(after).Should().Be(ReactionPhase.Peak);
    }

    [Test]
    public void TheActionThatWouldCompletePotencyWarnsThatItFinishesTheItem_AndEndsTheAttempt()
    {
        // Rank 50 against a level 3 recipe: Charge adds 22 in Calm and Rising and 44 at Peak, so the fourth Charge completes it.
        var engine = EngineAt(CalmStart, Settings(rank: 50, level: 3));
        for (var turn = 0; turn < 3; turn++)
        {
            engine.Preview(ReactionEngine.Charge).FinishesItem.Should().BeFalse($"turn {turn}");
            engine.Perform(ReactionEngine.Charge).Status.Should().Be(MinigameStatus.InProgress);
        }

        engine.CurrentPhase.Should().Be(ReactionPhase.Peak);
        var finishing = engine.Preview(ReactionEngine.Charge);
        finishing.FinishesItem.Should().BeTrue();
        finishing.Outcomes.Single().FinishesItem.Should().BeTrue();
        engine.Preview(ReactionEngine.Measure).FinishesItem.Should().BeFalse();
        engine.Preview(ReactionEngine.Refine).FinishesItem.Should().BeFalse();
        var cycleBefore = engine.CycleIndex;
        var qualityBefore = engine.Meters.Quality;

        var result = engine.Perform(ReactionEngine.Charge);

        result.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Meters.Progress.IsFull.Should().BeTrue();
        engine.Meters.Quality.Should().Be(qualityBefore, "quality is locked in when Potency fills");
        engine.CycleIndex.Should().Be(cycleBefore, "the cycle does not advance once the item is finished");
    }

    [Test]
    public void ABrokenPieceEndsTheAttemptAndTheActionThatBreaksItIsPreviewedAsBreakingIt()
    {
        var engine = EngineAt(CalmStart);
        for (var turn = 0; turn < 49; turn++)
        {
            engine.Preview(ReactionEngine.Refine).CanBreakItem.Should().BeFalse($"turn {turn}");
            engine.Perform(ReactionEngine.Refine).Status.Should().Be(MinigameStatus.InProgress);
        }

        engine.Meters.Integrity.Value.Should().Be(ReactionRules.ActionStability);
        var breaking = engine.Preview(ReactionEngine.Refine);
        breaking.CanBreakItem.Should().BeTrue();
        breaking.Outcomes.Single().BreaksItem.Should().BeTrue();
        breaking.FinishesItem.Should().BeFalse();

        var result = engine.Perform(ReactionEngine.Refine);

        result.Status.Should().Be(MinigameStatus.Failed);
        engine.Status.Should().Be(MinigameStatus.Failed);
        engine.Meters.Integrity.Value.Should().Be(0);
    }

    [Test]
    public void AfterTheAttemptEnds_EveryActionIsRefused()
    {
        var engine = EngineAt(CalmStart, Settings(rank: 50, level: 3));
        while (engine.Status == MinigameStatus.InProgress)
            engine.Perform(ReactionEngine.Charge);
        var meters = engine.DescribeMeters();

        foreach (var action in AllActions)
        {
            var preview = engine.Preview(action);
            var result = engine.Perform(action);

            preview.Enabled.Should().BeFalse(action);
            preview.DisabledReason.Should().Be("The attempt is over.");
            result.Performed.Should().BeFalse(action);
        }

        engine.DescribeMeters().Should().Equal(meters);
    }

    [Test]
    public void AnUnknownActionIsAnError()
    {
        var engine = EngineAt(CalmStart);

        var preview = () => engine.Preview("nap");
        var perform = () => engine.Perform("nap");

        preview.Should().Throw<ArgumentException>();
        perform.Should().Throw<ArgumentException>();
    }

    [Test]
    public void AHigherRankMakesEveryActionStrongerAndGivesMoreCp()
    {
        var low = EngineAt(CalmStart, Settings(rank: 20, level: 30));
        var high = EngineAt(CalmStart, Settings(rank: 50, level: 30));

        OnlyOutcome(high, ReactionEngine.Charge).Delta.Progress.Should().BeGreaterThan(OnlyOutcome(low, ReactionEngine.Charge).Delta.Progress);
        OnlyOutcome(high, ReactionEngine.Measure).Delta.Progress.Should().BeGreaterThanOrEqualTo(OnlyOutcome(low, ReactionEngine.Measure).Delta.Progress);
        OnlyOutcome(high, ReactionEngine.Refine).Delta.Quality.Should().BeGreaterThan(OnlyOutcome(low, ReactionEngine.Refine).Delta.Quality);
        high.Meters.Cp.Max.Should().BeGreaterThan(low.Meters.Cp.Max);
    }

    [Test]
    public void ARecipeAboveYourRankIsHarder()
    {
        var atLevel = EngineAt(CalmStart, Settings(rank: 30, level: 30));
        var under = EngineAt(CalmStart, Settings(rank: 30, level: 40));

        OnlyOutcome(under, ReactionEngine.Charge).Delta.Progress.Should().BeLessThan(OnlyOutcome(atLevel, ReactionEngine.Charge).Delta.Progress);
        OnlyOutcome(under, ReactionEngine.Refine).Delta.Quality.Should().BeLessThan(OnlyOutcome(atLevel, ReactionEngine.Refine).Delta.Quality);
    }

    [Test]
    public void TheOnlyRandomChoiceIsTheStartingPosition_WhichIsAlwaysCalmOrRising()
    {
        for (var roll = 0; roll <= 100; roll++)
        {
            var random = new ScriptedMinigameRandom(roll);

            var engine = new ReactionEngine(random, Settings());

            random.Calls.Should().Be(1, $"roll {roll}");
            engine.CurrentPhase.Should().BeOneOf(ReactionPhase.Calm, ReactionPhase.Rising);
        }
    }

    [Test]
    public void ARolledStartCanLandOnEveryCalmAndRisingPosition()
    {
        var starts = Enumerable.Range(1, 200)
            .Select(seed => new ReactionEngine(new SeededMinigameRandom(seed), Settings()).CycleIndex)
            .Distinct()
            .OrderBy(index => index)
            .ToList();

        starts.Should().Equal(0, 1, 2);
    }

    [Test]
    public void NothingIsRandomAfterTheStart_AndNoOpportunityIsEverAnnounced()
    {
        var random = new ScriptedMinigameRandom(1);
        var engine = new ReactionEngine(random, Settings());

        for (var turn = 0; turn < 12 && engine.Status == MinigameStatus.InProgress; turn++)
        {
            engine.PreviewAll();
            engine.Perform(turn % 2 == 0 ? ReactionEngine.Refine : ReactionEngine.Measure);
            engine.NextOpportunity.Should().BeNull();
        }

        random.Calls.Should().Be(1, "only the starting position is rolled");
    }

    [Test]
    public void AGivenStartPositionRollsNothing()
    {
        var random = new ScriptedMinigameRandom();

        var engine = new ReactionEngine(random, Settings(), 4);

        random.Calls.Should().Be(0);
        engine.CurrentPhase.Should().Be(ReactionPhase.Settling);
    }

    [TestCase(-1)]
    [TestCase(6)]
    public void AStartPositionOutsideTheCycleIsRefused(int position)
    {
        var act = () => new ReactionEngine(new SeededMinigameRandom(1), Settings(), position);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void TheEngineRefusesMissingInputs()
    {
        var noRandom = () => new ReactionEngine(null!, Settings());
        var noSettings = () => new ReactionEngine(new SeededMinigameRandom(1), null!);

        noRandom.Should().Throw<ArgumentNullException>();
        noSettings.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void EveryResultIsExactlyWhatThePreviewPromised_AcrossManySeedsRecipesAndRanks()
    {
        var patterns = Enum.GetValues<ReactionCyclePattern>();
        for (var seed = 1; seed <= 150; seed++)
        {
            var picker = new System.Random(seed);
            var settings = Settings(
                rank: picker.Next(0, 51),
                level: picker.Next(1, 51),
                intensity: picker.Next(1, 6),
                pattern: patterns[picker.Next(patterns.Length)],
                length: picker.Next(ReactionCycle.MinimumLength, ReactionCycle.MaximumLength + 1));
            var engine = new ReactionEngine(new SeededMinigameRandom(seed), settings);

            for (var turn = 0; turn < 80 && engine.Status == MinigameStatus.InProgress; turn++)
            {
                var action = AllActions[picker.Next(AllActions.Length)];
                var preview = engine.Preview(action);
                var before = engine.DescribeMeters();

                var result = engine.Perform(action);

                if (!preview.Enabled)
                {
                    result.Performed.Should().BeFalse($"seed {seed} turn {turn}");
                    engine.DescribeMeters().Should().Equal(before);
                    continue;
                }

                result.Outcome.Should().BeEquivalentTo(preview.Outcomes.Single(), $"seed {seed} turn {turn}");
                before.Zip(engine.DescribeMeters(), (b, a) => new MinigameMeterChange(a.Id, a.Label, b.Value, a.Value))
                    .Should().BeEquivalentTo(preview.Outcomes.Single().Changes);
                result.Status.Should().Be(preview.Outcomes.Single().ResultingStatus);
                foreach (var meter in engine.DescribeMeters())
                    meter.Value.Should().BeInRange(meter.Min, meter.Max);
            }
        }
    }
}
