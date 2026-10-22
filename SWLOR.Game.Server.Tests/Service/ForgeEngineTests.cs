using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Covers the Plasma Forge rules: every action's cost and result at each side of the working range, the previews, the
/// telegraphed Stable Containment, success, failure and the rank unlocks.
/// </summary>
public class ForgeEngineTests
{
    private const int Low = 35;
    private const int High = 65;

    /// <summary>A recipe with a 35-65 working range, no cooling, an even split and a crafter at the recipe's level without gear.</summary>
    private static ForgeSetup Setup(
        int startingThermal = 50,
        int cooling = 0,
        int shapeShare = 50,
        int rank = 20,
        int level = 20,
        int craftsmanship = 0,
        int control = 0,
        int gearCp = 0,
        int enhancementShapePercent = 0) =>
        new(new ForgeParameters(50, 30, cooling, startingThermal, shapeShare), level, rank, craftsmanship, control, gearCp, enhancementShapePercent);

    private static IMinigameRandom Quiet() => new ScriptedMinigameRandom(Enumerable.Repeat(100, 200).ToArray());

    private static ForgeEngine Engine(ForgeSetup setup = null, IMinigameRandom random = null) =>
        new(setup ?? Setup(), random ?? Quiet());

    private static MinigameActionPreview Preview(ForgeEngine engine, string action) => engine.Preview(action);

    private static MinigameOutcomePreview Only(ForgeEngine engine, string action) => engine.Preview(action).Outcomes.Single();

    private static void Press(ForgeEngine engine, int times)
    {
        for (var index = 0; index < times; index++)
            engine.Perform(ForgeActionIds.HydroPress).Performed.Should().BeTrue();
    }

    /// <summary>Presses until the piece is shaped enough to quench.</summary>
    private static void ShapeForQuench(ForgeEngine engine)
    {
        while (engine.Meters.Progress.Percent < ForgeRules.QuenchMinimumShapePercent)
            engine.Perform(ForgeActionIds.HydroPress).Performed.Should().BeTrue();

        engine.Status.Should().Be(MinigameStatus.InProgress);
    }

    /// <summary>A forge that has taken enough Shape for a quench: a no-cooling piece held at the given Thermal.</summary>
    private static ForgeEngine Shaped(int thermal)
    {
        var engine = Engine(Setup(startingThermal: thermal));
        ShapeForQuench(engine);
        return engine;
    }

    [Test]
    public void ANewCraft_StartsWithEmptyShapeAndFinishFullIntegrityAndTheRecipesThermal()
    {
        var engine = Engine(Setup(startingThermal: 44));

        var meters = engine.DescribeMeters();

        meters.Select(meter => meter.Id).Should().Equal(
            ForgeEngine.ThermalMeterId, MinigameMeterIds.Progress, MinigameMeterIds.Quality, MinigameMeterIds.Integrity, MinigameMeterIds.Cp);
        meters.Select(meter => meter.Label).Should().Equal("Thermal", "Shape", "Finish", "Integrity", "CP");
        meters.Single(meter => meter.Id == ForgeEngine.ThermalMeterId).Value.Should().Be(44);
        engine.Meters.Progress.Should().Be(new MinigameMeter(0, 1000));
        engine.Meters.Quality.Should().Be(new MinigameMeter(0, 1000));
        engine.Meters.Integrity.Should().Be(new MinigameMeter(100, 100));
        engine.Meters.Cp.Should().Be(new MinigameMeter(15, 15));
        engine.Type.Should().Be(CraftMinigameType.Forge);
        engine.Status.Should().Be(MinigameStatus.InProgress);
        engine.NextOpportunity.Should().BeNull();
    }

    [Test]
    public void TheWorkingRangeIsMarkedOnTheThermalMeter()
    {
        var thermal = Engine().DescribeMeters().Single(meter => meter.Id == ForgeEngine.ThermalMeterId);

        thermal.Min.Should().Be(0);
        thermal.Max.Should().Be(100);
        thermal.Mark.Should().Be(MinigameMeterMark.Range(Low, High, "Working range"));
    }

    [Test]
    public void TheWorkingRangeSpansTheWidthAroundTheCenter()
    {
        var even = new ForgeParameters(50, 30, 3, 50, 50);
        var odd = new ForgeParameters(50, 31, 3, 50, 50);

        (even.RangeLow, even.RangeHigh).Should().Be((35, 65));
        (odd.RangeLow, odd.RangeHigh).Should().Be((35, 66));
    }

