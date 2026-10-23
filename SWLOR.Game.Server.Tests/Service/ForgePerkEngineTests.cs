using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Covers what the Forgemaster and Artisan perks and the two styles change in the Plasma Forge: every stat the engine reads, the five
/// added actions, and that previews still equal results. The numbers come from a 35-65 working range (center 50) at recipe level 20
/// with a rank 20 crafter and 100 CP of gear, so CP is never the limit unless a test makes it one.
/// </summary>
public class ForgePerkEngineTests
{
    private const int Low = 35;
    private const int High = 65;

    private static ForgeSetup Setup(
        ForgePerkStats perks = null,
        int startingThermal = 50,
        int cooling = 0,
        int shapeShare = 50,
        int rank = 20,
        int level = 20,
        int gearCp = 100,
        int center = 50,
        int width = 30) =>
        new(new ForgeParameters(center, width, cooling, startingThermal, shapeShare), level, rank, 0, 0, gearCp, 0, perks);

    private static IMinigameRandom Quiet() => new ScriptedMinigameRandom(Enumerable.Repeat(100, 400).ToArray());

    private static ForgeEngine Engine(ForgePerkStats perks = null, IMinigameRandom random = null, int startingThermal = 50, int cooling = 0,
        int shapeShare = 50, int level = 20, int rank = 20, int gearCp = 100, int center = 50, int width = 30) =>
        new(Setup(perks, startingThermal, cooling, shapeShare, rank, level, gearCp, center, width), random ?? Quiet());

    private static MinigameOutcomePreview Only(ForgeEngine engine, string action) => engine.Preview(action).Outcomes.Single();

    /// <summary>A forge that has taken enough Shape for a quench. It has no cooling, so Thermal stays where the craft started; start inside the working range.</summary>
    private static ForgeEngine Shaped(ForgePerkStats perks, int thermal = 50)
    {
        var engine = Engine(perks, startingThermal: thermal, gearCp: 200);
        while (engine.Meters.Progress.Percent < ForgeRules.QuenchMinimumShapePercent)
            engine.Perform(ForgeActionIds.HydroPress).Performed.Should().BeTrue();

        engine.Status.Should().Be(MinigameStatus.InProgress);
        return engine;
    }

    // ---- no perks -------------------------------------------------------------------------------------------------------------

    [Test]
    public void WithoutPerks_TheActionsAreTheOriginalFive_AndEveryPreviewEqualsOnesWithNoStats()
    {
        var plain = Engine();
        var none = Engine(ForgePerkStats.None);

        plain.Actions.Select(action => action.Id).Should().Equal(ForgeRules.ActionIds);
        none.PreviewAll().Should().BeEquivalentTo(plain.PreviewAll());
        plain.RedLineReady.Should().BeFalse();
        plain.TemperHoldLeft.Should().Be(0);
        plain.PressStreak.Should().Be(0);
    }

    [Test]
    public void ANewSetupDefaultsToNoPerkStats()
    {
        new ForgeSetup(new ForgeParameters(50, 30, 3, 50, 50), 20, 20, 0, 0, 0).Perks.Should().BeSameAs(ForgePerkStats.None);
    }

    [TestCase(ForgeActionIds.OverdrivePress)]
    [TestCase(ForgeActionIds.SurgePlasma)]
    [TestCase(ForgeActionIds.RedLinePress)]
    [TestCase(ForgeActionIds.MicroHone)]
    [TestCase(ForgeActionIds.TemperHold)]
    public void AnAddedActionIsNotOfferedWithoutItsPerk_AndIsRefusedAsUnknown(string action)
    {
        var engine = Engine();

        engine.Actions.Select(info => info.Id).Should().NotContain(action);
        var preview = () => engine.Preview(action);
        preview.Should().Throw<ArgumentException>();
    }

    // ---- shared stats a style sets --------------------------------------------------------------------------------------------

    [Test]
    public void RangeShift_MovesTheWorkingRangeAndTheMarkTheMeterShows()
    {
        var engine = Engine(new ForgePerkStats { RangeShift = 10 });

        (engine.RangeLow, engine.RangeHigh).Should().Be((Low + 10, High + 10));
        engine.DescribeMeters().Single(meter => meter.Id == ForgeEngine.ThermalMeterId).Mark
            .Should().Be(MinigameMeterMark.Range(Low + 10, High + 10, "Working range"));
        Engine(new ForgePerkStats { RangeShift = -10 }).RangeLow.Should().Be(Low - 10);
    }

    [Test]
    public void RangeShift_MovesWhereTheActionsWork_ThermalFiftyIsNowTheCoolHalfOfTheHotRange()
    {
        var plain = Only(Engine(), ForgeActionIds.HydroPress);
        var hot = Only(Engine(new ForgePerkStats { RangeShift = 10 }), ForgeActionIds.HydroPress);
        var cold = Only(Engine(new ForgePerkStats { RangeShift = -10 }), ForgeActionIds.HydroPress);

        hot.Delta.Progress.Should().BeLessThan(plain.Delta.Progress, "Thermal 50 sits lower in the shifted-up range");
        cold.Delta.Progress.Should().BeGreaterThan(plain.Delta.Progress);
    }

