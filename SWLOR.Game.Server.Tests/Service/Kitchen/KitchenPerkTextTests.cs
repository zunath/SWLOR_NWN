using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

namespace SWLOR.Game.Server.Tests.Service.Kitchen;

/// <summary>Covers the words the Kitchen window shows for a Chef's perks, which are built from the same numbers the engine reads.</summary>
public class KitchenPerkTextTests
{
    private static readonly KitchenPerkStats WholeLine = new(
        PantryPushBonus: 1, PushSpreadReduction: 5, SeasonedPalateOwned: true, SeasonedPalateOn: true, SeasonedPalateSpreadPercent: 25,
        LowHeatDonenessPercent: 100, MediumDriftReductionPercent: 25, HighDriftReductionPercent: 20,
        ReducePercent: 35, ReduceCpReduction: 3, PeakAromaChancePoints: 10, StirCpReduction: 4, StirDonenessPercent: 50, SignatureDish: true);

    [Test]
    public void ACookWithNoPerks_HasNoPerkLines()
    {
        KitchenText.PerkLines(KitchenPerkStats.None).Should().BeEmpty();
    }

    [Test]
    public void EachNumberAPerkChanges_HasALineThatNamesIt()
    {
        var lines = KitchenText.PerkLines(WholeLine, new KitchenStats(50, 0, 0, 0, WholeLine));

        lines.Should().Contain("Each pantry pinch pushes 5 instead of 4.");
        lines.Should().Contain(line => line.Contains("spread 45% instead of 50%"));
        lines.Should().Contain(line => line.StartsWith("Seasoned Palate is ON") && line.Contains("half as far"));
        lines.Should().Contain("While it is on, pushes are also 25% less random.");
        lines.Should().Contain("At Low heat, Simmer gives 100% more Doneness.");
        lines.Should().Contain("At Medium heat, the flavor drifts 25% less.");
        lines.Should().Contain("At High heat, the flavor drifts 20% less.");
        lines.Should().Contain(line => line.StartsWith("Reduce: 9 CP") && line.Contains("35%"));
        lines.Should().Contain("Each turn has a 35% chance of announcing Peak Aroma.");
        lines.Should().Contain("Stir costs 8 CP and at Medium heat gives 50% of a Simmer's Doneness.");
        lines.Should().Contain(line => line.StartsWith("Signature Dish: once per dish") && line.Contains("(no CP)"));
    }

    [Test]
    public void SeasonedPalateIsOffUntilTheCookSwitchesItOn()
    {
        var off = WholeLine with { SeasonedPalateOn = false, SeasonedPalateSpreadPercent = 0 };

        KitchenText.PerkLines(off).Should().Contain(line => line.StartsWith("Seasoned Palate is off"));
        KitchenText.PerkLines(off).Should().NotContain(line => line.Contains("less random"), "the narrower spread counts only while it is on");
        KitchenText.PerkLines(KitchenPerkStats.None with { SeasonedPalateOwned = false }).Should().NotContain(line => line.Contains("Seasoned Palate"));
    }

    [Test]
    public void EveryPerkLineIsAsciiAndShortEnoughForTheSetupList()
    {
        foreach (var line in KitchenText.PerkLines(WholeLine, new KitchenStats(50, 0, 0, 0, WholeLine)))
        {
            line.Should().MatchRegex("^[\\x20-\\x7E]+$");
            line.Length.Should().BeLessThan(90, line);
        }
    }

    [Test]
    public void ThePantryLine_ShowsThePushThePerkGives()
    {
        KitchenText.PantryLine().Should().Contain("Salt +4 Savory");
        KitchenText.PantryLine(5).Should().Contain("Salt +5 Savory").And.Contain("Sugar +5 Sweet").And.Contain("Spice +5 Spice");
    }

    [Test]
    public void TheDriftText_ShowsWhatTheCooksPerksLetThrough()
    {
        var announced = new FlavorVector(0, 8, 0);
        var stats = new KitchenStats(20, 0, 0, 0, new KitchenPerkStats(MediumDriftReductionPercent: 25, HighDriftReductionPercent: 35));

        var plain = KitchenText.DriftText(announced, new FlavorVector(0, 4, 0), KitchenHeat.Medium);
        var perked = KitchenText.DriftText(announced, stats.Drift(announced, KitchenHeat.Medium), KitchenHeat.Medium, stats);

        plain.Should().Contain("Sweet +8 at High").And.Contain("Sweet +4 at Medium");
        perked.Should().Contain("Sweet +5 at High").And.Contain("Sweet +3 at Medium").And.Contain("At Medium heat: Sweet +3.");
    }

    [Test]
    public void TheAromaText_ShowsTheChanceTheCookHas()
    {
        KitchenText.AromaText(false).Should().Contain("25%");
        KitchenText.AromaText(false, 35).Should().Contain("35%");
        KitchenText.AromaText(true, 35).Should().StartWith("PEAK AROMA");
    }
}