    [TestCase(50, 1000, 1000)]
    [TestCase(60, 1200, 800)]
    [TestCase(40, 800, 1200)]
    [TestCase(35, 700, 1300)]
    public void TheShapeShareSplitsTheWorkBetweenShapeAndFinish(int shapeShare, int shape, int finish)
    {
        var engine = Engine(Setup(shapeShare: shapeShare));

        engine.Meters.Progress.Max.Should().Be(shape);
        engine.Meters.Quality.Max.Should().Be(finish);
    }

    [Test]
    public void ArmorNeedsMoreShapeThanWeaponsAndWeaponsMoreFinish()
    {
        var armor = Engine(Setup(shapeShare: 60));
        var weapon = Engine(Setup(shapeShare: 40));

        armor.Meters.Progress.Max.Should().BeGreaterThan(weapon.Meters.Progress.Max);
        weapon.Meters.Quality.Max.Should().BeGreaterThan(armor.Meters.Quality.Max);
    }

    [Test]
    public void EnhancementsRaiseTheShapeTheRecipeNeeds()
    {
        var plain = Engine(Setup());
        var enhanced = Engine(Setup(enhancementShapePercent: 30));

        enhanced.Meters.Progress.Max.Should().Be(1300);
        enhanced.Meters.Progress.Max.Should().BeGreaterThan(plain.Meters.Progress.Max);
        enhanced.Meters.Quality.Max.Should().Be(plain.Meters.Quality.Max);
    }

    [Test]
    public void AnEnhancementsProgressPenaltyIsComparedWithTheOriginalProgressForTheRecipesLevel()
    {
        // The level 20 chart row has 74 progress, so a penalty of 37 is half of it.
        ForgeRules.EnhancementShapePercent(37, 20).Should().Be(50);
        ForgeRules.EnhancementShapePercent(0, 20).Should().Be(0);
        ForgeRules.EnhancementShapePercent(-10, 20).Should().Be(0);
        ForgeRules.EnhancementShapePercent(100, 500).Should().BeGreaterThan(0, "a level past the chart uses its last row");
    }

    [TestCase(0, 0, 0)]
    [TestCase(0, 24, 18)]
    [TestCase(0, 25, 49)]
    [TestCase(0, 50, 68)]
    [TestCase(23, 50, 91)]
    [TestCase(37, 50, 105)]
    public void CpIsGearCpPlusThreeQuartersOfTheRankAndThePassiveFromRank25(int gearCp, int rank, int expected)
    {
        ForgeRules.MaxCp(gearCp, rank).Should().Be(expected);
        Engine(Setup(rank: rank, level: rank, gearCp: gearCp)).Meters.Cp.Max.Should().Be(expected);
    }

    [Test]
    public void ThermalFallsByTheRecipesCoolingRateAfterEveryAction()
    {
        var engine = Engine(Setup(startingThermal: 50, cooling: 6));

        engine.Perform(ForgeActionIds.HydroPress);
        engine.Thermal.Should().Be(44);

        engine.Perform(ForgeActionIds.VibroHone);
        engine.Thermal.Should().Be(38);

        engine.Perform(ForgeActionIds.RampPlasma);
        engine.Thermal.Should().Be(38 + ForgeRules.RampThermal - 6);
    }

    [Test]
    public void ThermalNeverFallsBelowZero()
    {
        var engine = Engine(Setup(startingThermal: 4, cooling: 6));

        engine.Perform(ForgeActionIds.VibroHone);

        engine.Thermal.Should().Be(0);
    }

    [Test]
    public void ARefusedActionDoesNotCool()
    {
        var engine = Engine(Setup(startingThermal: 50, cooling: 6, rank: 0, level: 3));

        engine.Perform(ForgeActionIds.RampPlasma).Performed.Should().BeFalse();

        engine.Thermal.Should().Be(50);
        engine.Turn.Should().Be(0);
    }

    // Ramp Plasma

    [Test]
    public void RampPlasma_RaisesThermalAndCostsCpAndALittleIntegrity()
    {
        var engine = Engine(Setup(startingThermal: 40));

        var preview = Preview(engine, ForgeActionIds.RampPlasma);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(ForgeRules.RampCp);
        preview.IntegrityCost.Should().Be(ForgeRules.RampIntegrity);
        var outcome = preview.Outcomes.Single();
        outcome.Change(ForgeEngine.ThermalMeterId).Delta.Should().Be(25);
        outcome.Delta.Should().Be(new MinigameMeterDelta(Integrity: -2, Cp: -8));

        engine.Perform(ForgeActionIds.RampPlasma);
        engine.Thermal.Should().Be(65);
        engine.Meters.Cp.Value.Should().Be(15 - ForgeRules.RampCp);
    }

