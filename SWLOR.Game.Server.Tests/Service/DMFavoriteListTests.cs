using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Tests.Service;

public class DMFavoriteListTests
{
    private static DMBlueprint Creature(string resref, string name, string categoryPath, DMBlueprintSource source = DMBlueprintSource.Custom) =>
        new(resref, name, categoryPath, DMBlueprintCategory.Creatures, source);

    private static readonly DMPalette CustomCreatures = new(
        new List<string> { "World Creatures / Dathomir", "NPCs / Guards", "Not in palette" },
        new List<DMBlueprint>
        {
            Creature("dath_rancor", "Rancor", "World Creatures / Dathomir"),
            Creature("guard", "Town Guard", "NPCs / Guards"),
            Creature("new_beast", "Zakkeg", "Not in palette")
        });

    private static readonly DMPalette StandardCreatures = new(
        new List<string> { "Animals" },
        new List<DMBlueprint> { Creature("nw_bat", "Bat", "Animals", DMBlueprintSource.Standard) });

    private static DMFavoriteBlueprint Favorite(string resref, string name, DMBlueprintSource source = DMBlueprintSource.Custom,
        DMBlueprintCategory category = DMBlueprintCategory.Creatures) =>
        new() { Category = category, Source = source, Resref = resref, Name = name };

    private static DMPalette GetPalette(DMBlueprintSource source) =>
        source == DMBlueprintSource.Custom ? CustomCreatures : StandardCreatures;

    [Test]
    public void Toggle_AddsThenRemovesTheSameBlueprint()
    {
        var favorites = new List<DMFavoriteBlueprint>();
        var rancor = CustomCreatures.Blueprints[0];

        DMFavoriteList.Toggle(favorites, rancor).Should().Be(DMFavoriteChange.Added);
        favorites.Should().ContainSingle();
        favorites[0].Resref.Should().Be("dath_rancor");
        favorites[0].Name.Should().Be("Rancor");
        DMFavoriteList.Contains(favorites, rancor).Should().BeTrue();

        DMFavoriteList.Toggle(favorites, rancor).Should().Be(DMFavoriteChange.Removed);
        favorites.Should().BeEmpty();
    }

    [Test]
    public void Matches_IgnoresResrefCaseButNotPaletteOrType()
    {
        var favorite = Favorite("DATH_RANCOR", "Rancor");

        favorite.Matches(Creature("dath_rancor", "Rancor", "x")).Should().BeTrue();
        favorite.Matches(Creature("dath_rancor", "Rancor", "x", DMBlueprintSource.Standard)).Should().BeFalse();
        favorite.Matches(new DMBlueprint("dath_rancor", "Rancor", "x", DMBlueprintCategory.Items, DMBlueprintSource.Custom))
            .Should().BeFalse();
    }

    [Test]
    public void Toggle_RefusesToAddPastTheLimitButStillRemoves()
    {
        var favorites = Enumerable.Range(0, DMFavoriteList.MaxFavorites)
            .Select(index => Favorite($"bp{index}", $"Blueprint {index}"))
            .ToList();

        DMFavoriteList.Toggle(favorites, CustomCreatures.Blueprints[0]).Should().Be(DMFavoriteChange.Full);
        favorites.Should().HaveCount(DMFavoriteList.MaxFavorites);

        DMFavoriteList.Toggle(favorites, Creature("bp3", "Blueprint 3", "x")).Should().Be(DMFavoriteChange.Removed);
        favorites.Should().HaveCount(DMFavoriteList.MaxFavorites - 1);
    }

    [Test]
    public void BuildPalette_UsesThePalettesCurrentNamesAndCategories()
    {
        // Saved under an old name: the blueprint's resref is what identifies it.
        var favorites = new List<DMFavoriteBlueprint> { Favorite("guard", "Old Guard Name"), Favorite("dath_rancor", "Rancor") };
        var unavailable = new HashSet<DMBlueprint>();

        var palette = DMFavoriteList.BuildPalette(favorites, DMBlueprintCategory.Creatures, GetPalette, unavailable);

        palette.Blueprints.Should().Equal(CustomCreatures.Blueprints[0], CustomCreatures.Blueprints[1]);
        palette.Blueprints.Select(blueprint => blueprint.Name).Should().Equal("Rancor", "Town Guard");
        palette.CategoryPaths.Should().Equal("World Creatures / Dathomir", "NPCs / Guards");
        unavailable.Should().BeEmpty();
    }

