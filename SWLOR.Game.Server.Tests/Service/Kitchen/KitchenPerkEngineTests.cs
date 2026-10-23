using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.StatService;
using static SWLOR.Game.Server.Tests.Service.Kitchen.KitchenTestKit;

namespace SWLOR.Game.Server.Tests.Service.Kitchen;

/// <summary>
/// Covers what a Chef's perks change in the Kitchen: each number the engine reads from <see cref="KitchenPerkStats"/>, the two actions
/// the line adds (Reduce and Signature Dish), and that a cook with no perks plays exactly the Kitchen as tuned.
/// </summary>
public class KitchenPerkEngineTests
{
    private static ScriptedMinigameRandom Script(params int[][] performs) =>
        new(performs.SelectMany(rolls => rolls).ToArray());

    private static KitchenStats Chef(KitchenPerkStats perks, int control = 0, int rank = 20, int craftsmanship = 0) =>
        new(rank, craftsmanship, control, 40, perks);

    private static KitchenEngine ChefEngine(KitchenPerkStats perks, IMinigameRandom random = null, KitchenRecipe recipe = null, int control = 0) =>
        Engine(random ?? new FixedRolls(50), recipe ?? Soup(), Chef(perks, control));

    private static int Meter(KitchenEngine engine, string id) => engine.Meter(id).Value;

    private static int Delta(MinigameOutcomePreview outcome, string meterId) => outcome.Change(meterId).Delta;

    #region Stat totals

    [Test]
    public void AnUnperkedCook_HasNoPerkNumbers_AndTheKitchenAsTuned()
    {
        KitchenPerkStats.None.HasAny.Should().BeFalse();
        Cook().Perks.Should().Be(KitchenPerkStats.None);
        new KitchenStats(20, 0, 0, 40, null).Perks.Should().Be(KitchenPerkStats.None);

        var stats = Cook();
        stats.PushSpreadPercent.Should().Be(50);
        stats.PantryPush.Should().Be(KitchenTuning.PantryPush);
        stats.SimmerBase(KitchenHeat.Low).Should().Be(KitchenTuning.SimmerLowDoneness);
        stats.SimmerBase(KitchenHeat.Medium).Should().Be(KitchenTuning.SimmerMediumDoneness);
        stats.SimmerBase(KitchenHeat.High).Should().Be(0);
        stats.StirBase(KitchenHeat.Medium).Should().Be(0);
        stats.StirCp.Should().Be(KitchenTuning.StirCp);
        stats.PeakAromaChancePercent.Should().Be(KitchenTuning.PeakAromaChancePercent);
        stats.Drift(new FlavorVector(0, 8, 0), KitchenHeat.High).Should().Be(new FlavorVector(0, 8, 0));
        stats.Drift(new FlavorVector(0, 8, 0), KitchenHeat.Medium).Should().Be(new FlavorVector(0, 4, 0));
        stats.Drift(new FlavorVector(0, 8, 0), KitchenHeat.Low).Should().Be(FlavorVector.Zero);
    }

    [Test]
    public void ThePerkStatsAreReadFromTheCooksStatTotals()
    {
        var totals = new Dictionary<StatType, int>
        {
            [StatType.KitchenPantryPush] = 1,
            [StatType.KitchenPushSpreadReduction] = 5,
            [StatType.KitchenSeasonedPalate] = 1,
            [StatType.KitchenSeasonedPalateSpreadPercent] = 25,
            [StatType.KitchenLowHeatDonenessPercent] = 100,
            [StatType.KitchenMediumDriftReductionPercent] = 25,
            [StatType.KitchenHighHeatDriftReductionPercent] = 35,
            [StatType.KitchenReducePercent] = 35,
            [StatType.KitchenReduceCpReduction] = 3,
            [StatType.KitchenPeakAromaChancePoints] = 10,
            [StatType.KitchenStirCpReduction] = 4,
            [StatType.KitchenStirDonenessPercent] = 50,
            [StatType.KitchenSignatureDish] = 1
        };
        int Total(StatType stat) => totals.GetValueOrDefault(stat);

        var on = KitchenPerkStats.FromStatTotals(Total, seasonedPalateOn: true);
        var off = KitchenPerkStats.FromStatTotals(Total);

        on.Should().Be(new KitchenPerkStats(
            1, 5, true, true, 25, 100, 25, 35, 35, 3, 10, 4, 50, true));
        off.Should().Be(on with { SeasonedPalateOn = false, SeasonedPalateSpreadPercent = 0 }, "Seasoned Palate and what it adds count only while it is switched on");
        off.SeasonedPalateOwned.Should().BeTrue("the window still offers the switch");
        KitchenPerkStats.FromStatTotals(_ => 0, seasonedPalateOn: true).Should().Be(KitchenPerkStats.None, "a cook without Seasoned Palate cannot switch it on");
    }

