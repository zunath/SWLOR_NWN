using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;
using static SWLOR.Game.Server.Tests.Service.Line.LineTestData;

namespace SWLOR.Game.Server.Tests.Service.Line;

/// <summary>Covers every Line action against every fish move, the forecast, the three endings and the shared engine contract.</summary>
public class LineEngineTests
{
    private static readonly string[] AllActions =
    {
        LineActionIds.SetHook, LineActionIds.Reel, LineActionIds.GiveLine, LineActionIds.Hold
    };

    [Test]
    public void ANewFight_StartsPartWayAlongTheLineWithNoTensionAndAFreshFish()
    {
        var engine = Engine(Setup(), FishMove.Run);

        engine.Type.Should().Be(CraftMinigameType.Line);
        engine.Status.Should().Be(MinigameStatus.InProgress);
        engine.Ending.Should().Be(LineEnding.InProgress);
        engine.Distance.Should().Be(16);
        engine.Tension.Should().Be(0);
        engine.TensionCap.Should().Be(20);
        engine.Stamina.Should().Be(16);
        engine.StaminaMax.Should().Be(16);
        engine.Move.Should().Be(FishMove.Run);
        engine.Round.Should().Be(0);
        engine.Turn.Should().Be(0);
        engine.NextOpportunity.Should().BeNull("the fish's move is the only thing the Line announces");
        engine.Actions.Select(action => action.Id).Should().Equal(AllActions);
    }

    [Test]
    public void TheSharedMetersHoldTheFightAndAreDescribedInTheLinesOwnWords()
    {
        var engine = Engine(Setup(), FishMove.Run);

        engine.Meters.Progress.Should().Be(new MinigameMeter(14, 30), "reeled in is the maximum Distance less the Distance");
        engine.Meters.Quality.Should().Be(new MinigameMeter(0, 16), "worn down is the maximum Stamina less the Stamina");
        engine.Meters.Integrity.Should().Be(new MinigameMeter(20, 20), "line strength is the Tension cap less the Tension");
        engine.Meters.Cp.Should().Be(new MinigameMeter(0, 0), "the Line has no CP");
        engine.DescribeMeters().Select(meter => (meter.Id, meter.Label)).Should().Equal(
            (MinigameMeterIds.Progress, "Reeled in"),
            (MinigameMeterIds.Quality, "Worn down"),
            (MinigameMeterIds.Integrity, "Line strength"));
    }

    [Test]
    public void TheFirstMoveIsRolledFromTheFishsPatternWhenNoneIsFixed()
    {
        var random = new ScriptedMinigameRandom(RollsThrash);

        var engine = new LineEngine(random, Setup());

        engine.Move.Should().Be(FishMove.Thrash);
        random.Calls.Should().Be(1);
        engine.Forecast.Should().Equal(FishMove.Thrash);
    }

    // Pull 4, reel 6, reel Tension 4, give-line 4 Distance and 6 relief, hold 2 Tension and 4 drain, set hook 2 Tension and 6 drain.
    [TestCase(LineActionIds.Reel, FishMove.Run, -6, 10, 0)]
    [TestCase(LineActionIds.Reel, FishMove.Dive, -6, 8, 0)]
    [TestCase(LineActionIds.Reel, FishMove.Thrash, -6, 10, 0)]
    [TestCase(LineActionIds.Reel, FishMove.Tire, -8, 4, -2)]
    [TestCase(LineActionIds.GiveLine, FishMove.Run, 8, -6, 0)]
    [TestCase(LineActionIds.GiveLine, FishMove.Dive, 4, -4, 0)]
    [TestCase(LineActionIds.GiveLine, FishMove.Thrash, 4, -3, 0)]
    [TestCase(LineActionIds.GiveLine, FishMove.Tire, 4, -6, -2)]
    [TestCase(LineActionIds.Hold, FishMove.Run, 2, 4, -4)]
    [TestCase(LineActionIds.Hold, FishMove.Dive, 0, 6, -4)]
    [TestCase(LineActionIds.Hold, FishMove.Thrash, 0, 2, -4)]
    [TestCase(LineActionIds.Hold, FishMove.Tire, 0, 2, -6)]
    [TestCase(LineActionIds.SetHook, FishMove.Run, 0, 2, -6)]
    [TestCase(LineActionIds.SetHook, FishMove.Dive, 0, 2, -6)]
    [TestCase(LineActionIds.SetHook, FishMove.Thrash, 0, 2, -6)]
    [TestCase(LineActionIds.SetHook, FishMove.Tire, 0, 2, -6)]
    public void EveryActionAgainstEveryMove_HasTheExactEffectThePlanDescribes(
        string actionId, FishMove move, int distance, int tension, int stamina)
    {
        LineEffects.Of(Setup(), actionId, move).Should().Be(new LineEffect(distance, tension, stamina));
    }