    [TestCase(90, 20, 10, 80, 100)]
    [TestCase(90, 20, -10, 70, 90)]
    [TestCase(12, 20, -10, 0, 20)]
    [TestCase(12, 20, 10, 12, 32)]
    public void RangeShift_MovesTheWholeRangeAndKeepsItInsideTheMeter_WithoutNarrowingIt(int center, int width, int shift, int low, int high)
    {
        var setup = Setup(new ForgePerkStats { RangeShift = shift }, center: center, width: width);

        (setup.RangeLow, setup.RangeHigh).Should().Be((low, high));
        (setup.RangeHigh - setup.RangeLow).Should().Be(width);
    }

    [Test]
    public void PressOutputPercent_ScalesEveryHydroPressGain_IncludingTheSetPiecesTrim()
    {
        Only(Engine(new ForgePerkStats { PressOutputPercent = 25 }), ForgeActionIds.HydroPress).Delta.Progress.Should().Be(226, "190 x 0.95 x 1.25");
        Only(Engine(new ForgePerkStats { PressOutputPercent = -25 }), ForgeActionIds.HydroPress).Delta.Progress.Should().Be(135, "190 x 0.95 x 0.75");

        var plain = Shaped(null);
        plain.Perform(ForgeActionIds.CryoQuench);
        var scaled = Shaped(new ForgePerkStats { PressOutputPercent = 25 });
        scaled.Perform(ForgeActionIds.CryoQuench);

        Only(plain, ForgeActionIds.VibroHone).Delta.Progress.Should().Be(ForgeRules.PostQuenchHoneShape);
        Only(scaled, ForgeActionIds.VibroHone).Delta.Progress.Should().Be(60, "48 x 1.25");
    }

    [Test]
    public void HoneOutputPercent_ScalesEveryVibroHoneGain_IncludingTheSetPiece()
    {
        Only(Engine(new ForgePerkStats { HoneOutputPercent = 25 }), ForgeActionIds.VibroHone).Delta.Quality.Should().Be(113, "90 x 1.25 = 112.5");
        Only(Engine(new ForgePerkStats { HoneOutputPercent = -25 }), ForgeActionIds.VibroHone).Delta.Quality.Should().Be(68, "90 x 0.75 = 67.5");

        var plain = Shaped(null);
        plain.Perform(ForgeActionIds.CryoQuench);
        var scaled = Shaped(new ForgePerkStats { HoneOutputPercent = 25 });
        scaled.Perform(ForgeActionIds.CryoQuench);

        Only(plain, ForgeActionIds.VibroHone).Delta.Quality.Should().Be(ForgeRules.PostQuenchHoneFinish);
        Only(scaled, ForgeActionIds.VibroHone).Delta.Quality.Should().Be(105, "84 x 1.25");
    }

    [Test]
    public void OutputPercentsAddBeforeTheyScaleAGain_AndNeverTakeItBelowAFloor()
    {
        var drained = Setup(new ForgePerkStats { PressOutputPercent = -500, HoneOutputPercent = -500 });

        drained.PressFactor(0).Should().BeApproximately(0.1, 1e-9);
        drained.HoneFactor.Should().BeApproximately(0.1, 1e-9);
        Setup(new ForgePerkStats { PressOutputPercent = 25, PressStreakPercent = 8 }).PressFactor(2).Should().BeApproximately(1.41, 1e-9);
    }

    // ---- Forgemaster stats ----------------------------------------------------------------------------------------------------

    [Test]
    public void RampCpReduction_LowersRampPlasmasCp_ToAMinimumOfOne()
    {
        Engine().Preview(ForgeActionIds.RampPlasma).CpCost.Should().Be(10);
        Engine(new ForgePerkStats { RampCpReduction = 2 }).Preview(ForgeActionIds.RampPlasma).CpCost.Should().Be(8);
        Engine(new ForgePerkStats { RampCpReduction = 2 }, level: 1, rank: 1).Preview(ForgeActionIds.RampPlasma).CpCost.Should().Be(1, "3 less 2");
        Engine(new ForgePerkStats { RampCpReduction = 9 }, level: 1, rank: 1).Preview(ForgeActionIds.RampPlasma).CpCost.Should().Be(1);
    }

    [Test]
    public void CoolingReduction_MakesThePieceLoseLessThermal_ButNeverLessThanOneLost()
    {
        HoneAt(null, 5, 45, "the recipe's own cooling");
        HoneAt(new ForgePerkStats { CoolingReduction = 1 }, 5, 46, "1 less");
        HoneAt(new ForgePerkStats { CoolingReduction = 2 }, 5, 47, "2 less");
        HoneAt(new ForgePerkStats { CoolingReduction = 2 }, 2, 49, "a piece that loses 2 still loses 1");
        HoneAt(new ForgePerkStats { CoolingReduction = 2 }, 1, 49, "a recipe that cools 1 cannot cool less");
        HoneAt(new ForgePerkStats { CoolingReduction = 2 }, 0, 50, "a recipe that does not cool stays that way");
    }

    private static void HoneAt(ForgePerkStats perks, int cooling, int expectedThermal, string because)
    {
        var engine = Engine(perks, startingThermal: 50, cooling: cooling);
        engine.Perform(ForgeActionIds.VibroHone);
        engine.Thermal.Should().Be(expectedThermal, because);
    }

    // Overdrive Press