    [Test]
    public void SharpPalateTakesPointsOffTheBaseSpreadBeforeControl_AndSeasonedPalateNarrowsWhatIsLeft()
    {
        new KitchenStats(20, 0, 0, 0, new KitchenPerkStats(PushSpreadReduction: 5)).PushSpreadPercent.Should().Be(45);
        new KitchenStats(20, 0, 20, 0, new KitchenPerkStats(PushSpreadReduction: 5)).PushSpreadPercent.Should().Be(28, "45 x 100 / 160");
        new KitchenStats(20, 0, 0, 0, new KitchenPerkStats(PushSpreadReduction: 5, SeasonedPalateOn: true, SeasonedPalateSpreadPercent: 25))
            .PushSpreadPercent.Should().Be(34, "45 less a quarter is 33.75");
        new KitchenStats(20, 0, 20, 0, new KitchenPerkStats(PushSpreadReduction: 5, SeasonedPalateOn: true, SeasonedPalateSpreadPercent: 25))
            .PushSpreadPercent.Should().Be(21, "28.125 less a quarter is 21.09");
        new KitchenStats(20, 0, 0, 0, new KitchenPerkStats(PushSpreadReduction: 500)).PushSpreadPercent.Should().Be(0);
    }

    [Test]
    public void TheDriftADishTakesIsTheAnnouncedOneWeakenedByTheHeatAndThePerks()
    {
        var announced = new FlavorVector(0, 8, 0);
        var slow = new KitchenStats(20, 0, 0, 0, new KitchenPerkStats(MediumDriftReductionPercent: 25));
        var flash = new KitchenStats(20, 0, 0, 0, new KitchenPerkStats(HighDriftReductionPercent: 35));

        slow.Drift(announced, KitchenHeat.Medium).Should().Be(new FlavorVector(0, 3, 0), "half of 8 less a quarter is 3");
        slow.Drift(announced, KitchenHeat.High).Should().Be(announced, "Slow Cooking leaves High alone");
        flash.Drift(announced, KitchenHeat.High).Should().Be(new FlavorVector(0, 5, 0), "8 less 35 percent is 5.2");
        flash.Drift(announced, KitchenHeat.Medium).Should().Be(new FlavorVector(0, 4, 0), "Flash Cooking leaves Medium alone");
        flash.Drift(announced, KitchenHeat.Low).Should().Be(FlavorVector.Zero);
    }

    [Test]
    public void TheCpOfTheStirAndReduceNeverFallBelowOne()
    {
        var stats = new KitchenStats(20, 0, 0, 0, new KitchenPerkStats(StirCpReduction: 100, ReduceCpReduction: 100));

        stats.StirCp.Should().Be(1);
        stats.ReduceCp.Should().Be(1);
    }

    #endregion

    #region Pantry Staples and Sharp Palate

