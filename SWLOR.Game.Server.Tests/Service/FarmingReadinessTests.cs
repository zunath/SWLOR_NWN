#nullable enable
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.FarmingDefinition;
using SWLOR.Game.Server.Service.FarmhandService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;
using static SWLOR.Game.Server.Tests.Feature.SpaceItemFixtures;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Fixes from the farming release-readiness review (2026-11-14): plots show their crop's stage, predators at the corrals stop
/// animals leaving or being butchered ahead of the penalty, the penalty stops butchering, animals that wandered off free their
/// places before a count, a hunt alive at a restart is always judged, and saplings are never feed.
/// </summary>
public class FarmingReadinessTests
{
    private static readonly DateTime Planted = new(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Dictionary<CropType, CropDefinition> Crops = new WoodlotListDefinition().BuildCrops();

    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(RepositoryRoot(), "SWLOR.Game.Server"), Path.Combine)).Replace("\r\n", "\n");

    private static string Between(string source, string start, string end)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        from.Should().BeGreaterThanOrEqualTo(0, $"'{start}' is in the source");
        return source[from..source.IndexOf(end, from + start.Length, StringComparison.Ordinal)];
    }

    private static GardenPlot Plot(int growHours = 12, bool tended = false)
    {
        var ripe = FarmingRules.GetRipeUtc(Planted, growHours);
        return new GardenPlot
        {
            Crop = CropType.Tomato,
            PlantedUtc = Planted,
            TendReadyUtc = FarmingRules.GetTendReadyUtc(Planted, growHours),
            RipeUtc = ripe,
            WitherUtc = FarmingRules.GetWitherUtc(ripe),
            Tended = tended
        };
    }

    private static Livestock Adult(double? penaltyUntilHours = null) => new()
    {
        Species = LivestockType.Warocas,
        PlacedUtc = Planted,
        Feedings = 3,
        LastFedUtc = Planted.AddHours(70),
        NoProductsUntilUtc = penaltyUntilHours is { } hours ? Planted.AddHours(hours) : null,
        LastProcessedUtc = Planted
    };

    // A plot's stage

    [Test]
    public void APlotShowsAPlantForEachStage_AndNoneWhenEmpty()
    {
        FarmingRules.GetCropAppearance(GardenPlotStage.Empty, false).Should().Be(0);
        FarmingRules.GetCropAppearance(GardenPlotStage.Growing, false).Should().Be(FarmingRules.GrowingCropAppearance);
        FarmingRules.GetCropAppearance(GardenPlotStage.ReadyToTend, false).Should().Be(FarmingRules.GrowingCropAppearance);
        FarmingRules.GetCropAppearance(GardenPlotStage.Ripe, false).Should().Be(FarmingRules.RipeCropAppearance);
        FarmingRules.GetCropAppearance(GardenPlotStage.Withered, false).Should().Be(FarmingRules.WitheredCropAppearance);
        FarmingRules.GetCropAppearance(GardenPlotStage.Ripe, true).Should().Be(FarmingRules.SaplingAppearance);
        FarmingRules.GetCropAppearance(GardenPlotStage.Withered, true).Should().Be(FarmingRules.WitheredCropAppearance);
        new[] { FarmingRules.EmptyPlotAppearance, FarmingRules.GrowingCropAppearance, FarmingRules.RipeCropAppearance, FarmingRules.WitheredCropAppearance, FarmingRules.SaplingAppearance }
            .Should().OnlyHaveUniqueItems("each stage looks different");
        foreach (var appearance in new[] { FarmingRules.GrowingCropAppearance, FarmingRules.RipeCropAppearance, FarmingRules.WitheredCropAppearance, FarmingRules.SaplingAppearance })
            FarmingRules.GetCropModelHeight(appearance).Should().BeInRange(FarmingRules.PlotSoilHeight - 0.1f, FarmingRules.PlotSoilHeight,
                "every crop stands in the empty bed's soil, its stems reaching no more than 10 cm below it");
    }

    [Test]
    public void EachStageAppearance_IsItsGardenPlotModel()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "tools", "GardenPlotModels.json")))
            root = root.Parent;
        root.Should().NotBeNull("the tests run inside the repository");
        var placeables = Path.Combine(root!.FullName, "SWLOR_Haks", "sw_2da", "placeables.2da");
        if (!File.Exists(placeables))
            Assert.Ignore("SWLOR_Haks/sw_2da is not checked out");

        // A placeables.2da appearance ID is the row's position after the three header lines, not its first-column label.
        var rows = File.ReadAllLines(placeables).Skip(3).ToArray();
        string ModelOf(int appearance) => Regex.Match(rows[appearance], @"^\S+\s+(""[^""]*""|\S+)\s+\S+\s+(\S+)").Groups[2].Value;
        ModelOf(FarmingRules.EmptyPlotAppearance).Should().Be("swc_gplot_empty");
        ModelOf(FarmingRules.GrowingCropAppearance).Should().Be("swc_gplot_sprout");
        ModelOf(FarmingRules.RipeCropAppearance).Should().Be("swc_gplot_ripe");
        ModelOf(FarmingRules.WitheredCropAppearance).Should().Be("swc_gplot_dead");
        ModelOf(FarmingRules.SaplingAppearance).Should().Be("swc_gplot_sapl");
        File.ReadAllText(Path.Combine(root.FullName, "Module", "utp", "growing_plant.utp.json"))
            .Should().MatchRegex($@"""Appearance"": \{{\s*""type"": ""dword"",\s*""value"": {FarmingRules.EmptyPlotAppearance}\b", "the Garden Plot is the empty bed");
    }

    [Test]
    public void APlotsNameSaysWhenItNeedsThePlanter()
    {
        FarmingRules.DescribePlotName("Garden Plot", null, GardenPlotStage.Empty).Should().Be("Garden Plot");
        FarmingRules.DescribePlotName("Garden Plot", "Tomato", GardenPlotStage.Growing).Should().Be("Garden Plot (Tomato)");
        FarmingRules.DescribePlotName("Garden Plot", "Tomato", GardenPlotStage.ReadyToTend).Should().Be("Garden Plot (Tomato, ready to tend)");
        FarmingRules.DescribePlotName("Garden Plot", "Tomato", GardenPlotStage.Ripe).Should().Be("Garden Plot (Tomato, ripe)");
        FarmingRules.DescribePlotName("Garden Plot", "Tomato", GardenPlotStage.Withered).Should().Be("Garden Plot (Tomato, withered)");
    }

    [Test]
    public void ThePlantChangesWhenTheStageNextChangesOnItsOwn()
    {
        var plot = Plot();
        FarmingRules.GetNextStageChangeUtc(plot, Planted).Should().Be(plot.TendReadyUtc);
        FarmingRules.GetNextStageChangeUtc(plot, plot.TendReadyUtc).Should().Be(plot.RipeUtc);
        FarmingRules.GetNextStageChangeUtc(plot, plot.RipeUtc).Should().Be(plot.WitherUtc);
        FarmingRules.GetNextStageChangeUtc(plot, plot.WitherUtc).Should().BeNull("a withered crop waits for a player");
        FarmingRules.GetNextStageChangeUtc(null, Planted).Should().BeNull();
    }

    [Test]
    public void APlotsPlantIsCreatedFreshForEachStage_AndOpensThePlot()
    {
        var farming = ReadSource("Service", "Farming.cs");
        var refresh = Between(farming, "private static void RefreshCropModel(", "private static void DestroyCropModel(");
        refresh.Should().Contain("DestroyCropModel(placeable);\n                model = CreateObject(ObjectType.Placeable, CropModelResref, CropModelLocation(placeable, appearance));",
                "a placeable's appearance does not change for players already in the area")
            .And.Contain("SetEventScript(model, EventScript.Placeable_OnUsed, ScriptName.OnGardenPlotUsed);")
            .And.Contain("SetEventScript(model, EventScript.Placeable_OnHeartbeat, ScriptName.OnGardenPlotCropHeartbeat);");
        Between(farming, "public static void CropModelHeartbeat()", "\n        }\n").Should()
            .Contain("if (!GetIsObjectValid(plot) || GetLocalObject(plot, CropModelVariable) != model)", "a plant never outlives its plot");
        Between(farming, "public static void UsePlot()", "Gui.TogglePlayerWindow").Should()
            .Contain("plot = GetLocalObject(OBJECT_SELF, CropModelPlotVariable);");
    }

    // Predators at the corrals

    [Test]
    public void PredatorsAtTheCorrals_StopCratingAndButchering_NotCollecting()
    {
        var now = Planted.AddHours(80);
        var animal = Adult();
        LivestockRules.CheckCrate(true, animal, now, true, predatorsAtCorrals: true).Should().Be(LivestockRefusal.PredatorsAtCorrals);
        LivestockRules.CheckButcher(true, animal, now, true, predatorsAtCorrals: true).Should().Be(LivestockRefusal.PredatorsAtCorrals);
        LivestockRules.CheckCrate(true, animal, now, true).Should().Be(LivestockRefusal.None);
        LivestockRules.CheckButcher(true, animal, now, true).Should().Be(LivestockRefusal.None);
        LivestockRules.Describe(LivestockRefusal.PredatorsAtCorrals).Should().NotBeEmpty();
    }

    [Test]
    public void ThePredatorPenalty_StopsButcheringAsItStopsCollecting()
    {
        var animal = Adult(penaltyUntilHours: 100);
        LivestockRules.CheckButcher(true, animal, Planted.AddHours(80), true).Should().Be(LivestockRefusal.PredatorPenalty, "meat is a product");
        LivestockRules.CheckButcher(true, animal, Planted.AddHours(101), true).Should().Be(LivestockRefusal.None);
    }

    [Test]
    public void TheCorralsAskWhetherPredatorsAreThere_AndClearWanderersBeforeCounting()
    {
        var corrals = ReadSource("Service", "Corrals.cs");
        Between(corrals, "public static bool ArePredatorsAtCorrals(", "\n        }\n").Should()
            .Contain("LivestockHunts.GetActive(cityId) != null")
            .And.Contain("situation.Type == SituationType.PredatorsAtTheCorrals && situation.State == CitySituationState.Open");
        var place = Between(corrals, "public static LivestockResult Place(", "LivestockRules.CheckPlace(");
        place.Should().Contain("ClearWanderedOff(cdKey, playerId, corralId, now);", "an animal that wandered off must not hold a place");
        ReadSource("Feature", "DialogDefinition", "CorralDialog.cs").Should().Contain("Corrals.ClearWanderedOff(");
    }

    // Hunts at a restart

    [Test]
    public void EveryHuntAliveAtARestart_IsJudgedFromThen()
    {
        var hunts = ReadSource("Service", "LivestockHunts.cs");
        var start = hunts[hunts.IndexOf("public static void ProcessAtServerStart()", StringComparison.Ordinal)..];
        start.Should().Contain("if (!_waves.ContainsKey(hunt.Id))\n                    Begin(hunt, city);",
            "a hunt still on whose corrals spawn late, or never, must still end");
    }

    // Feed

    [Test]
    public void ASaplingIsNeverFeed()
    {
        var hopper = new Dictionary<CropType, int> { [CropType.WoodSapling] = 20 };
        FarmhandRules.HasFeed(hopper, 5, type => Crops.GetValueOrDefault(type)).Should().BeFalse("a sapling's harvest is wood");
        ReadSource("Service", "Corrals.cs").Should().Contain("if (definition == null || crop == null || crop.IsSapling)");
        ReadSource("Service", "Farmhands.cs").Should().Contain("if (crop == null || crop.IsSapling)");
    }

    // Found in the client pass

    [Test]
    public void APennedAnimalNeverFights_SoAHuntIsThePlayersToWin()
    {
        var spawn = Between(ReadSource("Service", "Corrals.cs"), "public static uint SpawnCreature(", "_creatures[animal.Id] = creature;");
        spawn.Should().Contain("SetCommandable(false, creature);", "a level 46 Thune killed a hunt's wave in seconds, winning it for the city");
    }

    [Test]
    public void AnAnimalThatGrowsUpOnItsOwn_ShowsItsNewStage()
    {
        var corrals = ReadSource("Service", "Corrals.cs");
        Between(corrals, "public static void RefreshName(Livestock animal)", "private static int GetFreeSlot(").Should()
            .Contain("LivestockRules.GetMaturityUtc(animal)", "a fed young animal is renamed when its 72 hours are up");
        Between(corrals, "public static Livestock LoadAnimal(", "\n        }\n").Should().Contain("RefreshName(animal);");
    }

    [Test]
    public void TheGardenPlotWindow_FitsItsFourActions_AndShowsSeveralSeeds()
    {
        var window = ReadSource("Feature", "GuiDefinition", "GardenPlotDefinition.cs");
        window.Should().Contain(".SetInitialGeometry(0, 0, 480f, 600f)", "at 500 the seed list showed two rows");
        window.Split(".SetWidth(ActionButtonWidth)").Length.Should().Be(5, "each of the four actions fits the 420 panel");
        ReadSource("Feature", "GuiDefinition", "CityAffairsDefinition.cs").Should()
            .Contain(".BindText(model => model.SelectedText)\n                        .SetShowBorder(false)\n                        .SetScrollbars(NuiScrollbars.Y)",
                "the Predators at the Corrals text runs past the box's three lines");
    }
}