    [Test]
    public void RampPlasma_StopsAtTheMaximumAndIsRefusedThere()
    {
        var engine = Engine(Setup(startingThermal: 90));

        Only(engine, ForgeActionIds.RampPlasma).Change(ForgeEngine.ThermalMeterId).Delta.Should().Be(10);
        engine.Perform(ForgeActionIds.RampPlasma);
        engine.Thermal.Should().Be(100);

        var preview = Preview(engine, ForgeActionIds.RampPlasma);
        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Contain("maximum");
    }

    [Test]
    public void RampPlasma_NeedsEnoughCp()
    {
        var engine = Engine(Setup(rank: 10, level: 10, startingThermal: 40));

        engine.Meters.Cp.Value.Should().Be(7);
        var preview = Preview(engine, ForgeActionIds.RampPlasma);
        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Not enough CP.");
    }

    // Hydro-Press

    [TestCase(35, 91)]
    [TestCase(47, 117)]
    [TestCase(65, 156)]
    public void HydroPress_GainsMoreShapeTheHotterTheRangeInsideIt(int thermal, int shape)
    {
        var engine = Engine(Setup(startingThermal: thermal));

        var outcome = Only(engine, ForgeActionIds.HydroPress);

        outcome.Delta.Progress.Should().Be(shape);
        outcome.Delta.Integrity.Should().Be(-ForgeRules.PressIntegrityInRange);
        outcome.Delta.Cp.Should().Be(0);
        outcome.Label.Should().Be("Press in range");
    }

    [Test]
    public void HydroPress_BelowTheRangeDoesLittleAndCracksIntegrity()
    {
        var engine = Engine(Setup(startingThermal: 20));

        var outcome = Only(engine, ForgeActionIds.HydroPress);

        outcome.Delta.Progress.Should().Be(33);
        outcome.Delta.Integrity.Should().Be(-11);
        outcome.Label.Should().Be("Press below the range");
    }

    [Test]
    public void HydroPress_AboveTheRangeShapesMoreButCracksIntegrityHarder()
    {
        var engine = Engine(Setup(startingThermal: 80));

        var outcome = Only(engine, ForgeActionIds.HydroPress);
        var inRange = Only(Engine(Setup(startingThermal: 65)), ForgeActionIds.HydroPress);
        var below = Only(Engine(Setup(startingThermal: 20)), ForgeActionIds.HydroPress);

        outcome.Delta.Progress.Should().Be(187);
        outcome.Delta.Integrity.Should().Be(-15);
        outcome.Delta.Progress.Should().BeGreaterThan(inRange.Delta.Progress);
        outcome.Delta.Integrity.Should().BeLessThan(inRange.Delta.Integrity);
        outcome.Delta.Integrity.Should().BeLessThan(below.Delta.Integrity, "above the range cracks harder than below it");
        outcome.Label.Should().Be("Press above the range");
    }

    [Test]
    public void HydroPress_ShapeRisesAcrossTheWholeRangeAndKeepsRisingAboveIt()
    {
        var gains = Enumerable.Range(Low, High - Low + 1)
            .Concat(new[] { 75, 90, 100 })
            .Select(thermal => Only(Engine(Setup(startingThermal: thermal)), ForgeActionIds.HydroPress).Delta.Progress)
            .ToList();

        gains.Should().BeInAscendingOrder();
        gains.Distinct().Count().Should().BeGreaterThan(10);
    }

    // Vibro-Hone

    [TestCase(40, 105)]
    [TestCase(50, 105)]
    [TestCase(57, 85)]
    [TestCase(65, 63)]
    public void VibroHone_IsBestInTheCoolHalfOfTheRangeAndFallsTowardTheHotEdge(int thermal, int finish)
    {
        var outcome = Only(Engine(Setup(startingThermal: thermal)), ForgeActionIds.VibroHone);

        outcome.Delta.Quality.Should().Be(finish);
        outcome.Delta.Integrity.Should().Be(-ForgeRules.HoneIntegrity);
        outcome.Delta.Progress.Should().Be(0);
    }

    [TestCase(20, 37)]
    [TestCase(80, 37)]
    public void VibroHone_IsWeakOutsideTheRange(int thermal, int finish)
    {
        Only(Engine(Setup(startingThermal: thermal)), ForgeActionIds.VibroHone).Delta.Quality.Should().Be(finish);
    }

    // Rebond

    [Test]
    public void Rebond_RestoresIntegrityAtAThermalAndCpCost()
    {
        var engine = Engine(Setup(startingThermal: 60));
        engine.Perform(ForgeActionIds.VibroHone);
        engine.Perform(ForgeActionIds.VibroHone);
        engine.Meters.Integrity.Value.Should().Be(92);

        var outcome = Only(engine, ForgeActionIds.Rebond);

        outcome.Delta.Integrity.Should().Be(8, "integrity cannot pass its maximum");
        outcome.Delta.Cp.Should().Be(-ForgeRules.RebondCp);
        outcome.Change(ForgeEngine.ThermalMeterId).Delta.Should().Be(-ForgeRules.RebondThermal);
    }