    [Test]
    public void PantryStaples_MakeEachPinchPushOneMore()
    {
        var plain = ChefEngine(KitchenPerkStats.None);
        var staples = ChefEngine(new KitchenPerkStats(PantryPushBonus: 1));

        plain.Preview(KitchenActionIds.AddSalt).Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId)).Should().Equal(2, 4, 6);
        staples.Preview(KitchenActionIds.AddSalt).Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId)).Should().Equal(3, 5, 8);
        staples.Preview(KitchenActionIds.AddSugar).Outcomes.Select(o => Delta(o, KitchenEngine.SweetMeterId)).Should().Equal(3, 5, 8);
        staples.Preview(KitchenActionIds.AddSpice).Outcomes.Select(o => Delta(o, KitchenEngine.SpiceMeterId)).Should().Equal(3, 5, 8);
    }

    [Test]
    public void SharpPalate_NarrowsTheSpreadOfEveryPush()
    {
        var engine = ChefEngine(new KitchenPerkStats(PushSpreadReduction: 5));

        var meat = engine.Preview(KitchenActionIds.AddIngredient(MeatLine));

        meat.Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId)).Should().Equal(new[] { 7, 13, 19 }, "a 45 percent spread: 13 x 0.55 = 7.15 and 13 x 1.45 = 18.85");
    }

    #endregion

    #region Seasoned Palate

    [Test]
    public void SeasonedPalate_HalvesTheAddOfACommittedIngredient_AndKeepsWholePortions()
    {
        var engine = ChefEngine(new KitchenPerkStats(SeasonedPalateOwned: true, SeasonedPalateOn: true));

        var meat = engine.Preview(KitchenActionIds.AddIngredient(MeatLine));
        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));

        meat.Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId)).Should().Equal(
            new[] { 4, 7, 11 }, "(13, 0, 2) halved is (7, 0, 1) and a 50 percent spread leaves 3.5, 7 and 10.5");
        meat.Outcomes.Select(o => Delta(o, KitchenEngine.StockMeterId(MeatLine))).Should().OnlyContain(delta => delta == -1);
        engine.PortionsLeft(MeatLine).Should().Be(2, "it still takes one whole portion");
    }

    [Test]
    public void SeasonedPalate_DoesNotChangeThePantry()
    {
        var plain = ChefEngine(KitchenPerkStats.None);
        var seasoned = ChefEngine(new KitchenPerkStats(SeasonedPalateOwned: true, SeasonedPalateOn: true));

        seasoned.Preview(KitchenActionIds.AddSalt).Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId))
            .Should().Equal(plain.Preview(KitchenActionIds.AddSalt).Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId)));
    }

    [Test]
    public void SeasonedPalate_ThinsLessWithWater()
    {
        var engine = ChefEngine(new KitchenPerkStats(SeasonedPalateOwned: true, SeasonedPalateOn: true), Script(Rolls(50), Rolls(50), Rolls()));
        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));
        engine.Perform(KitchenActionIds.AddIngredient(HerbLine));
        var before = engine.Profile;

        engine.Perform(KitchenActionIds.AddIngredient(WaterLine));

        before.Should().Be(new FlavorVector(9, 0, 6), "(7, 0, 1) and (2, 0, 5), each half of the usual push");
        engine.Profile.Should().Be(new FlavorVector(8, 0, 5), "13 percent off, half of the usual 25 rounded up, of (9, 0, 6)");
        engine.PortionsLeft(WaterLine).Should().Be(0);
    }

    [Test]
    public void SeasonedPalate_WhenItIsSwitchedOff_PlaysTheKitchenAsTuned()
    {
        var off = ChefEngine(new KitchenPerkStats(SeasonedPalateOwned: true, SeasonedPalateOn: false, SeasonedPalateSpreadPercent: 0));
        var plain = ChefEngine(KitchenPerkStats.None);

        off.Preview(KitchenActionIds.AddIngredient(MeatLine)).Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId))
            .Should().Equal(plain.Preview(KitchenActionIds.AddIngredient(MeatLine)).Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId)));
    }

    [Test]
    public void SeasonedPalateII_NarrowsTheSpreadWhileItIsOn()
    {
        var engine = ChefEngine(new KitchenPerkStats(SeasonedPalateOwned: true, SeasonedPalateOn: true, SeasonedPalateSpreadPercent: 25));

        var meat = engine.Preview(KitchenActionIds.AddIngredient(MeatLine));

        meat.Outcomes.Select(o => Delta(o, KitchenEngine.SavoryMeterId)).Should().Equal(
            new[] { 4, 7, 10 }, "a 38 percent spread (50 less a quarter): 7 x 0.62 = 4.34 and 7 x 1.38 = 9.66, and the halved push is (7, 0, 1)");
    }

    [Test]
    public void PeakAroma_DoublesTheHalvedPush()
    {
        var engine = ChefEngine(
            new KitchenPerkStats(SeasonedPalateOwned: true, SeasonedPalateOn: true),
            Script(Rolls(outcome: 50, aroma: 10), Rolls(50)));
        engine.Perform(KitchenActionIds.AddSalt);
        engine.IsPeakAromaNext.Should().BeTrue();
        var profile = engine.Profile;

        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));

        (engine.Profile.Savory - profile.Savory).Should().Be(14, "the halved 7 doubled");
    }

    #endregion

    #region The styles

    [Test]
    public void SlowCooking_GivesMoreDonenessFromALowHeatSimmer_AndWeakensTheDriftAtMedium()
    {
        var slow = new KitchenPerkStats(LowHeatDonenessPercent: 100, MediumDriftReductionPercent: 25);
        var plain = ChefEngine(KitchenPerkStats.None);
        var engine = ChefEngine(slow);

        plain.Perform(KitchenActionIds.HeatLow);
        engine.Perform(KitchenActionIds.HeatLow);

        plain.Preview(KitchenActionIds.Simmer).Outcomes[0].Delta.Progress.Should().Be(5);
        engine.Preview(KitchenActionIds.Simmer).Outcomes[0].Delta.Progress.Should().Be(10, "twice the usual 5");

        engine.Perform(KitchenActionIds.HeatMedium);
        engine.Preview(KitchenActionIds.Simmer).Outcomes[0].Delta.Progress.Should().Be(12, "Medium is as usual");
        engine.NextDrift.Should().Be(new FlavorVector(0, 0, 8), "the dice give Spice up 8");
        engine.EffectiveNextDrift.Should().Be(new FlavorVector(0, 0, 3), "half of 8 less a quarter");
    }

    [Test]
    public void SlowCooking_TheDriftPromisedIsTheDriftThatHappens()
    {
        var engine = ChefEngine(new KitchenPerkStats(MediumDriftReductionPercent: 25));
        engine.Perform(KitchenActionIds.Simmer);
        var promised = engine.Preview(KitchenActionIds.Simmer).Outcomes[0];

        engine.Perform(KitchenActionIds.Simmer);

        Delta(promised, KitchenEngine.SpiceMeterId).Should().Be(3);
        Meter(engine, KitchenEngine.SpiceMeterId).Should().Be(3);
    }

    [Test]
    public void FlashCooking_WeakensTheDriftAtHighHeat_AndLeavesSearAlone()
    {
        var flash = new KitchenPerkStats(HighDriftReductionPercent: 20);
        var plain = ChefEngine(KitchenPerkStats.None);
        var engine = ChefEngine(flash);

        plain.Perform(KitchenActionIds.HeatHigh);
        engine.Perform(KitchenActionIds.HeatHigh);

        plain.EffectiveNextDrift.Should().Be(new FlavorVector(0, 0, 8));
        engine.EffectiveNextDrift.Should().Be(new FlavorVector(0, 0, 6), "8 less 20 percent is 6.4");
        engine.Preview(KitchenActionIds.Sear).IntegrityCost.Should().Be(plain.Preview(KitchenActionIds.Sear).IntegrityCost);
        engine.Preview(KitchenActionIds.Sear).Outcomes[0].Delta.Progress.Should().Be(36, "Sear gives the same Doneness");
    }

    [Test]
    public void FlashCooking_TheDriftPromisedAtHighHeatIsTheDriftThatHappens()
    {
        var engine = ChefEngine(new KitchenPerkStats(HighDriftReductionPercent: 20));
        engine.Perform(KitchenActionIds.HeatHigh);
        var promised = engine.Preview(KitchenActionIds.Sear).Outcomes[0];

        engine.Perform(KitchenActionIds.Sear);

        Delta(promised, KitchenEngine.SpiceMeterId).Should().Be(6);
        Meter(engine, KitchenEngine.SpiceMeterId).Should().Be(6);
    }

    #endregion

    #region Aromatic Kitchen and Steady Wrist

    [TestCase(35, true)]
    [TestCase(36, false)]
    public void AromaticKitchen_AnnouncesPeakAromaOnThirtyFivePercentOfTurns(int roll, bool announced)
    {
        var engine = ChefEngine(new KitchenPerkStats(PeakAromaChancePoints: 10), Script(Rolls(aroma: roll)));

        engine.Perform(KitchenActionIds.Simmer);

        engine.IsPeakAromaNext.Should().Be(announced);
    }

    [Test]
    public void WithoutAromaticKitchen_ARollOfThirtyFiveDoesNotAnnouncePeakAroma()
    {
        var engine = ChefEngine(KitchenPerkStats.None, Script(Rolls(aroma: 35)));

        engine.Perform(KitchenActionIds.Simmer);

        engine.IsPeakAromaNext.Should().BeFalse();
    }

    [Test]
    public void SteadyWrist_MakesStirCheaper_AndAtMediumHeatAddsHalfASimmersDoneness()
    {
        var engine = ChefEngine(new KitchenPerkStats(StirCpReduction: 4, StirDonenessPercent: 50));
        engine.Perform(KitchenActionIds.Simmer);

        var preview = engine.Preview(KitchenActionIds.Stir);
        engine.Perform(KitchenActionIds.Stir);

        preview.CpCost.Should().Be(8);
        preview.Outcomes[0].Delta.Progress.Should().Be(6, "half of the Medium simmer's 12");
        engine.Meters.Cp.Value.Should().Be(55 - 8);
        Meter(engine, MinigameMeterIds.Progress).Should().Be(12 + 6);
        Meter(engine, KitchenEngine.SpiceMeterId).Should().Be(0, "the announced drift was cancelled as usual");
    }

    [Test]
    public void SteadyWrist_AtHighHeatThereIsNoSimmerToShare_SoStirAddsNoDoneness()
    {
        var engine = ChefEngine(new KitchenPerkStats(StirCpReduction: 4, StirDonenessPercent: 50));
        engine.Perform(KitchenActionIds.HeatHigh);

        var preview = engine.Preview(KitchenActionIds.Stir);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(8);
        preview.Outcomes[0].Delta.Progress.Should().Be(0);
    }

    [Test]
    public void SteadyWrist_StirStillNeedsADriftToCancel()
    {
        var engine = ChefEngine(new KitchenPerkStats(StirCpReduction: 4, StirDonenessPercent: 50));

        var preview = engine.Preview(KitchenActionIds.Stir);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Nothing is drifting.");
    }

    #endregion

    #region Reduce

    private static KitchenPerkStats ReduceI => new(ReducePercent: 20);

    private static KitchenPerkStats ReduceII => new(ReducePercent: 35, ReduceCpReduction: 3);

    [Test]
    public void Reduce_IsAnActionOnlyACookWithThePerkHas()
    {
        Engine(new FixedRolls(50)).Actions.Select(a => a.Id).Should().NotContain(KitchenActionIds.Reduce);

        var engine = ChefEngine(ReduceI);

        engine.Actions.Select(a => a.Id).Should().Contain(KitchenActionIds.Reduce);
        engine.Actions.Single(a => a.Id == KitchenActionIds.Reduce).Name.Should().Be("Reduce");
        engine.Actions.Select(a => a.Id).Should().OnlyHaveUniqueItems();
        engine.PreviewAll().Should().HaveCount(engine.Actions.Count);
    }

    [Test]
    public void Reduce_PullsEveryFlavorTowardTheTargetsCenterForCp()
    {
        var engine = ChefEngine(ReduceI, Script(Rolls(50), Rolls()));
        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));
        engine.Profile.Should().Be(new FlavorVector(13, 0, 2));

        var preview = engine.Preview(KitchenActionIds.Reduce);
        var result = engine.Perform(KitchenActionIds.Reduce);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(12);
        preview.IntegrityCost.Should().Be(0);
        preview.Outcomes.Should().ContainSingle();
        result.Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        engine.Profile.Should().Be(new FlavorVector(16, 0, 5), "20 percent of (17, 0, 14) is (3.4, 0, 2.8)");
        engine.Meters.Cp.Value.Should().Be(55 - 12);
    }

    [Test]
    public void ReduceII_PullsFurtherAndCostsLess()
    {
        var engine = ChefEngine(ReduceII, Script(Rolls(50), Rolls()));
        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));

        var preview = engine.Preview(KitchenActionIds.Reduce);
        engine.Perform(KitchenActionIds.Reduce);

        preview.CpCost.Should().Be(9);
        engine.Profile.Should().Be(new FlavorVector(19, 0, 7), "35 percent of (17, 0, 14) is (5.95, 0, 4.9)");
    }

    [Test]
    public void Reduce_TakesATurnLikeEveryAction()
    {
        var engine = ChefEngine(ReduceI, Script(Rolls(50), Rolls()));
        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));
        var freshness = engine.Meters.Integrity.Value;

        engine.Perform(KitchenActionIds.Reduce);

        engine.Turn.Should().Be(2);
        engine.Meters.Integrity.Value.Should().Be(freshness - KitchenTuning.FreshnessDecayPerTurn);
    }

    [Test]
    public void Reduce_IsRefusedWhenItWouldChangeNothing()
    {
        var recipe = Soup(center: FlavorVector.Zero, inner: 10, outer: 30);
        var engine = ChefEngine(ReduceI, recipe: recipe);

        var preview = engine.Preview(KitchenActionIds.Reduce);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Contain("already as close");
    }

    [Test]
    public void Reduce_IsRefusedWithTooLittleCp()
    {
        var engine = Engine(new FixedRolls(50), Soup(), new KitchenStats(1, 0, 0, 0, ReduceI));

        var preview = engine.Preview(KitchenActionIds.Reduce);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Not enough CP.");
    }

    [Test]
    public void Reduce_WorksOnAnyKitchenRecipe_PetFoodAndEnhancementsIncluded()
    {
        var recipe = new KitchenRecipe(
            KitchenDish.PetFood,
            new KitchenTarget(new FlavorVector(60, 20, 0), 8, 24),
            60,
            15,
            new[] { Meat() });
        var engine = ChefEngine(ReduceI, recipe: recipe);

        engine.Actions.Select(a => a.Id).Should().Contain(KitchenActionIds.Reduce);
        engine.Preview(KitchenActionIds.Reduce).Enabled.Should().BeTrue();
    }

    #endregion

    #region Signature Dish

    [Test]
    public void SignatureDish_IsThreeActionsOnlyACookWithThePerkHas()
    {
        Engine(new FixedRolls(50)).Actions.Select(a => a.Id).Should().NotContain(KitchenActionIds.SignatureSavory);

        var engine = ChefEngine(new KitchenPerkStats(SignatureDish: true));

        engine.Actions.Select(a => a.Id).Should().EndWith(
            new[] { KitchenActionIds.SignatureSavory, KitchenActionIds.SignatureSweet, KitchenActionIds.SignatureSpice });
        engine.Actions.Select(a => a.Name).Should().Contain(new[] { "Signature Dish: Savory", "Signature Dish: Sweet", "Signature Dish: Spice" });
        engine.Actions.Select(a => a.Id).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void SignatureDish_SetsOneFlavorExactlyToTheTargetsCenter_ForNoCp()
    {
        var engine = ChefEngine(new KitchenPerkStats(SignatureDish: true), Script(Rolls()));

        var preview = engine.Preview(KitchenActionIds.SignatureSavory);
        var result = engine.Perform(KitchenActionIds.SignatureSavory);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(0);
        preview.Outcomes.Should().ContainSingle();
        Delta(preview.Outcomes[0], KitchenEngine.SavoryMeterId).Should().Be(30);
        Delta(preview.Outcomes[0], KitchenEngine.SpiceMeterId).Should().Be(0);
        result.Outcome.Should().BeEquivalentTo(preview.Outcomes[0]);
        Meter(engine, KitchenEngine.SavoryMeterId).Should().Be(30, "exactly the target's center, though no push in the kitchen reaches it in one");
        Meter(engine, KitchenEngine.SpiceMeterId).Should().Be(0, "the other flavors are untouched");
        engine.Meters.Cp.Value.Should().Be(55);
        engine.Turn.Should().Be(1, "it takes a turn");
        engine.SignatureUsed.Should().BeTrue();
    }

    [Test]
    public void SignatureDish_CanBeUsedOncePerDish()
    {
        var engine = ChefEngine(new KitchenPerkStats(SignatureDish: true), Script(Rolls(), Rolls()));
        engine.Perform(KitchenActionIds.SignatureSavory);

        foreach (var id in new[] { KitchenActionIds.SignatureSavory, KitchenActionIds.SignatureSweet, KitchenActionIds.SignatureSpice })
        {
            var preview = engine.Preview(id);
            preview.Enabled.Should().BeFalse(id);
            preview.DisabledReason.Should().Be("Signature Dish was already used on this dish.");
        }

        engine.Perform(KitchenActionIds.SignatureSpice).Performed.Should().BeFalse();
        Meter(engine, KitchenEngine.SpiceMeterId).Should().Be(0);
    }

    [Test]
    public void SignatureDish_IsRefusedForAFlavorAlreadyOnTheCenter_SoTheUseIsNotWasted()
    {
        var engine = ChefEngine(new KitchenPerkStats(SignatureDish: true));

        var preview = engine.Preview(KitchenActionIds.SignatureSweet);

        preview.Enabled.Should().BeFalse("the target's Sweet center is 0 and the empty pot is on it");
        preview.DisabledReason.Should().Be("Sweet is already on the target's center.");
        engine.Perform(KitchenActionIds.SignatureSweet).Performed.Should().BeFalse();
        engine.SignatureUsed.Should().BeFalse();
    }

    [Test]
    public void SignatureDish_TheAnnouncedDriftStillHappensAfterIt()
    {
        var engine = ChefEngine(new KitchenPerkStats(SignatureDish: true), Script(Rolls(axis: 0, direction: 0), Rolls(axis: 1, direction: 0)));
        engine.Perform(KitchenActionIds.Simmer);
        engine.NextDrift.Should().Be(new FlavorVector(-8, 0, 0));

        engine.Perform(KitchenActionIds.SignatureSavory);

        Meter(engine, KitchenEngine.SavoryMeterId).Should().Be(26, "snapped to 30, then the Medium drift of 4 pulled it down");
    }

    [Test]
    public void SignatureDish_WorksOnPetFoodAndCookingEnhancementRecipesToo()
    {
        var recipe = new KitchenRecipe(
            KitchenDish.PetFood,
            new KitchenTarget(new FlavorVector(60, 20, 0), 8, 24),
            60,
            15,
            new[] { Meat() });
        var engine = ChefEngine(new KitchenPerkStats(SignatureDish: true), recipe: recipe);

        engine.Perform(KitchenActionIds.SignatureSweet);

        Meter(engine, KitchenEngine.SweetMeterId).Should().Be(20);
    }

    #endregion

    #region Previews equal results

    [TestCase(KitchenActionIds.Reduce)]
    [TestCase(KitchenActionIds.SignatureSavory)]
    [TestCase(KitchenActionIds.SignatureSpice)]
    [TestCase(KitchenActionIds.Stir)]
    public void ThePerkActions_ResultIsWhatThePreviewPromised(string actionId)
    {
        var perks = new KitchenPerkStats(
            ReducePercent: 35, ReduceCpReduction: 3, SignatureDish: true, StirCpReduction: 4, StirDonenessPercent: 50, MediumDriftReductionPercent: 25);
        var engine = ChefEngine(perks, Script(Rolls(50), Rolls(axis: 0, direction: 1), Rolls()));
        engine.Perform(KitchenActionIds.AddIngredient(MeatLine));
        engine.Perform(KitchenActionIds.Simmer);
        var promised = engine.Preview(actionId).Outcomes[0];

        var result = engine.Perform(actionId);

        result.Performed.Should().BeTrue();
        result.Outcome.Should().BeEquivalentTo(promised);
    }

    #endregion
}
