using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using static SWLOR.Game.Server.Tests.Service.Kitchen.KitchenTestKit;

namespace SWLOR.Game.Server.Tests.Service.Kitchen;

/// <summary>Covers the words the Kitchen window shows: exact action rows, warnings, forecasts and the rules shown before commit.</summary>
public class KitchenTextTests
{
    private static ScriptedMinigameRandom Script(params int[][] performs) =>
        new(performs.SelectMany(rolls => rolls).ToArray());

    private static KitchenActionLine Line(KitchenEngine engine, string actionId) =>
        KitchenText.ActionLine(engine.Preview(actionId));

    [Test]
    public void AnAddRow_ShowsTheTypicalPushItsRangeTheCostAndTheChances()
    {
        var engine = Engine(new FixedRolls(100));

        var line = Line(engine, KitchenActionIds.AddIngredient(MeatLine));

        line.Summary.Should().Be("Savory +13 (+8 to +18) | Spice +2 (+1 to +3) | Quality +40 (+17 to +62) | Freshness -4 | Chances 25/50/25%");
        line.Tone.Should().Be(KitchenActionTone.Normal);
    }

    [Test]
    public void ASimmerRow_ShowsDonenessAndTheTurnsFreshnessCost()
    {
        var engine = Engine(new FixedRolls(100));

        Line(engine, KitchenActionIds.Simmer).Summary.Should().Be("Doneness +12 | Freshness -4");
    }

    [Test]
    public void AHeatRow_SaysWhatTheHeatBecomes()
    {
        var engine = Engine(new FixedRolls(100));

        Line(engine, KitchenActionIds.HeatHigh).Summary.Should().Be("Heat becomes High | Freshness -4");
    }

    [Test]
    public void StirAndCoverRows_ShowTheirCp()
    {
        var engine = Engine(Script(Rolls()));
        engine.Perform(KitchenActionIds.Simmer);

        Line(engine, KitchenActionIds.Stir).Summary.Should().Be("CP -8 | Freshness -4");
        Line(engine, KitchenActionIds.Cover).Summary.Should().Be("Lid on | CP -6 | Freshness -2");
    }

    [Test]
    public void AnUnavailableRow_ShowsWhyAndIsDrawnDimmed()
    {
        var engine = Engine(new FixedRolls(100));

        var line = Line(engine, KitchenActionIds.Sear);

        line.Summary.Should().Be("Searing needs High heat.");
        line.Tone.Should().Be(KitchenActionTone.Unavailable);
    }

    [Test]
    public void TheActionThatFinishesTheDishCarriesTheFinishesTheItemWarning()
    {
        var engine = Engine(Script(Rolls(), Rolls(), Rolls()), Soup(), QuarterCook());
        engine.Perform(KitchenActionIds.Simmer);
        engine.Perform(KitchenActionIds.Simmer);
        engine.Perform(KitchenActionIds.Simmer);

        var line = Line(engine, KitchenActionIds.Simmer);

        line.Tone.Should().Be(KitchenActionTone.Finishes);
        line.Summary.Should().EndWith("Finishes the item!");
        KitchenText.Tooltip(engine.Preview(KitchenActionIds.Simmer)).Should().Contain("Finishes the item!");
    }

    [Test]
    public void TheActionThatCouldRuinTheDishIsFlaggedToo()
    {
        var engine = Engine(Script(Rolls()), Soup(freshness: 8), Cook());
        engine.Perform(KitchenActionIds.Simmer);

        var line = Line(engine, KitchenActionIds.Simmer);

        line.Tone.Should().Be(KitchenActionTone.Ruins);
        line.Summary.Should().EndWith("Can ruin the dish!");
    }

    [Test]
    public void TheTooltip_ListsEveryResultWithItsChance()
    {
        var engine = Engine(new FixedRolls(100));

        var tooltip = KitchenText.Tooltip(engine.Preview(KitchenActionIds.AddIngredient(MeatLine)));

        tooltip.Should().Contain("Add Mynock Meat");
        tooltip.Should().Contain("25% Light hand: Quality +17, Freshness -4, Savory +8, Spice +1.");
        tooltip.Should().Contain("50% Steady hand: Quality +40, Freshness -4, Savory +13, Spice +2.");
        tooltip.Should().Contain("25% Heavy hand: Quality +62, Freshness -4, Savory +18, Spice +3.");
    }

    [Test]
    public void TheTooltipOfAnUnavailableActionSaysWhy()
    {
        var engine = Engine(new FixedRolls(100));

        KitchenText.Tooltip(engine.Preview(KitchenActionIds.Sear)).Should().Contain("Unavailable: Searing needs High heat.");
    }

    [Test]
    public void TheDriftText_NamesTheDriftAtEachHeat()
    {
        KitchenText.DriftText(FlavorVector.Zero, FlavorVector.Zero, KitchenHeat.Medium).Should().Be("No drift is announced.");
        KitchenText.DriftText(new FlavorVector(8, 0, 0), new FlavorVector(4, 0, 0), KitchenHeat.Medium).Should().Be(
            "Next drift: Savory +8 at High, Savory +4 at Medium, none at Low. At Medium heat: Savory +4.");
        KitchenText.DriftText(new FlavorVector(0, -8, 0), FlavorVector.Zero, KitchenHeat.Low).Should().Be(
            "Next drift: Sweet -8 at High, Sweet -4 at Medium, none at Low. At Low heat it does nothing.");
    }

