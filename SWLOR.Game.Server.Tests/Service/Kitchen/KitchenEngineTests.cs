using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using static SWLOR.Game.Server.Tests.Service.Kitchen.KitchenTestKit;

namespace SWLOR.Game.Server.Tests.Service.Kitchen;

/// <summary>
/// Covers the Kitchen engine: every action, preview, outcome and telegraphed event, the quality law, the thresholds that end an
/// attempt, and that a preview always equals what happens.
/// </summary>
public class KitchenEngineTests
{
    private static ScriptedMinigameRandom Script(params int[][] performs) =>
        new(performs.SelectMany(rolls => rolls).ToArray());

    private static int Savory(KitchenEngine engine) => engine.Meter(KitchenEngine.SavoryMeterId).Value;

    private static int Sweet(KitchenEngine engine) => engine.Meter(KitchenEngine.SweetMeterId).Value;

    private static int Spice(KitchenEngine engine) => engine.Meter(KitchenEngine.SpiceMeterId).Value;

    private static int Freshness(KitchenEngine engine) => engine.Meters.Integrity.Value;

    private static int Doneness(KitchenEngine engine) => engine.Meters.Progress.Value;

    private static int Delta(MinigameOutcomePreview outcome, string meterId) => outcome.Change(meterId).Delta;

    #region Structure

    [Test]
    public void TheKitchenIsTheKitchenMinigame()
    {
        Engine(new FixedRolls(100)).Type.Should().Be(CraftMinigameType.Kitchen);
    }

    [Test]
    public void TheActionsAreHeatSimmerSearStirCoverEachIngredientAndThePantry()
    {
        var engine = Engine(new FixedRolls(100));

        engine.Actions.Select(action => action.Id).Should().Equal(
            KitchenActionIds.HeatLow,
            KitchenActionIds.HeatMedium,
            KitchenActionIds.HeatHigh,
            KitchenActionIds.Simmer,
            KitchenActionIds.Sear,
            KitchenActionIds.Stir,
            KitchenActionIds.Cover,
            KitchenActionIds.AddIngredient(0),
            KitchenActionIds.AddIngredient(1),
            KitchenActionIds.AddIngredient(2),
            KitchenActionIds.AddSalt,
            KitchenActionIds.AddSugar,
            KitchenActionIds.AddSpice);
        engine.Actions.Single(action => action.Id == KitchenActionIds.AddIngredient(0)).Name.Should().Be("Add Mynock Meat");
        engine.Actions.Select(action => action.Id).Should().OnlyHaveUniqueItems();
        engine.PreviewAll().Should().HaveCount(engine.Actions.Count);
    }

    [Test]
    public void AnUnknownActionIsAnError()
    {
        var engine = Engine(new FixedRolls(100));

        var preview = () => engine.Preview("flambe");
        var perform = () => engine.Perform("add.ingredient.9");

        preview.Should().Throw<ArgumentException>();
        perform.Should().Throw<ArgumentException>();
    }

    [Test]
    public void TheDishStartsRawWithAFullPotOfFreshnessAndAllTheCp()
    {
        var engine = Engine(new FixedRolls(100));

        engine.Status.Should().Be(MinigameStatus.InProgress);
        engine.Turn.Should().Be(0);
        Doneness(engine).Should().Be(0);
        engine.Meters.Progress.Max.Should().Be(KitchenTuning.DonenessMax);
        Freshness(engine).Should().Be(StartingFreshness);
        engine.Meters.Integrity.Max.Should().Be(StartingFreshness);
        engine.Meters.Cp.Value.Should().Be(55, "40 gear CP plus 15 for rank 20");
        engine.Profile.Should().Be(FlavorVector.Zero);
        engine.Heat.Should().Be(KitchenHeat.Medium);
        engine.Meters.QualityPercent.Should().Be(0, "an empty pot is 34 from a target whose outer radius is 30");
        engine.NextOpportunity.Should().BeNull();
        engine.NextDrift.IsZero.Should().BeTrue();
    }

    [Test]
    public void TheMetersAreTheSharedFourThenFlavorDistanceHeatCoverAndEachIngredient()
    {
        var engine = Engine(new FixedRolls(100));

        var meters = engine.DescribeMeters();

        meters.Select(meter => meter.Id).Should().Equal(
            MinigameMeterIds.Progress, MinigameMeterIds.Quality, MinigameMeterIds.Integrity, MinigameMeterIds.Cp,
            KitchenEngine.SavoryMeterId, KitchenEngine.SweetMeterId, KitchenEngine.SpiceMeterId,
            KitchenEngine.DistanceMeterId, KitchenEngine.HeatMeterId, KitchenEngine.CoverMeterId,
            KitchenEngine.StockMeterId(0), KitchenEngine.StockMeterId(1), KitchenEngine.StockMeterId(2));
        meters.Select(meter => meter.Label).Should().Equal(
            "Doneness", "Quality", "Freshness", "CP", "Savory", "Sweet", "Spice", "Off Target", "Heat", "Cover",
            "Mynock Meat", "Viscaran Rosemary", "Distilled Water");
    }

