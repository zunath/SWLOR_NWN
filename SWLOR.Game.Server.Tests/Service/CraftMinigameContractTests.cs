using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Covers the shared engine contract with a test-only engine: the preview and the result come from one rule.</summary>
public class CraftMinigameContractTests
{
    private static readonly string[] AvailableActions =
    {
        TestForgeEngine.Strike, TestForgeEngine.Polish, TestForgeEngine.Mend, TestForgeEngine.Rush
    };

    [Test]
    public void Preview_ListsEveryOutcomeWithExactCostsChancesAndDeltas()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom());

        var preview = engine.Preview(TestForgeEngine.Strike);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(2);
        preview.IntegrityCost.Should().Be(5);
        preview.Outcomes.Select(outcome => outcome.Label).Should().Equal("Clean strike", "Glancing blow");
        preview.Outcomes.Select(outcome => outcome.ChancePercent).Should().Equal(75, 25);
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Progress: 30, Quality: 5, Integrity: -5, Cp: -2));
        preview.Outcomes[1].Delta.Should().Be(new MinigameMeterDelta(Progress: 10, Integrity: -5, Cp: -2));
        preview.Outcomes.Sum(outcome => outcome.ChancePercent).Should().Be(100);
    }

    [TestCase(TestForgeEngine.Strike, 0, 1)]
    [TestCase(TestForgeEngine.Strike, 1, 80)]
    [TestCase(TestForgeEngine.Rush, 0, 1)]
    [TestCase(TestForgeEngine.Rush, 1, 100)]
    [TestCase(TestForgeEngine.Polish, 0, 50)]
    [TestCase(TestForgeEngine.Mend, 0, 100)]
    public void Perform_AppliesExactlyTheOutcomeThePreviewPromised(string actionId, int outcomeIndex, int roll)
    {
        // Every outcome of every action, reached by scripting the roll that selects it, matches its preview.
        var engine = new TestForgeEngine(new ScriptedMinigameRandom(roll));
        var preview = engine.Preview(actionId);
        var promised = preview.Outcomes[outcomeIndex];
        var before = engine.Meters;

        var result = engine.Perform(actionId);

        result.Performed.Should().BeTrue();
        result.Outcome.Should().Be(promised);
        before.DeltaTo(engine.Meters).Should().Be(promised.Delta);
        result.Before.Should().Be(before);
        result.After.Should().Be(engine.Meters);
        result.Status.Should().Be(promised.ResultingStatus);
        engine.Status.Should().Be(promised.ResultingStatus);
    }

    [Test]
    public void Perform_AcrossManySeeds_AlwaysLandsOnAPreviewedOutcome()
    {
        for (var seed = 1; seed <= 200; seed++)
        {
            var random = new SeededMinigameRandom(seed);
            var engine = new TestForgeEngine(random);
            var picker = new System.Random(seed);

            for (var turn = 0; turn < 40 && engine.Status == MinigameStatus.InProgress; turn++)
            {
                var actionId = AvailableActions[picker.Next(AvailableActions.Length)];
                var preview = engine.Preview(actionId);
                var before = engine.Meters;

                var result = engine.Perform(actionId);

                if (!preview.Enabled)
                {
                    result.Performed.Should().BeFalse();
                    engine.Meters.Should().Be(before);
                    continue;
                }

                preview.Outcomes.Should().Contain(result.Outcome, $"seed {seed} turn {turn}");
                before.DeltaTo(engine.Meters).Should().Be(result.Outcome.Delta);
                engine.Meters.Cp.Value.Should().BeInRange(0, engine.Meters.Cp.Max);
                engine.Meters.Integrity.Value.Should().BeInRange(0, engine.Meters.Integrity.Max);
                engine.Meters.Progress.Value.Should().BeInRange(0, engine.Meters.Progress.Max);
            }
        }
    }

    [Test]
    public void Preview_FinishesTheItem_OnlyForTheActionThatCanComplete()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom());

        engine.Preview(TestForgeEngine.Rush).FinishesItem.Should().BeTrue();
        engine.Preview(TestForgeEngine.Rush).Outcomes[0].FinishesItem.Should().BeTrue();
        engine.Preview(TestForgeEngine.Rush).Outcomes[1].FinishesItem.Should().BeFalse();
        engine.Preview(TestForgeEngine.Rush).Outcomes[1].BreaksItem.Should().BeTrue();
        engine.Preview(TestForgeEngine.Rush).CanBreakItem.Should().BeTrue();
        engine.Preview(TestForgeEngine.Strike).FinishesItem.Should().BeFalse();
        engine.Preview(TestForgeEngine.Polish).FinishesItem.Should().BeFalse();
    }

    [Test]
    public void Preview_WarnsAboutTheActionThatWouldCompleteTheProgressMeter()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom(1, 1, 1));
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
    public void Preview_ClampsDeltasToTheMeterLimits()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom());

        // Mend at full integrity restores nothing; the preview says so instead of promising 30.
        var mend = engine.Preview(TestForgeEngine.Mend);

        mend.Outcomes.Should().ContainSingle().Which.Delta.Should().Be(new MinigameMeterDelta(Cp: -4));

        // Rush's 1000 progress is limited to the 100 the meter holds.
        engine.Preview(TestForgeEngine.Rush).Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Progress: 100, Cp: -1));
    }

    [Test]
    public void Perform_AfterTheAttemptEnds_RefusesWithoutRolling()
    {
        var random = new ScriptedMinigameRandom(1);
        var engine = new TestForgeEngine(random);
        engine.Perform(TestForgeEngine.Rush).Status.Should().Be(MinigameStatus.Succeeded);
        var callsAfterFinish = random.Calls;

        var refused = engine.Perform(TestForgeEngine.Polish);

        refused.Performed.Should().BeFalse();
        refused.RejectedReason.Should().Be("The attempt is over.");
        refused.Outcome.Should().BeNull();
        random.Calls.Should().Be(callsAfterFinish);
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
        // Mend costs 4 of the 30 CP and rolls nothing; repeating it leaves 2 CP, one short of Polish's 3.
        var engine = new TestForgeEngine(new ScriptedMinigameRandom());
        while (engine.Meters.Cp.Value >= 3)
            engine.Perform(TestForgeEngine.Mend);
        var before = engine.Meters;

        var polish = engine.Preview(TestForgeEngine.Polish);
        var refused = engine.Perform(TestForgeEngine.Polish);

        polish.Enabled.Should().BeFalse();
        polish.DisabledReason.Should().Be("Not enough CP.");
        polish.CpCost.Should().Be(3);
        refused.Performed.Should().BeFalse();
        refused.RejectedReason.Should().Be("Not enough CP.");
        engine.Meters.Should().Be(before);
    }

    [Test]
    public void Perform_AnActionTheEngineDisables_IsRefusedWithItsReason()
    {
        var random = new ScriptedMinigameRandom();
        var engine = new TestForgeEngine(random);

        var preview = engine.Preview(TestForgeEngine.Locked);
        var refused = engine.Perform(TestForgeEngine.Locked);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Needs a hammer.");
        preview.Outcomes.Should().BeEmpty();
        refused.Performed.Should().BeFalse();
        refused.RejectedReason.Should().Be("Needs a hammer.");
        random.Calls.Should().Be(0);
        engine.Turn.Should().Be(0);
    }

    [Test]
    public void Perform_AnUnknownAction_Throws()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom());

        var act = () => engine.Perform("not-an-action");

        act.Should().Throw<ArgumentException>().WithMessage("*not-an-action*");
    }

    [Test]
    public void SingleOutcomeActions_DoNotSpendChance()
    {
        var random = new ScriptedMinigameRandom();
        var engine = new TestForgeEngine(random);

        engine.Perform(TestForgeEngine.Polish);

        random.Calls.Should().Be(0);
    }

    [Test]
    public void Opportunity_IsTelegraphedBeforeItAppliesAndTheNextPreviewShowsIt()
    {
        var engine = new TestForgeEngine(new ScriptedMinigameRandom(1, 1, 1));
        engine.NextOpportunity.Should().BeNull();
        var plain = engine.Preview(TestForgeEngine.Strike).Outcomes[0].Delta.Progress;

        engine.Perform(TestForgeEngine.Strike);

        var announced = engine.NextOpportunity;
        announced.Should().NotBeNull();
        announced.TurnsAway.Should().BeGreaterThanOrEqualTo(1);
        announced.Name.Should().Be("Stable Containment");
        engine.Preview(TestForgeEngine.Strike).Outcomes[0].Delta.Progress.Should().Be(plain + 15);

        engine.Perform(TestForgeEngine.Strike);
        engine.NextOpportunity.Should().BeNull();
        var room = engine.Meters.Progress.Max - engine.Meters.Progress.Value;
        engine.Preview(TestForgeEngine.Strike).Outcomes[0].Delta.Progress.Should().Be(Math.Min(plain, room));
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
            new MinigameOutcomeRule("a", 60, new MinigameMeterDelta()),
            new MinigameOutcomeRule("b", 30, new MinigameMeterDelta())
        };

        var act = () => new MinigameActionRule(info, 0, 0, outcomes);

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
                lines.Add($"{result.ActionId}:{result.Performed}:{result.Outcome?.Label}:{result.After}");
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
}