    [Test]
    public void Rebond_RestoresTheFullAmountWhenThereIsRoom()
    {
        var engine = Engine(Setup(startingThermal: 20));
        Press(engine, 3);
        engine.Meters.Integrity.Value.Should().Be(100 - 3 * 11);

        engine.Perform(ForgeActionIds.Rebond);

        engine.Meters.Integrity.Value.Should().Be(100 - 3 * 11 + ForgeRules.RebondIntegrity);
        engine.Thermal.Should().Be(8);
        engine.Meters.Cp.Value.Should().Be(15 - ForgeRules.RebondCp);
    }

    [Test]
    public void Rebond_IsRefusedAtFullIntegrity()
    {
        var engine = Engine(Setup(rank: 15, level: 15));

        var full = Preview(engine, ForgeActionIds.Rebond);

        full.Enabled.Should().BeFalse();
        full.DisabledReason.Should().Contain("full");
        engine.Perform(ForgeActionIds.Rebond).Performed.Should().BeFalse();
    }

    [Test]
    public void Rebond_WithTooLittleCpIsRefused()
    {
        var engine = Engine(Setup(rank: 15, level: 15, startingThermal: 20));
        engine.Meters.Cp.Value.Should().Be(11);
        engine.Perform(ForgeActionIds.HydroPress);
        engine.Perform(ForgeActionIds.Rebond).Performed.Should().BeTrue();
        engine.Meters.Cp.Value.Should().Be(1);

        engine.Perform(ForgeActionIds.HydroPress);
        var preview = Preview(engine, ForgeActionIds.Rebond);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Not enough CP.");
    }

    // Cryo-Quench

    [Test]
    public void CryoQuench_NeedsThePieceShapedFirst()
    {
        var engine = Engine();

        var preview = Preview(engine, ForgeActionIds.CryoQuench);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Contain("80%");
        engine.Perform(ForgeActionIds.CryoQuench).Performed.Should().BeFalse();
    }

    [Test]
    public void CryoQuench_GivesALargeFinishBonusAtTheCenterOfTheRange()
    {
        var engine = Shaped(50);

        var outcome = Only(engine, ForgeActionIds.CryoQuench);

        outcome.Delta.Quality.Should().Be(ForgeRules.QuenchBaseFinish);
        outcome.Delta.Integrity.Should().Be(-ForgeRules.QuenchIntegrity);
        outcome.Label.Should().Be("Quenched in the working range");
    }

    [Test]
    public void CryoQuench_GivesLessAtTheEdgesOfTheRangeAndLittleOutsideIt()
    {
        var center = Only(Shaped(50), ForgeActionIds.CryoQuench).Delta.Quality;
        var edge = Only(Shaped(65), ForgeActionIds.CryoQuench).Delta.Quality;
        var nearCenter = Only(Shaped(55), ForgeActionIds.CryoQuench).Delta.Quality;
        var outside = Only(Shaped(90), ForgeActionIds.CryoQuench);

        center.Should().Be(300);
        edge.Should().Be(150);
        nearCenter.Should().Be(250);
        outside.Delta.Quality.Should().Be(45);
        outside.Label.Should().Be("Quenched outside the working range");
    }

    [Test]
    public void CryoQuench_SetsThePieceAndIsOncePerCraft()
    {
        var engine = Shaped(50);

        engine.Perform(ForgeActionIds.CryoQuench).Performed.Should().BeTrue();

        engine.IsQuenched.Should().BeTrue();
        engine.Thermal.Should().Be(0);
        var again = Preview(engine, ForgeActionIds.CryoQuench);
        again.Enabled.Should().BeFalse();
        again.DisabledReason.Should().Contain("Already quenched");
    }

    [Test]
    public void AfterTheQuench_RampPlasmaAndHydroPressAreDisabled()
    {
        var engine = Shaped(50);
        engine.Perform(ForgeActionIds.CryoQuench);

        var ramp = Preview(engine, ForgeActionIds.RampPlasma);
        var press = Preview(engine, ForgeActionIds.HydroPress);

        ramp.Enabled.Should().BeFalse();
        press.Enabled.Should().BeFalse();
        ramp.DisabledReason.Should().Contain("set");
        press.DisabledReason.Should().Contain("set");
        engine.Perform(ForgeActionIds.HydroPress).Performed.Should().BeFalse();
        engine.Perform(ForgeActionIds.RampPlasma).Performed.Should().BeFalse();
    }