    [Test]
    public void EachFlavorAxisMarksThePerfectRangeAndTheDistanceMeterMarksWhereQualityFalls()
    {
        var engine = Engine(new FixedRolls(100));

        engine.Meter(KitchenEngine.SavoryMeterId).Mark.Should().Be(MinigameMeterMark.Range(20, 40, "Perfect"));
        engine.Meter(KitchenEngine.SweetMeterId).Mark.Should().Be(MinigameMeterMark.Range(0, 10, "Perfect"));
        engine.Meter(KitchenEngine.SpiceMeterId).Mark.Should().Be(MinigameMeterMark.Range(6, 26, "Perfect"));
        var distance = engine.Meter(KitchenEngine.DistanceMeterId);
        distance.Value.Should().Be(34);
        distance.Mark.Should().Be(MinigameMeterMark.Range(10, 30, "Quality falls"));
    }

    [Test]
    public void TheAxesViewGivesBothZonesAndTheDishsPlaceOnEachAxis()
    {
        var engine = Engine(new FixedRolls(100));

        var savory = engine.DescribeAxes().Single(axis => axis.Axis == KitchenAxis.Savory);

        savory.Should().Be(new KitchenAxisView(KitchenAxis.Savory, "Savory", 0, 30, 20, 40, 0, 60));
        engine.DescribeAxes().Select(axis => axis.Name).Should().Equal("Savory", "Sweet", "Spice");
    }

    #endregion

    #region Add

    [Test]
    public void AddingAnIngredient_PreviewsALightSteadyAndHeavyPushWithTheirChances()
    {
        var engine = Engine(new FixedRolls(100));

        var preview = engine.Preview(KitchenActionIds.AddIngredient(MeatLine));

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(0);
        preview.IntegrityCost.Should().Be(0);
        preview.Outcomes.Select(outcome => outcome.Label).Should().Equal("Light hand", "Steady hand", "Heavy hand");
        preview.Outcomes.Select(outcome => outcome.ChancePercent).Should().Equal(25, 50, 25);
        preview.Outcomes.Select(outcome => Delta(outcome, KitchenEngine.SavoryMeterId)).Should().Equal(7, 13, 20);
        preview.Outcomes.Select(outcome => Delta(outcome, KitchenEngine.SpiceMeterId)).Should().Equal(1, 2, 3);
        preview.Outcomes.Select(outcome => Delta(outcome, KitchenEngine.SweetMeterId)).Should().OnlyContain(delta => delta == 0);
        preview.Outcomes.Select(outcome => Delta(outcome, MinigameMeterIds.Integrity)).Should().OnlyContain(delta => delta == -4);
        preview.Outcomes.Select(outcome => Delta(outcome, KitchenEngine.StockMeterId(MeatLine))).Should().OnlyContain(delta => delta == -1);
        preview.Outcomes.Select(outcome => Delta(outcome, MinigameMeterIds.Progress)).Should().OnlyContain(delta => delta == 0);
        preview.FinishesItem.Should().BeFalse();
    }

    [TestCase(10, 0, 7, 1)]
    [TestCase(50, 1, 13, 2)]
    [TestCase(90, 2, 20, 3)]
    public void AddingAnIngredient_LandsOnTheResultTheRollPicks(int roll, int index, int savory, int spice)
    {
        var engine = Engine(Script(Rolls(roll)));
        var promised = engine.Preview(KitchenActionIds.AddIngredient(MeatLine)).Outcomes[index];

        var result = engine.Perform(KitchenActionIds.AddIngredient(MeatLine));

        result.Performed.Should().BeTrue();
        result.Outcome.Should().BeEquivalentTo(promised);
        Savory(engine).Should().Be(savory);
        Spice(engine).Should().Be(spice);
        engine.PortionsLeft(MeatLine).Should().Be(2);
    }

