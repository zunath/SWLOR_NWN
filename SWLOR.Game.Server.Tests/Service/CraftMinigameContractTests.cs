using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Covers the shared engine contract with a test-only engine: the preview and the result come from one set of pure transitions,
/// for the shared meters, the engine's own Thermal meter and the end of the turn alike.
/// </summary>
public class CraftMinigameContractTests
{
    private static readonly string[] AvailableActions =
    {
        TestForgeEngine.Heat, TestForgeEngine.Strike, TestForgeEngine.Polish, TestForgeEngine.Mend, TestForgeEngine.Rush
    };

    /// <summary>The rolls an action uses: its outcome roll when it has several outcomes, then the forecast roll (100 announces nothing).</summary>
    private static int[] Rolls(int outcomeRoll, int forecastRoll = 100) =>
        outcomeRoll > 0 ? new[] { outcomeRoll, forecastRoll } : new[] { forecastRoll };

    private static List<MinigameMeterChange> ChangesBetween(
        IReadOnlyList<MinigameMeterView> before,
        IReadOnlyList<MinigameMeterView> after) =>
        before.Zip(after, (b, a) => new MinigameMeterChange(a.Id, a.Label, b.Value, a.Value)).ToList();

    [Test]
    public void Preview_ListsEveryOutcomeWithExactCostsChancesAndEveryMeterDelta()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom());

        var preview = engine.Preview(TestForgeEngine.Strike);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(2);
        preview.IntegrityCost.Should().Be(5);
        preview.Outcomes.Select(outcome => outcome.Label).Should().Equal("Clean strike", "Glancing blow");
        preview.Outcomes.Select(outcome => outcome.ChancePercent).Should().Equal(75, 25);
        preview.Outcomes.Sum(outcome => outcome.ChancePercent).Should().Be(100);

        // Thermal 50 is inside the working range, and the piece cools by 5 at the end of the turn.
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Progress: 30, Quality: 5, Integrity: -5, Cp: -2));
        preview.Outcomes[0].Change(TestForgeEngine.ThermalId).Delta.Should().Be(-TestForgeEngine.Cooling);
        preview.Outcomes[1].Delta.Should().Be(new MinigameMeterDelta(Progress: 10, Integrity: -5, Cp: -2));
        preview.Outcomes[1].Change(TestForgeEngine.ThermalId).Delta.Should().Be(-TestForgeEngine.Cooling);
    }

    [Test]
    public void DescribeMeters_ListsTheSharedMetersAndTheEnginesOwnWithTheirMarks()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom());

        var meters = engine.DescribeMeters();

        meters.Select(meter => meter.Id).Should().Equal(
            MinigameMeterIds.Progress, MinigameMeterIds.Quality, MinigameMeterIds.Integrity, MinigameMeterIds.Cp, TestForgeEngine.ThermalId);
        meters.Select(meter => meter.Label).Should().Equal("Shape", "Finish", "Integrity", "CP", "Thermal");
        var thermal = meters.Single(meter => meter.Id == TestForgeEngine.ThermalId);
        thermal.Value.Should().Be(50);
        thermal.Max.Should().Be(100);
        thermal.Mark.Should().Be(MinigameMeterMark.Range(40, 70, "Working range"));
        thermal.Mark!.IsLine.Should().BeFalse();
        MinigameMeterMark.Line(9, "Overload").IsLine.Should().BeTrue();
        meters.Single(meter => meter.Id == MinigameMeterIds.Cp).Value.Should().Be(30);
    }

    [TestCase(TestForgeEngine.Heat, 0, 0, 50)]
    [TestCase(TestForgeEngine.Heat, 0, 0, 90)]
    [TestCase(TestForgeEngine.Strike, 0, 1, 50)]
    [TestCase(TestForgeEngine.Strike, 1, 80, 50)]
    [TestCase(TestForgeEngine.Strike, 0, 1, 10)]
    [TestCase(TestForgeEngine.Rush, 0, 1, 50)]
    [TestCase(TestForgeEngine.Rush, 1, 100, 50)]
    [TestCase(TestForgeEngine.Polish, 0, 0, 80)]
    [TestCase(TestForgeEngine.Mend, 0, 0, 50)]
    public void Perform_ChangesEveryMeterByExactlyWhatThePreviewPromised(string actionId, int outcomeIndex, int outcomeRoll, int thermal)
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom(Rolls(outcomeRoll)), thermal);
        var promised = engine.Preview(actionId).Outcomes[outcomeIndex];
        var before = engine.DescribeMeters();
        var beforeShared = engine.Meters;

        var result = engine.Perform(actionId);

        var after = engine.DescribeMeters();
        result.Performed.Should().BeTrue();
        result.Outcome.Should().BeEquivalentTo(promised);
        ChangesBetween(before, after).Should().BeEquivalentTo(promised.Changes);
        beforeShared.DeltaTo(engine.Meters).Should().Be(promised.Delta);
        result.MetersAfter.Should().Equal(after);
        result.Before.Should().Be(beforeShared);
        result.After.Should().Be(engine.Meters);
        result.Status.Should().Be(promised.ResultingStatus);
        engine.Status.Should().Be(promised.ResultingStatus);
    }

    [Test]
    public void Perform_AcrossManySeeds_AlwaysLandsOnAPreviewedOutcomeForEveryMeter()
    {
        for (var seed = 1; seed <= 200; seed++)
        {
            var engine = new TestForgeEngine(new SeededMinigameRandom(seed));
            var picker = new System.Random(seed);

            for (var turn = 0; turn < 40 && engine.Status == MinigameStatus.InProgress; turn++)
            {
                var actionId = AvailableActions[picker.Next(AvailableActions.Length)];
                var preview = engine.Preview(actionId);
                var before = engine.DescribeMeters();

                var result = engine.Perform(actionId);

                if (!preview.Enabled)
                {
                    result.Performed.Should().BeFalse();
                    engine.DescribeMeters().Should().Equal(before);
                    continue;
                }

                preview.Outcomes.Should().ContainEquivalentOf(result.Outcome, $"seed {seed} turn {turn}");
                ChangesBetween(before, engine.DescribeMeters()).Should().BeEquivalentTo(result.Outcome.Changes);
                foreach (var meter in engine.DescribeMeters())
                    meter.Value.Should().BeInRange(meter.Min, meter.Max);
            }
        }
    }

    [Test]
    public void EndOfTurn_CoolingIsPartOfThePreviewAndTheResult()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom(Rolls(0)), 50);

        var polish = engine.Preview(TestForgeEngine.Polish).Outcomes.Single();
        var heat = engine.Preview(TestForgeEngine.Heat).Outcomes.Single();

        polish.Change(TestForgeEngine.ThermalId).Delta.Should().Be(-5, "the piece cools after the action");
        heat.Change(TestForgeEngine.ThermalId).Delta.Should().Be(25 - 5, "the heat is added, then the piece cools");

        engine.Perform(TestForgeEngine.Heat);
        engine.Thermal.Should().Be(70);
    }

    [Test]
    public void EndOfTurn_CanChangeSharedMetersToo_ForExampleScorchingAnOverheatedPiece()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom(Rolls(0)), 90);

        var heat = engine.Preview(TestForgeEngine.Heat).Outcomes.Single();

        // 90 + 25 caps at 100, cools to 95, and anything above 90 costs 3 integrity at the end of the turn.
        heat.Change(TestForgeEngine.ThermalId).Delta.Should().Be(5);
        heat.Delta.Integrity.Should().Be(-3);
        engine.Perform(TestForgeEngine.Heat).Outcome.Delta.Integrity.Should().Be(-3);
        engine.Meters.Integrity.Value.Should().Be(37);
    }

    [Test]
    public void EndOfTurn_DoesNotRunWhenTheActionEndsTheAttempt()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom(1), 50);
        var preview = engine.Preview(TestForgeEngine.Rush);

        preview.Outcomes.Select(outcome => outcome.Change(TestForgeEngine.ThermalId).Delta).Should().OnlyContain(delta => delta == 0);

        engine.Perform(TestForgeEngine.Rush);

        engine.Thermal.Should().Be(50);
    }

    [Test]
    public void Forecast_IsNeverPartOfAPreviewAndSpendsNoChanceUntilAnActionIsPerformed()
    {
        var random = new ScriptedMinigameRandom();
        var engine = new TestForgeEngine(random);

        engine.PreviewAll();

        random.Calls.Should().Be(0);
        engine.NextOpportunity.Should().BeNull();
        engine.PreviewAll().SelectMany(action => action.Outcomes)
            .Should().OnlyContain(outcome => outcome.Changes.Count == 5);
    }

    [Test]
    public void Forecast_RunsAfterTheResultAndAnnouncesTheNextTurnOnly()
    {
        var random = new ScriptedMinigameRandom(Rolls(0, forecastRoll: 1));
        var engine = new TestForgeEngine(random);

        var result = engine.Perform(TestForgeEngine.Polish);

        random.Calls.Should().Be(1);
        result.Outcome.Delta.Should().Be(new MinigameMeterDelta(Quality: 20, Integrity: -2, Cp: -3), "the announcement is not part of the result");
        engine.NextOpportunity.Should().NotBeNull();
        engine.NextOpportunity!.TurnsAway.Should().BeGreaterThanOrEqualTo(1);
        engine.NextOpportunity.Name.Should().Be("Stable Containment");
    }

    [Test]
    public void Forecast_IsNotRolledOnceTheAttemptHasEnded()
    {
        var random = new ScriptedMinigameRandom(1);
        var engine = new TestForgeEngine(random);

        engine.Perform(TestForgeEngine.Rush).Status.Should().Be(MinigameStatus.Succeeded);

        random.Calls.Should().Be(1, "only the outcome was rolled");
        engine.NextOpportunity.Should().BeNull();
    }

    [Test]
    public void Forecast_ThatMovesAMeterIsRefused()
    {
        var engine = new TestForgeEngineWithMeddlingForecast(new ScriptedMinigameRandom());

        var act = () => engine.Perform(TestForgeEngine.Polish);

        act.Should().Throw<InvalidOperationException>().WithMessage("*RollForecast*");
    }

    [Test]
    public void Opportunity_IsTelegraphedOneTurnAheadAppliesToThatTurnAndPreviewsShowIt()
    {
        // Strike 1 rolls a clean hit and then announces an opportunity; strike 2 uses it; strike 3 announces nothing.
        var engine = new TestForgeEngine(new ScriptedMinigameRandom(1, 1, 1, 100, 1, 100));
        engine.NextOpportunity.Should().BeNull();
        var plain = engine.Preview(TestForgeEngine.Strike).Outcomes[0].Delta.Progress;

        engine.Perform(TestForgeEngine.Strike);
        var announced = engine.NextOpportunity;
        announced.Should().NotBeNull();
        announced!.TurnsAway.Should().BeGreaterThanOrEqualTo(1);
        engine.Preview(TestForgeEngine.Strike).Outcomes[0].Delta.Progress.Should().Be(plain + plain / 2);

        engine.Perform(TestForgeEngine.Strike);
        engine.NextOpportunity.Should().BeNull("the end of the turn used it up");
        var room = engine.Meters.Progress.Max - engine.Meters.Progress.Value;
        engine.Preview(TestForgeEngine.Strike).Outcomes[0].Delta.Progress.Should().Be(Math.Min(plain, room));
    }

    [Test]
    public void Preview_WarnsAboutTheActionThatWouldCompleteTheProgressMeter()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom(1, 1, 1, 100, 1));
        engine.Perform(TestForgeEngine.Strike);
        engine.Perform(TestForgeEngine.Strike);
        engine.Meters.Progress.Value.Should().Be(30 + 45);

        var finishing = engine.Preview(TestForgeEngine.Strike);

        finishing.FinishesItem.Should().BeTrue();
        finishing.Outcomes[0].FinishesItem.Should().BeTrue();
        finishing.Outcomes[1].FinishesItem.Should().BeFalse();
        engine.Preview(TestForgeEngine.Polish).FinishesItem.Should().BeFalse();

        engine.Perform(TestForgeEngine.Strike).Status.Should().Be(MinigameStatus.Succeeded);
    }

    [Test]
    public void Preview_FinishesTheItem_OnlyForTheActionThatCanComplete()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom());

        var rush = engine.Preview(TestForgeEngine.Rush);

        rush.FinishesItem.Should().BeTrue();
        rush.Outcomes[0].FinishesItem.Should().BeTrue();
        rush.Outcomes[1].FinishesItem.Should().BeFalse();
        rush.Outcomes[1].BreaksItem.Should().BeTrue();
        rush.CanBreakItem.Should().BeTrue();
        engine.Preview(TestForgeEngine.Strike).FinishesItem.Should().BeFalse();
        engine.Preview(TestForgeEngine.Polish).FinishesItem.Should().BeFalse();
    }

    [Test]
    public void Preview_ClampsDeltasToTheMeterLimits()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom());

        // Mend at full integrity restores nothing; the preview says so instead of promising 30.
        var mend = engine.Preview(TestForgeEngine.Mend).Outcomes.Single();
        mend.Delta.Should().Be(new MinigameMeterDelta(Cp: -4));

        // Rush's 1000 progress is limited to the 100 the meter holds.
        engine.Preview(TestForgeEngine.Rush).Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Progress: 100, Cp: -1));
    }

    [Test]
    public void Perform_AfterTheAttemptEnds_RefusesWithoutRollingOrChangingAnything()
    {
        var random = new ScriptedMinigameRandom(1);
        var engine = new TestForgeEngine(random);
        engine.Perform(TestForgeEngine.Rush).Status.Should().Be(MinigameStatus.Succeeded);
        AssertRefusedWithoutChange(engine, random, TestForgeEngine.Polish, "The attempt is over.");
        engine.Preview(TestForgeEngine.Polish).Enabled.Should().BeFalse();
    }

    [Test]
    public void Perform_ABrokenPiece_EndsTheAttemptAsFailed()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom(100));

        var result = engine.Perform(TestForgeEngine.Rush);

        result.Status.Should().Be(MinigameStatus.Failed);
        result.Outcome.BreaksItem.Should().BeTrue();
        engine.Meters.Integrity.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Perform_WithoutEnoughCp_IsDisabledWithAReasonAndChangesNothing()
    {
        // Mend costs 4 of the 30 CP; repeating it leaves 2 CP, one short of Polish's 3. Each Mend rolls only its forecast.
        var random = new ScriptedMinigameRandom(Enumerable.Repeat(100, 20).ToArray());
        var engine = new TestForgeEngine(random);
        while (engine.Meters.Cp.Value >= 3)
            engine.Perform(TestForgeEngine.Mend);

        var polish = engine.Preview(TestForgeEngine.Polish);

        polish.Enabled.Should().BeFalse();
        polish.DisabledReason.Should().Be("Not enough CP.");
        polish.CpCost.Should().Be(3);
        AssertRefusedWithoutChange(engine, random, TestForgeEngine.Polish, "Not enough CP.");
    }

    [Test]
    public void Perform_AnActionTheEngineDisables_IsRefusedWithItsReason()
    {
        var random = new ScriptedMinigameRandom();
        var engine = new TestForgeEngine(random);

        var preview = engine.Preview(TestForgeEngine.Locked);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Needs a hammer.");
        preview.Outcomes.Should().BeEmpty();
        preview.FinishesItem.Should().BeFalse();
        AssertRefusedWithoutChange(engine, random, TestForgeEngine.Locked, "Needs a hammer.");
    }

    [Test]
    public void Perform_AnUnknownAction_Throws()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom());

        var act = () => engine.Perform("not-an-action");

        act.Should().Throw<ArgumentException>().WithMessage("*not-an-action*");
    }

    [Test]
    public void SingleOutcomeActions_SpendOnlyTheForecastRoll()
    {
        var random = new ScriptedMinigameRandom(100);
        var engine = new TestForgeEngine(random);

        engine.Perform(TestForgeEngine.Polish);

        random.Calls.Should().Be(1);
    }

    [Test]
    public void Opportunity_CannotBeAnnouncedForTheCurrentTurn()
    {
        var act = () => new MinigameOpportunity("now", "Now", "No warning.", 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ActionRule_RejectsChancesThatDoNotAddUpToOneHundred()
    {
        var info = new MinigameActionInfo("odd", "Odd", string.Empty);
        var outcomes = new[]
        {
            new MinigameOutcomeRule<TestForgeState>("a", 60, state => state),
            new MinigameOutcomeRule<TestForgeState>("b", 30, state => state)
        };

        var act = () => new MinigameActionRule<TestForgeState>(info, 0, 0, outcomes);

        act.Should().Throw<ArgumentException>().WithMessage("*add up to 90*");
    }

    [Test]
    public void SeededRandom_RepeatsTheSameRun()
    {
        string Transcript(int seed)
        {
            var engine = new TestForgeEngine(new SeededMinigameRandom(seed));
            var lines = new List<string>();
            for (var turn = 0; turn < 12 && engine.Status == MinigameStatus.InProgress; turn++)
            {
                var result = engine.Perform(AvailableActions[turn % AvailableActions.Length]);
                lines.Add($"{result.ActionId}:{result.Performed}:{result.Outcome?.Label}:{result.After}:{engine.Thermal}:{engine.NextOpportunity?.Id}");
            }

            return string.Join("|", lines);
        }

        Transcript(7).Should().Be(Transcript(7));
        Enumerable.Range(1, 30).Select(Transcript).Distinct().Count().Should().BeGreaterThan(1);
    }

    [Test]
    public void Meter_ReportsFractionAndWholePercent()
    {
        var meter = new MinigameMeter(1, 3);

        meter.Fraction.Should().BeApproximately(1f / 3f, 0.0000001f);
        meter.Percent.Should().Be(33);
        new MinigameMeter(3, 3).IsFull.Should().BeTrue();
        new MinigameMeter(0, 3).IsEmpty.Should().BeTrue();
        new MinigameMeter(0, 0).Percent.Should().Be(0);
        meter.Add(-5).Value.Should().Be(0);
        meter.Add(50).Value.Should().Be(3);
    }

    /// <summary>A refused action changes no state, advances nothing and rolls nothing, not even a forecast.</summary>
    private static void AssertRefusedWithoutChange(TestForgeEngine engine, ScriptedMinigameRandom random, string actionId, string reason)
    {
        var meters = engine.DescribeMeters();
        var opportunity = engine.NextOpportunity;
        var turn = engine.Turn;
        var status = engine.Status;
        var thermal = engine.Thermal;
        var hits = engine.Hits;
        var calls = random.Calls;

        var refused = engine.Perform(actionId);

        refused.Performed.Should().BeFalse();
        refused.RejectedReason.Should().Be(reason);
        refused.Outcome.Should().BeNull();
        engine.DescribeMeters().Should().Equal(meters);
        engine.NextOpportunity.Should().Be(opportunity);
        engine.Turn.Should().Be(turn);
        engine.Status.Should().Be(status);
        engine.Thermal.Should().Be(thermal);
        engine.Hits.Should().Be(hits);
        random.Calls.Should().Be(calls);
    }
}
