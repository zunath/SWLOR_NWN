using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.StatService;
using static SWLOR.Game.Server.Tests.Service.Fabricator.FabricatorTestData;

namespace SWLOR.Game.Server.Tests.Service.Fabricator;

/// <summary>
/// Covers the numbers the Builder perks change in the Fabricator: every stat on its own, the order cost reductions and minimums
/// apply in, Dual Feed's two actions, and that a crafter without perks plays exactly the Fabricator as tuned. Pinned numbers come
/// from <see cref="FabricatorTestData.Setup"/>: a full Run is 279 units and a Precision Pass 52 quality points.
/// </summary>
public class FabricatorPerkTests
{
    private static FabricatorSetup Perked(
        FabricatorPerkStats stats,
        int rank = 25,
        int level = 25,
        int gearCp = 20,
        int penalty = 0,
        FabricatorParameters parameters = null) =>
        new(parameters ?? Parameters(), level, BaseProgress, rank, 12, 13, gearCp, penalty, stats);

    private static FabricatorEngine PerkedEngine(FabricatorPerkStats stats, params int[] rolls) =>
        Engine(Perked(stats), rolls.Length == 0 ? Enumerable.Repeat(NoPrime, 40).ToArray() : rolls);

    private static readonly FabricatorPerkStats DualFeed = new(DualFeedUnlocked: true);

    // ---- no perks --------------------------------------------------------------------------------------------------

    [Test]
    public void ACrafterWithoutPerksPlaysTheFabricatorAsTuned()
    {
        var plain = Setup();
        var none = Perked(FabricatorPerkStats.None);
        var nullStats = new FabricatorSetup(Parameters(), 25, BaseProgress, 25, 12, 13, 20);

        foreach (var setup in new[] { none, nullStats })
        {
            setup.RunGain(true, false).Should().Be(plain.RunGain(true, false)).And.Be(279);
            setup.RunGain(false, true).Should().Be(plain.RunGain(false, true));
            setup.PrecisionGain(false).Should().Be(plain.PrecisionGain(false)).And.Be(52);
            setup.PrecisionGain(true).Should().Be(plain.PrecisionGain(true));
            setup.WearRate.Should().Be(plain.WearRate);
            setup.RetoolCp.Should().Be(5);
            setup.ServiceCp.Should().Be(10);
            setup.ServiceRestore.Should().Be(FabricatorRules.ServiceRestore);
            setup.PrimeWindowActions.Should().Be(FabricatorRules.PrimeWindowActions);
            setup.PrimeBonusPercent.Should().Be(FabricatorRules.PrimeBonusPercent);
            setup.AssembleCapPoints.Should().Be(FabricatorRules.AssembleCapPoints);
            setup.DualFeedUnlocked.Should().BeFalse();
        }

        Engine(none).Actions.Select(action => action.Id).Should().Equal(Engine(plain).Actions.Select(action => action.Id));
        FabricatorPerkStats.None.HasAny.Should().BeFalse();
        new FabricatorPerkStats(WearReduction: 1).HasAny.Should().BeTrue();
    }

