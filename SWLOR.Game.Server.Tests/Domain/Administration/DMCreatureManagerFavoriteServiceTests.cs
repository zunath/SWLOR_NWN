using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.DMPossessionEngine;

namespace SWLOR.Game.Server.Tests.Domain.Administration;

/// <summary>
/// Unit tests of the Creature Manager favorites instance: favorites follow the DM's public CD key onto any avatar and
/// onto the creatures they possess.
/// </summary>
[TestFixture]
public class DMCreatureManagerFavoriteServiceTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Npc = 50;
    private const string RecordId = "DMCreatureManagerFavorites:KEY1";

    private DMPossessionEngine _world = null!;
    private InMemoryDBService _db = null!;
    private DMCreatureManagerFavoriteService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new DMPossessionEngine();
        _db = new InMemoryDBService();
        _service = new DMCreatureManagerFavoriteService(_db, new DMPresenceService());
    }

    [TearDown]
    public void TearDown()
    {
        _world.Dispose();
    }

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
        var record = new DMCreatureManagerFavorites { Id = RecordId };
        record.Creatures.Add(new DMSavedCreatureFavorite("saved-1", "Guard"));
        _db.With(record);

        _service.Get(DM).Should().BeSameAs(record.Creatures);
        _service.Get(Creature).Should().BeSameAs(record.Creatures);
    }

    [Test]
    public void Set_SavesNothingWhenTheCDKeyCannotBeRead()
    {
        var change = _service.Set(Npc, "saved-1", "Guard", true, out var favorites);

        change.Should().Be(DMFavoriteChange.NotSaved);
        favorites.Should().BeEmpty();
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void Set_AddsTheCreatureAndSavesTheRecord()
    {
        _world.AddPlayer(DM, isDM: true);

        var change = _service.Set(DM, "saved-1", "Guard", true, out var favorites);

        change.Should().Be(DMFavoriteChange.Added);
        var record = _db.Saved.Should().ContainSingle().Subject.Should().BeOfType<DMCreatureManagerFavorites>().Subject;
        record.Id.Should().Be(RecordId);
        favorites.Should().BeSameAs(record.Creatures);
        favorites.Should().ContainSingle().Which.Name.Should().Be("Guard");
    }

    [Test]
    public void Set_RemovesTheCreatureWhenItIsNoLongerAFavorite()
    {
        _world.AddPlayer(DM, isDM: true);
        var record = new DMCreatureManagerFavorites { Id = RecordId };
        record.Creatures.Add(new DMSavedCreatureFavorite("saved-1", "Guard"));
        record.Creatures.Add(new DMSavedCreatureFavorite("saved-2", "Thug"));
        _db.With(record);

        var change = _service.Set(DM, "saved-1", "Guard", false, out var favorites);

        change.Should().Be(DMFavoriteChange.Removed);
        favorites.Select(favorite => favorite.CreatureId).Should().Equal("saved-2");
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(record);
    }

    [Test]
    public void Set_DoesNotSaveWhenTheListIsFull()
    {
        _world.AddPlayer(DM, isDM: true);
        var record = new DMCreatureManagerFavorites { Id = RecordId };
        for (var index = 0; index < DMSavedCreatureFavoriteList.MaxFavorites; index++)
            record.Creatures.Add(new DMSavedCreatureFavorite("saved-" + index, "Creature"));
        _db.With(record);

        var change = _service.Set(DM, "one-too-many", "Guard", true, out var favorites);

        change.Should().Be(DMFavoriteChange.Full);
        favorites.Should().HaveCount(DMSavedCreatureFavoriteList.MaxFavorites);
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void Remove_ReturnsAnEmptyListWhenTheCDKeyCannotBeRead()
    {
        _service.Remove(Npc, "saved-1").Should().BeEmpty();

        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void Remove_DropsTheCreatureAndSavesTheRecord()
    {
        _world.AddPlayer(DM, isDM: true);
        var record = new DMCreatureManagerFavorites { Id = RecordId };
        record.Creatures.Add(new DMSavedCreatureFavorite("saved-1", "Guard"));
        record.Creatures.Add(new DMSavedCreatureFavorite("saved-2", "Thug"));
        _db.With(record);

        var favorites = _service.Remove(DM, "saved-1");

        favorites.Select(favorite => favorite.CreatureId).Should().Equal("saved-2");
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(record);
    }

    [Test]
    public void Remove_SavesNothingForACreatureThatIsNotAFavorite()
    {
        _world.AddPlayer(DM, isDM: true);
        var record = new DMCreatureManagerFavorites { Id = RecordId };
        record.Creatures.Add(new DMSavedCreatureFavorite("saved-2", "Thug"));
        _db.With(record);

        var favorites = _service.Remove(DM, "saved-1");

        favorites.Should().ContainSingle().Which.CreatureId.Should().Be("saved-2");
        _db.Saved.Should().BeEmpty();
    }
}