    [Test]
    public void OverdrivePress_ShapesAtOneAndThreeQuartersOfAHydroPress_ForTwelveCpAndTwiceTheIntegrity()
    {
        var engine = Engine(new ForgePerkStats { OverdrivePressPercent = 75 });

        var press = engine.Preview(ForgeActionIds.HydroPress);
        var overdrive = engine.Preview(ForgeActionIds.OverdrivePress);

        overdrive.Enabled.Should().BeTrue();
        overdrive.CpCost.Should().Be(12);
        overdrive.IntegrityCost.Should().Be(press.IntegrityCost * 2);
        overdrive.Outcomes.Single().Delta.Progress.Should().Be(316, "190 x 0.95 x 1.75 = 315.9");
        press.Outcomes.Single().Delta.Progress.Should().Be(181);
        overdrive.Outcomes.Single().Label.Should().Be("Overdrive in range");
    }

    [Test]
    public void OverdrivePress_CannotBeUsedTwiceInARow_ButCanAfterAnyOtherAction()
    {
        var engine = Engine(new ForgePerkStats { OverdrivePressPercent = 75 });

        engine.Perform(ForgeActionIds.OverdrivePress).Performed.Should().BeTrue();
        var again = engine.Preview(ForgeActionIds.OverdrivePress);
        again.Enabled.Should().BeFalse();
        again.DisabledReason.Should().Be("Overdrive Press cannot be used twice in a row.");
        engine.Perform(ForgeActionIds.OverdrivePress).Performed.Should().BeFalse();

        engine.Perform(ForgeActionIds.VibroHone);
        engine.Preview(ForgeActionIds.OverdrivePress).Enabled.Should().BeTrue();
    }

    [Test]
    public void OverdrivePress_IsRefusedOnASetPiece_AndNeedsItsCp()
    {
        var set = Shaped(new ForgePerkStats { OverdrivePressPercent = 75 });
        set.Perform(ForgeActionIds.CryoQuench);

        set.Preview(ForgeActionIds.OverdrivePress).DisabledReason.Should().Be("The piece is set.");

        var poor = Engine(new ForgePerkStats { OverdrivePressPercent = 75 }, gearCp: 0, rank: 4, level: 4);
        poor.Preview(ForgeActionIds.OverdrivePress).DisabledReason.Should().Be("Not enough CP.");
    }

    [Test]
    public void OverdrivePress_TakesTheStylesOutputPercent_ButNotHammerRhythm()
    {
        var perks = new ForgePerkStats { OverdrivePressPercent = 75, PressOutputPercent = 25, PressStreakPercent = 8 };
        var engine = Engine(perks);

        Only(engine, ForgeActionIds.OverdrivePress).Delta.Progress.Should().Be(395, "190 x 0.95 x 1.25 x 1.75 = 394.8");
        engine.Perform(ForgeActionIds.HydroPress);
        engine.PressStreak.Should().Be(1);
        engine.Perform(ForgeActionIds.OverdrivePress);
        engine.PressStreak.Should().Be(0, "an Overdrive Press is not a Hydro-Press, so it ends the rhythm");
    }

    // Hammer Rhythm

    [Test]
    public void HammerRhythm_AddsToEachHydroPressMadeRightAfterAnother_UpToTheCap()
    {
        var engine = Engine(new ForgePerkStats { PressStreakPercent = 8 }, shapeShare: 75, gearCp: 200);
        var gains = new List<int>();

        for (var press = 0; press < 5; press++)
        {
            gains.Add(Only(engine, ForgeActionIds.HydroPress).Delta.Progress);
            engine.Perform(ForgeActionIds.HydroPress).Performed.Should().BeTrue();
        }

        gains.Should().Equal(181, 195, 209, 224, 224);
    }

    [Test]
    public void HammerRhythm_EndsWithAnyOtherAction_AndThePreviewShowsTheNextPressesBonus()
    {
        var engine = Engine(new ForgePerkStats { PressStreakPercent = 8 }, shapeShare: 75);

        engine.Perform(ForgeActionIds.HydroPress);
        engine.Perform(ForgeActionIds.HydroPress);
        engine.PressStreak.Should().Be(2);
        Only(engine, ForgeActionIds.HydroPress).Delta.Progress.Should().Be(209, "16% after two in a row");

        engine.Perform(ForgeActionIds.VibroHone);

        engine.PressStreak.Should().Be(0);
        Only(engine, ForgeActionIds.HydroPress).Delta.Progress.Should().Be(181);
    }

    [Test]
    public void APreviewDoesNotCountTowardHammerRhythm()
    {
        var engine = Engine(new ForgePerkStats { PressStreakPercent = 8 });

        engine.Preview(ForgeActionIds.HydroPress);
        engine.PreviewAll();

        engine.PressStreak.Should().Be(0);
    }

    [Test]
    public void WithoutHammerRhythm_NothingIsCounted()
    {
        var engine = Engine();

        engine.Perform(ForgeActionIds.HydroPress);
        engine.Perform(ForgeActionIds.HydroPress);

        engine.PressStreak.Should().Be(0);
    }

    // Scorch Guard

