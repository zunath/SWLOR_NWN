using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Tests.Service;

public class DMSavedCreatureFavoriteListTests
{
    [Test]
    public void Toggle_AddsThenRemovesTheSameSavedCreature()
    {
        var favorites = new List<DMSavedCreatureFavorite>();

        DMSavedCreatureFavoriteList.Toggle(favorites, "id-1", "Rancor").Should().Be(DMFavoriteChange.Added);
        favorites.Should().ContainSingle();
        favorites[0].CreatureId.Should().Be("id-1");
        favorites[0].Name.Should().Be("Rancor");
        DMSavedCreatureFavoriteList.Contains(favorites, "id-1").Should().BeTrue();
        DMSavedCreatureFavoriteList.Contains(favorites, "id-2").Should().BeFalse();

        DMSavedCreatureFavoriteList.Toggle(favorites, "id-1", "Rancor").Should().Be(DMFavoriteChange.Removed);
        favorites.Should().BeEmpty();
    }

    [Test]
    public void Toggle_RefusesToAddPastTheLimitButStillRemoves()
    {
        var favorites = Enumerable.Range(0, DMSavedCreatureFavoriteList.MaxFavorites)
            .Select(index => new DMSavedCreatureFavorite($"id-{index}", $"Creature {index}"))
            .ToList();

        DMSavedCreatureFavoriteList.Toggle(favorites, "new", "New").Should().Be(DMFavoriteChange.Full);
        favorites.Should().HaveCount(DMSavedCreatureFavoriteList.MaxFavorites);

        DMSavedCreatureFavoriteList.Toggle(favorites, "id-3", "Creature 3").Should().Be(DMFavoriteChange.Removed);
        favorites.Should().HaveCount(DMSavedCreatureFavoriteList.MaxFavorites - 1);
    }

    [Test]
    public void Remove_ReportsWhetherTheSavedCreatureWasAFavorite()
    {
        var favorites = new List<DMSavedCreatureFavorite> { new("id-1", "Rancor") };

        DMSavedCreatureFavoriteList.Remove(favorites, "id-2").Should().BeFalse();
        DMSavedCreatureFavoriteList.Remove(favorites, "id-1").Should().BeTrue();
        favorites.Should().BeEmpty();
    }

    [TestCase(45, 3, 0, 0, 20, 0, 0)]
    [TestCase(45, 3, 1, 20, 20, 0, 0)]
    [TestCase(45, 3, 2, 40, 5, 0, 3)]
    [TestCase(40, 3, 2, 40, 0, 0, 3)]
    [TestCase(0, 25, 1, 0, 0, 20, 5)]
    [TestCase(10, 0, 0, 0, 10, 0, 0)]
    [TestCase(0, 0, 0, 0, 0, 0, 0)]
    public void GetPage_ListsSavedCreaturesBeforeDeletedOnes(
        long savedTotal, int deletedTotal, int pageIndex,
        int savedOffset, int savedCount, int deletedOffset, int deletedCount)
    {
        DMSavedCreatureFavoriteList.GetPage(savedTotal, deletedTotal, pageIndex, 20)
            .Should().Be((savedOffset, savedCount, deletedOffset, deletedCount));
    }

    [Test]
    public void SavedFavorites_RoundTripThroughJson()
    {
        var saved = new DMCreatureManagerFavorites { Id = "DMCreatureManagerFavorites:KEY" };
        saved.Creatures.Add(new DMSavedCreatureFavorite("id-1", "Rancor"));
        saved.Creatures.Add(new DMSavedCreatureFavorite("id-2", "Town Guard"));

        var loaded = JsonConvert.DeserializeObject<DMCreatureManagerFavorites>(JsonConvert.SerializeObject(saved))!;

        loaded.Id.Should().Be(saved.Id);
        loaded.Creatures.Should().BeEquivalentTo(saved.Creatures, options => options.WithStrictOrdering());
    }
}
