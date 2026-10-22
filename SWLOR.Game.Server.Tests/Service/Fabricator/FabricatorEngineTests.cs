using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using static SWLOR.Game.Server.Tests.Service.Fabricator.FabricatorTestData;

namespace SWLOR.Game.Server.Tests.Service.Fabricator;

/// <summary>
/// Covers every Fabricator action, its preview and result, Prime Feedstock, the unlock ranks, the quality cap and the way an attempt
/// ends. Pinned numbers come from <see cref="FabricatorTestData.Setup"/>; the other expectations are built from the setup itself so a
/// tuning pass changes only the pinned group.
/// </summary>
public class FabricatorEngineTests
{
    private static readonly string[] AllActions =
    {
        FabricatorActionIds.RunFrame, FabricatorActionIds.RunShell, FabricatorActionIds.RunFittings, FabricatorActionIds.PrecisionPass,
        FabricatorActionIds.RetoolCutter, FabricatorActionIds.RetoolExtruder, FabricatorActionIds.RetoolBonder,
        FabricatorActionIds.Service, FabricatorActionIds.Assemble
    };

    private static MinigameMeterView Meter(FabricatorEngine engine, string id) => engine.DescribeMeters().Single(m => m.Id == id);

    private static string ProgressMeter(SubAssembly part) => FabricatorEngine.ProgressId(part);

    private static string QualityMeter(SubAssembly part) => FabricatorEngine.QualityId(part);

    // ---- setup and meters -------------------------------------------------------------------------------------------

    [Test]
    public void Meters_StartAtTheRecipeSizes_WithTheQualityCapMarkedOnTheQualityMeter()
    {
        var setup = Setup();
        var engine = Engine(setup);

        var meters = engine.DescribeMeters();

        meters.Select(meter => meter.Id).Should().Equal(
            MinigameMeterIds.Progress, MinigameMeterIds.Quality, MinigameMeterIds.Integrity, MinigameMeterIds.Cp,
            "frame_progress", "shell_progress", "fittings_progress", "frame_quality", "shell_quality", "fittings_quality");
        meters.Select(meter => meter.Label).Should().Equal(
            "Build progress", "Quality", "Machine Condition", "CP",
            "Frame progress", "Shell progress", "Fittings progress", "Frame quality", "Shell quality", "Fittings quality");
        meters.Select(meter => (meter.Value, meter.Max)).Should().Equal(
            (0, setup.MaxProgress), (0, 100), (100, 100), (69, 69),
            (0, setup.FrameSize), (0, setup.ShellSize), (0, setup.FittingsSize), (0, 100), (0, 100), (0, 100));
        Meter(engine, MinigameMeterIds.Quality).Mark.Should().Be(
            MinigameMeterMark.Line(FabricatorRules.AssembleCapPoints, $"Weakest + {FabricatorRules.AssembleCapPoints}"));
        engine.Type.Should().Be(CraftMinigameType.Fabricator);
        engine.Status.Should().Be(MinigameStatus.InProgress);
        engine.NextOpportunity.Should().BeNull();
        engine.Actions.Select(action => action.Id).Should().Equal(AllActions);
    }

    [Test]
    public void TheWorkIsDividedBetweenTheSubAssembliesByTheRecipesSizes_WithNothingLostToRounding()
    {
        var setup = Setup();

        (setup.FrameSize + setup.ShellSize + setup.FittingsSize).Should().Be(BaseProgress * FabricatorRules.ProgressScale);
        setup.FrameSize.Should().BeInRange((int)(BaseProgress * FabricatorRules.ProgressScale * 0.30) - 1, (int)(BaseProgress * FabricatorRules.ProgressScale * 0.30) + 1);
        setup.FittingsSize.Should().BeGreaterThan(setup.FrameSize);
        setup.MaxProgress.Should().Be(setup.FrameSize + setup.ShellSize + setup.FittingsSize);
        setup.SizeOf(SubAssembly.Frame).Should().Be(setup.FrameSize);
        setup.SizeOf(SubAssembly.Shell).Should().Be(setup.ShellSize);
        setup.SizeOf(SubAssembly.Fittings).Should().Be(setup.FittingsSize);
    }

    [Test]
    public void EnhancementsAddToWhatThePieceNeeds_WithoutSlowingARun()
    {
        var plain = Setup();
        var enhanced = Setup(penalty: 15);

        enhanced.MaxProgress.Should().Be(plain.MaxProgress + 15 * FabricatorRules.ProgressScale);
        enhanced.RunGain(true, false).Should().Be(plain.RunGain(true, false), "enhancements add to what is needed, not to what a Run does");
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
        var setup = new FabricatorSetup(null, 10, 45, 10, 0, 0, 0);

        setup.Parameters.Should().BeSameAs(FabricatorParameters.Fallback);
        setup.WearRate.Should().Be(FabricatorParameters.Fallback.WearRate);
    }