    [Test]
    public void ScorchGuard_CracksLessIntegrityOnlyForAPressAboveTheRange()
    {
        var guard = new ForgePerkStats { PressOverheatIntegrityReductionPercent = 25 };

        Only(Engine(guard, startingThermal: 80), ForgeActionIds.HydroPress).Delta.Integrity.Should().Be(-19, "25 less a quarter = 18.75");
        Only(Engine(null, startingThermal: 80), ForgeActionIds.HydroPress).Delta.Integrity.Should().Be(-25);
        Only(Engine(guard, startingThermal: 50), ForgeActionIds.HydroPress).Delta.Integrity.Should().Be(-10, "inside the range is unchanged");
        Only(Engine(guard, startingThermal: 20), ForgeActionIds.HydroPress).Delta.Integrity.Should().Be(-16, "below the range is unchanged");
    }

    [Test]
    public void ScorchGuard_AlsoLowersAnOverdrivePressAboveTheRange_AndNeverBelowOne()
    {
        var perks = new ForgePerkStats { PressOverheatIntegrityReductionPercent = 25, OverdrivePressPercent = 75 };

        Engine(perks, startingThermal: 80).Preview(ForgeActionIds.OverdrivePress).IntegrityCost.Should().Be(38);

        var total = new ForgePerkStats { PressOverheatIntegrityReductionPercent = 100 };
        Engine(total, startingThermal: 80).Preview(ForgeActionIds.HydroPress).IntegrityCost.Should().Be(1);
    }

    // Surge Plasma

    [Test]
    public void SurgePlasma_RaisesThermalByFortyFour_ForOneAndAHalfRampsCp()
    {
        var engine = Engine(new ForgePerkStats { SurgePlasmaThermal = 44 }, startingThermal: 40);

        var preview = engine.Preview(ForgeActionIds.SurgePlasma);

        preview.CpCost.Should().Be(15);
        preview.IntegrityCost.Should().Be(ForgeRules.RampIntegrity);
        preview.Outcomes.Single().Change(ForgeEngine.ThermalMeterId).Delta.Should().Be(44);
        engine.Perform(ForgeActionIds.SurgePlasma);
        engine.Thermal.Should().Be(84);
    }

    [Test]
    public void SurgePlasma_CostsOneAndAHalfOfTheReducedRamp_AndStopsAtTheMaximum()
    {
        var perks = new ForgePerkStats { SurgePlasmaThermal = 44, RampCpReduction = 2 };
        var engine = Engine(perks, startingThermal: 70);

        engine.Preview(ForgeActionIds.SurgePlasma).CpCost.Should().Be(12, "1.5 x 8");
        engine.Preview(ForgeActionIds.SurgePlasma).Outcomes.Single().Change(ForgeEngine.ThermalMeterId).Delta.Should().Be(30);
        engine.Perform(ForgeActionIds.SurgePlasma);
        engine.Thermal.Should().Be(100);

        var full = engine.Preview(ForgeActionIds.SurgePlasma);
        full.Enabled.Should().BeFalse();
        full.DisabledReason.Should().Contain("maximum");
    }

    [Test]
    public void SurgePlasma_IsRefusedOnASetPiece()
    {
        var set = Shaped(new ForgePerkStats { SurgePlasmaThermal = 44 });
        set.Perform(ForgeActionIds.CryoQuench);

        set.Preview(ForgeActionIds.SurgePlasma).DisabledReason.Should().Be("The piece is set.");
    }

    // Fast Rebond

    [Test]
    public void FastRebond_CostsLessThermalAndCp()
    {
        var plain = Engine(null, startingThermal: 60);
        var fast = Engine(new ForgePerkStats { RebondThermalReduction = 6, RebondCpReduction = 3 }, startingThermal: 60);
        plain.Perform(ForgeActionIds.HydroPress);
        fast.Perform(ForgeActionIds.HydroPress);

        plain.Preview(ForgeActionIds.Rebond).CpCost.Should().Be(14);
        plain.Preview(ForgeActionIds.Rebond).Outcomes.Single().Change(ForgeEngine.ThermalMeterId).Delta.Should().Be(-12);
        fast.Preview(ForgeActionIds.Rebond).CpCost.Should().Be(11);
        fast.Preview(ForgeActionIds.Rebond).Outcomes.Single().Change(ForgeEngine.ThermalMeterId).Delta.Should().Be(-6);
        fast.Preview(ForgeActionIds.Rebond).Outcomes.Single().Delta.Integrity.Should().Be(10, "the 25 restored stops at the 100 maximum, as it always has");
    }

    [Test]
    public void FastRebond_NeverCostsLessThanOneCp_AndIsStillRefusedAtFullIntegrity()
    {
        var cheap = Engine(new ForgePerkStats { RebondCpReduction = 50 }, level: 1, rank: 10);
        cheap.Preview(ForgeActionIds.Rebond).CpCost.Should().Be(1);
        cheap.Preview(ForgeActionIds.Rebond).DisabledReason.Should().Be("Integrity is already full.", "no refund loop at full Integrity");

        Engine(new ForgePerkStats { RebondThermalReduction = 99 }).Preview(ForgeActionIds.Rebond).DisabledReason
            .Should().Be("Integrity is already full.");
    }

    [Test]
    public void FastRebond_NeverRaisesThermal()
    {
        var engine = Engine(new ForgePerkStats { RebondThermalReduction = 99 }, startingThermal: 60);
        engine.Perform(ForgeActionIds.HydroPress);

        engine.Preview(ForgeActionIds.Rebond).Outcomes.Single().Change(ForgeEngine.ThermalMeterId).Delta.Should().Be(0);
    }

    // Red Line