    [Test]
    public void Reel_IsAlotMoreStrainAgainstARun_AndPullsInExtraDistanceAgainstATire()
    {
        var setup = Setup();

        var run = LineEffects.Of(setup, LineActionIds.Reel, FishMove.Run);
        var dive = LineEffects.Of(setup, LineActionIds.Reel, FishMove.Dive);
        var tire = LineEffects.Of(setup, LineActionIds.Reel, FishMove.Tire);

        run.Tension.Should().BeGreaterThan(dive.Tension);
        run.Tension.Should().Be(setup.ReelTension + setup.Pull + LineRules.RunReelStrain);
        tire.Distance.Should().Be(-(setup.ReelDistance + LineRules.TireReelDistance));
        run.Distance.Should().Be(-setup.ReelDistance);
    }

    [Test]
    public void Hold_ShrugsOffAThrash_WhichStrainsEveryOtherAction()
    {
        var setup = Setup(pull: 3);

        LineEffects.Of(setup, LineActionIds.Hold, FishMove.Thrash).Tension.Should().Be(LineRules.HoldTension);
        LineEffects.Of(setup, LineActionIds.Reel, FishMove.Thrash).Tension.Should().Be(setup.ReelTension + 3 + LineRules.ThrashSpike);
        LineEffects.Of(setup, LineActionIds.GiveLine, FishMove.Thrash).Tension.Should().BeGreaterThan(-LineRules.GiveLineRelief);
    }

    [Test]
    public void GiveLine_LowersTensionAndLetsOutDistance_AndTheFishTakesMoreOnARun()
    {
        var setup = Setup();

        var calm = LineEffects.Of(setup, LineActionIds.GiveLine, FishMove.Tire);
        var run = LineEffects.Of(setup, LineActionIds.GiveLine, FishMove.Run);

        calm.Tension.Should().Be(-LineRules.GiveLineRelief);
        calm.Distance.Should().Be(setup.GiveLineDistance);
        run.Distance.Should().Be(setup.GiveLineDistance + setup.Pull);
    }