    [Test]
    public void ThePerkStatsAreReadFromTheCraftersStatTotals_OneLookupPerStat()
    {
        var totals = new Dictionary<StatType, int>
        {
            [StatType.FabricatorRunPowerPercent] = 25,
            [StatType.FabricatorPrecisionPowerPercent] = -25,
            [StatType.FabricatorWearReduction] = 1,
            [StatType.FabricatorRetoolCpReductionPercent] = 50,
            [StatType.FabricatorServiceRestore] = 20,
            [StatType.FabricatorServiceCpReduction] = 4,
            [StatType.FabricatorDualFeedUnlocked] = 1,
            [StatType.FabricatorPrimeWindowActions] = 1,
            [StatType.FabricatorPrimeBonusPercent] = 25,
            [StatType.FabricatorAssembleCapPoints] = 15
        };

        var stats = FabricatorPerkStats.FromStatTotals(stat => totals.GetValueOrDefault(stat));

        stats.Should().Be(new FabricatorPerkStats(25, -25, 1, 50, 20, 4, true, 1, 25, 15));
        FabricatorPerkStats.FromStatTotals(_ => 0).Should().Be(FabricatorPerkStats.None);
        var act = () => FabricatorPerkStats.FromStatTotals(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ---- Run and Precision Pass (the styles) ----------------------------------------------------------------------

    [Test]
    public void ProductionRun_MakesARunSixtyPercentStronger_AndAPrecisionPassThreePercentWeaker()
    {
        var setup = Perked(new FabricatorPerkStats(RunPowerPercent: 60, PrecisionPowerPercent: -3));

        setup.RunGain(true, false).Should().Be(447, "249.2 x 1.12 x 1.6");
        setup.RunGain(false, false).Should().Be(223, "an off-head Run is half of the stronger Run");
        setup.PrecisionGain(false).Should().Be(50, "46 x 1.13 x 0.97");
    }

    [Test]
    public void FineTolerance_MakesAPrecisionPassAQuarterStronger_AndARunAQuarterWeaker()
    {
        var setup = Perked(new FabricatorPerkStats(RunPowerPercent: -25, PrecisionPowerPercent: 25));

        setup.RunGain(true, false).Should().Be(209, "249.2 x 1.12 x 0.75");
        setup.PrecisionGain(false).Should().Be(65, "46 x 1.13 x 1.25");
    }

    [Test]
    public void APowerPercentThatWouldCancelTheActionLeavesItAtLeastOne()
    {
        var setup = Perked(new FabricatorPerkStats(RunPowerPercent: -200, PrecisionPowerPercent: -200));

        setup.RunGain(true, false).Should().Be(1);
        setup.PrecisionGain(false).Should().Be(1);
    }

    [Test]
    public void ThePowerPercentsApplyToTheRunsAndPassesTheEnginePreviewsAndPerforms()
    {
        var setup = Perked(new FabricatorPerkStats(RunPowerPercent: 25, PrecisionPowerPercent: -25));
        var engine = Engine(setup, NoPrime, NoPrime);

        var run = engine.Preview(FabricatorActionIds.RunFittings);
        run.Outcomes[0].Change(FabricatorEngine.FittingsProgressId)!.Delta.Should().Be(349);
        engine.Perform(FabricatorActionIds.RunFittings).Performed.Should().BeTrue();
        engine.Part(SubAssembly.Fittings).Progress.Should().Be(349);

        var pass = engine.Preview(FabricatorActionIds.PrecisionPass);
        pass.Outcomes[0].Change(FabricatorEngine.FittingsQualityId)!.Delta.Should().Be(39);
        engine.Perform(FabricatorActionIds.PrecisionPass).Performed.Should().BeTrue();
        engine.Part(SubAssembly.Fittings).Quality.Should().Be(39);
    }

    // ---- wear ---------------------------------------------------------------------------------------------------------

    [Test]
    public void HardenedHeads_WearTheMachineOneLess_ForARunAndAPrecisionPassButNotARetool()
    {
        var setup = Perked(new FabricatorPerkStats(WearReduction: 1));
        var engine = Engine(setup, NoPrime, NoPrime);

        setup.WearRate.Should().Be(3);
        engine.Preview(FabricatorActionIds.RunFittings).IntegrityCost.Should().Be(3);
        engine.Perform(FabricatorActionIds.RunFittings).Performed.Should().BeTrue();
        engine.Preview(FabricatorActionIds.PrecisionPass).IntegrityCost.Should().Be(3);
        engine.Meters.Integrity.Value.Should().Be(97);
        engine.Preview(FabricatorActionIds.RetoolCutter).IntegrityCost.Should().Be(FabricatorRules.RetoolWear);
    }

    [TestCase(2, 1, 1)]
    [TestCase(2, 5, 1)]
    [TestCase(9, 1, 8)]
    [TestCase(1, 1, 1)]
    public void WearNeverFallsBelowOne(int wear, int reduction, int expected)
    {
        Perked(new FabricatorPerkStats(WearReduction: reduction), parameters: Parameters(wear: wear)).WearRate.Should().Be(expected);
    }

    // ---- Retool and Service costs ---------------------------------------------------------------------------------

    [TestCase(25, 0, 5)]
    [TestCase(25, 25, 4)]
    [TestCase(25, 50, 3)]
    [TestCase(12, 25, 2)]
    [TestCase(12, 50, 2)]
    [TestCase(1, 25, 1)]
    [TestCase(1, 50, 1)]
    [TestCase(53, 50, 5)]
    public void QuickChange_TakesAPercentOffARetoolsCp_ToTheNearestCp_AndNeverBelowOne(int level, int percent, int expected)
    {
        var setup = Perked(new FabricatorPerkStats(RetoolCpReductionPercent: percent), level: level);

        setup.RetoolCp.Should().Be(expected);
        Engine(setup).Preview(FabricatorActionIds.RetoolCutter).CpCost.Should().Be(expected);
    }

    [TestCase(25, 0, 10)]
    [TestCase(25, 2, 8)]
    [TestCase(25, 4, 6)]
    [TestCase(10, 4, 1)]
    [TestCase(10, 40, 1)]
    public void FieldService_TakesCpOffAServiceAndNeverBelowOne(int level, int reduction, int expected)
    {
        var setup = Perked(new FabricatorPerkStats(ServiceCpReduction: reduction), level: level);

        setup.ServiceCp.Should().Be(expected);
        Engine(setup).Preview(FabricatorActionIds.Service).CpCost.Should().Be(expected);
    }

    [Test]
    public void FieldService_RestoresMoreMachineCondition_AndThePreviewAndTheActionsDescriptionSayHowMuch()
    {
        var setup = Perked(new FabricatorPerkStats(ServiceRestoreBonus: 20), parameters: Parameters(wear: 9));
        var engine = Engine(setup, Enumerable.Repeat(NoPrime, 40).ToArray());
        foreach (var action in new[]
                 {
                     FabricatorActionIds.RunFittings, FabricatorActionIds.RunFrame, FabricatorActionIds.RunShell,
                     FabricatorActionIds.PrecisionPass, FabricatorActionIds.PrecisionPass
                 })
        {
            engine.Perform(action).Performed.Should().BeTrue(action);
        }

        var before = engine.Meters.Integrity.Value;
        var preview = engine.Preview(FabricatorActionIds.Service);

        setup.ServiceRestore.Should().Be(60);
        preview.Outcomes[0].Change(MinigameMeterIds.Integrity)!.Delta.Should().Be(Math.Min(60, 100 - before));
        engine.Actions.Single(action => action.Id == FabricatorActionIds.Service).Description.Should().Contain("Restores 60 Machine Condition");
        engine.Perform(FabricatorActionIds.Service).Performed.Should().BeTrue();
        engine.Meters.Integrity.Value.Should().Be(Math.Min(100, before + 60));
    }

    [Test]
    public void TheCostReductionsKeepTheirMinimumsEvenWhenTheyStack()
    {
        var setup = Perked(new FabricatorPerkStats(RetoolCpReductionPercent: 90, ServiceCpReduction: 99), level: 1, rank: 1);

        setup.RetoolCp.Should().Be(FabricatorRules.MinimumDiscountedCp);
        setup.ServiceCp.Should().Be(FabricatorRules.MinimumDiscountedCp);
    }

    // ---- Prime Feedstock ----------------------------------------------------------------------------------------------

    [Test]
    public void FeedstockReserve_MakesAPrimeWaitOneMoreAction()
    {
        var plain = Engine(Setup(), PrimeFirst, NoPrime, NoPrime, NoPrime);
        var reserve = Engine(Perked(new FabricatorPerkStats(PrimeWindowBonusActions: 1)), PrimeFirst, NoPrime, NoPrime, NoPrime);

        plain.Perform(FabricatorActionIds.RunFittings);
        reserve.Perform(FabricatorActionIds.RunFittings);

        plain.PrimeActionsLeft.Should().Be(2);
        reserve.PrimeActionsLeft.Should().Be(3);
        reserve.NextOpportunity.Description.Should().Contain("waits 3 more actions");

        // Retooling between the Bonder and the Extruder uses no head's prime, so only the waiting passes.
        reserve.Perform(FabricatorActionIds.RetoolExtruder);
        reserve.Perform(FabricatorActionIds.RetoolBonder);
        reserve.PrimedHead.Should().Be(FabricatorHead.Cutter, "it still waits on its third action");
        reserve.Perform(FabricatorActionIds.RetoolExtruder);
        reserve.PrimedHead.Should().BeNull("the prime passed after three actions");
    }

    [Test]
    public void FeedstockContract_RaisesThePrimesBonus_InThePreviewAndTheOpportunityText()
    {
        var setup = Perked(new FabricatorPerkStats(PrimeBonusPercentBonus: 25));
        var engine = Engine(setup, PrimeThird);

        setup.PrimeBonusPercent.Should().Be(75);
        setup.RunGain(true, true).Should().Be(488, "279.2 x 1.75");
        setup.PrecisionGain(true).Should().Be(91, "51.98 x 1.75");
        engine.NextOpportunity.Should().BeNull();

        var plain = Engine(Setup(), PrimeFirst);
        plain.Perform(FabricatorActionIds.RunFittings);
        var primed = Engine(setup, PrimeFirst);
        primed.Perform(FabricatorActionIds.RunFittings);
        plain.NextOpportunity.Description.Should().Contain("+50%");
        primed.NextOpportunity.Description.Should().Contain("+75%");
        primed.Preview(FabricatorActionIds.RetoolCutter).Enabled.Should().BeTrue();
    }

    // ---- Assemble -----------------------------------------------------------------------------------------------------

    [Test]
    public void MasterAssembler_RaisesTheAssembleCap_ToTheWeakestPlusThirtyFive()
    {
        var setup = Perked(new FabricatorPerkStats(AssembleCapBonusPoints: 15));

        setup.AssembleCapPoints.Should().Be(35);
        setup.CapFor(10, 100, 100).Should().Be(45);
        setup.CapFor(0, 100, 100).Should().Be(35);
        setup.CapFor(70, 90, 80).Should().Be(100, "the cap never exceeds the maximum quality");
        setup.PieceQuality(10, 100, 100).Should().Be(Math.Min(setup.AverageQuality(10, 100, 100), 45));
        Setup().CapFor(10, 100, 100).Should().Be(30);
    }

    [Test]
    public void TheQualityMetersMarkAndTheAssembleButtonShowThePerkedCap()
    {
        var engine = Engine(Perked(new FabricatorPerkStats(AssembleCapBonusPoints: 15)));

        engine.DescribeMeters().Single(meter => meter.Id == MinigameMeterIds.Quality).Mark.Should().Be(MinigameMeterMark.Line(35, "Weakest + 35"));
        engine.Actions.Single(action => action.Id == FabricatorActionIds.Assemble).Description.Should().Contain("weakest plus 35");
        engine.QualityCap.Should().Be(35);
    }

    [Test]
    public void AnAssembledPieceIsMadeAtTheHigherCap()
    {
        foreach (var (stats, expectedCap) in new[] { (FabricatorPerkStats.None, 20), (new FabricatorPerkStats(AssembleCapBonusPoints: 15), 35) })
        {
            var setup = Perked(stats);
            var engine = Engine(setup, Enumerable.Repeat(NoPrime, 80).ToArray());
            foreach (var part in FabricatorHeads.AllParts)
            {
                if (engine.ActiveHead != part.HeadFor())
                    engine.Perform(part.HeadFor().RetoolActionId());

                while (engine.Part(part).Progress < setup.SizeOf(part))
                    engine.Perform(part.RunActionId());
            }

            // Only the Shell is refined: the Frame and Fittings stay at no quality, so the piece is held to the cap.
            engine.Perform(FabricatorHeads.AllHeads[(int)SubAssembly.Shell].RetoolActionId());
            while (engine.Part(SubAssembly.Shell).Quality < 100)
                engine.Perform(FabricatorActionIds.PrecisionPass);

            engine.Perform(FabricatorActionIds.Assemble).Performed.Should().BeTrue();

            engine.Meters.QualityPercent.Should().Be(Math.Min(expectedCap, setup.AverageQuality(0, 100, 0)));
            engine.Status.Should().Be(MinigameStatus.Succeeded);
        }
    }

    // ---- Dual Feed ----------------------------------------------------------------------------------------------------

    [Test]
    public void DualFeedIsOfferedOnlyToACrafterWhoHasIt_AsTwoActionsAfterTheRuns()
    {
        Engine(Setup()).Actions.Select(action => action.Id).Should().NotContain(
            new[] { FabricatorActionIds.DualFeedFirst, FabricatorActionIds.DualFeedSecond });

        var actions = PerkedEngine(DualFeed).Actions.Select(action => action.Id).ToList();

        actions.Should().Equal(
            FabricatorActionIds.RunFrame, FabricatorActionIds.RunShell, FabricatorActionIds.RunFittings,
            FabricatorActionIds.DualFeedFirst, FabricatorActionIds.DualFeedSecond,
            FabricatorActionIds.PrecisionPass, FabricatorActionIds.RetoolCutter, FabricatorActionIds.RetoolExtruder,
            FabricatorActionIds.RetoolBonder, FabricatorActionIds.Service, FabricatorActionIds.Assemble);
        PerkedEngine(DualFeed).PreviewAll().Should().HaveCount(11);
        FabricatorSetup.UnlockRank(FabricatorActionIds.DualFeedFirst).Should().Be(0);
        FabricatorSetup.UnlockRank(FabricatorActionIds.DualFeedSecond).Should().Be(0);
    }

    [Test]
    public void DualFeedsTwoActionsAreTheLoadedHeadsSubAssemblyPlusEachOfTheOtherTwo_NamedBySubAssembly()
    {
        var engine = PerkedEngine(DualFeed);
        engine.ActiveHead.Should().Be(FabricatorHead.Bonder);

        // The button names the other sub-assembly; the loaded head's own is always part of it, and the description says so.
        engine.Preview(FabricatorActionIds.DualFeedFirst).Action.Name.Should().Be("Dual Feed + Frame");
        engine.Preview(FabricatorActionIds.DualFeedSecond).Action.Name.Should().Be("Dual Feed + Shell");
        engine.Preview(FabricatorActionIds.DualFeedFirst).Action.Description.Should().Contain("Builds the Fittings and the Frame");

        engine.Perform(FabricatorActionIds.RetoolExtruder).Performed.Should().BeTrue();
        engine.Preview(FabricatorActionIds.DualFeedFirst).Action.Name.Should().Be("Dual Feed + Frame");
        engine.Preview(FabricatorActionIds.DualFeedSecond).Action.Name.Should().Be("Dual Feed + Fittings");
        engine.Preview(FabricatorActionIds.DualFeedSecond).Action.Description.Should().Contain("Builds the Shell and the Fittings");

        engine.Perform(FabricatorActionIds.RetoolCutter).Performed.Should().BeTrue();
        engine.Preview(FabricatorActionIds.DualFeedFirst).Action.Name.Should().Be("Dual Feed + Shell");
        engine.Preview(FabricatorActionIds.DualFeedSecond).Action.Name.Should().Be("Dual Feed + Fittings");

        FabricatorActionText.ButtonText(engine.Preview(FabricatorActionIds.DualFeedFirst)).Should().Be($"Dual Feed + Shell ({engine.Setup.RetoolCp} CP)")
            .And.HaveLength(24, "as long as Retool (Extruder) with its CP, which the button holds");
    }

    [Test]
    public void DualFeed_BuildsBothSubAssembliesAtSixtyPercentOfAFullRun_ForARetoolsCpAndOneRunsWear()
    {
        var setup = Perked(DualFeed);
        var engine = Engine(setup, NoPrime, NoPrime);
        var full = setup.RunGain(true, false);

        var preview = engine.Preview(FabricatorActionIds.DualFeedFirst);

        setup.DualFeedGain(false).Should().Be(167, "279.2 x 0.6");
        setup.DualFeedGain(false).Should().BeCloseTo((int)(full * 0.6), 1);
        (2 * setup.DualFeedGain(false)).Should().BeCloseTo((int)(full * 1.2), 2, "120% of a Run in all");
        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(setup.RetoolCp);
        preview.IntegrityCost.Should().Be(setup.WearRate);
        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].Change(FabricatorEngine.FittingsProgressId)!.Delta.Should().Be(167);
        preview.Outcomes[0].Change(FabricatorEngine.FrameProgressId)!.Delta.Should().Be(167);
        preview.Outcomes[0].Change(FabricatorEngine.ShellProgressId)!.Delta.Should().Be(0);
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Progress: 334, Integrity: -setup.WearRate, Cp: -setup.RetoolCp));