    [Test]
    public void AfterTheQuench_VibroHoneWorksAtAFlatRateWhateverTheThermal()
    {
        var engine = Shaped(50);
        engine.Perform(ForgeActionIds.CryoQuench);
        var finishes = new List<int>();

        for (var index = 0; index < 3; index++)
        {
            var outcome = Only(engine, ForgeActionIds.VibroHone);
            finishes.Add(outcome.Delta.Quality);
            var remaining = engine.Meters.Progress.Max - engine.Meters.Progress.Value;
            outcome.Delta.Progress.Should().Be(Math.Min(ForgeRules.PostQuenchHoneShape, remaining), "the set piece is trimmed to size");
            outcome.Label.Should().Be("Set piece honed");
            if (engine.Perform(ForgeActionIds.VibroHone).Status != MinigameStatus.InProgress)
                break;
        }

        finishes.Should().OnlyContain(finish => finish == ForgeRules.PostQuenchHoneFinish);
        finishes.Distinct().Should().HaveCount(1);
    }

    [Test]
    public void AfterTheQuench_TrimmingTheSetPieceCanFinishTheItem()
    {
        var engine = Shaped(50);
        engine.Perform(ForgeActionIds.CryoQuench);
        var remaining = engine.Meters.Progress.Max - engine.Meters.Progress.Value;
        var hones = (int)Math.Ceiling(remaining / (double)ForgeRules.PostQuenchHoneShape);

        for (var index = 1; index < hones; index++)
        {
            Preview(engine, ForgeActionIds.VibroHone).FinishesItem.Should().BeFalse();
            engine.Perform(ForgeActionIds.VibroHone);
        }

        var last = Preview(engine, ForgeActionIds.VibroHone);
        last.FinishesItem.Should().BeTrue();
        engine.Perform(ForgeActionIds.VibroHone).Status.Should().Be(MinigameStatus.Succeeded);
    }

    [Test]
    public void AfterTheQuench_RebondStillRepairs()
    {
        var engine = Shaped(50);
        engine.Perform(ForgeActionIds.CryoQuench);

        var rebond = Preview(engine, ForgeActionIds.Rebond);

        rebond.Enabled.Should().BeTrue();
        rebond.Outcomes.Single().Delta.Integrity.Should().BePositive();
    }

    // Stable Containment

    [Test]
    public void StableContainment_IsAnnouncedForTheNextActionWhenTheForecastRollIsAtOrBelowTheChance()
    {
        var engine = Engine(Setup(), new ScriptedMinigameRandom(ForgeRules.StableContainmentChancePercent));

        engine.Perform(ForgeActionIds.VibroHone);

        var opportunity = engine.NextOpportunity;
        opportunity.Should().NotBeNull();
        opportunity!.Id.Should().Be(ForgeRules.StableContainmentId);
        opportunity.Name.Should().Be("Stable Containment");
        opportunity.TurnsAway.Should().Be(1);
        opportunity.Description.Should().Contain("+50%");
    }

    [Test]
    public void StableContainment_IsNotAnnouncedWhenTheForecastRollMissesTheChance()
    {
        var engine = Engine(Setup(), new ScriptedMinigameRandom(ForgeRules.StableContainmentChancePercent + 1));

        engine.Perform(ForgeActionIds.VibroHone);

        engine.NextOpportunity.Should().BeNull();
    }

    [Test]
    public void StableContainment_GivesTheNextHydroPressFiftyPercentMoreShape()
    {
        var engine = Engine(Setup(startingThermal: 47), new ScriptedMinigameRandom(1));
        Only(engine, ForgeActionIds.HydroPress).Delta.Progress.Should().Be(117);
        engine.Perform(ForgeActionIds.RampPlasma);
        engine.NextOpportunity.Should().NotBeNull();
        engine.Thermal.Should().Be(72);

        var boosted = Only(engine, ForgeActionIds.HydroPress);

        var expected = (int)Math.Round(
            ForgeRules.PressBaseShape * ForgeRules.PressMultiplier(72, Low, High) * 1.5, MidpointRounding.AwayFromZero);
        expected.Should().Be(256);
        boosted.Delta.Progress.Should().Be(256);
        boosted.Label.Should().Contain("Stable Containment");
        boosted.Delta.Integrity.Should().Be(Only(Engine(Setup(startingThermal: 72)), ForgeActionIds.HydroPress).Delta.Integrity);
    }

    [Test]
    public void StableContainment_GivesTheNextVibroHoneFiftyPercentMoreFinish()
    {
        var engine = Engine(Setup(startingThermal: 20, control: 10), new ScriptedMinigameRandom(1));
        engine.Perform(ForgeActionIds.RampPlasma);
        engine.NextOpportunity.Should().NotBeNull();
        engine.Thermal.Should().Be(45);

        var boosted = Only(engine, ForgeActionIds.VibroHone);

        boosted.Delta.Quality.Should().Be(173, "105 x 1.10 x 1.50");
        boosted.Label.Should().Contain("Stable Containment");
    }