    [Test]
    public void ThePeakAromaText_ShinesWhenAnnouncedAndOtherwiseGivesTheChance()
    {
        KitchenText.AromaText(true).Should().Be("PEAK AROMA: your next Add is twice as strong!");
        KitchenText.AromaText(false).Should().Contain("25% chance");
    }

    [Test]
    public void TheThresholdText_ShowsWhichSlotsTheQualityAlreadyGuarantees()
    {
        KitchenText.ThresholdText(60, new[] { 1, 2 }).Should().Be("Enhancements: slot 1 needs 50% (met), slot 2 needs 85% (not met).");
        KitchenText.ThresholdText(85, new[] { 1, 2 }).Should().Be("Enhancements: slot 1 needs 50% (met), slot 2 needs 85% (met).");
        KitchenText.ThresholdText(40, new[] { 1 }).Should().Be("Enhancements: slot 1 needs 50% (not met).");
        KitchenText.ThresholdText(100, Array.Empty<int>()).Should().Be("No enhancements in this dish.");
    }

    [Test]
    public void TheThresholdText_LeavesOutSlotsThatHoldNothing()
    {
        KitchenText.ThresholdText(60, new[] { 2 }).Should().Be("Enhancements: slot 2 needs 85% (not met).");
    }

    [Test]
    public void TheFailureRule_IsShownBeforeTheMaterialsAreCommitted()
    {
        var rule = KitchenText.FailureRule();

        rule.Should().Contain("65%");
        rule.Should().Contain("15% of the XP");
        rule.Should().Contain("each ingredient stack and each enhancement");
    }

    [Test]
    public void TheAxisAndTargetText_GiveBothZones()
    {
        var engine = Engine(new FixedRolls(100));

        KitchenText.AxisText(engine.DescribeAxes()[0]).Should().Be("0 (target 30, perfect 20-40, nothing outside 0-60)");
        KitchenText.TargetText(engine.Recipe.Target).Should().Be(
            "Target flavor: Savory 30, Sweet 0, Spice 16. Perfect within 10; no quality at 30 or more.");
        KitchenText.DistanceText(34, engine.Recipe.Target).Should().Be(
            "Off target by 34. Perfect within 10; no quality at 30 or more. No quality yet.");
        KitchenText.DistanceText(12, engine.Recipe.Target).Should().EndWith("Quality is falling.");
        KitchenText.DistanceText(4, engine.Recipe.Target).Should().EndWith("Perfect.");
    }

    [Test]
    public void TheHeatText_NamesTheHeatAndTheLid()
    {
        KitchenText.HeatText(KitchenHeat.Low, 0).Should().Be("Heat Low: cooks slowly and holds the flavor steady.");
        KitchenText.HeatText(KitchenHeat.High, 1).Should().Be(
            "Heat High: cooks fast and drifts the flavor every turn. The lid is on for 1 more turn.");
    }

    [Test]
    public void TheIngredientLines_ShowThePushOfOnePortion()
    {
        KitchenText.IngredientLine(Meat()).Should().Be("Mynock Meat x3: Savory +13, Spice +2 per portion (3 portions)");
        KitchenText.IngredientLine(Water()).Should().Be("Distilled Water x1: thins every flavor by 25% per portion (1 portion)");
    }

    [Test]
    public void AnUnevenStack_ListsWhatEachPortionPushes()
    {
        var bream = new KitchenIngredientLine(
            "mercanbaligi", "Mercanbaligi", 6, new KitchenComponentFlavor(KitchenIngredientKind.Flavor, new FlavorVector(11, 1, 2)));

        KitchenText.IngredientLine(bream).Should().Be(
            "Mercanbaligi x6: Savory +17/+17/+16/+16, Sweet +2/+2/+1/+1, Spice +3 per portion (4 portions)");
        KitchenText.IngredientLine(Water(6)).Should().Be(
            "Distilled Water x6: thins every flavor by 38/38/37/37% per portion (4 portions)");
        KitchenText.PantryLine().Should().Be("Pantry (free, unlimited): Salt +4 Savory, Sugar +4 Sweet, Spice +4 Spice.");
    }

    [Test]
    public void EveryPlayerFacingKitchenTextIsAsciiOnly()
    {
        var engine = Engine(new FixedRolls(100));
        var texts = new List<string>
        {
            KitchenDefinition.HelpText, KitchenText.FailureRule(), KitchenText.PantryLine(), KitchenText.AromaText(true),
            KitchenText.AromaText(false), KitchenText.HeatText(KitchenHeat.High, 2),
            KitchenText.DriftText(new FlavorVector(8, 0, 0), new FlavorVector(8, 0, 0), KitchenHeat.High),
            KitchenText.ThresholdText(70, new[] { 1, 2 })
        };
        foreach (var preview in engine.PreviewAll())
        {
            var line = KitchenText.ActionLine(preview);
            texts.Add(line.Summary);
            texts.Add(line.Tooltip);
        }

        texts.Concat(engine.Actions.SelectMany(action => new[] { action.Name, action.Description }))
            .Should().OnlyContain(text => text.All(character => character < 128), "game text uses ASCII punctuation only");
    }
}
