using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using static SWLOR.Game.Server.Tests.Service.Circuit.CircuitTestData;

namespace SWLOR.Game.Server.Tests.Service.Circuit;

/// <summary>
/// Covers what the Overclocker and Technician perks change in the Circuit: every stat the engine reads, the two added actions,
/// Surge, Failsafe and Diagnostics, and that previews still equal results. Pinned numbers come from <see cref="CircuitTestData.Setup"/>.
/// </summary>
public class CircuitPerkEngineTests
{
    /// <summary>Every stat at the value the best purchases of both lines grant together.</summary>
    private static readonly CircuitPerkStats EveryPerk = new()
    {
        LoadBoostPercent = 6,
        DischargePowerPercent = 10,
        OverloadLine = 2,
        ExtraLoadPerBuildAction = 1,
        CascadeDischarge = true,
        BoostLoad = 1,
        BoostCpReduction = 1,
        OverloadIntegrityReductionPercent = 33,
        Surge = true,
        ActionIntegrityReduction = 2,
        TunePowerPercent = 25,
        ForecastTurns = 1,
        OverloadGrace = 2,
        MicroTune = true,
        SignalChancePercent = 10,
        PatchRestore = 10,
        PatchCpReduction = 2,
        Failsafe = true
    };

    private static CircuitEngine Perked(CircuitPerkStats perks, int startingLoad = 0, params int[] rolls) =>
        Engine(Setup(perks: perks), startingLoad, rolls);

    private static MinigameOutcomePreview Only(CircuitEngine engine, string actionId) => engine.Preview(actionId).Outcomes.Single();

    // ---- no perks --------------------------------------------------------------------------------------------------------

    [Test]
    public void WithoutPerks_TheActionsAndEveryPreviewAreTheOriginalSeven()
    {
        var plain = Engine(Setup(), 6);
        var none = Engine(Setup(perks: CircuitPerkStats.None), 6);

        plain.Actions.Select(action => action.Id).Should().Equal(
            CircuitActionIds.Wire, CircuitActionIds.Tune, CircuitActionIds.Boost, CircuitActionIds.DischargeAssembly,
            CircuitActionIds.DischargeCalibration, CircuitActionIds.Ground, CircuitActionIds.Patch);
        none.PreviewAll().Should().BeEquivalentTo(plain.PreviewAll());
        plain.SurgeReady.Should().BeFalse();
        plain.FailsafeReady.Should().BeFalse();
    }

    [Test]
    public void WithoutDiagnostics_TheForecastRollsOnceAndNeverAnnouncesTwoAhead()
    {
        var random = new ScriptedMinigameRandom(Signal, Signal);
        var engine = new CircuitEngine(random, Setup());

        engine.Perform(CircuitActionIds.Wire);
        engine.Perform(CircuitActionIds.Wire);

        random.Calls.Should().Be(2);
        engine.LaterOpportunity.Should().BeNull();
        engine.NextOpportunity!.TurnsAway.Should().Be(1);
    }

    // ---- stats the Overclocker line grants -------------------------------------------------------------------------------

    [Test]
    public void LoadBoost_AddsToTheRecipesBoostPerPoint_AndNeverGoesBelowZero()
    {
        Setup(perks: new CircuitPerkStats { LoadBoostPercent = 3 }).LoadBoostPercent.Should().Be(13);
        Perked(new CircuitPerkStats { LoadBoostPercent = 3 }, 6).Preview(CircuitActionIds.Wire).Outcomes[0].Delta.Progress
            .Should().Be(15, "8 at Load 0 becomes 15 at Load 6 with a 13% boost");
        Perked(new CircuitPerkStats { LoadBoostPercent = -3 }, 6).Preview(CircuitActionIds.Wire).Outcomes[0].Delta.Progress
            .Should().Be(12, "a 7% boost");

        var drained = Setup(boost: 6, perks: new CircuitPerkStats { LoadBoostPercent = -10 });
        drained.LoadBoostPercent.Should().Be(0);
        drained.LoadMultiplier(9).Should().Be(1.0);
    }

    [Test]
    public void CapacitorBank_MakesEveryDischargeBurstLarger()
    {
        var perks = new CircuitPerkStats { DischargePowerPercent = 10 };

        Only(Perked(perks, 9), CircuitActionIds.DischargeAssembly).Delta.Progress.Should().Be(47, "43 plain");
        Only(Perked(perks, 9), CircuitActionIds.DischargeCalibration).Delta.Quality.Should().Be(689, "626 plain");
    }