    [Test]
    public void RedLinePress_IsAHydroPressWithDoubleShapeAndNoIntegrityCost_OnlyAboveTheRange()
    {
        var engine = Engine(new ForgePerkStats { RedLineCharges = 1 }, startingThermal: 80, shapeShare: 75);

        var press = engine.Preview(ForgeActionIds.HydroPress).Outcomes.Single();
        var redLine = engine.Preview(ForgeActionIds.RedLinePress);

        redLine.Enabled.Should().BeTrue();
        redLine.IntegrityCost.Should().Be(0);
        redLine.CpCost.Should().Be(0);
        redLine.Outcomes.Single().Delta.Progress.Should().Be(547, "190 x 1.44 x 2 = 547.2");
        redLine.Outcomes.Single().Delta.Progress.Should().BeCloseTo(press.Delta.Progress * 2, 1);
        redLine.Outcomes.Single().Delta.Integrity.Should().Be(0);
        engine.RedLineReady.Should().BeTrue();
    }

    [TestCase(20)]
    [TestCase(50)]
    [TestCase(65)]
    public void RedLinePress_IsRefusedWhileThermalIsInsideOrBelowTheRange(int thermal)
    {
        var preview = Engine(new ForgePerkStats { RedLineCharges = 1 }, startingThermal: thermal).Preview(ForgeActionIds.RedLinePress);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Needs Thermal above the working range.");
    }

    [Test]
    public void RedLinePress_WorksOncePerCraft()
    {
        var engine = Engine(new ForgePerkStats { RedLineCharges = 1 }, startingThermal: 80, shapeShare: 75);

        engine.Perform(ForgeActionIds.RedLinePress).Performed.Should().BeTrue();

        engine.RedLineReady.Should().BeFalse();
        var spent = engine.Preview(ForgeActionIds.RedLinePress);
        spent.Enabled.Should().BeFalse();
        spent.DisabledReason.Should().Be("The Red Line Press is spent this craft.");
        engine.Perform(ForgeActionIds.RedLinePress).Performed.Should().BeFalse();
    }

    [Test]
    public void RedLinePress_IsAHydroPressForHammerRhythm_AndTakesTheStyle()
    {
        var perks = new ForgePerkStats { RedLineCharges = 1, PressStreakPercent = 8, PressOutputPercent = 25 };
        var engine = Engine(perks, startingThermal: 80, shapeShare: 75);

        engine.Perform(ForgeActionIds.HydroPress);
        engine.PressStreak.Should().Be(1);

        Only(engine, ForgeActionIds.RedLinePress).Delta.Progress.Should().Be(728, "190 x 1.44 x 1.33 x 2 = 727.8 with the streak and style added");
        engine.Perform(ForgeActionIds.RedLinePress);
        engine.PressStreak.Should().Be(2);
    }

    [Test]
    public void RedLinePress_IsRefusedOnASetPiece()
    {
        var set = Shaped(new ForgePerkStats { RedLineCharges = 1 });
        set.Perform(ForgeActionIds.CryoQuench);

        set.Preview(ForgeActionIds.RedLinePress).DisabledReason.Should().Be("The piece is set.");
    }

    // ---- Artisan stats --------------------------------------------------------------------------------------------------------

    [Test]
    public void HoneIntegrityReduction_LowersVibroHonesIntegrity_ToAMinimumOfOne()
    {
        Only(Engine(), ForgeActionIds.VibroHone).Delta.Integrity.Should().Be(-8);
        Only(Engine(new ForgePerkStats { HoneIntegrityReduction = 2 }), ForgeActionIds.VibroHone).Delta.Integrity.Should().Be(-6);
        Only(Engine(new ForgePerkStats { HoneIntegrityReduction = 20 }), ForgeActionIds.VibroHone).Delta.Integrity.Should().Be(-1);
    }

    [Test]
    public void HoneIntegrityReduction_DoesNotChangeMicroHonesOwnCost()
    {
        var engine = Engine(new ForgePerkStats { HoneIntegrityReduction = 3, MicroHonePercent = 80 });

        engine.Preview(ForgeActionIds.MicroHone).IntegrityCost.Should().Be(6);
        engine.Preview(ForgeActionIds.VibroHone).IntegrityCost.Should().Be(5);
    }

    [TestCase(10, 45, true)]
    [TestCase(10, 46, false)]
    [TestCase(20, 55, true)]
    [TestCase(20, 56, false)]
    [TestCase(0, 35, true)]
    [TestCase(0, 36, false)]
    public void ContainmentChancePercent_RaisesTheChanceStableContainmentIsAnnounced(int bonus, int roll, bool announced)
    {
        var engine = Engine(new ForgePerkStats { ContainmentChancePercent = bonus }, new ScriptedMinigameRandom(roll));

        engine.Perform(ForgeActionIds.VibroHone);

        (engine.NextOpportunity?.Id == ForgeRules.StableContainmentId).Should().Be(announced);
    }

    [Test]
    public void ContainmentChancePercent_IsClampedBetweenNothingAndAlways()
    {
        Setup(new ForgePerkStats { ContainmentChancePercent = 90 }).ContainmentChancePercent.Should().Be(100);
        Setup(new ForgePerkStats { ContainmentChancePercent = -90 }).ContainmentChancePercent.Should().Be(0);
    }

    // Micro-Hone

