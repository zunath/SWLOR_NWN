using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerRequestRulesTests
{
    private static readonly MarketCategoryType[] Categories =
    {
        MarketCategoryType.Recipe,
        MarketCategoryType.Incubation,
        MarketCategoryType.Music,
    };

    private static readonly string[] PropResrefs =
    {
        "prop_datachip", "prop_keycard_a", "prop_keycard_b", "prop_keycard_c", "prop_codecard",
        "prop_circuit", "prop_powercell", "prop_idtags", "prop_datadisc",
    };

    [Test]
    public void MarketFilter_HidingCategories_SavesThemAndRestoresTheRest()
    {
        var excluded = MarketCategoryFilter.GetExcluded(Categories, new[] { MarketCategoryType.Recipe });

        excluded.Should().BeEquivalentTo(new[] { MarketCategoryType.Incubation, MarketCategoryType.Music });
        MarketCategoryFilter.GetActive(Categories, excluded).Should().Equal(MarketCategoryType.Recipe);
    }

    [Test]
    public void MarketFilter_CategoryAddedLater_ShowsForSavedExclusions()
    {
        var active = MarketCategoryFilter.GetActive(
            Categories,
            new HashSet<MarketCategoryType> { MarketCategoryType.Incubation, MarketCategoryType.Music, MarketCategoryType.Food });

        active.Should().Equal(MarketCategoryType.Recipe);
    }

    [Test]
    public void MarketFilter_NoToggleAndEveryToggle_SaveNothing()
    {
        MarketCategoryFilter.GetExcluded(Categories, Array.Empty<MarketCategoryType>()).Should().BeEmpty();
        MarketCategoryFilter.GetExcluded(Categories, Categories).Should().BeEmpty();
    }

    [Test]
    public void MarketFilter_RecordWithoutSavedFilter_HasNoActiveCategories()
    {
        MarketCategoryFilter.GetActive(Categories, null).Should().BeEmpty();
        MarketCategoryFilter.GetActive(Categories, new HashSet<MarketCategoryType>()).Should().BeEmpty();
    }

    [TestCase("Chair, Wood", "Chair, Wood", "")]
    [TestCase("", "Chair, Wood", "")]
    [TestCase("Throne", "Chair, Wood", "Throne")]
    public void StructureWorldName_OnlyOverridesTemplateForPlayerNames(string custom, string catalog, string expected)
    {
        StructureWorldName.Resolve(custom, catalog).Should().Be(expected);
    }

    [TestCase("Rook's Rest", " A quiet room. ", "Rook's Rest: A quiet room.")]
    [TestCase("", "A quiet room.", "A quiet room.")]
    [TestCase("Rook's Rest", "   ", "")]
    [TestCase("Rook's Rest", null, "")]
    public void PropertyDescription_NamesThePropertyAndSkipsBlankText(string name, string description, string expected)
    {
        PropertyDescriptionText.Format(name, description).Should().Be(expected);
    }

    [Test]
    public void RestartRestore_CoversStancesAndBlazingSpikesOnly()
    {
        new BastionStanceStatusEffect().RestoresAfterServerRestart.Should().BeTrue();
        new BlazingSpikesStatusEffect().RestoresAfterServerRestart.Should().BeTrue();
        new AkimboStanceStatusEffect().RestoresAfterServerRestart.Should().BeFalse("it does not survive a logout either");
        new KnockdownStatusEffect().RestoresAfterServerRestart.Should().BeFalse();
    }

    [Test]
    public void PropItems_AreMarketBlockedAndSoldByThePlacedAccessoryShop()
    {
        var root = FindRepositoryRoot();
        var blueprint = File.ReadAllText(Path.Combine(root.FullName, "Module", "utm", "night_accessory.utm.json"));
        var placed = File.ReadAllText(Path.Combine(root.FullName, "Module", "git", "veles_tradecon.git.json"));

        foreach (var resref in PropResrefs)
        {
            using var item = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json")));
            item.RootElement.GetProperty("AddCost").GetProperty("value").GetInt32().Should().Be(50, resref);
            item.RootElement.GetProperty("VarTable").GetProperty("value")[0].GetProperty("Name").GetProperty("value").GetString()
                .Should().Be(PlayerMarket.NoMarketListingVariable, resref);
            blueprint.Should().Contain($"\"{resref}\"");
            placed.Should().Contain($"\"{resref}\"");
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module", "uti")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