    [Test]
    public void TheFabricatorStartsWithTheHeadOfTheSubAssemblyTheQualityWeighsMost_AndTheCutterOnATie()
    {
        Engine(Setup(parameters: Parameters(frameQuality: 50, shellQuality: 30))).ActiveHead.Should().Be(FabricatorHead.Cutter);
        Engine(Setup(parameters: Parameters(frameQuality: 20, shellQuality: 50))).ActiveHead.Should().Be(FabricatorHead.Extruder);
        Engine(Setup(parameters: Parameters(frameQuality: 20, shellQuality: 30))).ActiveHead.Should().Be(FabricatorHead.Bonder);
        Engine(Setup(parameters: Parameters(frameQuality: 34, shellQuality: 33))).ActiveHead.Should().Be(FabricatorHead.Cutter);
    }

    [Test]
    public void EachHeadServesOneSubAssembly()
    {
        FabricatorHead.Cutter.Serves().Should().Be(SubAssembly.Frame);
        FabricatorHead.Extruder.Serves().Should().Be(SubAssembly.Shell);
        FabricatorHead.Bonder.Serves().Should().Be(SubAssembly.Fittings);
        SubAssembly.Frame.HeadFor().Should().Be(FabricatorHead.Cutter);
        SubAssembly.Shell.HeadFor().Should().Be(FabricatorHead.Extruder);
        SubAssembly.Fittings.HeadFor().Should().Be(FabricatorHead.Bonder);
    }

    [TestCase(22, 0.85)]
    [TestCase(25, 1.00)]
    [TestCase(30, 1.25)]
    [TestCase(45, 1.25)]
    public void RankAgainstTheRecipe_PenalizesBelowAndRewardsAboveForRun_UpToFiveRanks(int rank, double factor)
    {
        Setup(rank: rank, craftsmanship: 0).ProgressFactor.Should().BeApproximately(factor, 1e-9);
    }

    [TestCase(22, 0.85)]
    [TestCase(25, 1.00)]
    [TestCase(45, 1.00)]
    public void RankAgainstTheRecipe_PenalizesPrecisionBelowButNeverRewardsAbove(int rank, double factor)
    {
        Setup(rank: rank, control: 0).QualityFactor.Should().BeApproximately(factor, 1e-9);
    }

    [Test]
    public void CraftsmanshipPowersRun_AndControlPowersPrecisionPass_OnePercentPerPoint()
    {
        var plain = Setup(craftsmanship: 0, control: 0);

        Setup(craftsmanship: 30, control: 0).RunGain(true, false).Should().BeGreaterThan(plain.RunGain(true, false));
        Setup(craftsmanship: 30, control: 0).PrecisionGain(false).Should().Be(plain.PrecisionGain(false));
        Setup(craftsmanship: 0, control: 30).PrecisionGain(false).Should().BeGreaterThan(plain.PrecisionGain(false));
        Setup(craftsmanship: 0, control: 30).RunGain(true, false).Should().Be(plain.RunGain(true, false));
        Setup(craftsmanship: 30, control: 0).ProgressFactor.Should().BeApproximately(1.3, 1e-9);
        Setup(craftsmanship: 0, control: 30).QualityFactor.Should().BeApproximately(1.3, 1e-9);
    }

    [Test]
    public void RetoolAndServiceCostMoreCpAtHigherRecipeLevels()
    {
        Setup(level: 5, rank: 5).RetoolCp.Should().BeLessThan(Setup(level: 50, rank: 50).RetoolCp);
        Setup(level: 5, rank: 5).ServiceCp.Should().BeLessThan(Setup(level: 50, rank: 50).ServiceCp);
        Setup(level: 1, rank: 1).RetoolCp.Should().BeGreaterThan(0);
        Setup().RetoolCp.Should().Be(FabricatorRules.RetoolCp(25));
        Setup().ServiceCp.Should().Be(FabricatorRules.ServiceCp(25));
    }

    // ---- Run ---------------------------------------------------------------------------------------------------------