    [Test]
    public void MicroHone_RaisesFinishByEightyPercentOfAHonesBestResult_AtAnyThermal()
    {
        foreach (var thermal in new[] { 5, 36, 50, 65, 66, 95 })
        {
            var engine = Engine(new ForgePerkStats { MicroHonePercent = 80 }, startingThermal: thermal);
            var preview = engine.Preview(ForgeActionIds.MicroHone);

            preview.Enabled.Should().BeTrue();
            preview.CpCost.Should().Be(4);
            preview.IntegrityCost.Should().Be(6);
            preview.Outcomes.Single().Delta.Quality.Should().Be(72, $"90 x 0.8 at Thermal {thermal}");
            preview.Outcomes.Single().Delta.Progress.Should().Be(0);
        }
    }

    [Test]
    public void MicroHone_WorksAfterACryoQuench_AtEightyPercentOfTheSetPiecesFlatRate()
    {
        var set = Shaped(new ForgePerkStats { MicroHonePercent = 80 });
        set.Perform(ForgeActionIds.CryoQuench);

        var micro = set.Preview(ForgeActionIds.MicroHone);

        micro.Enabled.Should().BeTrue();
        micro.Outcomes.Single().Delta.Quality.Should().Be(67, "84 x 0.8 = 67.2");
        micro.Outcomes.Single().Delta.Progress.Should().Be(0, "it only finishes; the trim stays a Vibro-Hone's");
    }

    [Test]
    public void MicroHone_TakesTheStylesHoneOutputAndStableContainment()
    {
        var perks = new ForgePerkStats { MicroHonePercent = 80, HoneOutputPercent = 25, ContainmentHonePercent = 25 };
        var announced = Engine(perks, new ScriptedMinigameRandom(1, 100, 100));

        Only(announced, ForgeActionIds.MicroHone).Delta.Quality.Should().Be(90, "90 x 0.8 x 1.25");
        announced.Perform(ForgeActionIds.RampPlasma);
        announced.NextOpportunity.Should().NotBeNull();

        Only(announced, ForgeActionIds.MicroHone).Delta.Quality.Should().Be(158, "90 x 0.8 x 1.25 x 1.75 = 157.5");
    }

    // Steady Quench and Cool Hand

    [TestCase(35, 0, 180)]
    [TestCase(35, 40, 420)]
    [TestCase(50, 0, 600)]
    [TestCase(50, 40, 600)]
    [TestCase(42, 0, 376)]
    [TestCase(42, 40, 504)]
    public void QuenchEdgePercent_KeepsMoreOfTheCenterBonusTowardTheEdges(int thermal, int edgeBonus, int finish)
    {
        var set = Shaped(new ForgePerkStats { QuenchEdgePercent = edgeBonus }, thermal);

        Only(set, ForgeActionIds.CryoQuench).Delta.Quality.Should().Be(finish);
    }

    [Test]
    public void QuenchEdgePercent_DoesNotChangeAQuenchOutsideTheRange()
    {
        var plain = Shaped(null);
        var guarded = Shaped(new ForgePerkStats { QuenchEdgePercent = 40 });
        plain.Perform(ForgeActionIds.RampPlasma);
        guarded.Perform(ForgeActionIds.RampPlasma);
        plain.Thermal.Should().BeGreaterThan(High);

        Only(plain, ForgeActionIds.CryoQuench).Delta.Quality.Should().Be(90, "600 x 0.15");
        Only(guarded, ForgeActionIds.CryoQuench).Delta.Quality.Should().Be(90);
    }

    [TestCase(65, 0, 54)]
    [TestCase(65, 20, 72)]
    [TestCase(57, 0, 73)]
    [TestCase(57, 20, 82)]
    [TestCase(45, 20, 90)]
    public void HoneHotEdgePercent_KeepsMoreOfAVibroHonesFinishInTheHotHalf(int thermal, int bonus, int finish)
    {
        Only(Engine(new ForgePerkStats { HoneHotEdgePercent = bonus }, startingThermal: thermal), ForgeActionIds.VibroHone).Delta.Quality
            .Should().Be(finish);
    }

    [Test]
    public void HoneHotEdgePercent_NeverMakesAHoneBetterThanItsBest()
    {
        Setup(new ForgePerkStats { HoneHotEdgePercent = 90 }).HoneHotEdgeMultiplier.Should().Be(1.0);
        Only(Engine(new ForgePerkStats { HoneHotEdgePercent = 90 }, startingThermal: 65), ForgeActionIds.VibroHone).Delta.Quality.Should().Be(90);
    }

    // Temper Hold

    [Test]
    public void TemperHold_StopsTheNextTwoActionsFromCooling_ForTenCp()
    {
        var engine = Engine(new ForgePerkStats { TemperHoldActions = 2 }, startingThermal: 50, cooling: 6);

        var preview = engine.Preview(ForgeActionIds.TemperHold);
        preview.CpCost.Should().Be(10);
        preview.IntegrityCost.Should().Be(0);

        engine.Perform(ForgeActionIds.TemperHold);
        engine.Thermal.Should().Be(44, "the Temper Hold action itself cools as usual");
        engine.TemperHoldLeft.Should().Be(2);

        engine.Perform(ForgeActionIds.VibroHone);
        engine.Thermal.Should().Be(44);
        engine.TemperHoldLeft.Should().Be(1);

        engine.Perform(ForgeActionIds.VibroHone);
        engine.Thermal.Should().Be(44);
        engine.TemperHoldLeft.Should().Be(0);

        engine.Perform(ForgeActionIds.VibroHone);
        engine.Thermal.Should().Be(38, "the third action after it cools again");
    }