    [Test]
    public void AddingIngredients_QualityFollowsWhereTheFlavorSits()
    {
        var engine = Engine(Script(Rolls(50), Rolls(50)));

        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));
        engine.Meters.QualityPercent.Should().Be(40, "(13, 0, 2) is 22.0 from the center, 40% of the way up the slope");
        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));

        // (26, 0, 4) is 12.6 from (30, 0, 16): just outside the 10 inner radius.
        engine.Meters.QualityPercent.Should().Be(87);
        engine.Meters.QualityPercent.Should().Be(engine.Recipe.Target.QualityPercent(engine.Profile));
    }

    [Test]
    public void AddingEveryPortionOfAStack_PushesExactlyTheWholeStack()
    {
        var stack = Soup(freshness: 100, center: new FlavorVector(78, 0, 12), outer: 40, lines: new[] { Meat(6) });
        var engine = Engine(Script(Rolls(50), Rolls(50), Rolls(50), Rolls(50)), stack);
        var pushed = new List<int>();

        for (var portion = 0; portion < 4; portion++)
        {
            var before = Savory(engine);
            engine.Perform(KitchenActionIds.AddIngredient(0));
            pushed.Add(Savory(engine) - before);
        }

        pushed.Should().Equal(20, 20, 19, 19);
        engine.Profile.Should().Be(new FlavorVector(78, 0, 12), "6 x (13, 0, 2), however the stack is split");
    }

    [Test]
    public void AddingAnIngredient_StopsWhenTheCommittedPortionsAreGone()
    {
        var engine = Engine(new FixedRolls(50), Soup(freshness: 200), Cook());
        for (var portion = 0; portion < 3; portion++)
            engine.Perform(KitchenActionIds.AddIngredient(MeatLine)).Performed.Should().BeTrue();

        var preview = engine.Preview(KitchenActionIds.AddIngredient(MeatLine));
        var turnBefore = engine.Turn;
        var result = engine.Perform(KitchenActionIds.AddIngredient(MeatLine));

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("No Mynock Meat left.");
        result.Performed.Should().BeFalse();
        engine.Turn.Should().Be(turnBefore);
        engine.PortionsLeft(MeatLine).Should().Be(0);
        engine.Meter(KitchenEngine.StockMeterId(MeatLine)).Value.Should().Be(0);
    }

    [Test]
    public void ARefusedAction_ChangesNothingAndSpendsNoChance()
    {
        var random = new ScriptedMinigameRandom();
        var engine = Engine(random);
        var meters = engine.DescribeMeters();

        var result = engine.Perform(KitchenActionIds.Sear);

        result.Performed.Should().BeFalse();
        result.RejectedReason.Should().Be("Searing needs High heat.");
        engine.DescribeMeters().Should().Equal(meters);
        engine.Turn.Should().Be(0);
        random.Calls.Should().Be(0);
    }

    [Test]
    public void ControlNarrowsTheSpread()
    {
        var steady = Engine(new FixedRolls(100), stats: Cook(control: 0));
        var skilled = Engine(new FixedRolls(100), stats: Cook(control: 20));

        var plain = steady.Preview(KitchenActionIds.AddIngredient(MeatLine)).Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId));
        var narrow = skilled.Preview(KitchenActionIds.AddIngredient(MeatLine)).Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId));

        plain.Should().Equal(new[] { 7, 13, 20 }, "a 50 percent spread: 13 x 0.5 = 6.5 and 13 x 1.5 = 19.5");
        narrow.Should().Equal(new[] { 9, 13, 17 }, "a 31 percent spread: 13 x 0.69 = 8.97 and 13 x 1.31 = 17.03");
    }

    [Test]
    public void WithEnoughControlThereIsOneSteadyResult()
    {
        var engine = Engine(new FixedRolls(100), stats: Cook(control: 30000));

        var preview = engine.Preview(KitchenActionIds.AddIngredient(MeatLine));

        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].Label.Should().Be("Steady hand");
        preview.Outcomes[0].ChancePercent.Should().Be(100);
        Delta(preview.Outcomes[0], KitchenEngine.SavoryMeterId).Should().Be(13);
    }

    [Test]
    public void PantrySeasonings_PushWeaklyOnOneAxisForFree()
    {
        var engine = Engine(new FixedRolls(100));

        var salt = engine.Preview(KitchenActionIds.AddSalt);
        var sugar = engine.Preview(KitchenActionIds.AddSugar);
        var spice = engine.Preview(KitchenActionIds.AddSpice);

        salt.Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId)).Should().Equal(new[] { 2, 4, 6 }, "4 x 0.5 = 2 and 4 x 1.5 = 6");
        sugar.Outcomes.Select(o => Delta(o, KitchenEngine.SweetMeterId)).Should().Equal(2, 4, 6);
        spice.Outcomes.Select(o => Delta(o, KitchenEngine.SpiceMeterId)).Should().Equal(2, 4, 6);
        new[] { salt, sugar, spice }.Should().OnlyContain(preview => preview.CpCost == 0 && preview.IntegrityCost == 0);
        salt.Outcomes.Select(o => Delta(o, KitchenEngine.SweetMeterId)).Should().OnlyContain(delta => delta == 0);
    }

    [Test]
    public void PantrySeasonings_NeverRunOut()
    {
        var engine = Engine(new FixedRolls(50), Soup(freshness: 400), Cook());

        for (var pinch = 0; pinch < 20; pinch++)
            engine.Perform(KitchenActionIds.AddSalt).Performed.Should().BeTrue();

        engine.Preview(KitchenActionIds.AddSalt).Enabled.Should().BeTrue();
        Savory(engine).Should().Be(80);
        engine.Meter(KitchenEngine.StockMeterId(MeatLine)).Value.Should().Be(3, "the committed ingredients were not touched");
    }

    [Test]
    public void TheFlavorNeverLeavesTheFlavorCube()
    {
        var engine = Engine(new FixedRolls(100), Soup(freshness: 400, center: new FlavorVector(90, 0, 0), outer: 40), Cook());

        for (var pinch = 0; pinch < 40; pinch++)
            engine.Perform(KitchenActionIds.AddSalt);

        Savory(engine).Should().Be(100);
        engine.Meter(KitchenEngine.SavoryMeterId).Max.Should().Be(100);
    }

    [Test]
    public void Water_ThinsEveryFlavorByAQuarterAndUsesUpItsPortion()
    {
        var engine = Engine(Script(Rolls(50), Rolls(50), Rolls()));
        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));
        engine.Perform(KitchenActionIds.AddIngredient(HerbLine));
        engine.Profile.Should().Be(new FlavorVector(16, 0, 12));

        var preview = engine.Preview(KitchenActionIds.AddIngredient(WaterLine));
        engine.Perform(KitchenActionIds.AddIngredient(WaterLine));

        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].Label.Should().Be("Diluted");
        engine.Profile.Should().Be(new FlavorVector(12, 0, 9), "75 percent of (16, 0, 12)");
        engine.PortionsLeft(WaterLine).Should().Be(0);
        engine.Preview(KitchenActionIds.AddIngredient(WaterLine)).Enabled.Should().BeFalse();
    }

    #endregion

    #region Heat and cooking

    [Test]
    public void AdjustingTheHeat_TakesATurnAndSetsTheHeat()
    {
        var engine = Engine(Script(Rolls()));
        var preview = engine.Preview(KitchenActionIds.HeatHigh);

        var result = engine.Perform(KitchenActionIds.HeatHigh);

        preview.Outcomes.Should().ContainSingle();
        Delta(preview.Outcomes[0], KitchenEngine.HeatMeterId).Should().Be(1);
        Delta(preview.Outcomes[0], MinigameMeterIds.Integrity).Should().Be(-4, "the turn costs Freshness like any other");
        result.Performed.Should().BeTrue();
        engine.Heat.Should().Be(KitchenHeat.High);
        engine.Turn.Should().Be(1);
        Freshness(engine).Should().Be(76);
        Doneness(engine).Should().Be(0, "adjusting the heat cooks nothing");
        engine.Meter(KitchenEngine.HeatMeterId).Value.Should().Be((int)KitchenHeat.High);
    }

    [TestCase(KitchenActionIds.HeatLow, KitchenHeat.Low)]
    [TestCase(KitchenActionIds.HeatMedium, KitchenHeat.Medium)]
    public void AdjustingTheHeat_ToTheSettingItAlreadyHasIsRefused(string actionId, KitchenHeat heat)
    {
        var engine = Engine(Script(Rolls(), Rolls()));
        engine.Perform(KitchenActionIds.Heat(heat == KitchenHeat.Low ? KitchenHeat.Medium : KitchenHeat.Low));
        engine.Perform(KitchenActionIds.Heat(heat));

        var preview = engine.Preview(actionId);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be($"The heat is already {heat}.");
    }

    [Test]
    public void Simmer_AddsDonenessAtMediumAndLessAtLow()
    {
        var engine = Engine(Script(Rolls(), Rolls(), Rolls()));

        engine.Preview(KitchenActionIds.Simmer).Outcomes[0].Delta.Progress.Should().Be(12);
        engine.Perform(KitchenActionIds.Simmer);
        Doneness(engine).Should().Be(12);

        engine.Perform(KitchenActionIds.HeatLow);
        engine.Preview(KitchenActionIds.Simmer).Outcomes[0].Delta.Progress.Should().Be(5);
        engine.Perform(KitchenActionIds.Simmer);
        Doneness(engine).Should().Be(17);
    }

    [Test]
    public void Simmer_IsRefusedAtHighHeat()
    {
        var engine = Engine(Script(Rolls()));
        engine.Perform(KitchenActionIds.HeatHigh);

        var preview = engine.Preview(KitchenActionIds.Simmer);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Turn the heat down to Medium or Low to simmer.");
    }

    [Test]
    public void Sear_AddsALotOfDonenessAtHighHeatAndCostsFreshness()
    {
        var engine = Engine(Script(Rolls(), Rolls()));
        engine.Perform(KitchenActionIds.HeatHigh);

        var preview = engine.Preview(KitchenActionIds.Sear);
        engine.Perform(KitchenActionIds.Sear);

        preview.IntegrityCost.Should().Be(6);
        preview.CpCost.Should().Be(0);
        preview.Outcomes[0].Delta.Progress.Should().Be(36);
        Delta(preview.Outcomes[0], MinigameMeterIds.Integrity).Should().Be(-10, "6 for the sear and 4 for the turn");
        Doneness(engine).Should().Be(36);
        Freshness(engine).Should().Be(80 - 4 - 10);
    }

    [TestCase(KitchenActionIds.HeatMedium)]
    [TestCase(KitchenActionIds.HeatLow)]
    public void Sear_IsRefusedBelowHighHeat(string heatAction)
    {
        var engine = Engine(Script(Rolls()));
        if (heatAction != KitchenActionIds.HeatMedium)
            engine.Perform(heatAction);

        var preview = engine.Preview(KitchenActionIds.Sear);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Searing needs High heat.");
    }

    [Test]
    public void Craftsmanship_RaisesDonenessAndSoDoesRankAboveTheRecipe()
    {
        var plain = Engine(new FixedRolls(100));
        var crafty = Engine(new FixedRolls(100), stats: Cook(craftsmanship: 30));
        var overqualified = Engine(new FixedRolls(100), stats: Cook(rank: 30));

        var simmer = KitchenTuning.SimmerMediumDoneness;
        var craftyFactor = 1 + KitchenTuning.CraftsmanshipDonenessPerPoint * 30;
        plain.Preview(KitchenActionIds.Simmer).Outcomes[0].Delta.Progress.Should().Be(simmer);
        crafty.Preview(KitchenActionIds.Simmer).Outcomes[0].Delta.Progress.Should()
            .Be((int)Math.Round(simmer * craftyFactor, MidpointRounding.AwayFromZero), $"{simmer} x {craftyFactor}");
        overqualified.Preview(KitchenActionIds.Simmer).Outcomes[0].Delta.Progress.Should()
            .Be((int)Math.Round(simmer * KitchenTuning.LevelFactorMax, MidpointRounding.AwayFromZero), "the most a high rank adds");
    }

    #endregion

    #region Drift

    [Test]
    public void TheNextDriftIsRolledAfterAnActionAndShownBeforeItHappens()
    {
        var random = Script(Rolls(axis: 1, direction: 1));
        var engine = Engine(random);

        engine.Perform(KitchenActionIds.Simmer);

        engine.NextDrift.Should().Be(new FlavorVector(0, 8, 0));
        engine.EffectiveNextDrift.Should().Be(new FlavorVector(0, 4, 0), "Medium heat lets half of it through");
        Sweet(engine).Should().Be(0, "nothing has moved yet");
    }

    [TestCase(0, 0, -8, 0, 0)]
    [TestCase(0, 1, 8, 0, 0)]
    [TestCase(1, 0, 0, -8, 0)]
    [TestCase(2, 1, 0, 0, 8)]
    public void TheDriftPushesOneAxisUpOrDown(int axis, int direction, int savory, int sweet, int spice)
    {
        var engine = Engine(Script(Rolls(axis: axis, direction: direction)));

        engine.Perform(KitchenActionIds.Simmer);

        engine.NextDrift.Should().Be(new FlavorVector(savory, sweet, spice));
    }

    [Test]
    public void TheAnnouncedDriftMovesTheFlavorAtTheEndOfTheNextTurnByTheHeatItEndsAt()
    {
        var engine = Engine(Script(Rolls(axis: 1, direction: 1)));
        engine.Perform(KitchenActionIds.Simmer);

        var simmer = engine.Preview(KitchenActionIds.Simmer).Outcomes[0];
        var high = engine.Preview(KitchenActionIds.HeatHigh).Outcomes[0];
        var low = engine.Preview(KitchenActionIds.HeatLow).Outcomes[0];

        Delta(simmer, KitchenEngine.SweetMeterId).Should().Be(4, "Medium: half of 8");
        Delta(high, KitchenEngine.SweetMeterId).Should().Be(8, "High: all of it");
        Delta(low, KitchenEngine.SweetMeterId).Should().Be(0, "Low holds the flavor steady");
    }

    [Test]
    public void TheDriftHappensOnceAndANewOneIsRolled()
    {
        var engine = Engine(Script(Rolls(axis: 1, direction: 1), Rolls(axis: 2, direction: 0), Rolls(axis: 2, direction: 0)));
        engine.Perform(KitchenActionIds.Simmer);

        engine.Perform(KitchenActionIds.Simmer);

        Sweet(engine).Should().Be(4);
        engine.NextDrift.Should().Be(new FlavorVector(0, 0, -8));

        engine.Perform(KitchenActionIds.Simmer);

        Sweet(engine).Should().Be(4, "the first drift is spent");
        Spice(engine).Should().Be(0, "a drift cannot push an axis below zero");
    }

    [Test]
    public void Stir_CancelsTheAnnouncedDriftForCp()
    {
        var engine = Engine(Script(Rolls(axis: 0, direction: 1), Rolls()));
        engine.Perform(KitchenActionIds.Simmer);
        var preview = engine.Preview(KitchenActionIds.Stir);

        engine.Perform(KitchenActionIds.Stir);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(KitchenTuning.StirCp);
        Delta(preview.Outcomes[0], KitchenEngine.SavoryMeterId).Should().Be(0, "the drift that would have moved it is gone");
        Savory(engine).Should().Be(0);
        engine.Meters.Cp.Value.Should().Be(55 - KitchenTuning.StirCp);
        Doneness(engine).Should().Be(12, "stirring does not cook");
    }

    [Test]
    public void Stir_IsRefusedWhenNothingIsDrifting()
    {
        var engine = Engine(new FixedRolls(100));

        var preview = engine.Preview(KitchenActionIds.Stir);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Nothing is drifting.");
    }

    [Test]
    public void Stir_IsRefusedAtLowHeatWhichAlreadyHoldsTheFlavorSteady()
    {
        var engine = Engine(Script(Rolls(axis: 0, direction: 1)));
        engine.Perform(KitchenActionIds.HeatLow);

        var preview = engine.Preview(KitchenActionIds.Stir);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Low heat holds the flavor steady.");
    }

    #endregion

    #region Cover

    [Test]
    public void Cover_HalvesFreshnessLossForThreeTurnsCountingItsOwn()
    {
        var engine = Engine(Script(Rolls(), Rolls(), Rolls(), Rolls()));
        var preview = engine.Preview(KitchenActionIds.Cover);

        engine.Perform(KitchenActionIds.Cover);

        preview.CpCost.Should().Be(6);
        Delta(preview.Outcomes[0], MinigameMeterIds.Integrity).Should().Be(-2);
        Delta(preview.Outcomes[0], KitchenEngine.CoverMeterId).Should().Be(2, "3 turns, one of which passes at the end of this one");
        Freshness(engine).Should().Be(78);
        engine.CoverTurns.Should().Be(2);

        engine.Perform(KitchenActionIds.Simmer);
        engine.Perform(KitchenActionIds.Simmer);
        Freshness(engine).Should().Be(74);
        engine.CoverTurns.Should().Be(0);

        engine.Perform(KitchenActionIds.Simmer);
        Freshness(engine).Should().Be(70, "the lid has done its three turns");
    }

    [Test]
    public void Cover_IsRefusedWhileTheLidIsOn()
    {
        var engine = Engine(Script(Rolls()));
        engine.Perform(KitchenActionIds.Cover);

        var preview = engine.Preview(KitchenActionIds.Cover);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("The lid is already on.");
    }

    [Test]
    public void Cover_HalvesTheFreshnessASearCosts()
    {
        var engine = Engine(Script(Rolls(), Rolls()));
        engine.Perform(KitchenActionIds.HeatHigh);
        engine.Preview(KitchenActionIds.Sear).IntegrityCost.Should().Be(6);
        engine.Perform(KitchenActionIds.Cover);

        var preview = engine.Preview(KitchenActionIds.Sear);

        preview.IntegrityCost.Should().Be(3);
        Delta(preview.Outcomes[0], MinigameMeterIds.Integrity).Should().Be(-5, "3 for the sear and 2 for the covered turn");
    }

    #endregion

    #region Peak Aroma

    [Test]
    public void PeakAroma_IsAnnouncedForTheNextActionWithTheForecastRoll()
    {
        var engine = Engine(Script(Rolls(aroma: 25)));

        engine.Perform(KitchenActionIds.Simmer);

        engine.NextOpportunity.Should().NotBeNull();
        engine.NextOpportunity.Name.Should().Be("Peak Aroma");
        engine.NextOpportunity.Description.Should().Be("The next Add is twice as strong.");
        engine.NextOpportunity.TurnsAway.Should().Be(1);
        engine.IsPeakAromaNext.Should().BeTrue();
    }

    [Test]
    public void PeakAroma_IsNotAnnouncedOnARollAboveItsChance()
    {
        var engine = Engine(Script(Rolls(aroma: 26)));

        engine.Perform(KitchenActionIds.Simmer);

        engine.NextOpportunity.Should().BeNull();
        engine.IsPeakAromaNext.Should().BeFalse();
    }

    [Test]
    public void PeakAroma_MakesTheNextAddTwiceAsStrongInThePreviewAndTheResult()
    {
        var engine = Engine(Script(Rolls(aroma: 25), Rolls(50)));
        engine.Perform(KitchenActionIds.Simmer);

        var preview = engine.Preview(KitchenActionIds.AddIngredient(MeatLine));
        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));

        preview.Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId)).Should().Equal(13, 26, 39);
        preview.Outcomes.Select(o => Delta(o, KitchenEngine.SpiceMeterId)).Should().Equal(2, 4, 6);
        Savory(engine).Should().Be(26);
        Spice(engine).Should().Be(4);
    }

    [Test]
    public void PeakAroma_AlsoDoublesAPantryPushAndAPortionOfWater()
    {
        var engine = Engine(Script(Rolls(aroma: 25)));
        engine.Perform(KitchenActionIds.Simmer);

        var salt = engine.Preview(KitchenActionIds.AddSalt);
        var water = engine.Preview(KitchenActionIds.AddIngredient(WaterLine));

        salt.Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId)).Should().Equal(new[] { 4, 8, 12 }, "4 x 2 x 0.5 = 4, 8 and 12");
        water.Outcomes.Should().ContainSingle();
    }

    [Test]
    public void PeakAroma_ThinsTwiceAsMuchWhenWaterIsAdded()
    {
        var engine = Engine(Script(Rolls(50), Rolls(aroma: 25), Rolls()));
        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));
        engine.Perform(KitchenActionIds.Simmer);
        engine.Profile.Should().Be(new FlavorVector(13, 0, 2));

        engine.Perform(KitchenActionIds.AddIngredient(WaterLine));

        engine.Profile.Should().Be(new FlavorVector(7, 0, 1), "half of (13, 0, 2)");
    }

    [Test]
    public void PeakAroma_IsUsedOrLostAfterTheNextAction()
    {
        var engine = Engine(Script(Rolls(aroma: 25), Rolls()));
        engine.Perform(KitchenActionIds.Simmer);

        engine.Perform(KitchenActionIds.Simmer);

        engine.NextOpportunity.Should().BeNull("it was announced for one turn only");
        Delta(engine.Preview(KitchenActionIds.AddIngredient(MeatLine)).Outcomes[1], KitchenEngine.SavoryMeterId).Should().Be(13);
    }

    #endregion

    #region Ending the attempt

    [Test]
    public void TheDishIsDoneWhenDonenessIsFull_AndThePreviewWarnsTheActionFinishesIt()
    {
        var engine = Engine(Script(Rolls(), Rolls(), Rolls()), Soup(), QuarterCook());
        engine.Perform(KitchenActionIds.Simmer);
        engine.Perform(KitchenActionIds.Simmer);
        engine.Perform(KitchenActionIds.Simmer);
        Doneness(engine).Should().Be(3 * QuarterSimmer);
        var freshnessBefore = Freshness(engine);

        var preview = engine.Preview(KitchenActionIds.Simmer);
        var result = engine.Perform(KitchenActionIds.Simmer);

        preview.FinishesItem.Should().BeTrue();
        preview.Outcomes[0].ResultingStatus.Should().Be(MinigameStatus.Succeeded);
        result.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Status.Should().Be(MinigameStatus.Succeeded);
        Doneness(engine).Should().Be(KitchenTuning.DonenessMax);
        Freshness(engine).Should().Be(freshnessBefore, "the turn ends there: no decay and no drift");
        engine.Preview(KitchenActionIds.Simmer).Enabled.Should().BeFalse();
        engine.Preview(KitchenActionIds.Simmer).DisabledReason.Should().Be("The attempt is over.");
    }

    [Test]
    public void AFinishedDishKeepsTheQualityItsFlavorEarned()
    {
        var engine = Engine(
            Script(Rolls(50), Rolls(50), Rolls(), Rolls(), Rolls()),
            Soup(),
            QuarterCook());
        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));
        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));
        engine.Perform(KitchenActionIds.Simmer);
        engine.Perform(KitchenActionIds.Simmer);
        engine.Perform(KitchenActionIds.Simmer);
        engine.Perform(KitchenActionIds.Simmer);

        engine.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Meters.Quality.Value.Should().Be(engine.Recipe.Target.QualityPercent(engine.Profile));
        engine.Meters.Quality.Max.Should().Be(100);
    }

    [Test]
    public void TheDishIsRuinedWhenFreshnessRunsOut()
    {
        var engine = Engine(Script(Rolls()), Soup(freshness: 8), Cook());
        engine.Perform(KitchenActionIds.Simmer);
        Freshness(engine).Should().Be(4);

        var preview = engine.Preview(KitchenActionIds.Simmer);
        var result = engine.Perform(KitchenActionIds.Simmer);

        preview.CanBreakItem.Should().BeTrue();
        preview.Outcomes[0].ResultingStatus.Should().Be(MinigameStatus.Failed);
        result.Status.Should().Be(MinigameStatus.Failed);
        Freshness(engine).Should().Be(0);
        engine.Status.Should().Be(MinigameStatus.Failed);
        engine.Perform(KitchenActionIds.Simmer).Performed.Should().BeFalse();
    }

    [Test]
    public void ASearThatFinishesTheDishBeatsFreshnessRunningOutOnTheSameAction()
    {
        var engine = Engine(
            Script(Rolls()),
            Soup(freshness: 10),
            Cook(craftsmanship: CraftsmanshipFor(KitchenTuning.SearDoneness, KitchenTuning.DonenessMax)));
        engine.Perform(KitchenActionIds.HeatHigh);
        Freshness(engine).Should().Be(6);

        var preview = engine.Preview(KitchenActionIds.Sear);
        engine.Perform(KitchenActionIds.Sear);

        preview.Outcomes[0].ResultingStatus.Should().Be(MinigameStatus.Succeeded);
        engine.Status.Should().Be(MinigameStatus.Succeeded);
        Freshness(engine).Should().Be(0);
    }

    [Test]
    public void FreshnessFallsEveryTurnWhateverTheActionIs()
    {
        var engine = Engine(Script(Rolls(), Rolls(50), Rolls()));

        engine.Perform(KitchenActionIds.HeatLow);
        engine.Perform(KitchenActionIds.AddSalt);
        engine.Perform(KitchenActionIds.HeatMedium);

        Freshness(engine).Should().Be(StartingFreshness - 12);
    }

    #endregion

    #region CP

    [Test]
    public void StirAndCover_NeedTheCpTheyCost()
    {
        var engine = Engine(Script(Rolls(axis: 0, direction: 1)), Soup(), Cook(rank: 10, gearCp: 0));
        engine.Meters.Cp.Value.Should().Be(7);
        engine.Perform(KitchenActionIds.Simmer);

        var stir = engine.Preview(KitchenActionIds.Stir);
        var cover = engine.Preview(KitchenActionIds.Cover);

        stir.Enabled.Should().BeFalse();
        stir.DisabledReason.Should().Be("Not enough CP.");
        cover.Enabled.Should().BeTrue();
    }

    [Test]
    public void ARankOneCookHasNoCpSoNeitherStirNorCoverIsAvailable()
    {
        var engine = Engine(new FixedRolls(100), Soup(), Cook(rank: 1, gearCp: 0));

        engine.Meters.Cp.Value.Should().Be(0);
        engine.Preview(KitchenActionIds.Cover).Enabled.Should().BeFalse();
        engine.Preview(KitchenActionIds.Cover).DisabledReason.Should().Be("Not enough CP.");
    }

    #endregion

    #region Preview equals result

    [Test]
    public void APreviewIsNeverPartOfTheForecastAndSpendsNoChance()
    {
        var random = new ScriptedMinigameRandom();
        var engine = Engine(random);

        engine.PreviewAll();

        random.Calls.Should().Be(0);
        engine.PreviewAll().SelectMany(action => action.Outcomes).Should().OnlyContain(outcome => outcome.Changes.Count == 13);
    }

    [Test]
    public void TheForecastRollsInOrder_DriftAxisDriftDirectionThenPeakAroma()
    {
        var random = Script(Rolls(axis: 2, direction: 0, aroma: 25));
        var engine = Engine(random);

        engine.Perform(KitchenActionIds.Simmer);

        random.Calls.Should().Be(3);
        engine.NextDrift.Should().Be(new FlavorVector(0, 0, -8));
        engine.IsPeakAromaNext.Should().BeTrue();
    }

    [Test]
    public void TheForecastIsNotRolledOnceTheAttemptHasEnded()
    {
        var random = new ScriptedMinigameRandom();
        var engine = Engine(random, Soup(), Cook(craftsmanship: CraftsmanshipFor(KitchenTuning.SimmerMediumDoneness, KitchenTuning.DonenessMax)));

        engine.Perform(KitchenActionIds.Simmer);

        engine.Status.Should().Be(MinigameStatus.Succeeded);
        random.Calls.Should().Be(0);
    }

    [TestCase(KitchenActionIds.HeatLow, 0)]
    [TestCase(KitchenActionIds.HeatHigh, 0)]
    [TestCase(KitchenActionIds.Simmer, 0)]
    [TestCase(KitchenActionIds.Cover, 0)]
    [TestCase(KitchenActionIds.AddSalt, 10)]
    [TestCase(KitchenActionIds.AddSugar, 50)]
    [TestCase(KitchenActionIds.AddSpice, 90)]
    [TestCase("add.ingredient.0", 10)]
    [TestCase("add.ingredient.0", 50)]
    [TestCase("add.ingredient.0", 90)]
    [TestCase("add.ingredient.1", 50)]
    [TestCase("add.ingredient.2", 0)]
    public void Perform_ChangesEveryMeterByExactlyWhatThePreviewPromised(string actionId, int outcomeRoll)
    {
        var rolls = outcomeRoll > 0 ? Rolls(outcomeRoll, axis: 1, direction: 1) : Rolls(axis: 1, direction: 1);
        var engine = Engine(Script(Rolls(axis: 0, direction: 1), rolls), Soup(freshness: 120), Cook(control: 13));
        engine.Perform(KitchenActionIds.Simmer);
        var index = outcomeRoll switch { 0 => 0, <= 25 => 0, <= 75 => 1, _ => 2 };
        var promised = engine.Preview(actionId).Outcomes[index];
        var before = engine.DescribeMeters();
        var beforeShared = engine.Meters;

        var result = engine.Perform(actionId);

        var after = engine.DescribeMeters();
        result.Performed.Should().BeTrue();
        result.Outcome.Should().BeEquivalentTo(promised);
        before.Zip(after, (b, a) => new MinigameMeterChange(a.Id, a.Label, b.Value, a.Value))
            .Should().BeEquivalentTo(promised.Changes);
        beforeShared.DeltaTo(engine.Meters).Should().Be(promised.Delta);
        result.MetersAfter.Should().Equal(after);
        result.Status.Should().Be(promised.ResultingStatus);
    }

    [Test]
    public void Perform_AcrossManySeeds_AlwaysLandsOnAPreviewedOutcomeAndKeepsEveryInvariant()
    {
        for (var seed = 1; seed <= 40; seed++)
        {
            var recipe = Soup(freshness: 140);
            var stats = Cook(craftsmanship: seed % 40, control: seed % 30);
            var engine = Engine(new SeededMinigameRandom(seed), recipe, stats);
            var picker = new System.Random(seed);

            for (var turn = 0; turn < 60 && engine.Status == MinigameStatus.InProgress; turn++)
            {
                var actionId = engine.Actions[picker.Next(engine.Actions.Count)].Id;
                var preview = engine.Preview(actionId);
                var before = engine.DescribeMeters();

                var result = engine.Perform(actionId);

                if (!preview.Enabled)
                {
                    result.Performed.Should().BeFalse();
                    engine.DescribeMeters().Should().Equal(before);
                    continue;
                }

                preview.Outcomes.Should().ContainEquivalentOf(result.Outcome, $"seed {seed} turn {turn}");
                before.Zip(engine.DescribeMeters(), (b, a) => new MinigameMeterChange(a.Id, a.Label, b.Value, a.Value))
                    .Should().BeEquivalentTo(result.Outcome.Changes);
                foreach (var meter in engine.DescribeMeters())
                    meter.Value.Should().BeInRange(meter.Min, meter.Max, $"{meter.Id} seed {seed} turn {turn}");

                engine.Meters.Quality.Value.Should().Be(recipe.Target.QualityPercent(engine.Profile));
            }
        }
    }

    #endregion
}