    [Test]
    public void BuildPalette_KeepsMissingBlueprintsAsUnavailable()
    {
        var favorites = new List<DMFavoriteBlueprint> { Favorite("retired_npc", "Retired Trader"), Favorite("guard", "Town Guard") };
        var unavailable = new HashSet<DMBlueprint>();

        var palette = DMFavoriteList.BuildPalette(favorites, DMBlueprintCategory.Creatures, GetPalette, unavailable);

        palette.CategoryPaths.Should().Equal("NPCs / Guards", DMFavoriteList.UnavailableCategory);
        var missing = palette.Blueprints.Single(blueprint => blueprint.Resref == "retired_npc");
        missing.Name.Should().Be("Retired Trader");
        missing.CategoryPath.Should().Be(DMFavoriteList.UnavailableCategory);
        missing.Source.Should().Be(DMBlueprintSource.Custom);
        unavailable.Should().BeEquivalentTo(new[] { missing });
        missing.IsInCategory(DMFavoriteList.UnavailableCategory).Should().BeTrue();

        // An unavailable favorite can still be removed.
        DMFavoriteList.Toggle(favorites, missing).Should().Be(DMFavoriteChange.Removed);
        favorites.Select(favorite => favorite.Resref).Should().Equal("guard");
    }

    [Test]
    public void BuildPalette_ListsOnlyTheChosenTypeAndReadsOnlyThePalettesItUses()
    {
        var favorites = new List<DMFavoriteBlueprint>
        {
            Favorite("guard", "Town Guard"),
            Favorite("guard", "Town Guard"),
            Favorite("blaster", "Blaster", category: DMBlueprintCategory.Items)
        };
        var reads = new List<DMBlueprintSource>();

        var palette = DMFavoriteList.BuildPalette(favorites, DMBlueprintCategory.Creatures, source =>
        {
            reads.Add(source);
            return GetPalette(source);
        }, new HashSet<DMBlueprint>());

        palette.Blueprints.Select(blueprint => blueprint.Resref).Should().Equal("guard");
        reads.Should().Equal(DMBlueprintSource.Custom);
    }

    [Test]
    public void BuildPalette_MixesPalettesSortedByNameWithCustomCategoriesFirst()
    {
        var favorites = new List<DMFavoriteBlueprint>
        {
            Favorite("nw_bat", "Bat", DMBlueprintSource.Standard),
            Favorite("new_beast", "Zakkeg"),
            Favorite("dath_rancor", "Rancor")
        };

        var palette = DMFavoriteList.BuildPalette(favorites, DMBlueprintCategory.Creatures, GetPalette, new HashSet<DMBlueprint>());

        palette.Blueprints.Select(blueprint => blueprint.Name).Should().Equal("Bat", "Rancor", "Zakkeg");
        palette.CategoryPaths.Should().Equal("World Creatures / Dathomir", "Not in palette", "Animals");
    }

    [Test]
    public void SavedFavorites_RoundTripThroughJson()
    {
        var saved = new DMCreatorFavorites { Id = "DMCreatorFavorites:KEY" };
        saved.Blueprints.Add(Favorite("nw_bat", "Bat", DMBlueprintSource.Standard));
        saved.Blueprints.Add(Favorite("blaster", "Blaster", category: DMBlueprintCategory.Items));

        var loaded = JsonConvert.DeserializeObject<DMCreatorFavorites>(JsonConvert.SerializeObject(saved))!;

        loaded.Id.Should().Be(saved.Id);
        loaded.Blueprints.Should().BeEquivalentTo(saved.Blueprints, options => options.WithStrictOrdering());
    }
}