    [Test]
    public void TemperHold_ThePreviewOfTheNextActionShowsNoCooling()
    {
        var engine = Engine(new ForgePerkStats { TemperHoldActions = 2 }, startingThermal: 50, cooling: 6);
        engine.Perform(ForgeActionIds.TemperHold);

        Only(engine, ForgeActionIds.VibroHone).Change(ForgeEngine.ThermalMeterId).Delta.Should().Be(0);
        Only(engine, ForgeActionIds.RampPlasma).Change(ForgeEngine.ThermalMeterId).Delta.Should().Be(ForgeRules.RampThermal);
    }

    [Test]
    public void TemperHold_CannotBeUsedAgainWhileHolding_OrOnASetPiece()
    {
        var engine = Engine(new ForgePerkStats { TemperHoldActions = 2 }, cooling: 3);
        engine.Perform(ForgeActionIds.TemperHold);

        var again = engine.Preview(ForgeActionIds.TemperHold);
        again.Enabled.Should().BeFalse();
        again.DisabledReason.Should().Be("The piece is already holding its heat.");

        engine.Perform(ForgeActionIds.VibroHone);
        engine.Perform(ForgeActionIds.VibroHone);
        engine.Preview(ForgeActionIds.TemperHold).Enabled.Should().BeTrue("the hold has ended");

        var set = Shaped(new ForgePerkStats { TemperHoldActions = 2 });
        set.Perform(ForgeActionIds.CryoQuench);
        set.Preview(ForgeActionIds.TemperHold).DisabledReason.Should().Be("The piece is set.");
    }

    [Test]
    public void TemperHold_LongerHoldsKeepThePieceHotLonger()
    {
        var engine = Engine(new ForgePerkStats { TemperHoldActions = 3 }, startingThermal: 60, cooling: 5);

        engine.Perform(ForgeActionIds.TemperHold);
        engine.Thermal.Should().Be(55);
        for (var action = 0; action < 3; action++)
            engine.Perform(ForgeActionIds.VibroHone);

        engine.Thermal.Should().Be(55);
        engine.Perform(ForgeActionIds.VibroHone);
        engine.Thermal.Should().Be(50);
    }

    // Containment Focus

    [Test]
    public void ContainmentFocus_RaisesStableContainmentsBonusForVibroHoneOnly()
    {
        var perks = new ForgePerkStats { ContainmentHonePercent = 25 };
        var plain = Engine(null, new ScriptedMinigameRandom(1));
        var focused = Engine(perks, new ScriptedMinigameRandom(1));
        plain.Perform(ForgeActionIds.VibroHone);
        focused.Perform(ForgeActionIds.VibroHone);
        plain.NextOpportunity.Should().NotBeNull();
        focused.NextOpportunity.Should().NotBeNull();

        Only(plain, ForgeActionIds.VibroHone).Delta.Quality.Should().Be(135, "90 x 1.5");
        Only(focused, ForgeActionIds.VibroHone).Delta.Quality.Should().Be(158, "90 x 1.75 = 157.5");
        Only(focused, ForgeActionIds.HydroPress).Delta.Progress.Should().Be(Only(plain, ForgeActionIds.HydroPress).Delta.Progress);
        focused.NextOpportunity!.Description.Should().Contain("+75% for Vibro-Hone");
    }

    // Perfect Temper

    [TestCase(50, 900, "Perfect Temper")]
    [TestCase(52, 816, "Perfect Temper")]
    [TestCase(48, 816, "Perfect Temper")]
    [TestCase(53, 516, "Quenched in the working range")]
    [TestCase(35, 180, "Quenched in the working range")]
    public void PerfectTemper_GivesHalfAgainTheFinishWithinTwoOfTheRangesCenter(int thermal, int finish, string label)
    {
        var set = Shaped(new ForgePerkStats { PerfectTemperPercent = 50 }, thermal);

        var outcome = Only(set, ForgeActionIds.CryoQuench);

        outcome.Delta.Quality.Should().Be(finish);
        outcome.Label.Should().Be(label);
    }

    [Test]
    public void PerfectTemper_FollowsTheShiftedRangeCenter()
    {
        var perks = new ForgePerkStats { PerfectTemperPercent = 50, RangeShift = 10 };
        var engine = Engine(perks, startingThermal: 60, gearCp: 200);
        while (engine.Meters.Progress.Percent < ForgeRules.QuenchMinimumShapePercent)
            engine.Perform(ForgeActionIds.HydroPress);

        Only(engine, ForgeActionIds.CryoQuench).Delta.Quality.Should().Be(900, "the center is now 60");
    }

    // ---- previews and results -------------------------------------------------------------------------------------------------

    private static ForgePerkStats EveryPerk() => new()
    {
        RangeShift = 10,
        PressOutputPercent = 25,
        HoneOutputPercent = -25,
        RampCpReduction = 2,
        CoolingReduction = 2,
        OverdrivePressPercent = 75,
        PressStreakPercent = 8,
        PressOverheatIntegrityReductionPercent = 25,
        SurgePlasmaThermal = 44,
        RebondThermalReduction = 6,
        RebondCpReduction = 3,
        RedLineCharges = 1,
        HoneIntegrityReduction = 2,
        ContainmentChancePercent = 20,
        MicroHonePercent = 80,
        QuenchEdgePercent = 40,
        HoneHotEdgePercent = 20,
        TemperHoldActions = 2,
        ContainmentHonePercent = 25,
        PerfectTemperPercent = 50
    };