    [Test]
    public void StableContainment_BoostsThePostQuenchHoneToo()
    {
        var engine = Shaped(50);
        engine.Perform(ForgeActionIds.CryoQuench);
        var plain = Only(engine, ForgeActionIds.VibroHone);

        var boostedEngine = Engine(Setup(startingThermal: 50), new ScriptedMinigameRandom(Enumerable.Repeat(1, 50).ToArray()));
        ShapeForQuench(boostedEngine);
        boostedEngine.Perform(ForgeActionIds.CryoQuench);
        boostedEngine.NextOpportunity.Should().NotBeNull();
        var boosted = Only(boostedEngine, ForgeActionIds.VibroHone);

        boosted.Delta.Quality.Should().Be((int)Math.Round(plain.Delta.Quality * 1.5, MidpointRounding.AwayFromZero));
        boosted.Delta.Progress.Should().Be((int)Math.Round(plain.Delta.Progress * 1.5, MidpointRounding.AwayFromZero));
    }

    [Test]
    public void StableContainment_DoesNotChangeRampRebondOrQuenchOrWhatTheyCost()
    {
        var quiet = Shaped(50);
        var lucky = Engine(Setup(startingThermal: 50), new ScriptedMinigameRandom(Enumerable.Repeat(1, 50).ToArray()));
        ShapeForQuench(lucky);
        lucky.NextOpportunity.Should().NotBeNull();

        foreach (var action in new[] { ForgeActionIds.RampPlasma, ForgeActionIds.CryoQuench })
        {
            var withOpportunity = Only(lucky, action);
            var without = Only(quiet, action);
            withOpportunity.Delta.Should().Be(without.Delta);
        }

        Preview(lucky, ForgeActionIds.HydroPress).IntegrityCost.Should().Be(Preview(quiet, ForgeActionIds.HydroPress).IntegrityCost);
    }

    [Test]
    public void StableContainment_LastsOneActionAndOnlyHelps()
    {
        var engine = Engine(Setup(), new ScriptedMinigameRandom(1, 100, 100));
        engine.Perform(ForgeActionIds.VibroHone);
        engine.NextOpportunity.Should().NotBeNull();

        engine.Perform(ForgeActionIds.RampPlasma);

        engine.NextOpportunity.Should().BeNull("the announced action passed without using it, and nothing was lost but the bonus");
        engine.Meters.Integrity.Value.Should().BeGreaterThan(80);
    }

    [Test]
    public void StableContainment_IsRolledAgainAfterEveryPerformedActionAndNeverByAPreview()
    {
        var random = new ScriptedMinigameRandom(100, 100);
        var engine = Engine(Setup(), random);

        engine.Preview(ForgeActionIds.HydroPress);
        engine.PreviewAll();
        random.Calls.Should().Be(0);

        engine.Perform(ForgeActionIds.HydroPress);
        random.Calls.Should().Be(1);
        engine.Perform(ForgeActionIds.HydroPress);
        random.Calls.Should().Be(2);
    }

    [Test]
    public void ARefusedActionRollsNothing()
    {
        var random = new ScriptedMinigameRandom();
        var engine = Engine(Setup(rank: 0, level: 3), random);

        engine.Perform(ForgeActionIds.CryoQuench).Performed.Should().BeFalse();

        random.Calls.Should().Be(0);
    }

    // Success and failure

    [Test]
    public void TheCraftSucceedsAsSoonAsShapeIsFullAndTheLastActionSaysItFinishesTheItem()
    {
        var engine = Engine(Setup(startingThermal: 65, shapeShare: 30));
        engine.Meters.Progress.Max.Should().Be(600);

        var presses = 0;
        while (!Preview(engine, ForgeActionIds.HydroPress).FinishesItem)
        {
            engine.Perform(ForgeActionIds.HydroPress);
            presses++;
            engine.Status.Should().Be(MinigameStatus.InProgress);
            presses.Should().BeLessThan(10);
        }

        var last = Preview(engine, ForgeActionIds.HydroPress);
        last.Outcomes.Single().ResultingStatus.Should().Be(MinigameStatus.Succeeded);
        last.Outcomes.Single().FinishesItem.Should().BeTrue();

        var result = engine.Perform(ForgeActionIds.HydroPress);

        result.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Meters.Progress.IsFull.Should().BeTrue();
        presses.Should().Be(3);
    }