    [Test]
    public void HeatSinks_RaiseTheLineTheMeterMarksAndTheOverloadChanceUses_UpToTheHighestLoad()
    {
        var perks = new CircuitPerkStats { OverloadLine = 2 };
        var setup = Setup(perks: perks);

        setup.OverloadLine.Should().Be(8);
        Perked(perks).DescribeMeters().Single(meter => meter.Id == CircuitEngine.LoadId).Mark.Should().Be(MinigameMeterMark.Line(8, "Overload line"));
        Perked(perks, 7).Preview(CircuitActionIds.Wire).Outcomes.Should().ContainSingle("Load 8 is on the raised line");
        Perked(perks, 8).Preview(CircuitActionIds.Wire).Outcomes[1].ChancePercent.Should().Be(8);

        Setup(line: 9, perks: perks).OverloadLine.Should().Be(10, "the line never passes the highest Load");
        Engine(Setup(line: 9, perks: perks), 9).Preview(CircuitActionIds.Wire).Outcomes.Should().ContainSingle();
    }

    [Test]
    public void ExtraLoadPerBuildAction_RaisesWireAndTuneButNotBoostOrAnythingElse()
    {
        var engine = Perked(new CircuitPerkStats { ExtraLoadPerBuildAction = 1 });

        engine.Preview(CircuitActionIds.Wire).Outcomes[0].Change(CircuitEngine.LoadId)!.Delta.Should().Be(2);
        engine.Preview(CircuitActionIds.Tune).Outcomes[0].Change(CircuitEngine.LoadId)!.Delta.Should().Be(2);
        engine.Preview(CircuitActionIds.Boost).Outcomes[0].Change(CircuitEngine.LoadId)!.Delta.Should().Be(3);
        engine.Actions.Single(action => action.Id == CircuitActionIds.Wire).Description.Should().Be("Builds Assembly and adds 2 Load.");
        Perked(new CircuitPerkStats { ExtraLoadPerBuildAction = 1 }, 9).Preview(CircuitActionIds.Wire).Outcomes[0]
            .Change(CircuitEngine.LoadId)!.After.Should().Be(10, "Load stops at its maximum");
    }

    [Test]
    public void OverdriveBoost_AddsOneMoreLoadForOneLessCp()
    {
        var plain = Engine(Setup()).Preview(CircuitActionIds.Boost);
        var boost = Perked(new CircuitPerkStats { BoostLoad = 1, BoostCpReduction = 1 }).Preview(CircuitActionIds.Boost);

        plain.CpCost.Should().Be(4);
        boost.CpCost.Should().Be(3);
        boost.Outcomes[0].Change(CircuitEngine.LoadId)!.Delta.Should().Be(4);
        boost.Outcomes[0].Delta.Cp.Should().Be(-3);
    }

    [Test]
    public void ShockAbsorbers_CutTheIntegrityAnOverloadCosts_ToAtLeastOne()
    {
        var perks = new CircuitPerkStats { OverloadIntegrityReductionPercent = 33 };

        Setup().OverloadIntegrityCost.Should().Be(21);
        Setup(perks: perks).OverloadIntegrityCost.Should().Be(14);
        Perked(perks, 6).Preview(CircuitActionIds.Wire).Outcomes[1].Delta.Integrity.Should().Be(-17, "3 for the Wire and 14 for the overload");
        Setup(perks: new CircuitPerkStats { OverloadIntegrityReductionPercent = 100 }).OverloadIntegrityCost.Should().Be(1);
    }

