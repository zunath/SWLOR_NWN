using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The city Market Stall: one for each city level, the newest closing when the city drops a level (the structures stay),
/// a vendor for each open stall, and the stall's structure, item and module resources.
/// </summary>
public class MarketStallRulesTests
{
    private static readonly DateTime Start = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static (string Id, DateTime PlacedUtc)[] Stalls(int count)
    {
        return Enumerable.Range(1, count).Select(i => ($"stall-{i}", Start.AddHours(i))).ToArray();
    }

    #region The limit

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(3, 3)]
    [TestCase(5, 5)]
    [TestCase(0, 0)]
    public void ACity_MayHaveOneStallForEachLevel(int level, int expected)
    {
        MarketStallRules.GetLimit(level).Should().Be(expected);
    }

    [Test]
    public void APlacement_IsRefusedAtTheLimit_AndAllowedBelowIt()
    {
        MarketStallRules.GetPlacementError(3, 2).Should().BeEmpty();
        MarketStallRules.GetPlacementError(3, 3).Should().Contain("3 market stalls");
        MarketStallRules.GetPlacementError(3, 4).Should().NotBeEmpty("a city over its limit places no more");
        MarketStallRules.GetPlacementError(1, 1).Should().Contain("1 market stall.");
        MarketStallRules.GetPlacementError(1, 0).Should().BeEmpty();
    }

    [Test]
    public void ARefusal_IsPlainAscii()
    {
        MarketStallRules.GetPlacementError(2, 2).Should().MatchRegex("^[\\x20-\\x7E]+$");
    }

    #endregion

    #region Closing and reopening

    [Test]
    public void WithinTheLimit_EveryStallIsOpen()
    {
        MarketStallRules.SelectOpenStalls(Stalls(3), 3).Should().BeEquivalentTo(new[] { "stall-1", "stall-2", "stall-3" });
        MarketStallRules.SelectOpenStalls(Stalls(2), 5).Should().HaveCount(2);
    }

    [Test]
    public void ALevelDrop_ClosesTheNewestStallsAboveTheLimit()
    {
        var stalls = Stalls(4);

        MarketStallRules.SelectOpenStalls(stalls, 4).Should().HaveCount(4);
        var afterDrop = MarketStallRules.SelectOpenStalls(stalls, 2);

        afterDrop.Should().BeEquivalentTo(new[] { "stall-1", "stall-2" }, "the oldest two stay open");
        stalls.Select(x => x.Item1).Except(afterDrop).Should().BeEquivalentTo(new[] { "stall-3", "stall-4" }, "the newest two close, and keep standing");
    }

    [Test]
    public void AStallThatClosed_ReopensWhenTheCityRisesAgain()
    {
        var stalls = Stalls(3);

        MarketStallRules.SelectOpenStalls(stalls, 1).Should().BeEquivalentTo(new[] { "stall-1" });
        MarketStallRules.SelectOpenStalls(stalls, 3).Should().HaveCount(3);
    }

    [Test]
    public void APickedUpStall_LetsTheNewestClosedOneOpen()
    {
        var stalls = Stalls(3).Where(x => x.Item1 != "stall-1").ToArray();

        MarketStallRules.SelectOpenStalls(stalls, 2).Should().BeEquivalentTo(new[] { "stall-2", "stall-3" });
    }

    [Test]
    public void TheOrder_DoesNotDependOnHowTheStallsAreListed()
    {
        var shuffled = Stalls(4).Reverse().ToArray();

        MarketStallRules.SelectOpenStalls(shuffled, 2).Should().BeEquivalentTo(new[] { "stall-1", "stall-2" });
    }

    [Test]
    public void StallsPlacedAtTheSameMoment_AreOrderedByIdSoTheChoiceIsStable()
    {
        var stalls = new[] { ("b", Start), ("a", Start), ("c", Start) };

        MarketStallRules.SelectOpenStalls(stalls, 2).Should().BeEquivalentTo(new[] { "a", "b" });
        MarketStallRules.SelectOpenStalls(stalls.Reverse().ToArray(), 2).Should().BeEquivalentTo(new[] { "a", "b" });
    }

    [Test]
    public void ACityWithNoStallsOrNoLevel_OpensNothing()
    {
        MarketStallRules.SelectOpenStalls(Array.Empty<(string, DateTime)>(), 3).Should().BeEmpty();
        MarketStallRules.SelectOpenStalls(null, 3).Should().BeEmpty();
        MarketStallRules.SelectOpenStalls(Stalls(2), 0).Should().BeEmpty();
    }

    [TestCase(true, "Market Stall")]
    [TestCase(false, "Market Stall (Closed)")]
    public void AStallsName_SaysWhenItIsClosed(bool isOpen, string expected)
    {
        MarketStallRules.GetStallName("Market Stall", isOpen).Should().Be(expected);
        MarketStallRules.GetStallName(" ", isOpen).Should().Be(expected);
    }

    #endregion

    #region Themes and the vendor

    [TestCase(MarketStallTheme.Invalid, MarketStallTheme.GeneralGoods)]
    [TestCase(MarketStallTheme.GeneralGoods, MarketStallTheme.GeneralGoods)]
    [TestCase(MarketStallTheme.CraftingSupplies, MarketStallTheme.CraftingSupplies)]
    [TestCase(MarketStallTheme.Decor, MarketStallTheme.Decor)]
    [TestCase((MarketStallTheme)99, MarketStallTheme.GeneralGoods)]
    public void AStallWithNoValidTheme_SellsGeneralGoods(MarketStallTheme theme, MarketStallTheme expected)
    {
        MarketStallRules.NormalizeTheme(theme).Should().Be(expected);
    }

    [Test]
    public void EveryThemeHasAStoreAndAName()
    {
        foreach (var theme in new[] { MarketStallTheme.GeneralGoods, MarketStallTheme.CraftingSupplies, MarketStallTheme.Decor })
        {
            CityStoreCatalog.Get(CityStoreRules.GetKindForTheme(theme)).Should().NotBeNull();
            MarketStallRules.DescribeTheme(theme).Should().NotBeNullOrWhiteSpace();
        }

        new[] { MarketStallTheme.GeneralGoods, MarketStallTheme.CraftingSupplies, MarketStallTheme.Decor }
            .Select(MarketStallRules.DescribeTheme).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void TheVendor_StandsBesideTheStallAsTheExistingMarketsLayThemOut()
    {
        // The Highcrest market stand at (52.43, 125.89), facing 270 degrees, has its vendor at (54.5, 125.9), facing east.
        var placement = MarketStallRules.GetVendorPlacement(52.43f, 125.89f, 270f);

        placement.X.Should().BeApproximately(54.5f, 0.1f);
        placement.Y.Should().BeApproximately(125.9f, 0.1f);
        placement.FacingDegrees.Should().BeApproximately(0f, 0.001f);
    }

    [TestCase(0f, 0f, 2.1f, 90f)]
    [TestCase(90f, -2.1f, 0f, 180f)]
    [TestCase(180f, 0f, -2.1f, 270f)]
    [TestCase(300f, 2.1f * 0.8660254f, 2.1f * 0.5f, 30f)]
    public void TheVendor_TurnsWithTheStall(float stallFacing, float dx, float dy, float vendorFacing)
    {
        var placement = MarketStallRules.GetVendorPlacement(10f, 20f, stallFacing);

        placement.X.Should().BeApproximately(10f + dx, 0.001f);
        placement.Y.Should().BeApproximately(20f + dy, 0.001f);
        placement.FacingDegrees.Should().BeApproximately(vendorFacing, 0.001f);
        Math.Sqrt(Math.Pow(placement.X - 10f, 2) + Math.Pow(placement.Y - 20f, 2)).Should().BeApproximately(MarketStallRules.VendorDistanceMeters, 0.001);
    }

    #endregion

    #region The structure and its module resources

    [Test]
    public void TheStall_IsACityOnlyStructureWithTheReservedId()
    {
        ((int)StructureType.MarketStall).Should().Be(5102);

        var stall = StructureType.MarketStall.GetAttribute<StructureType, StructureAttribute>();
        stall.Name.Should().Be("Market Stall");
        stall.IsActive.Should().BeTrue();
        stall.RestrictedPropertyTypes.Should().Be(PropertyType.City, "stalls stand in the open, in a city");
        stall.LayoutType.Should().Be(PropertyLayoutType.Invalid, "a stall is a placeable, not a building with an interior");
        stall.CanBeRetrieved.Should().BeTrue();
        stall.Description.Should().MatchRegex("^[\\x20-\\x7E]+$");
    }

    [Test]
    public void TheStall_KeepsHostileSpawnsAndResourceNodesAwayLikeAnyBuilding()
    {
        SpawnExclusionRules.IsQualifyingStructure(StructureType.MarketStall).Should().BeTrue();
    }

    [Test]
    public void TheStall_StandsOnAnExistingPlaceable()
    {
        var stall = StructureType.MarketStall.GetAttribute<StructureType, StructureAttribute>();

        File.Exists(Path.Combine(Root, "Module", "utp", $"{stall.Resref}.utp.json"))
            .Should().BeTrue($"the stall placeable '{stall.Resref}' must exist in the module, so no new art is needed");
    }

    [Test]
    public void TheStallsItem_IsTheStructureItemOfItsId_AndAPlayerItemBecauseARecipeMakesIt()
    {
        var path = Path.Combine(Root, "Module", "uti", "structure_5102.uti.json");
        File.Exists(path).Should().BeTrue();

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var item = document.RootElement;
        Text(item, "Tag").Should().Be("structure_5102");
        Text(item, "TemplateResRef").Should().Be("structure_5102");
        item.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be("Market Stall");
        item.GetProperty("Description").GetProperty("value").GetProperty("0").GetString().Should().MatchRegex("^[\\x20-\\x7E]+$");

        item.TryGetProperty("VarTable", out _).Should().BeFalse("a Fabrication recipe makes the stall, so it is a player item (no NO_ECONOMY flag)");

        File.ReadAllText(Path.Combine(Root, "Module", "itp", "itempalcus.itp.json"))
            .Should().Contain("\"structure_5102\"", "the toolset palette lists the item");
    }

    [Test]
    public void TheVendorBlueprint_IsPlotAndOpensTheStallConversation()
    {
        var path = Path.Combine(Root, "Module", "utc", "cs_stallvendor.utc.json");
        File.Exists(path).Should().BeTrue();

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var utc = document.RootElement;
        Text(utc, "Tag").Should().Be("cs_stallvendor");
        Text(utc, "TemplateResRef").Should().Be("cs_stallvendor");
        utc.GetProperty("Plot").GetProperty("value").GetInt32().Should().Be(1, "vendors cannot be harmed");
        Text(utc, "ScriptDialogue").Should().Be("x2_def_onconv");
        Text(utc, "Conversation").Should().BeEmpty("the conversation is the code menu named by the CONVERSATION local, not a graph");

        var conversation = utc.GetProperty("VarTable").GetProperty("value").EnumerateArray()
            .Single(local => Text(local, "Name") == "CONVERSATION");
        conversation.GetProperty("Value").GetProperty("value").GetString().Should().Be(nameof(MarketStallDialog));

        File.ReadAllText(Path.Combine(Root, "Module", "itp", "creaturepalcus.itp.json")).Should().Contain("\"cs_stallvendor\"");
    }

    [Test]
    public void TheDialog_HasAMainPage()
    {
        new MarketStallDialog().Build().DefaultPageId.Should().Be("MAIN_PAGE");
    }

    #endregion

    #region The wiring

    [Test]
    public void TheStall_IsWiredIntoPlacementAndRetrieval()
    {
        var actions = Source("Service", "PropertyService", "StructureChangedAction.cs");

        actions.Should().Contain("Assign(StructureType.MarketStall, StructureChangeType.PositionChanged, MarketStalls.OnStallPlaced);");
        actions.Should().Contain("Assign(StructureType.MarketStall, StructureChangeType.Retrieved, MarketStalls.OnStallRetrieved);");
    }

    [Test]
    public void PlacingAStall_IsRefusedAtTheCityLevelLimit_AndAStallIsNotCountedAsABuilding()
    {
        var property = Source("Service", "Property.cs");

        property.Should().Contain("MarketStallRules.GetPlacementError(");
        property.Should().Contain("CityBuildingRules.CountsAsBuilding(structureType)");
        CityBuildingRules.CountsAsBuilding(StructureType.MarketStall).Should().BeFalse();
    }

    [Test]
    public void ACityLevelChange_ClosesOrReopensTheStalls()
    {
        var property = Source("Service", "Property.cs");
        var change = property.IndexOf("CityOfficers.EndAppointmentsAboveLimit(city);", StringComparison.Ordinal);
        var refresh = property.IndexOf("MarketStalls.Refresh(city.Id);", StringComparison.Ordinal);

        change.Should().BeGreaterThan(0);
        refresh.Should().BeGreaterThan(change, "the stalls are checked against the level the city just took");
    }

    [Test]
    public void ThePickOfATheme_FollowsTheCityRoles_AndNeverTheDepartedMayor()
    {
        var stalls = Source("Service", "MarketStalls.cs");

        stalls.Should().Contain("CityPowers.GetRole(city, playerId)");
        stalls.Should().Contain("role is CityRole.Mayor or CityRole.ActingMayor");
        stalls.Should().Contain("if (role == CityRole.DepartedMayor)");
        stalls.Should().Contain("PropertyPermissionType.EditStructures");
    }

    [Test]
    public void AClosedStall_KeepsItsStructure_AndOnlyLosesItsVendor()
    {
        var stalls = Source("Service", "MarketStalls.cs");

        stalls.Should().Contain("if (!isOpen)\n                    Despawn(placeable);");
        stalls.Should().NotContain("DeleteProperty", "a stall past the limit closes and stays");
    }

    #endregion

    private static string Text(JsonElement element, string key) => element.GetProperty(key).GetProperty("value").GetString();

    private static string Source(params string[] path)
    {
        var full = Path.Combine(new[] { Root, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }
}