    [Test]
    public void AFinishedCraftRefusesEveryAction()
    {
        var engine = Engine(Setup(startingThermal: 65, shapeShare: 30));
        Press(engine, 4);
        engine.Status.Should().Be(MinigameStatus.Succeeded);

        foreach (var action in ForgeRules.ActionIds)
        {
            var preview = Preview(engine, action);
            preview.Enabled.Should().BeFalse(action);
            preview.DisabledReason.Should().Be("The attempt is over.");
            engine.Perform(action).Performed.Should().BeFalse(action);
        }
    }

    [Test]
    public void TheCraftBreaksWhenIntegrityReachesZeroAndThePreviewWarnsBeforeTheClick()
    {
        var engine = Engine(Setup(startingThermal: 100, shapeShare: 60));
        var breaks = false;

        for (var turn = 0; turn < 20 && engine.Status == MinigameStatus.InProgress; turn++)
        {
            var preview = Preview(engine, ForgeActionIds.HydroPress);
            if (preview.CanBreakItem)
                breaks = true;

            var result = engine.Perform(ForgeActionIds.HydroPress);
            if (result.Status == MinigameStatus.Failed)
            {
                preview.CanBreakItem.Should().BeTrue("the window warns before the click that breaks the piece");
                preview.Outcomes.Single().BreaksItem.Should().BeTrue();
            }
        }

        engine.Status.Should().Be(MinigameStatus.Failed);
        engine.Meters.Integrity.IsEmpty.Should().BeTrue();
        breaks.Should().BeTrue();
        Preview(engine, ForgeActionIds.Rebond).DisabledReason.Should().Be("The attempt is over.");
    }

    // Rank unlocks

    [TestCase(ForgeActionIds.HydroPress, 0)]
    [TestCase(ForgeActionIds.VibroHone, 5)]
    [TestCase(ForgeActionIds.RampPlasma, 10)]
    [TestCase(ForgeActionIds.Rebond, 15)]
    [TestCase(ForgeActionIds.CryoQuench, 20)]
    public void EveryActionUnlocksAtItsRank(string action, int unlockRank)
    {
        ForgeRules.UnlockRank(action).Should().Be(unlockRank);

        var locked = Preview(Engine(Setup(rank: Math.Max(0, unlockRank - 1), level: 1)), action);
        if (unlockRank > 0)
        {
            locked.Enabled.Should().BeFalse();
            locked.DisabledReason.Should().Be($"Unlocks at Smithery rank {unlockRank}.");
        }

        var unlocked = Preview(Engine(Setup(rank: unlockRank, level: 1, startingThermal: 40)), action);
        unlocked.DisabledReason.Should().NotStartWith("Unlocks at");
    }

    [Test]
    public void ALockedActionIsRefusedAndChangesNothing()
    {
        var engine = Engine(Setup(rank: 4, level: 4));
        var before = engine.DescribeMeters();

        var result = engine.Perform(ForgeActionIds.VibroHone);

        result.Performed.Should().BeFalse();
        result.RejectedReason.Should().Contain("rank 5");
        engine.DescribeMeters().Should().Equal(before);
    }

    [Test]
    public void AnUnknownActionIsAnError()
    {
        var act = () => Engine().Preview("polish");

        act.Should().Throw<ArgumentException>();
    }

    // Rank and gear

    [Test]
    public void ARankAboveTheRecipesLevelRaisesOutputAndOneBelowLowersIt()
    {
        var atLevel = Only(Engine(Setup(rank: 20, level: 20, startingThermal: 47)), ForgeActionIds.HydroPress).Delta.Progress;
        var above = Only(Engine(Setup(rank: 24, level: 20, startingThermal: 47)), ForgeActionIds.HydroPress).Delta.Progress;
        var below = Only(Engine(Setup(rank: 17, level: 20, startingThermal: 47)), ForgeActionIds.HydroPress).Delta.Progress;

        atLevel.Should().Be(117);
        above.Should().Be((int)Math.Round(117 * 1.20));
        below.Should().Be((int)Math.Round(117 * 0.85));
    }

    [Test]
    public void TheRankFactorIsCapped()
    {
        ForgeRules.RankFactor(50, 1).Should().Be(ForgeRules.MaximumRankFactor);
        ForgeRules.RankFactor(0, 50).Should().Be(ForgeRules.MinimumRankFactor);
        ForgeRules.RankFactor(10, 10).Should().Be(1.0);
    }

    [Test]
    public void CraftsmanshipPowersShapeAndControlPowersFinish()
    {
        var plain = Engine(Setup(startingThermal: 47));
        var craftsman = Engine(Setup(startingThermal: 47, craftsmanship: 20));
        var controller = Engine(Setup(startingThermal: 47, control: 20));

        Only(craftsman, ForgeActionIds.HydroPress).Delta.Progress.Should().Be(140, "117 x 1.20");
        Only(craftsman, ForgeActionIds.VibroHone).Delta.Quality.Should().Be(Only(plain, ForgeActionIds.VibroHone).Delta.Quality);
        Only(controller, ForgeActionIds.VibroHone).Delta.Quality.Should().Be(126, "105 x 1.20");
        Only(controller, ForgeActionIds.HydroPress).Delta.Progress.Should().Be(Only(plain, ForgeActionIds.HydroPress).Delta.Progress);
    }