    [Test]
    public void Run_WithTheSubAssembliesOwnHead_BuildsItAtFullEffect_AndCostsOnlyWear()
    {
        var setup = Setup();
        var engine = Engine(setup, NoPrime);
        engine.ActiveHead.Should().Be(FabricatorHead.Bonder);

        var preview = engine.Preview(FabricatorActionIds.RunFittings);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(0);
        preview.IntegrityCost.Should().Be(setup.WearRate);
        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].ChancePercent.Should().Be(100);
        preview.Outcomes[0].Change(FabricatorEngine.FittingsProgressId)!.Delta.Should().Be(setup.RunGain(true, false));
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Progress: setup.RunGain(true, false), Integrity: -setup.WearRate));
        preview.FinishesItem.Should().BeFalse();

        var result = engine.Perform(FabricatorActionIds.RunFittings);

        result.Performed.Should().BeTrue();
        result.Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        engine.Part(SubAssembly.Fittings).Progress.Should().Be(setup.RunGain(true, false));
        engine.Meters.Progress.Value.Should().Be(setup.RunGain(true, false));
        engine.Meters.Integrity.Value.Should().Be(100 - setup.WearRate);
        engine.Part(SubAssembly.Frame).Progress.Should().Be(0);
    }

    [Test]
    public void Run_WithAnotherHead_GivesHalfTheEffect_WithoutRetooling()
    {
        var setup = Setup();
        var engine = Engine(setup, NoPrime);

        var preview = engine.Preview(FabricatorActionIds.RunFrame);

        setup.RunGain(false, false).Should().Be((int)Math.Round(setup.RunGain(true, false) / 2.0, MidpointRounding.AwayFromZero));
        preview.Outcomes[0].Change(FabricatorEngine.FrameProgressId)!.Delta.Should().Be(setup.RunGain(false, false));
        preview.CpCost.Should().Be(0);

        engine.Perform(FabricatorActionIds.RunFrame).Performed.Should().BeTrue();

        engine.ActiveHead.Should().Be(FabricatorHead.Bonder, "an off-head Run does not change heads");
        engine.Part(SubAssembly.Frame).Progress.Should().Be(setup.RunGain(false, false));
    }

    [Test]
    public void Run_StopsAtTheSubAssembliesSize_AndIsThenDisabled()
    {
        var setup = Setup();
        var engine = Engine(setup, Enumerable.Repeat(NoPrime, 20).ToArray());

        while (engine.Part(SubAssembly.Fittings).Progress < setup.FittingsSize)
        {
            var before = engine.Part(SubAssembly.Fittings).Progress;
            var preview = engine.Preview(FabricatorActionIds.RunFittings);
            preview.Outcomes[0].Change(FabricatorEngine.FittingsProgressId)!.Delta.Should().Be(
                Math.Min(setup.RunGain(true, false), setup.FittingsSize - before), "the preview shows the progress that is actually added");
            engine.Perform(FabricatorActionIds.RunFittings).Performed.Should().BeTrue();
        }

        engine.Part(SubAssembly.Fittings).Progress.Should().Be(setup.FittingsSize);
        var done = engine.Preview(FabricatorActionIds.RunFittings);
        done.Enabled.Should().BeFalse();
        done.DisabledReason.Should().Be("The Fittings is already complete.");
        engine.Perform(FabricatorActionIds.RunFittings).Performed.Should().BeFalse();
    }

    [Test]
    public void TheProgressMeterIsTheSumOfTheSubAssemblies_AndFillingItDoesNotFinishTheItem()
    {
        var setup = Setup();
        var engine = Built(setup);

        engine.Meters.Progress.Value.Should().Be(setup.MaxProgress);
        engine.Meters.Progress.IsFull.Should().BeTrue();
        engine.Meters.Progress.Value.Should().Be(
            engine.Part(SubAssembly.Frame).Progress + engine.Part(SubAssembly.Shell).Progress + engine.Part(SubAssembly.Fittings).Progress);
        engine.Status.Should().Be(MinigameStatus.InProgress, "the craft succeeds on Assemble, not on a full progress meter");
        engine.IsComplete.Should().BeTrue();
    }

    // ---- Precision Pass ----------------------------------------------------------------------------------------------

    [Test]
    public void PrecisionPass_RaisesTheQualityOfTheLoadedHeadsSubAssemblyOnly_AndNeedsSomeProgressFirst()
    {
        var setup = Setup();
        var engine = Engine(setup, NoPrime, NoPrime);

        var early = engine.Preview(FabricatorActionIds.PrecisionPass);
        early.Enabled.Should().BeFalse();
        early.DisabledReason.Should().Be("Run the Fittings first: there is nothing to refine yet.");
        engine.Perform(FabricatorActionIds.PrecisionPass).Performed.Should().BeFalse();

        engine.Perform(FabricatorActionIds.RunFittings);
        var preview = engine.Preview(FabricatorActionIds.PrecisionPass);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(0);
        preview.IntegrityCost.Should().Be(setup.WearRate);
        preview.Outcomes[0].Change(FabricatorEngine.FittingsQualityId)!.Delta.Should().Be(setup.PrecisionGain(false));
        preview.Outcomes[0].Change(FabricatorEngine.FrameQualityId)!.Delta.Should().Be(0);

        var result = engine.Perform(FabricatorActionIds.PrecisionPass);

        result.Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        engine.Part(SubAssembly.Fittings).Quality.Should().Be(setup.PrecisionGain(false));
        engine.Part(SubAssembly.Frame).Quality.Should().Be(0);
        engine.Part(SubAssembly.Shell).Quality.Should().Be(0);
    }

    [Test]
    public void PrecisionPass_StopsAtFullQuality_AndIsThenDisabled()
    {
        var engine = Built(Setup());
        engine.Perform(FabricatorActionIds.RetoolBonder).Performed.Should().BeFalse("the Bonder is already loaded after building the Fittings last");
        while (engine.Part(SubAssembly.Fittings).Quality < FabricatorRules.MaxQuality)
            engine.Perform(FabricatorActionIds.PrecisionPass).Performed.Should().BeTrue();

        engine.Part(SubAssembly.Fittings).Quality.Should().Be(FabricatorRules.MaxQuality);
        var preview = engine.Preview(FabricatorActionIds.PrecisionPass);
        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("The Fittings is already at full quality.");
    }

    [Test]
    public void ThePrecisionPassOfAnotherHeadIsNotPossibleOffHead_SoRefiningAllThreeMeansRetooling()
    {
        var setup = Setup();
        var engine = Built(setup);

        engine.Perform(FabricatorActionIds.PrecisionPass);
        engine.Part(SubAssembly.Frame).Quality.Should().Be(0);

        engine.Perform(FabricatorActionIds.RetoolCutter).Performed.Should().BeTrue();
        engine.Perform(FabricatorActionIds.PrecisionPass).Performed.Should().BeTrue();

        engine.Part(SubAssembly.Frame).Quality.Should().Be(setup.PrecisionGain(false));
    }

    // ---- Retool ------------------------------------------------------------------------------------------------------

    [Test]
    public void Retool_LoadsTheHead_CostsCpAndALittleWear_AndTheLoadedHeadCannotBeRetooledTo()
    {
        var setup = Setup();
        var engine = Engine(setup, NoPrime);

        var loaded = engine.Preview(FabricatorActionIds.RetoolBonder);
        loaded.Enabled.Should().BeFalse();
        loaded.DisabledReason.Should().Be("The Bonder is already loaded.");

        var preview = engine.Preview(FabricatorActionIds.RetoolCutter);
        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(setup.RetoolCp);
        preview.IntegrityCost.Should().Be(FabricatorRules.RetoolWear);
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Integrity: -FabricatorRules.RetoolWear, Cp: -setup.RetoolCp));

        var result = engine.Perform(FabricatorActionIds.RetoolCutter);

        result.Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        engine.ActiveHead.Should().Be(FabricatorHead.Cutter);
        engine.Meters.Cp.Value.Should().Be(setup.MaxCp - setup.RetoolCp);
    }

    [Test]
    public void Retool_NeedsTheCp_AndARefusedActionChangesNothing()
    {
        var setup = Setup(rank: 25, gearCp: 0, level: 60);
        var random = new ScriptedMinigameRandom(NoPrime, NoPrime);
        var engine = new FabricatorEngine(random, setup);
        while (engine.Meters.Cp.Value >= setup.RetoolCp)
        {
            engine.Perform(FabricatorActionIds.RetoolCutter);
            engine.Perform(FabricatorActionIds.RetoolExtruder);
            random.Enqueue(NoPrime);
            random.Enqueue(NoPrime);
        }

        var preview = engine.Preview(FabricatorActionIds.RetoolBonder);
        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Not enough CP.");
        var meters = engine.DescribeMeters();
        var turn = engine.Turn;
        var calls = random.Calls;

        var refused = engine.Perform(FabricatorActionIds.RetoolBonder);

        refused.Performed.Should().BeFalse();
        refused.RejectedReason.Should().Be("Not enough CP.");
        refused.Outcome.Should().BeNull();
        engine.DescribeMeters().Should().Equal(meters);
        engine.Turn.Should().Be(turn);
        random.Calls.Should().Be(calls, "a refused action rolls nothing, not even a forecast");
    }

    // ---- Service -----------------------------------------------------------------------------------------------------

    [Test]
    public void Service_IsLockedBelowRankTen_AndTheOnlyActionThatIs()
    {
        var engine = Engine(Setup(rank: 9, level: 9));

        foreach (var action in AllActions.Where(id => id != FabricatorActionIds.Service))
            engine.Preview(action).DisabledReason.Should().NotContain("Unlocks", action);

        var service = engine.Preview(FabricatorActionIds.Service);
        service.Enabled.Should().BeFalse();
        service.DisabledReason.Should().Be("Unlocks at Fabrication rank 10.");
        engine.Perform(FabricatorActionIds.Service).Performed.Should().BeFalse();
        FabricatorSetup.UnlockRank(FabricatorActionIds.Service).Should().Be(10);
        AllActions.Where(id => id != FabricatorActionIds.Service).Should().OnlyContain(id => FabricatorSetup.UnlockRank(id) == 0);
        FabricatorSetup.UnlockRank("bogus").Should().Be(-1);
    }

    [Test]
    public void Service_RestoresMachineCondition_CostsCp_AndNeedsSomethingToRestore()
    {
        var setup = Setup();
        var engine = Engine(setup, Enumerable.Repeat(NoPrime, 10).ToArray());

        var full = engine.Preview(FabricatorActionIds.Service);
        full.Enabled.Should().BeFalse();
        full.DisabledReason.Should().Be("Machine Condition is already full.");

        for (var i = 0; i < 4; i++)
            engine.Perform(FabricatorActionIds.RunFrame);
        var before = engine.Meters.Integrity.Value;
        var preview = engine.Preview(FabricatorActionIds.Service);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(setup.ServiceCp);
        preview.IntegrityCost.Should().Be(0);
        var restored = Math.Min(FabricatorRules.ServiceRestore, FabricatorRules.MaxMachineCondition - before);
        restored.Should().BeGreaterThan(0);
        preview.Outcomes[0].Delta.Should().Be(new MinigameMeterDelta(Integrity: restored, Cp: -setup.ServiceCp));

        var result = engine.Perform(FabricatorActionIds.Service);

        result.Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        engine.Meters.Integrity.Value.Should().Be(before + restored);
        engine.Meters.Cp.Value.Should().Be(setup.MaxCp - setup.ServiceCp);
    }

    // ---- Assemble and quality ----------------------------------------------------------------------------------------

    [Test]
    public void Assemble_IsDisabledUntilAllThreeSubAssembliesAreComplete()
    {
        var engine = Engine(Setup(), Enumerable.Repeat(NoPrime, 20).ToArray());
        engine.Preview(FabricatorActionIds.Assemble).DisabledReason.Should().Be("Complete the Frame, the Shell and the Fittings first.");

        while (engine.Part(SubAssembly.Fittings).Progress < engine.Setup.FittingsSize)
            engine.Perform(FabricatorActionIds.RunFittings);
        engine.Preview(FabricatorActionIds.Assemble).Enabled.Should().BeFalse("the Fittings alone are not enough");
        engine.Perform(FabricatorActionIds.Assemble).Performed.Should().BeFalse();
        engine.Status.Should().Be(MinigameStatus.InProgress);
    }

    [Test]
    public void Assemble_FinishesTheItem_AndItsPreviewWarnsThatItDoes()
    {
        var engine = Built(Setup());
        Refine(engine);

        var preview = engine.Preview(FabricatorActionIds.Assemble);

        preview.Enabled.Should().BeTrue();
        preview.FinishesItem.Should().BeTrue();
        preview.CanBreakItem.Should().BeFalse();
        preview.CpCost.Should().Be(0);
        preview.IntegrityCost.Should().Be(0);
        preview.Outcomes.Should().ContainSingle().Which.ResultingStatus.Should().Be(MinigameStatus.Succeeded);

        var result = engine.Perform(FabricatorActionIds.Assemble);

        result.Performed.Should().BeTrue();
        result.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Meters.Quality.IsFull.Should().BeTrue();
        engine.Meters.QualityPercent.Should().Be(100);
        engine.PreviewAll().Should().OnlyContain(p => !p.Enabled && p.DisabledReason == "The attempt is over.");
    }

    [Test]
    public void OnlyAssembleFinishesTheItem()
    {
        var engine = Built(Setup());
        Refine(engine);

        foreach (var preview in engine.PreviewAll().Where(p => p.Action.Id != FabricatorActionIds.Assemble))
            preview.FinishesItem.Should().BeFalse(preview.Action.Id);
    }

    [Test]
    public void TheQualityMeterShowsTheWeightedAverage_AndAssembleCapsItAtTheWeakestPlusTwenty()
    {
        var setup = Setup(parameters: Parameters(frameQuality: 50, shellQuality: 30));
        var engine = Built(setup);
        // The Fittings are loaded: refine them to full, then the Shell, leaving the Frame at 0.
        while (engine.Part(SubAssembly.Fittings).Quality < 100)
            engine.Perform(FabricatorActionIds.PrecisionPass);
        engine.Perform(FabricatorActionIds.RetoolExtruder);
        while (engine.Part(SubAssembly.Shell).Quality < 100)
            engine.Perform(FabricatorActionIds.PrecisionPass);

        // Weighted average: Frame 0 x 50%, Shell 100 x 30%, Fittings 100 x 20%.
        engine.Meters.QualityPercent.Should().Be(50);
        engine.QualityCap.Should().Be(FabricatorRules.AssembleCapPoints);
        engine.AssembledQuality.Should().Be(FabricatorRules.AssembleCapPoints);

        var preview = engine.Preview(FabricatorActionIds.Assemble);
        preview.Outcomes[0].Change(MinigameMeterIds.Quality)!.Delta.Should().Be(FabricatorRules.AssembleCapPoints - 50);
        engine.Perform(FabricatorActionIds.Assemble);
        engine.Meters.QualityPercent.Should().Be(FabricatorRules.AssembleCapPoints);
        engine.Status.Should().Be(MinigameStatus.Succeeded);
    }

    [Test]
    public void LiftingTheWeakestSubAssemblyRaisesTheCap_AndTheCapNeverExceedsFullQuality()
    {
        var setup = Setup();

        setup.CapFor(0, 100, 100).Should().Be(FabricatorRules.AssembleCapPoints);
        setup.CapFor(60, 100, 100).Should().Be(60 + FabricatorRules.AssembleCapPoints);
        setup.CapFor(90, 100, 100).Should().Be(100);
        setup.PieceQuality(100, 100, 100).Should().Be(100);
        setup.PieceQuality(0, 0, 0).Should().Be(0);
    }

    [Test]
    public void AQualityShareWeighsEachSubAssembly_SoABedIsLiftedMostByItsFrame()
    {
        var bed = Setup(parameters: Parameters(frameSize: 55, shellSize: 30, frameQuality: 60, shellQuality: 25));

        bed.AverageQuality(100, 0, 0).Should().Be(60);
        bed.AverageQuality(0, 100, 0).Should().Be(25);
        bed.AverageQuality(0, 0, 100).Should().Be(15);
        bed.AverageQuality(100, 100, 100).Should().Be(100);
    }

    // ---- Prime Feedstock ---------------------------------------------------------------------------------------------

    [Test]
    public void EachHeadHasTheSameChanceToBePrimed_AndTheRollPicksTheHeadFromTheChanceBands()
    {
        var chance = FabricatorRules.PrimeChancePerHeadPercent;
        var heads = new[] { (1, FabricatorHead.Cutter), (chance, FabricatorHead.Cutter), (chance + 1, FabricatorHead.Extruder),
            (2 * chance, FabricatorHead.Extruder), (2 * chance + 1, FabricatorHead.Bonder), (3 * chance, FabricatorHead.Bonder) };

        foreach (var (roll, head) in heads)
        {
            var engine = Engine(Setup(), roll);
            engine.Perform(FabricatorActionIds.RunFittings);

            engine.PrimedHead.Should().Be(head, $"a roll of {roll}");
            engine.NextOpportunity.Should().NotBeNull();
            engine.NextOpportunity!.Id.Should().Be(FabricatorActionIds.PrimeFeedstock);
            engine.NextOpportunity.Name.Should().Be($"Prime Feedstock: {head}");
            engine.NextOpportunity.TurnsAway.Should().Be(1);
            engine.NextOpportunity.Description.Should().Contain($"+{FabricatorRules.PrimeBonusPercent}%");
            engine.PrimeActionsLeft.Should().Be(FabricatorRules.PrimeWindowActions);
        }

        var none = Engine(Setup(), 3 * chance + 1);
        none.Perform(FabricatorActionIds.RunFittings);
        none.PrimedHead.Should().BeNull();
        none.NextOpportunity.Should().BeNull();
    }

    [Test]
    public void AHeadWithNoWorkLeftIsNeverNamed()
    {
        var setup = Setup();
        var engine = Built(setup);
        Refine(engine);
        // Everything is complete and at full quality, so no head has any work.
        var random = new ScriptedMinigameRandom(PrimeFirst);
        var fresh = new FabricatorEngine(random, setup);
        fresh.Perform(FabricatorActionIds.RunFittings);
        fresh.PrimedHead.Should().Be(FabricatorHead.Cutter);

        engine.NextOpportunity.Should().BeNull();
        engine.PrimedHead.Should().BeNull();
    }

    [Test]
    public void PrimeFeedstockGivesTheNamedHeadsNextAction_TheBonus_AndIsThenUsedUp()
    {
        var setup = Setup(penalty: 100);
        var engine = Engine(setup, PrimeThird, NoPrime, NoPrime);
        engine.Perform(FabricatorActionIds.RunFrame);
        engine.PrimedHead.Should().Be(FabricatorHead.Bonder, "the Bonder is the third head with work");
        engine.ActiveHead.Should().Be(FabricatorHead.Bonder);

        var preview = engine.Preview(FabricatorActionIds.RunFittings);
        preview.Outcomes[0].Change(FabricatorEngine.FittingsProgressId)!.Delta.Should().Be(setup.RunGain(true, true));
        preview.Outcomes[0].Label.Should().Contain("Prime Feedstock");
        setup.RunGain(true, true).Should().BeGreaterThan(setup.RunGain(true, false));

        var result = engine.Perform(FabricatorActionIds.RunFittings);

        result.Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        engine.Part(SubAssembly.Fittings).Progress.Should().Be(setup.RunGain(true, true));
        engine.PrimedHead.Should().BeNull("the bonus is used once");
        engine.NextOpportunity.Should().BeNull();
        engine.Preview(FabricatorActionIds.RunFittings).Outcomes[0].Label.Should().NotContain("Prime Feedstock");
    }

    [Test]
    public void PrimeFeedstockAlsoBoostsThePrecisionPassOfTheNamedHead()
    {
        var setup = Setup(penalty: 100);
        var engine = Engine(setup, NoPrime, PrimeThird, NoPrime);
        engine.Perform(FabricatorActionIds.RunFittings);
        engine.Perform(FabricatorActionIds.RunFittings);
        engine.PrimedHead.Should().Be(FabricatorHead.Bonder);

        var preview = engine.Preview(FabricatorActionIds.PrecisionPass);

        preview.Outcomes[0].Change(FabricatorEngine.FittingsQualityId)!.Delta.Should().Be(Math.Min(100, setup.PrecisionGain(true)));
        preview.Outcomes[0].Label.Should().Contain("Prime Feedstock");
        engine.Perform(FabricatorActionIds.PrecisionPass);
        engine.PrimedHead.Should().BeNull();
    }

    [Test]
    public void ARunOffHeadWithTheNamedHeadLoadedStillUsesThePrime_ButAnotherHeadsActionDoesNot()
    {
        var setup = Setup(penalty: 100);
        var engine = Engine(setup, PrimeThird, NoPrime, NoPrime);
        engine.Perform(FabricatorActionIds.RunFrame);

        engine.Preview(FabricatorActionIds.RunFrame).Outcomes[0].Change(FabricatorEngine.FrameProgressId)!.Delta.Should().Be(setup.RunGain(false, true));

        var other = Engine(setup, PrimeFirst, NoPrime, NoPrime);
        other.Perform(FabricatorActionIds.RunFittings);
        other.PrimedHead.Should().Be(FabricatorHead.Cutter);
        other.Preview(FabricatorActionIds.RunFittings).Outcomes[0].Change(FabricatorEngine.FittingsProgressId)!.Delta
            .Should().Be(setup.RunGain(true, false), "the Bonder is loaded, so the Cutter's prime does not apply");
        other.Preview(FabricatorActionIds.PrecisionPass).Outcomes[0].Label.Should().NotContain("Prime");
    }

    [Test]
    public void ARetoolToThePrimedHeadInTimeCatchesTheBonus_OnItsFirstActionAfterward()
    {
        var setup = Setup(penalty: 100);
        var engine = Engine(setup, PrimeFirst, NoPrime, NoPrime);
        engine.Perform(FabricatorActionIds.RunFittings);
        engine.PrimedHead.Should().Be(FabricatorHead.Cutter);
        engine.PrimeActionsLeft.Should().Be(FabricatorRules.PrimeWindowActions);

        engine.Perform(FabricatorActionIds.RetoolCutter);
        engine.PrimedHead.Should().Be(FabricatorHead.Cutter, "the retool itself does not use the prime");
        engine.PrimeActionsLeft.Should().Be(FabricatorRules.PrimeWindowActions - 1);

        var preview = engine.Preview(FabricatorActionIds.RunFrame);
        preview.Outcomes[0].Change(FabricatorEngine.FrameProgressId)!.Delta.Should().Be(setup.RunGain(true, true));
        engine.Perform(FabricatorActionIds.RunFrame);
        engine.PrimedHead.Should().BeNull();
        engine.Part(SubAssembly.Frame).Progress.Should().Be(setup.RunGain(true, true));
    }

    [Test]
    public void APrimeThatItsHeadDoesNotUseInTimePasses_AndTheOpportunityTextCountsDown()
    {
        var setup = Setup(penalty: 100);
        var engine = Engine(setup, PrimeFirst, NoPrime, NoPrime, NoPrime);
        engine.Perform(FabricatorActionIds.RunFittings);
        var announced = engine.NextOpportunity;
        announced!.Description.Should().Contain($"{FabricatorRules.PrimeWindowActions} more actions");

        engine.Perform(FabricatorActionIds.RunShell);
        engine.PrimedHead.Should().Be(FabricatorHead.Cutter);
        engine.NextOpportunity!.Description.Should().Contain("1 more action").And.NotContain("actions.");

        engine.Perform(FabricatorActionIds.RunShell);
        engine.PrimedHead.Should().BeNull("the window passed");
        engine.NextOpportunity.Should().BeNull();
    }

    [Test]
    public void NothingNewIsAnnouncedWhileAPrimeIsWaiting_AndTheForecastRollsNothingThen()
    {
        var random = new ScriptedMinigameRandom(PrimeFirst);
        var engine = new FabricatorEngine(random, Setup());
        engine.Perform(FabricatorActionIds.RunFittings);
        random.Calls.Should().Be(1);

        engine.Perform(FabricatorActionIds.RunFittings);

        random.Calls.Should().Be(1, "a waiting prime needs no forecast roll");
        engine.PrimedHead.Should().Be(FabricatorHead.Cutter);
    }

    [Test]
    public void APreviewDoesNotIncludeTheForecast_AndRollsNothing()
    {
        var random = new ScriptedMinigameRandom(PrimeFirst);
        var engine = new FabricatorEngine(random, Setup());

        engine.PreviewAll();

        random.Calls.Should().Be(0);
        engine.NextOpportunity.Should().BeNull();
    }

    // ---- failure and the end of the attempt -------------------------------------------------------------------------

    [Test]
    public void AnEmptyMachineConditionBreaksThePiece_AndThePreviewWarnsBeforeTheClick()
    {
        var setup = Setup(penalty: 5000, parameters: Parameters(wear: 9));
        var engine = Engine(setup, Enumerable.Repeat(NoPrime, 30).ToArray());
        for (var i = 0; i < 10; i++)
            engine.Perform(FabricatorActionIds.RunFittings).Performed.Should().BeTrue();
        engine.Meters.Integrity.Value.Should().Be(10);

        var preview = engine.Preview(FabricatorActionIds.RunFittings);
        preview.CanBreakItem.Should().BeFalse("10 Machine Condition covers a wear of 9");
        engine.Perform(FabricatorActionIds.RunFittings);
        engine.Meters.Integrity.Value.Should().Be(1);

        var last = engine.Preview(FabricatorActionIds.RunFittings);
        last.CanBreakItem.Should().BeTrue();
        last.Outcomes[0].ResultingStatus.Should().Be(MinigameStatus.Failed);

        var result = engine.Perform(FabricatorActionIds.RunFittings);

        result.Status.Should().Be(MinigameStatus.Failed);
        engine.Status.Should().Be(MinigameStatus.Failed);
        engine.Meters.Integrity.IsEmpty.Should().BeTrue();
        engine.Perform(FabricatorActionIds.RunFittings).Performed.Should().BeFalse();
    }

    [Test]
    public void AnAssembleAtLowMachineConditionStillSucceeds()
    {
        var engine = Built(Setup());
        engine.Meters.Integrity.Value.Should().BeGreaterThan(0);

        engine.Perform(FabricatorActionIds.Assemble).Status.Should().Be(MinigameStatus.Succeeded);
    }

    [Test]
    public void ARefusedActionChangesNothing_ForEveryActionTheEngineRefuses()
    {
        var random = new ScriptedMinigameRandom(NoPrime);
        var engine = new FabricatorEngine(random, Setup(rank: 5, level: 5));
        var meters = engine.DescribeMeters();

        foreach (var action in new[] { FabricatorActionIds.Service, FabricatorActionIds.Assemble, FabricatorActionIds.PrecisionPass, FabricatorActionIds.RetoolBonder })
        {
            var refused = engine.Perform(action);

            refused.Performed.Should().BeFalse(action);
            refused.Outcome.Should().BeNull(action);
        }

        engine.DescribeMeters().Should().Equal(meters);
        engine.Turn.Should().Be(0);
        random.Calls.Should().Be(0);
    }

    [Test]
    public void AnUnknownActionIsAnArgumentError()
    {
        var act = () => Engine(Setup()).Preview("bogus");

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void ASetupIsRequired()
    {
        var act = () => new FabricatorEngine(new ScriptedMinigameRandom(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ---- previews equal results, forks and whole crafts ---------------------------------------------------------------

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void EveryPerformedActionMatchesItsPreview_AcrossASeededRandomCraft(int seed)
    {
        var setup = Setup();
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
    public void ForkContinuesFromTheExactAttempt_AndEvolvesIndependently()
    {
        var setup = Setup();
        var original = Engine(setup, Enumerable.Repeat(NoPrime, 10).ToArray());
        original.Perform(FabricatorActionIds.RunFittings);
        original.Perform(FabricatorActionIds.PrecisionPass);

        var fork = (FabricatorEngine)original.Fork(new ScriptedMinigameRandom(PrimeFirst, NoPrime));
        fork.DescribeMeters().Should().Equal(original.DescribeMeters());
        fork.ActiveHead.Should().Be(original.ActiveHead);
        fork.Turn.Should().Be(original.Turn);

        fork.Perform(FabricatorActionIds.RetoolCutter);
        original.PrimedHead.Should().BeNull("the fork rolled its own announcement");
        fork.PrimedHead.Should().Be(FabricatorHead.Cutter);
        fork.Perform(FabricatorActionIds.RunFrame);

        original.ActiveHead.Should().Be(FabricatorHead.Bonder);
        original.Part(SubAssembly.Frame).Progress.Should().Be(0);
        fork.Part(SubAssembly.Frame).Progress.Should().BeGreaterThan(0);
    }

    [Test]
    public void ASimpleCraft_BuildsRefinesAndAssemblesWithinTheBudgets_AtEveryGear()
    {
        foreach (var (craftsmanship, control, gearCp) in new[] { (0, 0, 0), (12, 13, 26), (30, 29, 37) })
        {
            var setup = Setup(craftsmanship: craftsmanship, control: control, gearCp: gearCp, parameters: Parameters(wear: 3));
            var engine = Built(setup);
            Refine(engine);
            engine.Perform(FabricatorActionIds.Assemble).Performed.Should().BeTrue();

            engine.Status.Should().Be(MinigameStatus.Succeeded, $"gear {craftsmanship}/{control}/{gearCp}");
            engine.Meters.QualityPercent.Should().Be(100);
        }
    }

    // ---- pinned numbers: the group a tuning pass updates -------------------------------------------------------------

    [Test]
    public void PinnedNumbers_ForTheStandardSetup()
    {
        var setup = Setup();

        (setup.FrameSize, setup.ShellSize, setup.FittingsSize).Should().Be((267, 267, 356));
        setup.RunGain(true, false).Should().Be(279);
        setup.RunGain(false, false).Should().Be(140);
        setup.RunGain(true, true).Should().Be(419);
        setup.PrecisionGain(false).Should().Be(47);
        setup.PrecisionGain(true).Should().Be(71);
        setup.RetoolCp.Should().Be(5);
        setup.ServiceCp.Should().Be(10);
        setup.MaxCp.Should().Be(69);
    }
}