    [Test]
    public void WithEveryPerk_TheWindowListsTheAddedActionsBesideTheOnesTheyBelongWith()
    {
        var engine = Engine(EveryPerk());

        engine.Actions.Select(action => action.Id).Should().Equal(
            ForgeActionIds.RampPlasma, ForgeActionIds.SurgePlasma, ForgeActionIds.HydroPress, ForgeActionIds.OverdrivePress,
            ForgeActionIds.RedLinePress, ForgeActionIds.VibroHone, ForgeActionIds.MicroHone, ForgeActionIds.Rebond,
            ForgeActionIds.TemperHold, ForgeActionIds.CryoQuench);
        engine.Actions.Select(action => action.Name).Should().Equal(
            "Ramp Plasma", "Surge Plasma", "Hydro-Press", "Overdrive Press", "Red Line Press", "Vibro-Hone", "Micro-Hone", "Rebond",
            "Temper Hold", "Cryo-Quench");
        engine.PreviewAll().Should().HaveCount(10);
    }

    [Test]
    public void PerformAcrossManySeedsWithEveryPerk_AlwaysLandsOnAPreviewedOutcome_AndKeepsEveryMeterInRange()
    {
        for (var seed = 1; seed <= 200; seed++)
        {
            var engine = Engine(EveryPerk(), new SeededMinigameRandom(seed), startingThermal: 55, cooling: 5, level: 25, rank: 30);
            var picker = new System.Random(seed);

            for (var turn = 0; turn < 60 && engine.Status == MinigameStatus.InProgress; turn++)
            {
                var action = engine.Actions[picker.Next(engine.Actions.Count)].Id;
                var preview = engine.Preview(action);
                var before = engine.DescribeMeters();

                var result = engine.Perform(action);

                if (!preview.Enabled)
                {
                    result.Performed.Should().BeFalse();
                    engine.DescribeMeters().Should().Equal(before);
                    continue;
                }

                preview.Outcomes.Should().ContainEquivalentOf(result.Outcome, $"seed {seed} turn {turn} {action}");
                engine.DescribeMeters().Zip(before, (after, was) => new MinigameMeterChange(after.Id, after.Label, was.Value, after.Value))
                    .Should().BeEquivalentTo(result.Outcome.Changes, $"seed {seed} turn {turn} {action}");
                foreach (var meter in engine.DescribeMeters())
                    meter.Value.Should().BeInRange(meter.Min, meter.Max);
            }
        }
    }

    [Test]
    public void AForkContinuesFromTheSameCounters_AndIsIndependentOfTheOriginal()
    {
        var engine = Engine(EveryPerk(), startingThermal: 80, cooling: 4);
        engine.Perform(ForgeActionIds.HydroPress);
        engine.Perform(ForgeActionIds.TemperHold);

        var fork = (ForgeEngine)engine.Fork(Quiet());
        fork.Perform(ForgeActionIds.RedLinePress).Performed.Should().BeTrue();

        fork.RedLineReady.Should().BeFalse();
        engine.RedLineReady.Should().BeTrue("the original did not use the Red Line Press");
        fork.TemperHoldLeft.Should().Be(engine.TemperHoldLeft - 1);
        engine.TemperHoldLeft.Should().Be(2);
    }

    [Test]
    public void TheSameSeedPlaysTheSamePerkedCraft()
    {
        string Play(int seed)
        {
            var engine = Engine(EveryPerk(), new SeededMinigameRandom(seed), startingThermal: 50, cooling: 4);
            var trail = new List<string>();
            foreach (var action in new[] { ForgeActionIds.HydroPress, ForgeActionIds.VibroHone, ForgeActionIds.TemperHold, ForgeActionIds.HydroPress })
            {
                engine.Perform(action);
                trail.Add($"{engine.Thermal}/{engine.NextOpportunity?.Id}/{engine.Meters.Progress.Value}/{engine.Meters.Quality.Value}");
            }

            return string.Join(";", trail);
        }

        Play(7).Should().Be(Play(7));
    }

    [Test]
    public void ThePerkSummaryNamesTheStyleAndWhatThePerksAreCounting()
    {
        var engine = Engine(EveryPerk(), startingThermal: 50, cooling: 4);

        ForgePreviewText.PerkSummary(engine, ForgeStyle.HotForging).Should().Be(
            "Style: Hot Forging  |  Hammer Rhythm: next Hydro-Press +0%  |  Red Line ready");

        engine.Perform(ForgeActionIds.HydroPress);
        engine.Perform(ForgeActionIds.HydroPress);
        engine.Perform(ForgeActionIds.TemperHold);
        ForgePreviewText.PerkSummary(engine, ForgeStyle.None).Should().Be(
            "Hammer Rhythm: next Hydro-Press +0%  |  Red Line ready  |  Temper Hold: 2 actions left");

        ForgePreviewText.PerkSummary(Engine(), ForgeStyle.None).Should().BeEmpty("a crafter with no style and no perks sees nothing");
    }
}
