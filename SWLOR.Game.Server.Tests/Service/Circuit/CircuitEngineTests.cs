using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using static SWLOR.Game.Server.Tests.Service.Circuit.CircuitTestData;

namespace SWLOR.Game.Server.Tests.Service.Circuit;

/// <summary>
/// Covers every Circuit action, its preview and result, the overload branches, the Clean Signal opportunity, the unlock ranks and
/// the way an attempt ends. Pinned numbers come from <see cref="CircuitTestData.Setup"/>.
/// </summary>
public class CircuitEngineTests
{
    private static readonly string[] AllActions =
    {
        CircuitActionIds.Wire, CircuitActionIds.Tune, CircuitActionIds.Boost, CircuitActionIds.DischargeAssembly,
        CircuitActionIds.DischargeCalibration, CircuitActionIds.Ground, CircuitActionIds.Patch
    };

    // Discharge output by Load spent, 1 to 10, for Setup(): Assembly 89 and Calibration 1296.
    private static readonly int[] AssemblyBursts = { 3, 6, 10, 14, 19, 24, 30, 36, 43, 50 };
    private static readonly int[] CalibrationBursts = { 40, 88, 143, 205, 275, 351, 436, 527, 626, 732 };

    private static MinigameMeterView Meter(CircuitEngine engine, string id) => engine.DescribeMeters().Single(m => m.Id == id);

    private static List<MinigameMeterChange> ChangesBetween(
        IReadOnlyList<MinigameMeterView> before,
        IReadOnlyList<MinigameMeterView> after) =>
        before.Zip(after, (b, a) => new MinigameMeterChange(a.Id, a.Label, b.Value, a.Value)).ToList();

    // ---- setup and meters -------------------------------------------------------------------------------------------

    [Test]
    public void Meters_StartAtTheRecipeSizesWithTheOverloadLineMarkedOnLoad()
    {
        var engine = Engine(Setup());

        var meters = engine.DescribeMeters();

        meters.Select(meter => meter.Id).Should().Equal(
            MinigameMeterIds.Progress, MinigameMeterIds.Quality, MinigameMeterIds.Integrity, MinigameMeterIds.Cp, CircuitEngine.LoadId);
        meters.Select(meter => meter.Label).Should().Equal("Assembly", "Calibration", "Integrity", "CP", "Load");
        meters.Select(meter => (meter.Value, meter.Max)).Should().Equal((0, 89), (0, 1296), (70, 70), (69, 69), (0, 10));
        var load = Meter(engine, CircuitEngine.LoadId);
        load.Mark.Should().Be(MinigameMeterMark.Line(6, "Overload line"));
        load.Mark!.IsLine.Should().BeTrue();
        engine.Type.Should().Be(CraftMinigameType.Circuit);
        engine.Status.Should().Be(MinigameStatus.InProgress);
        engine.NextOpportunity.Should().BeNull();
    }

    [Test]
    public void TheAssemblyShareMovesWorkBetweenTheMeters_AndEnhancementsAddAssemblyWithoutSlowingWire()
    {
        var shared = Setup(share: 60);
        shared.MaxAssembly.Should().Be(107, "89 x 60 / 50");
        shared.MaxCalibration.Should().Be(1037, "1296 x 40 / 50");

        var enhanced = Setup(penalty: 15);
        enhanced.MaxAssembly.Should().Be(104);
        enhanced.AssemblySize.Should().Be(89);
        enhanced.WireGain(0).Should().Be(Setup().WireGain(0), "enhancements add to what is needed, not to what a Wire does");
    }

    [TestCase(24, 38)]
    [TestCase(25, 69)]
    [TestCase(50, 20 + 37 + 31)]
    [TestCase(0, 20)]
    public void MaxCp_IsGearPlusThreeQuartersOfTheRank_PlusTheRankTwentyFivePassive(int rank, int expected)
    {
        Setup(rank: rank).MaxCp.Should().Be(expected);
    }

    [Test]
    public void ARecipeWithoutParametersPlaysWithTheFallbackValues()
    {
        var setup = new CircuitSetup(null, 10, 45, 629, 60, 10, 0, 0, 0);

        setup.Parameters.Should().BeSameAs(CircuitParameters.Fallback);
        setup.OverloadLine.Should().Be(6);
    }