    [Test]
    public void ControlAlsoPowersTheQuenchButCraftsmanshipDoesNot()
    {
        var plain = Only(Shaped(50), ForgeActionIds.CryoQuench).Delta.Quality;

        var controlled = Engine(Setup(startingThermal: 50, control: 20));
        ShapeForQuench(controlled);
        var craft = Engine(Setup(startingThermal: 50, craftsmanship: 20));
        ShapeForQuench(craft);

        Only(controlled, ForgeActionIds.CryoQuench).Delta.Quality.Should().Be((int)Math.Round(plain * 1.2));
        Only(craft, ForgeActionIds.CryoQuench).Delta.Quality.Should().Be(plain);
    }

    // The preview and the result

    [Test]
    public void PerformChangesEveryMeterByExactlyWhatThePreviewPromised()
    {
        foreach (var action in ForgeRules.ActionIds)
        {
            var engine = Engine(Setup(startingThermal: 60, cooling: 4));
            Press(engine, 6);
            if (engine.Status != MinigameStatus.InProgress)
                continue;

            var preview = engine.Preview(action);
            var before = engine.DescribeMeters();

            var result = engine.Perform(action);

            if (!preview.Enabled)
            {
                result.Performed.Should().BeFalse(action);
                engine.DescribeMeters().Should().Equal(before);
                continue;
            }

            var promised = preview.Outcomes.Single();
            result.Outcome.Should().BeEquivalentTo(promised);
            engine.DescribeMeters().Zip(before, (after, was) => new MinigameMeterChange(after.Id, after.Label, was.Value, after.Value))
                .Should().BeEquivalentTo(promised.Changes);
            result.Status.Should().Be(promised.ResultingStatus);
        }
    }

    [Test]
    public void PerformAcrossManySeedsAlwaysLandsOnAPreviewedOutcome()
    {
        for (var seed = 1; seed <= 150; seed++)
        {
            var engine = Engine(Setup(startingThermal: 55, cooling: 4, rank: 30, level: 25), new SeededMinigameRandom(seed));
            var picker = new System.Random(seed);

            for (var turn = 0; turn < 40 && engine.Status == MinigameStatus.InProgress; turn++)
            {
                var action = ForgeRules.ActionIds[picker.Next(ForgeRules.ActionIds.Count)];
                var preview = engine.Preview(action);
                var before = engine.DescribeMeters();

                var result = engine.Perform(action);

                if (!preview.Enabled)
                {
                    result.Performed.Should().BeFalse();
                    engine.DescribeMeters().Should().Equal(before);
                    continue;
                }

                preview.Outcomes.Should().ContainEquivalentOf(result.Outcome, $"seed {seed} turn {turn}");
                foreach (var meter in engine.DescribeMeters())
                    meter.Value.Should().BeInRange(meter.Min, meter.Max);
            }
        }
    }

    [Test]
    public void TheSameSeedPlaysTheSameCraft()
    {
        string Play(int seed)
        {
            var engine = Engine(Setup(startingThermal: 50, cooling: 4), new SeededMinigameRandom(seed));
            var trail = new List<string>();
            foreach (var action in new[] { ForgeActionIds.HydroPress, ForgeActionIds.VibroHone, ForgeActionIds.RampPlasma, ForgeActionIds.HydroPress })
            {
                engine.Perform(action);
                trail.Add($"{engine.Thermal}/{engine.NextOpportunity?.Id}/{engine.Meters.Progress.Value}/{engine.Meters.Quality.Value}");
            }

            return string.Join(";", trail);
        }

        Play(7).Should().Be(Play(7));
    }

    [Test]
    public void EveryActionStartsInTheOrderTheWindowListsThem()
    {
        var engine = Engine();

        engine.Actions.Select(action => action.Id).Should().Equal(ForgeRules.ActionIds);
        engine.Actions.Select(action => action.Name).Should().Equal("Ramp Plasma", "Hydro-Press", "Vibro-Hone", "Rebond", "Cryo-Quench");
        engine.PreviewAll().Should().HaveCount(5);
    }

    [Test]
    public void TheEngineNeedsParameters()
    {
        var act = () => Engine(new ForgeSetup(null, 10, 10, 0, 0, 0));

        act.Should().Throw<ArgumentException>();
    }
}