        var result = engine.Perform(FabricatorActionIds.DualFeedFirst);

        result.Performed.Should().BeTrue();
        result.Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        engine.Part(SubAssembly.Fittings).Progress.Should().Be(167);
        engine.Part(SubAssembly.Frame).Progress.Should().Be(167);
        engine.Part(SubAssembly.Shell).Progress.Should().Be(0);
        engine.ActiveHead.Should().Be(FabricatorHead.Bonder, "the loaded head does not change");
        engine.Meters.Integrity.Value.Should().Be(100 - setup.WearRate);
        engine.Meters.Cp.Value.Should().Be(setup.MaxCp - setup.RetoolCp);
        engine.Meters.Progress.Value.Should().Be(334);
    }

    [Test]
    public void DualFeedCostsAnActionLessAndAPointOfWearLessThanTwoRuns_ButAtTheCpOfARetool()
    {
        var setup = Perked(DualFeed);
        var dual = Engine(setup, NoPrime, NoPrime, NoPrime);
        dual.Perform(FabricatorActionIds.DualFeedSecond);

        var runs = Engine(setup, NoPrime, NoPrime, NoPrime);
        runs.Perform(FabricatorActionIds.RunFittings);
        runs.Perform(FabricatorActionIds.RunShell);

        runs.Meters.Integrity.Value.Should().Be(100 - 2 * setup.WearRate);
        dual.Meters.Integrity.Value.Should().BeGreaterThan(runs.Meters.Integrity.Value);
        dual.Turn.Should().Be(1);
        runs.Turn.Should().Be(2);
        dual.Meters.Cp.Value.Should().BeLessThan(runs.Meters.Cp.Value);
    }

    [Test]
    public void DualFeedStopsAtEachSubAssembliesSize_AndTheCapShowsInThePreview()
    {
        var setup = Perked(DualFeed);
        var engine = Engine(setup, Enumerable.Repeat(NoPrime, 20).ToArray());
        while (engine.Part(SubAssembly.Frame).Progress + setup.DualFeedGain(false) < setup.FrameSize)
            engine.Perform(FabricatorActionIds.DualFeedFirst).Performed.Should().BeTrue();

        var before = engine.Part(SubAssembly.Frame).Progress;
        var preview = engine.Preview(FabricatorActionIds.DualFeedFirst);

        preview.Outcomes[0].Change(FabricatorEngine.FrameProgressId)!.Delta.Should().Be(setup.FrameSize - before);
        engine.Perform(FabricatorActionIds.DualFeedFirst).Performed.Should().BeTrue();
        engine.Part(SubAssembly.Frame).Progress.Should().Be(setup.FrameSize);
    }

    [Test]
    public void DualFeedIsDisabledWhenEitherSubAssemblyIsAlreadyComplete()
    {
        var setup = Perked(DualFeed);
        var engine = Engine(setup, Enumerable.Repeat(NoPrime, 20).ToArray());
        while (engine.Part(SubAssembly.Frame).Progress < setup.FrameSize)
            engine.Perform(FabricatorActionIds.RunFrame);

        var withFrame = engine.Preview(FabricatorActionIds.DualFeedFirst);
        var withShell = engine.Preview(FabricatorActionIds.DualFeedSecond);

        withFrame.Enabled.Should().BeFalse();
        withFrame.DisabledReason.Should().Be("The Frame is already complete.");
        withShell.Enabled.Should().BeTrue("the Frame is not part of the Fittings and Shell feed");
        engine.Perform(FabricatorActionIds.DualFeedFirst).Performed.Should().BeFalse();

        while (engine.Part(SubAssembly.Fittings).Progress < setup.FittingsSize)
            engine.Perform(FabricatorActionIds.RunFittings);

        engine.Preview(FabricatorActionIds.DualFeedSecond).DisabledReason.Should().Be("The Fittings is already complete.");
    }

    [Test]
    public void DualFeedNeedsTheCpOfARetool()
    {
        var setup = Perked(DualFeed, gearCp: 0, rank: 0, level: 25);
        var engine = Engine(setup, NoPrime);

        setup.MaxCp.Should().BeLessThan(setup.RetoolCp);
        var preview = engine.Preview(FabricatorActionIds.DualFeedFirst);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Not enough CP.");
        engine.Perform(FabricatorActionIds.DualFeedFirst).Performed.Should().BeFalse();
    }

    [Test]
    public void DualFeedCostsWhatQuickChangeMakesARetoolCost()
    {
        var setup = Perked(new FabricatorPerkStats(DualFeedUnlocked: true, RetoolCpReductionPercent: 50));

        setup.RetoolCp.Should().Be(3);
        Engine(setup).Preview(FabricatorActionIds.DualFeedSecond).CpCost.Should().Be(3);
    }

    [Test]
    public void DualFeedUsesAndSpendsThePrimeOfTheLoadedHeadOnly()
    {
        var setup = Perked(DualFeed);
        var announced = Engine(setup, PrimeFirst, NoPrime, NoPrime);
        announced.Perform(FabricatorActionIds.RunFittings);
        announced.PrimedHead.Should().Be(FabricatorHead.Cutter);

        // The Cutter is primed but the Bonder is loaded: Dual Feed gets no bonus and the prime stays.
        var preview = announced.Preview(FabricatorActionIds.DualFeedFirst);
        preview.Outcomes[0].Change(FabricatorEngine.FrameProgressId)!.Delta.Should().Be(setup.DualFeedGain(false));
        announced.Perform(FabricatorActionIds.DualFeedFirst).Performed.Should().BeTrue();
        announced.PrimedHead.Should().Be(FabricatorHead.Cutter);
        announced.PrimeActionsLeft.Should().Be(1);

        // Retool to the primed Cutter and feed: the bonus applies to both and the prime is spent.
        var engine = Engine(setup, PrimeFirst, NoPrime, NoPrime);
        engine.Perform(FabricatorActionIds.RunFittings);
        engine.Perform(FabricatorActionIds.RetoolCutter);
        var boosted = engine.Preview(FabricatorActionIds.DualFeedFirst);
        boosted.Outcomes[0].Label.Should().Contain("Prime Feedstock");
        boosted.Outcomes[0].Change(FabricatorEngine.FrameProgressId)!.Delta.Should().Be(setup.DualFeedGain(true));
        boosted.Outcomes[0].Change(FabricatorEngine.ShellProgressId)!.Delta.Should().Be(setup.DualFeedGain(true));
        setup.DualFeedGain(true).Should().BeGreaterThan(setup.DualFeedGain(false));
        engine.Perform(FabricatorActionIds.DualFeedFirst).Performed.Should().BeTrue();
        engine.PrimedHead.Should().BeNull();
    }

    [Test]
    public void DualFeedBeatsTwoRunsOnTimeAndLosesToAPlainRunWhenOneSubAssemblyIsNearlyDone()
    {
        var setup = Perked(DualFeed);
        var engine = Engine(setup, Enumerable.Repeat(NoPrime, 20).ToArray());

        // Two sub-assemblies with plenty of work: one Dual Feed (334 in all) against one Run (279).
        var dual = engine.Preview(FabricatorActionIds.DualFeedFirst).Outcomes[0].Delta.Progress;
        var run = engine.Preview(FabricatorActionIds.RunFittings).Outcomes[0].Delta.Progress;
        dual.Should().BeGreaterThan(run);

        // With the Frame nearly done a Dual Feed adds only what the Frame has room for.
        while (engine.Part(SubAssembly.Frame).Progress + setup.RunGain(false, false) < setup.FrameSize)
            engine.Perform(FabricatorActionIds.RunFrame);
        var nearly = engine.Preview(FabricatorActionIds.DualFeedFirst).Outcomes[0].Delta.Progress;
        nearly.Should().BeLessThan(dual);
    }

    // ---- previews equal results with every perk ----------------------------------------------------------------

    private static readonly FabricatorPerkStats Everything = new(
        RunPowerPercent: 25, PrecisionPowerPercent: -25, WearReduction: 1, RetoolCpReductionPercent: 50, ServiceRestoreBonus: 20,
        ServiceCpReduction: 4, DualFeedUnlocked: true, PrimeWindowBonusActions: 1, PrimeBonusPercentBonus: 25, AssembleCapBonusPoints: 15);

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void EveryPerformedActionMatchesItsPreview_WithEveryBuilderPerk(int seed)
    {
        var setup = Perked(Everything, parameters: Parameters(wear: 5));
        var engine = new FabricatorEngine(new SeededMinigameRandom(seed), setup);
        var chooser = new Random(seed);

        for (var step = 0; step < 80 && engine.Status == MinigameStatus.InProgress; step++)
        {
            var enabled = engine.PreviewAll().Where(p => p.Enabled).ToList();
            if (enabled.Count == 0)
                break;

            var preview = enabled[chooser.Next(enabled.Count)];
            var before = engine.DescribeMeters();
            var result = engine.Perform(preview.Action.Id);

            result.Performed.Should().BeTrue();
            result.Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
            result.Status.Should().Be(preview.Outcomes[0].ResultingStatus);
            var after = engine.DescribeMeters();
            foreach (var change in preview.Outcomes[0].Changes)
            {
                after.Single(m => m.Id == change.Id).Value.Should().Be(change.After, $"{preview.Action.Id} {change.Id}");
                before.Single(m => m.Id == change.Id).Value.Should().Be(change.Before);
            }
        }
    }

    [Test]
    public void AForkOfAPerkedEngineKeepsItsPerksAndItsDualFeed()
    {
        var engine = Engine(Perked(Everything), NoPrime, NoPrime);
        engine.Perform(FabricatorActionIds.DualFeedFirst).Performed.Should().BeTrue();

        var fork = (FabricatorEngine)engine.Fork(new ScriptedMinigameRandom(NoPrime));

        fork.Actions.Select(action => action.Id).Should().Equal(engine.Actions.Select(action => action.Id));
        fork.Preview(FabricatorActionIds.DualFeedSecond).Outcomes[0].Delta.Should().Be(engine.Preview(FabricatorActionIds.DualFeedSecond).Outcomes[0].Delta);
        fork.Perform(FabricatorActionIds.DualFeedSecond).Performed.Should().BeTrue();
        engine.Turn.Should().Be(1);
    }

    // ---- the window's words ---------------------------------------------------------------------------------------

    [Test]
    public void TheWindowListsTheNumbersTheCraftersPerksChange_OrSaysTheyHaveNone()
    {
        FabricatorActionText.PerkSummary(FabricatorPerkStats.None).Should().Be("Builder perks: none.");
        FabricatorActionText.PerkSummary(Everything).Should().Be(
            "Builder perks: Run +25%, Precision Pass -25%, wear -1, Retool CP -50%, Service restores +20, Service CP -4, Dual Feed, " +
            "Prime Feedstock waits 1 more action, Prime Feedstock +25% stronger, Assemble cap +15.");
        FabricatorActionText.PerkSummary(new FabricatorPerkStats(PrimeWindowBonusActions: 2)).Should().Contain("waits 2 more actions");
    }

    [Test]
    public void TheWindowBuildsItsSetupFromTheCraftersStatTotals_AndShowsTheirPerks()
    {
        var viewModel = ReadSource("Feature", "GuiDefinition", "ViewModel", "FabricatorCraftViewModel.cs");
        var definition = ReadSource("Feature", "GuiDefinition", "FabricatorCraftDefinition.cs");

        viewModel.Should().Contain("FabricatorPerkStats.FromStatTotals(stat => Stat.GetStatAdjustment(Player, stat))");
        viewModel.Should().Contain("FabricatorActionText.PerkSummary(setup.PerkStats)");
        viewModel.Should().Contain("setup.AssembleCapPoints", "the quality line shows the crafter's cap, not the unperked one");
        viewModel.Should().NotContain("FabricatorRules.AssembleCapPoints");
        definition.Should().Contain("model => model.PerkSummary");
        definition.Should().Contain("ActionListHeight = 11 * ActionRowHeight", "Dual Feed adds two rows to the nine");
    }

    [Test]
    public void AllPerkedPlayerFacingTextIsAsciiPunctuation()
    {
        var engine = Engine(Perked(Everything), NoPrime, NoPrime, NoPrime);
        var texts = new List<string> { FabricatorActionText.PerkSummary(Everything) };
        foreach (var preview in engine.PreviewAll())
        {
            texts.Add(FabricatorActionText.ButtonText(preview));
            texts.Add(FabricatorActionText.Tooltip(preview, 0));
            texts.Add(preview.Action.Description);
        }

        foreach (var action in engine.Actions)
            texts.Add(action.Description);

        foreach (var text in texts)
            text.Should().MatchRegex("^[\\x20-\\x7E\\n]*$", "game text is ASCII");
    }

    private static string ReadSource(params string[] relativePath)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return File.ReadAllText(relativePath.Aggregate(Path.Combine(directory!.FullName, "SWLOR.Game.Server"), Path.Combine));
    }
}