    [Test]
    public void CascadeDischarge_IsOnlyOfferedWithThePerk_AndSpendsAllLoadIntoBothMeters()
    {
        Engine(Setup(), 9).Actions.Should().NotContain(action => action.Id == CircuitActionIds.DischargeBoth);

        var engine = Perked(new CircuitPerkStats { CascadeDischarge = true }, 9, Rolls());
        engine.Actions.Select(action => action.Id).Should().Contain(CircuitActionIds.DischargeBoth);
        engine.Actions.Select(action => action.Id).Should().Equal(
            CircuitActionIds.Wire, CircuitActionIds.Tune, CircuitActionIds.Boost, CircuitActionIds.DischargeAssembly,
            CircuitActionIds.DischargeCalibration, CircuitActionIds.DischargeBoth, CircuitActionIds.Ground, CircuitActionIds.Patch);

        var preview = engine.Preview(CircuitActionIds.DischargeBoth);
        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(5);
        preview.IntegrityCost.Should().Be(4);
        preview.Outcomes.Should().ContainSingle("it never overloads");
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Progress: 26, Quality: 376, Integrity: -4, Cp: -5));
        preview.Outcomes[0].Change(CircuitEngine.LoadId)!.After.Should().Be(0);

        engine.Perform(CircuitActionIds.DischargeBoth).Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        engine.Load.Should().Be(0);
        engine.Meters.Progress.Value.Should().Be(26);
        engine.Meters.Quality.Value.Should().Be(376);
    }

    [Test]
    public void CascadeDischarge_GivesSixtyPercentToEachMeter_SoMoreThanOneBurstInTotal()
    {
        var engine = Perked(new CircuitPerkStats { CascadeDischarge = true }, 9);
        var assembly = Only(engine, CircuitActionIds.DischargeAssembly).Delta.Progress;
        var calibration = Only(engine, CircuitActionIds.DischargeCalibration).Delta.Quality;
        var both = Only(engine, CircuitActionIds.DischargeBoth).Delta;

        both.Progress.Should().BeInRange((int)(assembly * 0.55), (int)(assembly * 0.65));
        both.Quality.Should().BeInRange((int)(calibration * 0.55), (int)(calibration * 0.65));
    }

    [Test]
    public void CascadeDischarge_NeedsLoadAndCp()
    {
        var empty = Perked(new CircuitPerkStats { CascadeDischarge = true });
        empty.Preview(CircuitActionIds.DischargeBoth).Enabled.Should().BeFalse();
        empty.Preview(CircuitActionIds.DischargeBoth).DisabledReason.Should().Be("There is no Load to discharge.");

        var poor = Engine(Setup(rank: 0, level: 1, gearCp: 4, perks: new CircuitPerkStats { CascadeDischarge = true }), 5);
        poor.Preview(CircuitActionIds.DischargeBoth).Enabled.Should().BeFalse();
        poor.Preview(CircuitActionIds.DischargeBoth).DisabledReason.Should().Be("Not enough CP.");
    }

    // ---- stats the Technician line grants --------------------------------------------------------------------------------

    [TestCase(1, 2, 2, 2, 3)]
    [TestCase(2, 1, 1, 1, 2)]
    public void InsulatedTraces_CutWireTuneBoostAndDischargeIntegrity_ButNotGroundOrPatch(
        int reduction, int wire, int tune, int boost, int discharge)
    {
        var engine = Perked(new CircuitPerkStats { ActionIntegrityReduction = reduction }, 5);

        engine.Preview(CircuitActionIds.Wire).IntegrityCost.Should().Be(wire);
        engine.Preview(CircuitActionIds.Tune).IntegrityCost.Should().Be(tune);
        engine.Preview(CircuitActionIds.Boost).IntegrityCost.Should().Be(boost);
        engine.Preview(CircuitActionIds.DischargeAssembly).IntegrityCost.Should().Be(discharge);
        engine.Preview(CircuitActionIds.DischargeCalibration).IntegrityCost.Should().Be(discharge);
        engine.Preview(CircuitActionIds.Ground).IntegrityCost.Should().Be(2);
        engine.Preview(CircuitActionIds.Patch).IntegrityCost.Should().Be(0);
    }

    [Test]
    public void InsulatedTraces_NeverTakeAnActionBelowOneIntegrity()
    {
        var engine = Perked(new CircuitPerkStats { ActionIntegrityReduction = 10, CascadeDischarge = true, MicroTune = true }, 5);

        foreach (var id in new[]
                 {
                     CircuitActionIds.Wire, CircuitActionIds.Tune, CircuitActionIds.MicroTune, CircuitActionIds.Boost,
                     CircuitActionIds.DischargeAssembly, CircuitActionIds.DischargeBoth
                 })
        {
            engine.Preview(id).IntegrityCost.Should().Be(1, id);
        }
    }

    [Test]
    public void FineCalibration_MakesEveryTuneBuildMoreCalibration()
    {
        Perked(new CircuitPerkStats { TunePowerPercent = 10 }).Preview(CircuitActionIds.Tune).Outcomes[0].Delta.Quality.Should().Be(89, "81 plain");
        Perked(new CircuitPerkStats { TunePowerPercent = 25 }).Preview(CircuitActionIds.Tune).Outcomes[0].Delta.Quality.Should().Be(101);
        Perked(new CircuitPerkStats { TunePowerPercent = 25 }, 6).Preview(CircuitActionIds.Tune).Outcomes[0].Delta.Quality.Should().Be(161, "129 plain at Load 6");
    }

    [Test]
    public void SignalLock_RaisesTheChanceOfAnAnnouncement()
    {
        Setup().CleanSignalChancePercent.Should().Be(25);
        Setup(perks: new CircuitPerkStats { SignalChancePercent = 10 }).CleanSignalChancePercent.Should().Be(35);

        var locked = Perked(new CircuitPerkStats { SignalChancePercent = 10 }, 0, 35);
        locked.Perform(CircuitActionIds.Wire);
        locked.NextOpportunity.Should().NotBeNull("a roll of 35 is within a 35% chance");

        var missed = Perked(new CircuitPerkStats { SignalChancePercent = 10 }, 0, 36);
        missed.Perform(CircuitActionIds.Wire);
        missed.NextOpportunity.Should().BeNull();

        var plain = Engine(Setup(), 0, 26);
        plain.Perform(CircuitActionIds.Wire);
        plain.NextOpportunity.Should().BeNull("a roll of 26 misses the plain 25%");
    }

    [Test]
    public void PrecisionPatch_RestoresMoreIntegrityForLessCp()
    {
        var perks = new CircuitPerkStats { PatchRestore = 10, PatchCpReduction = 2 };
        var plain = Engine(Setup(integrity: 140), 6, Overloads, NoSignal);
        var engine = Engine(Setup(integrity: 140, perks: perks), 6, Overloads, NoSignal);
        plain.Perform(CircuitActionIds.Wire);
        engine.Perform(CircuitActionIds.Wire);

        plain.Meters.Integrity.Value.Should().Be(95, "140 - 3 - 42");
        plain.Preview(CircuitActionIds.Patch).CpCost.Should().Be(8);
        Only(plain, CircuitActionIds.Patch).Delta.Integrity.Should().Be(20);
        engine.Preview(CircuitActionIds.Patch).CpCost.Should().Be(6);
        Only(engine, CircuitActionIds.Patch).Delta.Integrity.Should().Be(30);
        engine.Actions.Single(action => action.Id == CircuitActionIds.Patch).Description.Should().Be("Restores 30 Integrity and costs CP.");
    }

    [Test]
    public void MicroTune_BuildsAShareOfATunesCalibrationWithoutLoad()
    {
        Engine(Setup()).Actions.Should().NotContain(action => action.Id == CircuitActionIds.MicroTune);

        var engine = Perked(new CircuitPerkStats { MicroTune = true });
        engine.Actions.Select(action => action.Id).Should().Equal(
            CircuitActionIds.Wire, CircuitActionIds.Tune, CircuitActionIds.MicroTune, CircuitActionIds.Boost,
            CircuitActionIds.DischargeAssembly, CircuitActionIds.DischargeCalibration, CircuitActionIds.Ground, CircuitActionIds.Patch);

        var preview = engine.Preview(CircuitActionIds.MicroTune);
        preview.CpCost.Should().Be(2);
        preview.IntegrityCost.Should().Be(2);
        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Quality: 56, Integrity: -2, Cp: -2));
        preview.Outcomes[0].Change(CircuitEngine.LoadId)!.Delta.Should().Be(0);
        Perked(new CircuitPerkStats { MicroTune = true }, 6).Preview(CircuitActionIds.MicroTune).Outcomes[0].Delta.Quality.Should().Be(90);
        Perked(new CircuitPerkStats { MicroTune = true, TunePowerPercent = 10 }, 6).Preview(CircuitActionIds.MicroTune).Outcomes[0].Delta.Quality
            .Should().Be(99, "Fine Calibration raises it as it raises a Tune");
    }

    [Test]
    public void MicroTune_FollowsTheOrdinaryOverloadRule_SoItOnlyOverloadsWithLoadAlreadyAboveTheLine()
    {
        var perks = new CircuitPerkStats { MicroTune = true };

        Perked(perks, 6).Preview(CircuitActionIds.MicroTune).Outcomes.Should().ContainSingle("Load 6 is on the line");
        var risky = Perked(perks, 7).Preview(CircuitActionIds.MicroTune);
        risky.Outcomes.Should().HaveCount(2);
        risky.Outcomes[1].ChancePercent.Should().Be(8);
        risky.Outcomes[1].Change(CircuitEngine.LoadId)!.After.Should().Be(0);
    }

    [Test]
    public void ACleanSignalDoublesATuneButNotAMicroTune()
    {
        var engine = Perked(new CircuitPerkStats { MicroTune = true }, 0, Signal, NoSignal);
        engine.Perform(CircuitActionIds.Wire);
        engine.NextOpportunity.Should().NotBeNull();

        engine.Preview(CircuitActionIds.Tune).Outcomes[0].Delta.Quality.Should().Be(2 * Setup().TuneGain(1));
        engine.Preview(CircuitActionIds.MicroTune).Outcomes[0].Delta.Quality.Should().Be(Setup(perks: new CircuitPerkStats { MicroTune = true }).MicroTuneGain(1));
    }

    [Test]
    public void SafeModeGrace_MeansNoOverloadChanceUntilPastTheGrace_ThenTheUsualChance()
    {
        var setup = Setup(perks: new CircuitPerkStats { OverloadGrace = 2 });

        setup.OverloadChancePercent(7).Should().Be(0);
        setup.OverloadChancePercent(8).Should().Be(0, "2 points over is inside the grace");
        setup.OverloadChancePercent(9).Should().Be(24, "3 points over: the usual 8% per point");
        setup.OverloadChancePercent(10).Should().Be(32);
        Engine(setup, 7).Preview(CircuitActionIds.Wire).Outcomes.Should().ContainSingle();
        Engine(setup, 8).Preview(CircuitActionIds.Wire).Outcomes[1].ChancePercent.Should().Be(24);
        Engine(Setup(), 7).Preview(CircuitActionIds.Wire).Outcomes[1].ChancePercent.Should().Be(16, "without Safe Mode");
    }

    // ---- Surge -----------------------------------------------------------------------------------------------------------

    [Test]
    public void Surge_TurnsTheFirstOverloadIntoAFullDischargeIntoTheMeterFurtherFromFull()
    {
        var engine = Perked(new CircuitPerkStats { Surge = true }, 6, Overloads, NoSignal);
        engine.SurgeReady.Should().BeTrue();

        var preview = engine.Preview(CircuitActionIds.Wire);
        preview.Outcomes[1].Label.Should().Be(CircuitEngine.SurgeLabel);
        preview.Outcomes[1].ChancePercent.Should().Be(8);
        CircuitActionText.RiskText(preview).Should().Be("Surge 8%: Calibration +436, Load to 0.");

        var result = engine.Perform(CircuitActionIds.Wire);

        result.Outcome.Should().BeEquivalentTo(preview.Outcomes[1]);
        engine.Meters.Progress.Value.Should().Be(14, "the Wire still resolved");
        engine.Meters.Quality.Value.Should().Be(436, "Calibration was further from full, so it took the 7 Load burst");
        engine.Meters.Integrity.Value.Should().Be(67, "no overload cost");
        engine.Load.Should().Be(0);
        engine.SurgeReady.Should().BeFalse();
    }

    [Test]
    public void Surge_DischargesIntoAssemblyWhenItIsFurtherFromFull_AndOnATie()
    {
        var tune = Perked(new CircuitPerkStats { Surge = true }, 6, Overloads, NoSignal);
        tune.Perform(CircuitActionIds.Tune);
        tune.Meters.Quality.Value.Should().Be(129);
        tune.Meters.Progress.Value.Should().Be(30, "Assembly was further from full: the 7 Load burst");

        var tie = Perked(new CircuitPerkStats { Surge = true }, 8, Overloads, NoSignal);
        tie.Perform(CircuitActionIds.Boost);
        tie.Meters.Progress.Value.Should().Be(50, "both meters were empty, so Assembly takes the 10 Load burst");
        tie.Meters.Quality.Value.Should().Be(0);
    }

    [Test]
    public void Surge_IsSpentByTheFirstOverload_SoTheSecondCostsIntegrityAsUsual()
    {
        var engine = Perked(new CircuitPerkStats { Surge = true }, 6, Overloads, NoSignal, NoSignal, NoSignal, Overloads, NoSignal);
        engine.Perform(CircuitActionIds.Wire).Outcome.Label.Should().Be(CircuitEngine.SurgeLabel);
        engine.Perform(CircuitActionIds.Boost);
        engine.Perform(CircuitActionIds.Boost);
        engine.Load.Should().Be(6);

        var preview = engine.Preview(CircuitActionIds.Wire);

        preview.Outcomes[1].Label.Should().Be(CircuitEngine.OverloadLabel);
        engine.Perform(CircuitActionIds.Wire).Outcome.Label.Should().Be(CircuitEngine.OverloadLabel);
        engine.Meters.Integrity.Value.Should().Be(70 - 3 - 3 - 3 - 3 - 21);
    }

    [Test]
    public void Surge_NeverReplacesAnActionThatHolds()
    {
        var engine = Perked(new CircuitPerkStats { Surge = true }, 6, Holds, NoSignal);

        engine.Perform(CircuitActionIds.Wire).Outcome.Label.Should().Be("Wired");
        engine.SurgeReady.Should().BeTrue();
        engine.Load.Should().Be(7);
    }

    // ---- Failsafe --------------------------------------------------------------------------------------------------------

    [Test]
    public void Failsafe_KeepsThePieceAtOneIntegrityTheFirstTimeItWouldBreak_ButNotTheSecond()
    {
        var engine = Engine(Setup(integrity: 6, perks: new CircuitPerkStats { Failsafe = true }), 0, Rolls().Concat(Rolls()).Concat(Rolls()).ToArray());
        engine.FailsafeReady.Should().BeTrue();

        engine.Perform(CircuitActionIds.Wire);
        engine.Meters.Integrity.Value.Should().Be(3);

        var preview = engine.Preview(CircuitActionIds.Wire);
        preview.Outcomes[0].ResultingStatus.Should().Be(MinigameStatus.InProgress, "the Failsafe holds the piece");
        preview.Outcomes[0].Delta.Integrity.Should().Be(-2);

        engine.Perform(CircuitActionIds.Wire).Status.Should().Be(MinigameStatus.InProgress);
        engine.Meters.Integrity.Value.Should().Be(1);
        engine.FailsafeReady.Should().BeFalse();

        engine.Preview(CircuitActionIds.Wire).Outcomes[0].ResultingStatus.Should().Be(MinigameStatus.Failed);
        engine.Perform(CircuitActionIds.Wire).Status.Should().Be(MinigameStatus.Failed);
    }

    [Test]
    public void Failsafe_AlsoSavesThePieceFromAnOverloadThatWouldBreakIt()
    {
        var engine = Engine(Setup(integrity: 4, perks: new CircuitPerkStats { Failsafe = true }), 6, Overloads, NoSignal);

        var result = engine.Perform(CircuitActionIds.Wire);

        result.Outcome.Label.Should().Be(CircuitEngine.OverloadLabel);
        result.Status.Should().Be(MinigameStatus.InProgress, "the Wire took 3 and the overload the last 1");
        engine.Meters.Integrity.Value.Should().Be(1);
        engine.FailsafeReady.Should().BeFalse();
    }

    [Test]
    public void Failsafe_IsNotSpentByAnActionThatFinishesTheItem()
    {
        var setup = Setup(baseProgress: 10, craftsmanship: 1000, integrity: 3, perks: new CircuitPerkStats { Failsafe = true });
        var engine = Engine(setup, 6, Holds);

        engine.Perform(CircuitActionIds.Wire).Status.Should().Be(MinigameStatus.Succeeded);
        engine.FailsafeReady.Should().BeTrue("the item was finished, so nothing was saved");
    }

    // ---- Diagnostics -----------------------------------------------------------------------------------------------------

    [Test]
    public void Diagnostics_AnnouncesASignalTwoActionsAheadAndKeepsItAnnouncedUntilItApplies()
    {
        var perks = new CircuitPerkStats { ForecastTurns = 1 };
        Setup(perks: perks).ForecastDepth.Should().Be(2);

        // First action: the next action and the one after are both rolled (no signal next, a signal after). Later actions roll one.
        var engine = Perked(perks, 0, NoSignal, Signal, NoSignal, NoSignal);
        engine.Perform(CircuitActionIds.Wire);

        engine.NextOpportunity.Should().BeNull();
        engine.LaterOpportunity.Should().NotBeNull();
        engine.LaterOpportunity!.Id.Should().Be(CircuitActionIds.CleanSignal);
        engine.LaterOpportunity.TurnsAway.Should().Be(2);
        CircuitActionText.OpportunityText(engine).Should().Be("Clean Signal in 2 actions: your Tune then is doubled.");

        engine.Perform(CircuitActionIds.Wire);

        engine.LaterOpportunity.Should().BeNull();
        engine.NextOpportunity.Should().NotBeNull("it is one action ahead now");
        engine.NextOpportunity!.TurnsAway.Should().Be(1);
        engine.Preview(CircuitActionIds.Tune).Outcomes[0].Label.Should().Contain("Clean Signal");
        CircuitActionText.OpportunityText(engine).Should().Be($"Clean Signal next action: {engine.NextOpportunity.Description}");

        engine.Perform(CircuitActionIds.Tune).Outcome.Label.Should().Be("Tuned (Clean Signal)");
        engine.NextOpportunity.Should().BeNull();
    }

    [Test]
    public void Diagnostics_RollsBothSlotsOnTheFirstForecastAndOneSlotAfter_SoEveryActionHasTheSameChance()
    {
        var random = new ScriptedMinigameRandom(Signal, Signal, Signal);
        var engine = new CircuitEngine(random, Setup(perks: new CircuitPerkStats { ForecastTurns = 1 }));

        engine.Perform(CircuitActionIds.Wire);

        random.Calls.Should().Be(2);
        engine.NextOpportunity.Should().NotBeNull("the second action can be announced by the first forecast");
        engine.LaterOpportunity.Should().NotBeNull();

        engine.Perform(CircuitActionIds.Wire);

        random.Calls.Should().Be(3, "from the second forecast on, one roll per action");
        engine.NextOpportunity!.TurnsAway.Should().Be(1, "the signal announced two ahead is one ahead now");
        engine.LaterOpportunity!.TurnsAway.Should().Be(2);
    }

    [Test]
    public void Diagnostics_PreviewsAShiftedSignalWithoutRollingAnything()
    {
        var random = new ScriptedMinigameRandom(NoSignal, Signal);
        var engine = new CircuitEngine(random, Setup(perks: new CircuitPerkStats { ForecastTurns = 1 }));
        engine.Perform(CircuitActionIds.Wire);
        var calls = random.Calls;

        engine.PreviewAll();

        random.Calls.Should().Be(calls);
        engine.LaterOpportunity.Should().NotBeNull();
    }

    // ---- the whole line --------------------------------------------------------------------------------------------------

    [Test]
    public void WithEveryPerk_TheActionsAreTheSevenPlusTheTwoAddedOnes()
    {
        Perked(EveryPerk).Actions.Select(action => action.Id).Should().Equal(
            CircuitActionIds.Wire, CircuitActionIds.Tune, CircuitActionIds.MicroTune, CircuitActionIds.Boost,
            CircuitActionIds.DischargeAssembly, CircuitActionIds.DischargeCalibration, CircuitActionIds.DischargeBoth,
            CircuitActionIds.Ground, CircuitActionIds.Patch);
    }

    [Test]
    public void WithEveryPerk_AcrossManySeeds_PerformAlwaysLandsOnAPreviewedOutcomeAndTheSavesAreSpentOnce()
    {
        var actionIds = Perked(EveryPerk).Actions.Select(action => action.Id).ToArray();
        var surges = 0;
        var failsafes = 0;
        var signalsTwoAhead = 0;

        for (var seed = 1; seed <= 200; seed++)
        {
            var engine = new CircuitEngine(new SeededMinigameRandom(seed), Setup(line: 4, integrity: 30, perks: EveryPerk));
            var picker = new System.Random(seed);
            var surgesThisCraft = 0;

            for (var turn = 0; turn < 60 && engine.Status == MinigameStatus.InProgress; turn++)
            {
                var actionId = actionIds[picker.Next(actionIds.Length)];
                var preview = engine.Preview(actionId);
                var before = engine.DescribeMeters();
                var failsafeBefore = engine.FailsafeReady;
                signalsTwoAhead += engine.LaterOpportunity != null ? 1 : 0;

                var result = engine.Perform(actionId);

                if (!preview.Enabled)
                {
                    result.Performed.Should().BeFalse();
                    engine.DescribeMeters().Should().Equal(before);
                    continue;
                }

                preview.Outcomes.Should().ContainEquivalentOf(result.Outcome, $"seed {seed} turn {turn}");
                preview.Outcomes.Sum(outcome => outcome.ChancePercent).Should().Be(100);
                engine.Status.Should().Be(result.Outcome.ResultingStatus);
                foreach (var meter in engine.DescribeMeters())
                    meter.Value.Should().BeInRange(meter.Min, meter.Max);

                if (result.Outcome.Label == CircuitEngine.SurgeLabel)
                    surgesThisCraft++;

                failsafes += failsafeBefore && !engine.FailsafeReady ? 1 : 0;
            }

            surgesThisCraft.Should().BeLessThanOrEqualTo(1, $"seed {seed}: Surge is once per craft");
            surges += surgesThisCraft;
        }

        surges.Should().BeGreaterThan(0, "the walk reaches a Surge");
        failsafes.Should().BeGreaterThan(0, "the walk reaches a Failsafe");
        signalsTwoAhead.Should().BeGreaterThan(0, "the walk meets a signal announced two actions ahead");
    }

    // ---- window text -----------------------------------------------------------------------------------------------------

    [Test]
    public void TheLoadRuleNamesTheGraceAndTheSurgeWhenTheCrafterHasThem()
    {
        CircuitActionText.LoadRule(Setup()).Should().Be(
            "Safe at or below Load 6. An action that leaves Load above it can overload: +8% per point over the line, costing 21 Integrity and resetting Load to 0. Discharge and Ground never overload.");

        var perked = CircuitActionText.LoadRule(Setup(perks: new CircuitPerkStats { OverloadGrace = 2, Surge = true, OverloadIntegrityReductionPercent = 33 }));
        perked.Should().Contain("Actions cannot overload at or below Load 8 (the line is 6, plus 2 of grace).");
        perked.Should().Contain("costs 14 Integrity");
        perked.Should().Contain("except the first one, which Surge turns into a Discharge");
    }

    [Test]
    public void ThePerkSummaryListsTheStyleAndTheSavesStillReady()
    {
        CircuitActionText.PerkSummary(Engine(Setup()), CircuitStyle.None).Should().BeEmpty();
        CircuitActionText.PerkSummary(Engine(Setup()), CircuitStyle.Overclock).Should().Be("Style: Overclock");

        var engine = Perked(new CircuitPerkStats { Surge = true, Failsafe = true }, 6, Overloads, NoSignal);
        CircuitActionText.PerkSummary(engine, CircuitStyle.SafeMode).Should().Be("Style: Safe Mode  |  Surge ready  |  Failsafe ready");
        engine.Perform(CircuitActionIds.Wire);
        CircuitActionText.PerkSummary(engine, CircuitStyle.None).Should().Be("Surge used  |  Failsafe ready");
    }

    [Test]
    public void TheNewActionsReadAsTheirOwnRows_AndEveryPerkedTextIsAscii()
    {
        var engine = Perked(EveryPerk, 9);
        var texts = engine.PreviewAll().SelectMany(preview => new[]
        {
            CircuitActionText.ButtonText(preview), CircuitActionText.RowText(preview), CircuitActionText.Tooltip(preview),
            preview.Action.Description
        }).Append(CircuitActionText.LoadRule(engine.Setup)).Append(CircuitActionText.OpportunityText(engine));

        CircuitActionText.ButtonText(engine.Preview(CircuitActionIds.DischargeBoth)).Should().Be("Cascade Discharge (5 CP)");
        CircuitActionText.ButtonText(engine.Preview(CircuitActionIds.MicroTune)).Should().Be("Micro-Tune (2 CP)");
        CircuitActionText.RiskText(engine.Preview(CircuitActionIds.DischargeBoth)).Should().Be(CircuitActionText.NoRisk);
        texts.Should().OnlyContain(text => text.All(character => character < 128));
    }
}