    [Test]
    public void APreviewShowsTheExactEffect_AndPerformingItChangesTheMetersByExactlyThat()
    {
        var engine = Engine(Setup(), FishMove.Run, FishMove.Tire);

        var preview = engine.Preview(LineActionIds.Reel);
        var before = engine.DescribeMeters();
        var result = engine.Perform(LineActionIds.Reel);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(0);
        preview.IntegrityCost.Should().Be(0);
        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].ChancePercent.Should().Be(100);
        preview.Outcomes[0].Label.Should().Be("Reel against a Run");
        result.Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        engine.Distance.Should().Be(10);
        engine.Tension.Should().Be(10);
        engine.Stamina.Should().Be(16);
        before.Zip(engine.DescribeMeters(), (b, a) => new MinigameMeterChange(a.Id, a.Label, b.Value, a.Value))
            .Should().BeEquivalentTo(result.Outcome.Changes);
    }

    [Test]
    public void EveryActionAndMove_PerformsExactlyWhatItsPreviewPromised()
    {
        foreach (var move in new[] { FishMove.Run, FishMove.Dive, FishMove.Thrash, FishMove.Tire })
        {
            foreach (var actionId in AllActions)
            {
                var engine = Engine(Setup(stamina: 24, cap: 60), move, FishMove.Tire);
                var promised = engine.Preview(actionId).Outcomes.Single();

                var result = engine.Perform(actionId);

                result.Performed.Should().BeTrue($"{actionId} against {move}");
                result.Outcome.Should().BeEquivalentTo(promised, $"{actionId} against {move}");
                engine.Meters.Should().Be(result.After);
            }
        }
    }

    [Test]
    public void SetHook_IsOnlyForTheFirstTurn_AndNeedsNoMovePassedToIt()
    {
        var engine = Engine(Setup(), FishMove.Tire, FishMove.Tire);
        engine.Preview(LineActionIds.SetHook).Enabled.Should().BeTrue();

        engine.Perform(LineActionIds.Hold).Performed.Should().BeTrue();

        var preview = engine.Preview(LineActionIds.SetHook);
        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Only on the first turn.");
        engine.Perform(LineActionIds.SetHook).Performed.Should().BeFalse();
        engine.Round.Should().Be(1);
    }

    [Test]
    public void SetHook_DrainsStaminaAndSpendsTheFishsFirstMoveOnTheStrike()
    {
        var engine = Engine(Setup(), FishMove.Thrash, FishMove.Run);

        var result = engine.Perform(LineActionIds.SetHook);

        result.Outcome.Label.Should().Be("Hook set");
        engine.Stamina.Should().Be(10);
        engine.Tension.Should().Be(2);
        engine.Distance.Should().Be(16, "the Thrash never landed");
        engine.Move.Should().Be(FishMove.Run, "the next move is announced");
    }

    [Test]
    public void ARefusedAction_ChangesNothingAndRollsNothing()
    {
        var random = new ScriptedMinigameRandom(RollsDive);
        var engine = new LineEngine(random, Setup(), FishMove.Run);
        engine.Perform(LineActionIds.Hold);
        var meters = engine.DescribeMeters();
        var calls = random.Calls;
        var turn = engine.Turn;

        var refused = engine.Perform(LineActionIds.SetHook);

        refused.Performed.Should().BeFalse();
        refused.RejectedReason.Should().Be("Only on the first turn.");
        refused.Outcome.Should().BeNull();
        engine.DescribeMeters().Should().Equal(meters);
        engine.Move.Should().Be(FishMove.Dive);
        engine.Turn.Should().Be(turn);
        random.Calls.Should().Be(calls);
    }

    [Test]
    public void TheForecast_IsRolledAfterTheResult_AndAPreviewNeverRollsIt()
    {
        var random = new ScriptedMinigameRandom(RollsDive);
        var engine = new LineEngine(random, Setup(), FishMove.Run);

        engine.PreviewAll();
        random.Calls.Should().Be(0);

        var result = engine.Perform(LineActionIds.Reel);

        random.Calls.Should().Be(1);
        engine.Move.Should().Be(FishMove.Dive);
        result.Outcome.Label.Should().Be("Reel against a Run", "the result answers the move announced before the click");
        result.Outcome.Change(MinigameMeterIds.Integrity).After.Should().Be(20 - 10);
    }

    [Test]
    public void TheForecast_AfterEachTurnFollowsThePatternOfThePreviousMove()
    {
        // A rare fish that has just run is most likely to run again; the roll below is inside the Run weight after a Run.
        var setup = Setup(rarity: FishRarity.Rare);
        var random = new ScriptedMinigameRandom(40);
        var engine = new LineEngine(random, setup, FishMove.Run);

        engine.Perform(LineActionIds.GiveLine);

        engine.Move.Should().Be(FishMove.Run);
    }

    [Test]
    public void AnExhaustedFish_OnlyRests_AndItsForecastRollsNothing()
    {
        var random = new ScriptedMinigameRandom();
        var engine = new LineEngine(random, Setup(stamina: 2), FishMove.Dive);

        engine.Perform(LineActionIds.Hold);

        engine.Stamina.Should().Be(0);
        engine.Move.Should().Be(FishMove.Tire);
        random.Calls.Should().Be(0);
        engine.Perform(LineActionIds.Hold).Performed.Should().BeTrue();
        engine.Move.Should().Be(FishMove.Tire);
    }

    [Test]
    public void SeeingTwoMovesAhead_ShowsBothAndShiftsTheSecondUpAfterEachTurn()
    {
        var engine = EngineSeeingTwo(Setup(effects: SeesTwoMoves), FishMove.Run, FishMove.Thrash, FishMove.Tire);

        engine.Forecast.Should().Equal(FishMove.Run, FishMove.Thrash);

        engine.Perform(LineActionIds.GiveLine);

        engine.Forecast.Should().Equal(FishMove.Thrash, FishMove.Tire);
    }

    [Test]
    public void SeeingOneMoveAhead_HidesTheFollowingMove()
    {
        var engine = Engine(Setup(), FishMove.Run, FishMove.Dive);

        engine.Forecast.Should().Equal(FishMove.Run);
        engine.Perform(LineActionIds.GiveLine);
        engine.Forecast.Should().Equal(FishMove.Dive);
    }

    [Test]
    public void ReelingInTheFish_LandsItAndSucceedsTheFight()
    {
        var engine = Engine(Setup(), FishMove.Tire, FishMove.Tire);

        engine.Perform(LineActionIds.Reel).Status.Should().Be(MinigameStatus.InProgress);
        var landing = engine.Preview(LineActionIds.Reel);
        landing.FinishesItem.Should().BeTrue();
        landing.Outcomes[0].FinishesItem.Should().BeTrue();
        LineActionText.Ending(landing.Outcomes[0]).Should().Be(LineActionText.LandsTheFish);

        var result = engine.Perform(LineActionIds.Reel);

        result.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Ending.Should().Be(LineEnding.Landed);
        engine.Distance.Should().Be(0);
    }

    [Test]
    public void TensionAtItsCap_SnapsTheLine_AndTheFishIsLost()
    {
        var engine = Engine(Setup(cap: 16), FishMove.Run, FishMove.Run);

        engine.Perform(LineActionIds.Reel).Status.Should().Be(MinigameStatus.InProgress);
        var snapping = engine.Preview(LineActionIds.Reel);
        snapping.CanBreakItem.Should().BeTrue();
        LineActionText.Ending(snapping.Outcomes[0]).Should().Be(LineActionText.SnapsTheLine);

        var result = engine.Perform(LineActionIds.Reel);

        result.Status.Should().Be(MinigameStatus.Failed);
        engine.Ending.Should().Be(LineEnding.Snapped);
        engine.Tension.Should().Be(16);
        engine.Distance.Should().BeGreaterThan(0);
    }

    [Test]
    public void DistanceAtItsMaximum_LetsTheFishEscape()
    {
        var engine = Engine(Setup(pull: 1), FishMove.Run, FishMove.Run, FishMove.Run);

        engine.Perform(LineActionIds.GiveLine);
        engine.Perform(LineActionIds.GiveLine);
        engine.Distance.Should().Be(26);
        var escaping = engine.Preview(LineActionIds.GiveLine);
        LineActionText.Ending(escaping.Outcomes[0]).Should().Be(LineActionText.FishEscapes);

        var result = engine.Perform(LineActionIds.GiveLine);

        result.Status.Should().Be(MinigameStatus.Failed);
        engine.Ending.Should().Be(LineEnding.Escaped);
        engine.Distance.Should().Be(30);
        engine.Tension.Should().Be(0);
    }

    [Test]
    public void ALandingReel_BeatsTheSnapItWouldCauseOnTheSameTurn()
    {
        var engine = Engine(Setup(cap: 10, start: 6, max: 16), FishMove.Run);

        var result = engine.Perform(LineActionIds.Reel);

        engine.Tension.Should().Be(10, "a Reel against a Run adds 10 Tension, the whole cap");
        result.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Ending.Should().Be(LineEnding.Landed);
    }

    [Test]
    public void AfterTheFightEnds_ActionsAreRefused_AndNoForecastIsRolled()
    {
        var random = new ScriptedMinigameRandom();
        var engine = new LineEngine(random, Setup(cap: 10, start: 6, max: 16), FishMove.Run);
        engine.Perform(LineActionIds.Reel).Status.Should().Be(MinigameStatus.Succeeded);
        random.Calls.Should().Be(0, "no forecast is rolled once the fight has ended");

        var refused = engine.Perform(LineActionIds.Hold);

        refused.Performed.Should().BeFalse();
        refused.RejectedReason.Should().Be("The attempt is over.");
        engine.Preview(LineActionIds.Hold).Enabled.Should().BeFalse();
    }

    [Test]
    public void AnUnknownAction_Throws()
    {
        var act = () => Engine(Setup(), FishMove.Run).Perform("cast");

        act.Should().Throw<ArgumentException>().WithMessage("*cast*");
    }

    [Test]
    public void WearingTheFishDown_ThenReelingAgainstItsRestsLandsItWithoutSnapping()
    {
        // Set Hook, then Hold twice drains 6 + 4 + 4 of the 14 Stamina, and the exhausted fish only rests.
        var engine = Engine(Setup(stamina: 14), FishMove.Run, FishMove.Thrash, FishMove.Dive);

        engine.Perform(LineActionIds.SetHook);
        engine.Perform(LineActionIds.Hold);
        engine.Perform(LineActionIds.Hold);
        engine.Stamina.Should().Be(0);
        engine.Move.Should().Be(FishMove.Tire);
        engine.Tension.Should().BeLessThan(engine.TensionCap);

        engine.Perform(LineActionIds.Reel);
        engine.Perform(LineActionIds.Reel);

        engine.Ending.Should().Be(LineEnding.Landed);
    }

    [Test]
    public void Fork_ContinuesFromTheExactFightAndEvolvesIndependently()
    {
        var original = Engine(Setup(), FishMove.Run, FishMove.Dive);
        original.Perform(LineActionIds.GiveLine);
        var fork = (LineEngine)original.Fork(new ScriptedMinigameRandom(RollsTire));

        fork.Distance.Should().Be(original.Distance);
        fork.Move.Should().Be(FishMove.Dive);

        fork.Perform(LineActionIds.Reel);

        fork.Move.Should().Be(FishMove.Tire);
        original.Move.Should().Be(FishMove.Dive);
        original.Round.Should().Be(1);
        fork.Round.Should().Be(2);
        original.Distance.Should().Be(24);
    }

    [Test]
    public void AcrossManySeeds_EveryMeterStaysInRange_AndEveryEndingMatchesTheStatus()
    {
        for (var seed = 1; seed <= 200; seed++)
        {
            var setup = LineSetup.ForFish(20 + seed % 15, FishRarity.Common, 25, 2);
            var engine = new LineEngine(new SeededMinigameRandom(seed), setup);
            var picker = new System.Random(seed);

            for (var turn = 0; turn < 60 && engine.Status == MinigameStatus.InProgress; turn++)
            {
                var actionId = AllActions[picker.Next(AllActions.Length)];
                var preview = engine.Preview(actionId);
                var result = engine.Perform(actionId);

                result.Performed.Should().Be(preview.Enabled);
                foreach (var meter in engine.DescribeMeters())
                    meter.Value.Should().BeInRange(meter.Min, meter.Max);
            }

            var expected = engine.Ending switch
            {
                LineEnding.InProgress => MinigameStatus.InProgress,
                LineEnding.Landed => MinigameStatus.Succeeded,
                _ => MinigameStatus.Failed
            };
            engine.Status.Should().Be(expected, $"seed {seed}");
        }
    }
}
