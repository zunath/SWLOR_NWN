using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.DMPossessionEngine;

namespace SWLOR.Game.Server.Tests.Domain.Administration;

/// <summary>
/// Unit tests of the DM Creator favorites instance: favorites follow the DM's public CD key onto any avatar and onto the
/// creatures they possess, and are read from the saved record each time so a second session is not overwritten.
/// </summary>
[TestFixture]
public class DMCreatorFavoriteServiceTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Npc = 50;
    private const string RecordId = "DMCreatorFavorites:KEY1";

    private DMPossessionEngine _world = null!;
    private InMemoryDBService _db = null!;
    private DMCreatorFavoriteService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new DMPossessionEngine();
        _db = new InMemoryDBService();
        _service = new DMCreatorFavoriteService(_db, new DMPresenceService());
    }

    [TearDown]
    public void TearDown()
    {
        _world.Dispose();
    }

    private static DMBlueprint Blueprint(string resref, DMBlueprintCategory category = DMBlueprintCategory.Creatures) =>
        new(resref, "Name of " + resref, "Path", category, DMBlueprintSource.Custom);

    [Test]
    public void Get_ReturnsAnEmptyListWhenTheCDKeyCannotBeRead()
    {
        _service.Get(Npc).Should().BeEmpty();

        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void Get_ReturnsAnEmptyListWhenNothingIsSaved()
    {
        _world.AddPlayer(DM, isDM: true);

        _service.Get(DM).Should().BeEmpty();

        _db.Reads.Should().Equal(RecordId);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void Get_ReturnsTheSavedFavoritesOfAPossessingDM(int possessionType)
    {
        _world.AddPlayer(DM, isDM: true);
        _world.Possess(DM, Creature, possessionType);
        var record = new DMCreatorFavorites { Id = RecordId };
        record.Blueprints.Add(new DMFavoriteBlueprint(Blueprint("rodian")));
        _db.With(record);

        _service.Get(DM).Should().BeSameAs(record.Blueprints);
        _service.Get(Creature).Should().BeSameAs(record.Blueprints);
    }

    [Test]
    public void Toggle_SavesNothingWhenTheCDKeyCannotBeRead()
    {
        var change = _service.Toggle(Npc, Blueprint("rodian"), out var favorites);

        change.Should().Be(DMFavoriteChange.NotSaved);
        favorites.Should().BeEmpty();
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void Toggle_AddsABlueprintAndSavesTheRecord()
    {
        _world.AddPlayer(DM, isDM: true);

        var change = _service.Toggle(DM, Blueprint("rodian"), out var favorites);

        change.Should().Be(DMFavoriteChange.Added);
        var record = _db.Saved.Should().ContainSingle().Subject.Should().BeOfType<DMCreatorFavorites>().Subject;
        record.Id.Should().Be(RecordId);
        favorites.Should().BeSameAs(record.Blueprints);
        favorites.Should().ContainSingle().Which.Resref.Should().Be("rodian");
    }

    [Test]
    public void Toggle_RemovesABlueprintThatIsAlreadyAFavorite()
    {
        _world.AddPlayer(DM, isDM: true);
        var record = new DMCreatorFavorites { Id = RecordId };
        record.Blueprints.Add(new DMFavoriteBlueprint(Blueprint("rodian")));
        record.Blueprints.Add(new DMFavoriteBlueprint(Blueprint("twilek")));
        _db.With(record);

        var change = _service.Toggle(DM, Blueprint("RODIAN"), out var favorites);

        change.Should().Be(DMFavoriteChange.Removed);
        favorites.Select(favorite => favorite.Resref).Should().Equal("twilek");
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(record);
    }

    [Test]
    public void Toggle_ReadsTheSavedListSoASecondSessionIsNotOverwritten()
    {
        _world.AddPlayer(DM, isDM: true);
        _service.Toggle(DM, Blueprint("rodian"), out _);
        var otherSession = new DMCreatorFavorites { Id = RecordId };
        otherSession.Blueprints.AddRange(new[] { new DMFavoriteBlueprint(Blueprint("rodian")), new DMFavoriteBlueprint(Blueprint("twilek")) });
        _db.With(otherSession);

        _service.Toggle(DM, Blueprint("wookiee"), out var favorites);

        favorites.Select(favorite => favorite.Resref).Should().Equal("rodian", "twilek", "wookiee");
    }

    [Test]
    public void Toggle_DoesNotSaveWhenTheListIsFull()
    {
        _world.AddPlayer(DM, isDM: true);
        var record = new DMCreatorFavorites { Id = RecordId };
        for (var index = 0; index < DMFavoriteList.MaxFavorites; index++)
            record.Blueprints.Add(new DMFavoriteBlueprint(Blueprint("blueprint" + index)));
        _db.With(record);

        var change = _service.Toggle(DM, Blueprint("one_too_many"), out var favorites);

        change.Should().Be(DMFavoriteChange.Full);
        favorites.Should().HaveCount(DMFavoriteList.MaxFavorites);
        _db.Saved.Should().BeEmpty();
    }
}