    [TestCase(22, 7)]
    [TestCase(25, 8)]
    [TestCase(30, 11)]
    [TestCase(45, 11)]
    public void RankAgainstTheRecipe_PenalizesBelowAndRewardsAboveForAssembly_UpToFiveRanks(int rank, int wire)
    {
        Setup(rank: rank).WireGain(0).Should().Be(wire);
    }

    [TestCase(22, 68)]
    [TestCase(25, 81)]
    [TestCase(45, 81)]
    public void RankAgainstTheRecipe_PenalizesCalibrationBelowButNeverRewardsAbove(int rank, int tune)
    {
        Setup(rank: rank).TuneGain(0).Should().Be(tune);
    }

    // ---- Wire --------------------------------------------------------------------------------------------------------

    [Test]
    public void Wire_BuildsAssemblyAddsOneLoadAndCostsIntegrity_AndTheResultMatchesThePreview()
    {
        var engine = Engine(Setup(), rolls: Rolls());

        var preview = engine.Preview(CircuitActionIds.Wire);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(0);
        preview.IntegrityCost.Should().Be(3);
        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].ChancePercent.Should().Be(100);
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Progress: 8, Integrity: -3));
        preview.Outcomes[0].Change(CircuitEngine.LoadId)!.Delta.Should().Be(1);
        preview.FinishesItem.Should().BeFalse();

        var result = engine.Perform(CircuitActionIds.Wire);

        result.Performed.Should().BeTrue();
        result.Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        engine.Meters.Progress.Value.Should().Be(8);
        engine.Meters.Integrity.Value.Should().Be(67);
        engine.Load.Should().Be(1);
    }

    [Test]
    public void EveryLoadPointAmplifiesWire_ByTheRecipesBoost()
    {
        Engine(Setup(), 5).Preview(CircuitActionIds.Wire).Outcomes[0].Delta.Progress.Should().Be(13, "8 at Load 0, 50% more at Load 5");
        Engine(Setup(boost: 14, line: 9), 5).Preview(CircuitActionIds.Wire).Outcomes[0].Delta.Progress.Should().Be(14);
        Engine(Setup(boost: 6, line: 9), 5).Preview(CircuitActionIds.Wire).Outcomes[0].Delta.Progress.Should().Be(11);
    }

    // ---- Tune and Clean Signal ----------------------------------------------------------------------------------------

    [Test]
    public void Tune_BuildsCalibrationAddsOneLoadAndSpendsCp()
    {
        var engine = Engine(Setup(), rolls: Rolls());

        var preview = engine.Preview(CircuitActionIds.Tune);

        preview.CpCost.Should().Be(3);
        preview.IntegrityCost.Should().Be(3);
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Quality: 81, Integrity: -3, Cp: -3));
        preview.Outcomes[0].Change(CircuitEngine.LoadId)!.Delta.Should().Be(1);

        engine.Perform(CircuitActionIds.Tune).Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        engine.Meters.Quality.Value.Should().Be(81);
        engine.Meters.Cp.Value.Should().Be(66);
    }

    [Test]
    public void ACleanSignalIsAnnouncedBeforeItAppliesAndDoublesTheNextTune()
    {
        var engine = Engine(Setup(), rolls: Rolls(forecastRoll: NoSignal).Concat(Rolls(forecastRoll: Signal)).ToArray());

        engine.Preview(CircuitActionIds.Tune).Outcomes[0].Delta.Quality.Should().Be(81);
        engine.Perform(CircuitActionIds.Wire);
        engine.NextOpportunity.Should().BeNull();
        engine.Perform(CircuitActionIds.Wire);

        engine.NextOpportunity.Should().NotBeNull();
        engine.NextOpportunity!.Id.Should().Be(CircuitActionIds.CleanSignal);
        engine.NextOpportunity.Name.Should().Be("Clean Signal");
        engine.NextOpportunity.TurnsAway.Should().Be(1);

        var preview = engine.Preview(CircuitActionIds.Tune);
        preview.Outcomes[0].Label.Should().Contain("Clean Signal");
        preview.Outcomes[0].Delta.Quality.Should().Be(2 * Setup().TuneGain(2), "the doubled gain at the Load then");
    }

    [Test]
    public void ACleanSignalDoublesOneTuneThenIsGone()
    {
        var engine = Engine(Setup(), rolls: Rolls(forecastRoll: Signal).Concat(Rolls()).ToArray());
        engine.Perform(CircuitActionIds.Wire);
        var promised = engine.Preview(CircuitActionIds.Tune).Outcomes[0];

        var result = engine.Perform(CircuitActionIds.Tune);

        result.Outcome.Should().BeEquivalentTo(promised);
        result.Outcome.Delta.Quality.Should().Be(2 * Setup().TuneGain(1));
        engine.NextOpportunity.Should().BeNull("a signal applies to the next action only");
        engine.Preview(CircuitActionIds.Tune).Outcomes[0].Delta.Quality.Should().Be(Setup().TuneGain(2));
    }

    [Test]
    public void ACleanSignalPassesIfTheNextActionIsNotATune_AndNeverHelpsAnotherAction()
    {
        var engine = Engine(Setup(), rolls: Rolls(forecastRoll: Signal).Concat(Rolls()).ToArray());
        engine.Perform(CircuitActionIds.Wire);
        var wire = engine.Preview(CircuitActionIds.Wire).Outcomes[0];

        engine.Perform(CircuitActionIds.Wire).Outcome.Should().BeEquivalentTo(wire);

        engine.NextOpportunity.Should().BeNull();
    }

    [Test]
    public void ACleanSignalCanBeAnnouncedAtRankZero_BecauseTuneIsOpenFromTheStart()
    {
        var random = new ScriptedMinigameRandom(NoSignal, Signal);
        var engine = new CircuitEngine(random, Setup(rank: 0, level: 1));

        engine.Perform(CircuitActionIds.Wire);
        engine.NextOpportunity.Should().BeNull();
        engine.Perform(CircuitActionIds.Wire);

        engine.NextOpportunity!.Id.Should().Be(CircuitActionIds.CleanSignal);
        random.Calls.Should().Be(2);
    }

    [Test]
    public void Previews_SpendNoChance_AndNeverIncludeTheForecast()
    {
        var random = new ScriptedMinigameRandom();
        var engine = new CircuitEngine(random, Setup(), 7);

        var previews = engine.PreviewAll();

        random.Calls.Should().Be(0);
        previews.Should().HaveCount(7);
        previews.SelectMany(preview => preview.Outcomes).Should().OnlyContain(outcome => outcome.Changes.Count == 5);
        engine.NextOpportunity.Should().BeNull();
    }

    // ---- Boost ----------------------------------------------------------------------------------------------------------

    [Test]
    public void Boost_AddsThreeLoadForCp_AndStopsAtTheMaximum()
    {
        var engine = Engine(Setup(), rolls: Rolls());

        var preview = engine.Preview(CircuitActionIds.Boost);

        preview.CpCost.Should().Be(4);
        preview.IntegrityCost.Should().Be(3);
        preview.Outcomes.Should().ContainSingle("Load 3 is below the line");
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Integrity: -3, Cp: -4));
        preview.Outcomes[0].Change(CircuitEngine.LoadId)!.Delta.Should().Be(3);
        engine.Perform(CircuitActionIds.Boost);
        engine.Load.Should().Be(3);

        var full = Engine(Setup(), 10).Preview(CircuitActionIds.Boost);
        full.Enabled.Should().BeFalse();
        full.DisabledReason.Should().Be("Load is already at its maximum.");
        Engine(Setup(), 8).Preview(CircuitActionIds.Boost).Outcomes.Last().Change(CircuitEngine.LoadId)!.After
            .Should().Be(0, "an overload from Load 8 leaves 0 Load");
    }

    // ---- Discharge ---------------------------------------------------------------------------------------------------

    [Test]
    public void Discharge_SpendsAllLoadAsABurstThatGrowsFasterThanTheLoad()
    {
        for (var load = 1; load <= 10; load++)
        {
            var engine = Engine(Setup(), load);
            var assembly = engine.Preview(CircuitActionIds.DischargeAssembly).Outcomes.Single();
            var calibration = engine.Preview(CircuitActionIds.DischargeCalibration).Outcomes.Single();

            assembly.Delta.Progress.Should().Be(AssemblyBursts[load - 1], $"Load {load}");
            assembly.Delta.Quality.Should().Be(0);
            calibration.Delta.Quality.Should().Be(CalibrationBursts[load - 1], $"Load {load}");
            calibration.Delta.Progress.Should().Be(0);
            assembly.Change(CircuitEngine.LoadId)!.After.Should().Be(0);
            calibration.Delta.Integrity.Should().Be(-4);
            calibration.Delta.Cp.Should().Be(0);
        }

        // 10 Load is more than twice the burst of 5 Load: the output per point rises with Load.
        CalibrationBursts[9].Should().BeGreaterThan(2 * CalibrationBursts[4]);
    }

    [Test]
    public void Discharge_NeverOverloadsEvenFarAboveTheLine_AndEmptiesLoad()
    {
        var engine = Engine(Setup(line: 4), 10, Rolls());

        var preview = engine.Preview(CircuitActionIds.DischargeCalibration);

        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].ChancePercent.Should().Be(100);
        engine.Perform(CircuitActionIds.DischargeCalibration);
        engine.Load.Should().Be(0);
        engine.Meters.Quality.Value.Should().Be(732);
    }

    [TestCase(CircuitActionIds.DischargeAssembly)]
    [TestCase(CircuitActionIds.DischargeCalibration)]
    public void Discharge_NeedsLoad(string actionId)
    {
        var random = new ScriptedMinigameRandom();
        var engine = new CircuitEngine(random, Setup());

        var preview = engine.Preview(actionId);
        var result = engine.Perform(actionId);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("There is no Load to discharge.");
        result.Performed.Should().BeFalse();
        random.Calls.Should().Be(0);
        engine.Turn.Should().Be(0);
    }

    // ---- Ground ------------------------------------------------------------------------------------------------------

    [Test]
    public void Ground_SafelyLowersLoadByThreeForCp_NeverOverloading()
    {
        var engine = Engine(Setup(line: 3), 10, Rolls());

        var preview = engine.Preview(CircuitActionIds.Ground);

        preview.CpCost.Should().Be(3);
        preview.IntegrityCost.Should().Be(2);
        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Integrity: -2, Cp: -3));
        preview.Outcomes[0].Change(CircuitEngine.LoadId)!.Delta.Should().Be(-3);
        engine.Perform(CircuitActionIds.Ground);
        engine.Load.Should().Be(7);
    }

    [Test]
    public void Ground_NeverGoesBelowZero_AndNeedsLoad()
    {
        Engine(Setup(), 2).Preview(CircuitActionIds.Ground).Outcomes[0].Change(CircuitEngine.LoadId)!.After.Should().Be(0);

        var empty = Engine(Setup()).Preview(CircuitActionIds.Ground);
        empty.Enabled.Should().BeFalse();
        empty.DisabledReason.Should().Be("There is no Load to ground.");
    }

    // ---- Patch -------------------------------------------------------------------------------------------------------

    [Test]
    public void Patch_RestoresIntegrityForCp_OnlyWhenSomeIsMissing()
    {
        var full = Engine(Setup()).Preview(CircuitActionIds.Patch);
        full.Enabled.Should().BeFalse();
        full.DisabledReason.Should().Be("Integrity is already full.");

        var engine = Engine(Setup(), rolls: Rolls().Concat(Rolls()).ToArray());
        engine.Perform(CircuitActionIds.Wire);
        var preview = engine.Preview(CircuitActionIds.Patch);

        preview.CpCost.Should().Be(8);
        preview.IntegrityCost.Should().Be(0);
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Integrity: 3, Cp: -8), "67 + 20 stops at the maximum of 70");
        engine.Perform(CircuitActionIds.Patch).Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        engine.Meters.Integrity.Value.Should().Be(70);
    }

    [Test]
    public void Patch_CanOverloadWhileLoadIsAboveTheLine_BecauseItDoesNotLowerLoad()
    {
        var engine = Engine(Setup(), 8, Rolls(Holds));
        engine.Perform(CircuitActionIds.Wire);
        engine.Load.Should().Be(9);

        var preview = engine.Preview(CircuitActionIds.Patch);

        preview.Outcomes.Select(outcome => outcome.ChancePercent).Should().Equal(76, 24);
    }

    // ---- overload ----------------------------------------------------------------------------------------------------

    [TestCase(5, 0)]
    [TestCase(6, 8)]
    [TestCase(7, 16)]
    [TestCase(8, 24)]
    [TestCase(9, 32)]
    [TestCase(10, 32)]
    public void OverloadChance_RisesByEightPercentForEveryPointTheActionLeavesAboveTheLine(int loadBefore, int expected)
    {
        var preview = Engine(Setup(), loadBefore).Preview(CircuitActionIds.Wire);

        if (expected == 0)
        {
            preview.Outcomes.Should().ContainSingle();
            return;
        }

        preview.Outcomes.Select(outcome => outcome.Label).Should().Equal("Wired", CircuitEngine.OverloadLabel);
        preview.Outcomes.Select(outcome => outcome.ChancePercent).Should().Equal(100 - expected, expected);
    }

    [Test]
    public void OverloadChance_IsCappedAndZeroAtOrBelowTheLine()
    {
        var setup = new CircuitSetup(new CircuitParameters(-2, 10, 50), 25, 89, 1296, 70, 25, 0, 0, 0);

        setup.OverloadChancePercent(10).Should().Be(CircuitRules.MaxOverloadChancePercent);
        Setup().OverloadChancePercent(6).Should().Be(0);
        Setup().OverloadChancePercent(0).Should().Be(0);
    }

    [Test]
    public void AnOverload_StillResolvesTheAction_ThenCostsAThirdOfIntegrityAndEmptiesLoad()
    {
        var engine = Engine(Setup(), 6, Rolls(Overloads));

        var preview = engine.Preview(CircuitActionIds.Wire);

        preview.Outcomes.Should().HaveCount(2);
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Progress: 14, Integrity: -3));
        preview.Outcomes[0].Change(CircuitEngine.LoadId)!.After.Should().Be(7);
        preview.Outcomes[1].Delta.Should().Be(new MinigameMeterDelta(Progress: 14, Integrity: -24), "3 for the action and 30% of 70 for the overload");
        preview.Outcomes[1].Change(CircuitEngine.LoadId)!.After.Should().Be(0);

        var result = engine.Perform(CircuitActionIds.Wire);

        result.Outcome.Label.Should().Be(CircuitEngine.OverloadLabel);
        result.Outcome.Should().BeEquivalentTo(preview.Outcomes[1]);
        engine.Meters.Progress.Value.Should().Be(14);
        engine.Meters.Integrity.Value.Should().Be(46);
        engine.Load.Should().Be(0);
        engine.Status.Should().Be(MinigameStatus.InProgress);
    }

    [Test]
    public void WhenTheActionHolds_NoIntegrityIsLostBeyondItsCost()
    {
        var engine = Engine(Setup(), 6, Rolls(Holds));

        var result = engine.Perform(CircuitActionIds.Wire);

        result.Outcome.Label.Should().Be("Wired");
        engine.Meters.Integrity.Value.Should().Be(67);
        engine.Load.Should().Be(7);
    }

    [Test]
    public void TheOverloadBranchOfTune_ResolvesTheTuneInFull()
    {
        var engine = Engine(Setup(), 8, Rolls(Overloads));
        var preview = engine.Preview(CircuitActionIds.Tune);

        engine.Perform(CircuitActionIds.Tune).Outcome.Should().BeEquivalentTo(preview.Outcomes[1]);

        engine.Meters.Quality.Value.Should().Be(Setup().TuneGain(8), "Tune still resolves at the Load it was used at");
        engine.Load.Should().Be(0);
        engine.Meters.Cp.Value.Should().Be(66);
    }

    // ---- ending the attempt ------------------------------------------------------------------------------------------

    [Test]
    public void AnActionThatFillsAssemblyFinishesTheItem_OnBothBranches_EvenWhenItWouldOverload()
    {
        var setup = Setup(baseProgress: 10, craftsmanship: 1000);
        var engine = Engine(setup, 6, Rolls(Overloads));

        var preview = engine.Preview(CircuitActionIds.Wire);

        preview.FinishesItem.Should().BeTrue();
        preview.Outcomes.Should().OnlyContain(outcome => outcome.FinishesItem);
        var result = engine.Perform(CircuitActionIds.Wire);
        result.Status.Should().Be(MinigameStatus.Succeeded);
        result.Outcome.Label.Should().Be(CircuitEngine.OverloadLabel);
        engine.Meters.Progress.IsFull.Should().BeTrue();
        engine.Meters.Integrity.Value.Should().BeGreaterThan(0);
    }

    [Test]
    public void AnOverloadThatEmptiesIntegrityBreaksThePiece_AndThePreviewWarnsOfIt()
    {
        var engine = Engine(Setup(integrity: 4), 6, Rolls(Overloads));

        var preview = engine.Preview(CircuitActionIds.Wire);

        preview.CanBreakItem.Should().BeTrue();
        preview.Outcomes[0].BreaksItem.Should().BeFalse();
        preview.Outcomes[1].BreaksItem.Should().BeTrue();
        engine.Perform(CircuitActionIds.Wire).Status.Should().Be(MinigameStatus.Failed);
        engine.Meters.Integrity.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void AFinishedAttemptRefusesEveryAction_WithoutSpendingChance()
    {
        var setup = Setup(baseProgress: 10, craftsmanship: 1000);
        var random = new ScriptedMinigameRandom();
        var engine = new CircuitEngine(random, setup, 5);
        engine.Perform(CircuitActionIds.Wire).Status.Should().Be(MinigameStatus.Succeeded);
        var callsBefore = random.Calls;

        foreach (var action in AllActions)
        {
            var preview = engine.Preview(action);
            preview.Enabled.Should().BeFalse(action);
            preview.DisabledReason.Should().Be("The attempt is over.");
            engine.Perform(action).Performed.Should().BeFalse(action);
        }

        random.Calls.Should().Be(callsBefore);
    }

    [Test]
    public void TheQualityMeterIsTheCalibration_AsAPercentageOfItsSize()
    {
        var engine = Engine(Setup(), 8, Rolls());

        engine.Perform(CircuitActionIds.DischargeCalibration);

        engine.Meters.Quality.Value.Should().Be(527);
        engine.Meters.QualityPercent.Should().Be(40, "527 of 1296");
    }

    // ---- unlock ranks and CP -----------------------------------------------------------------------------------------

    [TestCase(CircuitActionIds.Wire, 0)]
    [TestCase(CircuitActionIds.DischargeAssembly, 0)]
    [TestCase(CircuitActionIds.Tune, 0)]
    [TestCase(CircuitActionIds.DischargeCalibration, 0)]
    [TestCase(CircuitActionIds.Patch, 10)]
    [TestCase(CircuitActionIds.Ground, 0)]
    [TestCase(CircuitActionIds.Boost, 0)]
    public void EveryActionUnlocksAtItsRank(string actionId, int unlockRank)
    {
        CircuitSetup.UnlockRank(actionId).Should().Be(unlockRank);

        if (unlockRank > 0)
        {
            var locked = Engine(Setup(rank: unlockRank - 1), 5).Preview(actionId);
            locked.Enabled.Should().BeFalse();
            locked.DisabledReason.Should().Be($"Unlocks at Engineering rank {unlockRank}.");
        }

        // At the rank, give the action what it needs: Load to spend or ground, and missing Integrity to patch.
        var setup = Setup(rank: unlockRank, level: unlockRank, gearCp: 50);
        var engine = Engine(setup, 5);
        if (actionId == CircuitActionIds.Patch)
        {
            engine = new CircuitEngine(new ScriptedMinigameRandom(Rolls().Concat(Rolls()).ToArray()), setup, 5);
            engine.Perform(CircuitActionIds.Wire);
        }

        engine.Preview(actionId).Enabled.Should().BeTrue(engine.Preview(actionId).DisabledReason);
    }

    [Test]
    public void ALockedActionIsRefused_ChangingNothing()
    {
        var random = new ScriptedMinigameRandom();
        var engine = new CircuitEngine(random, Setup(rank: 9, level: 9), 5);
        var before = engine.DescribeMeters();

        var result = engine.Perform(CircuitActionIds.Patch);

        result.Performed.Should().BeFalse();
        result.RejectedReason.Should().Be("Unlocks at Engineering rank 10.");
        engine.DescribeMeters().Should().Equal(before);
        random.Calls.Should().Be(0);
        engine.Turn.Should().Be(0);
    }

    [Test]
    public void AnActionTheCrafterCannotPayFor_IsDisabled()
    {
        var engine = Engine(Setup(rank: 5, level: 5, gearCp: 0), rolls: Rolls().Concat(Rolls()).ToArray());

        engine.Meters.Cp.Value.Should().Be(3);
        engine.Perform(CircuitActionIds.Tune).Performed.Should().BeTrue();
        var preview = engine.Preview(CircuitActionIds.Tune);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Not enough CP.");
        engine.Perform(CircuitActionIds.Tune).Performed.Should().BeFalse();
    }

    [Test]
    public void AnUnknownActionIsAnError()
    {
        var engine = Engine(Setup());

        var act = () => engine.Preview("overdrive");

        act.Should().Throw<ArgumentException>();
        ((Action)(() => new CircuitEngine(new ScriptedMinigameRandom(), null))).Should().Throw<ArgumentNullException>();
    }

    // ---- the engine contract -----------------------------------------------------------------------------------------

    [Test]
    public void Perform_AcrossManySeeds_AlwaysLandsOnAPreviewedOutcomeForEveryMeter()
    {
        var overloads = 0;
        var signals = 0;
        for (var seed = 1; seed <= 150; seed++)
        {
            var engine = new CircuitEngine(new SeededMinigameRandom(seed), Setup(line: 4));
            var picker = new System.Random(seed);

            for (var turn = 0; turn < 60 && engine.Status == MinigameStatus.InProgress; turn++)
            {
                var actionId = AllActions[picker.Next(AllActions.Length)];
                var preview = engine.Preview(actionId);
                var before = engine.DescribeMeters();
                var signalBefore = engine.NextOpportunity != null;

                var result = engine.Perform(actionId);

                if (!preview.Enabled)
                {
                    result.Performed.Should().BeFalse();
                    engine.DescribeMeters().Should().Equal(before);
                    continue;
                }

                preview.Outcomes.Should().ContainEquivalentOf(result.Outcome, $"seed {seed} turn {turn}");
                preview.Outcomes.Sum(outcome => outcome.ChancePercent).Should().Be(100);
                ChangesBetween(before, engine.DescribeMeters()).Should().BeEquivalentTo(result.Outcome.Changes);
                engine.Status.Should().Be(result.Outcome.ResultingStatus);
                foreach (var meter in engine.DescribeMeters())
                    meter.Value.Should().BeInRange(meter.Min, meter.Max);

                overloads += result.Outcome.Label == CircuitEngine.OverloadLabel ? 1 : 0;
                signals += signalBefore ? 1 : 0;
            }
        }

        overloads.Should().BeGreaterThan(0, "the walk reaches the overload branch");
        signals.Should().BeGreaterThan(0, "the walk meets a Clean Signal");
    }

    [Test]
    public void Perform_RollsTheOutcomeBeforeTheForecast()
    {
        var random = new ScriptedMinigameRandom(Overloads, Signal);
        var engine = new CircuitEngine(random, Setup(), 6);

        var result = engine.Perform(CircuitActionIds.Wire);

        result.Outcome.Label.Should().Be(CircuitEngine.OverloadLabel);
        random.Calls.Should().Be(2);
        engine.NextOpportunity.Should().NotBeNull();
    }

    // ---- a whole craft -----------------------------------------------------------------------------------------------

    [TestCase(10, 10, 12, 13, 20)]
    [TestCase(25, 25, 12, 13, 20)]
    [TestCase(25, 25, 30, 29, 37)]
    [TestCase(50, 50, 30, 29, 37)]
    [TestCase(50, 50, 0, 0, 0)]
    public void AtLevelCraft_FinishesInTheTargetLength_WithGoodQuality_WhenPlayedSafely(
        int rank, int level, int craftsmanship, int control, int gearCp)
    {
        var tier = CircuitComponentGrades.TierOfLevel(level);
        var setup = Setup(
            rank: rank, level: level, craftsmanship: craftsmanship, control: control, gearCp: gearCp,
            line: CircuitParameterProfile.LineForGrade(tier));

        var lengths = new List<int>();
        var qualities = new List<int>();
        for (var seed = 1; seed <= 25; seed++)
        {
            var (status, actions, quality, _) = PlaySafely(setup, seed);

            status.Should().Be(MinigameStatus.Succeeded, $"seed {seed}");
            actions.Should().BeInRange(10, 24, $"seed {seed}");
            quality.Should().BeGreaterThanOrEqualTo(60, $"seed {seed}");
            lengths.Add(actions);
            qualities.Add(quality);
        }

        TestContext.Out.WriteLine(
            $"rank {rank}, gear {craftsmanship}/{control}/{gearCp}: actions {lengths.Average():F1} ({lengths.Min()}-{lengths.Max()}), " +
            $"quality {qualities.Average():F0}% ({qualities.Min()}-{qualities.Max()})");
    }

    [Test]
    public void WiringAloneFinishesButWithNoQuality_SoQualityNeedsTheOtherActions()
    {
        var engine = new CircuitEngine(new SeededMinigameRandom(7), Setup(line: 9));

        var actions = 0;
        while (engine.Status == MinigameStatus.InProgress && actions < 60)
        {
            engine.Perform(CircuitActionIds.Wire);
            actions++;
        }

        engine.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Meters.QualityPercent.Should().Be(0);
        actions.Should().BeInRange(6, 14);
    }
}
