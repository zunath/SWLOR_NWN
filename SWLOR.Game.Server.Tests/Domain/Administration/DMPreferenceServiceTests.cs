using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.DMPossessionEngine;

namespace SWLOR.Game.Server.Tests.Domain.Administration;

/// <summary>
/// Unit tests of the DM preferences instance: the engine is the model of DM possession, and the database an in-memory
/// one. Preferences follow the DM's public CD key, which resolves from the object the DM controls.
/// </summary>
[TestFixture]
public class DMPreferenceServiceTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Npc = 50;
    private const string DMKey = "DM:KEY1";

    private DMPossessionEngine _world = null!;
    private InMemoryDBService _db = null!;
    private DMPreferenceService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new DMPossessionEngine();
        _db = new InMemoryDBService();
        _service = new DMPreferenceService(_db, new DMPresenceService());
    }

    [TearDown]
    public void TearDown()
    {
        _world.Dispose();
    }

    [Test]
    public void Get_ReturnsTheDefaultsWhenTheCDKeyCannotBeRead()
    {
        var preferences = _service.Get(Npc);

        preferences.Id.Should().NotStartWith("DM:", "the defaults carry no DM key, so they cannot be saved over a DM's record");
        preferences.EmoteStyle.Should().Be(EmoteStyle.Regular);
        preferences.CommsFilter.Should().Be(DMCommsFilter.AllParties);
        preferences.ShowHelmet.Should().BeTrue();
        preferences.ShowCloak.Should().BeTrue();
        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void Get_ReturnsTheDefaultsKeyedByTheCDKeyWhenNothingIsSaved()
    {
        _world.AddPlayer(DM, isDM: true);

        var preferences = _service.Get(DM);

        preferences.Id.Should().Be(DMKey);
        preferences.EmoteStyle.Should().Be(EmoteStyle.Regular);
        _db.Reads.Should().Equal(DMKey);
        _db.Saved.Should().BeEmpty("reading never saves");
    }

    [Test]
    public void Get_ReturnsTheSavedPreferences()
    {
        _world.AddPlayer(DM, isDM: true);
        var saved = new DMPreferences { Id = DMKey, EmoteStyle = EmoteStyle.Novel, ShowHelmet = false };
        _db.With(saved);

        _service.Get(DM).Should().BeSameAs(saved);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void Get_FollowsAPossessingDMOntoTheCreatureTheyControl(int possessionType)
    {
        _world.AddPlayer(DM, isDM: true);
        _world.Possess(DM, Creature, possessionType);
        var saved = new DMPreferences { Id = DMKey, CommsFilter = DMCommsFilter.OwnParty };
        _db.With(saved);

        _service.Get(DM).Should().BeSameAs(saved, "the avatar's key resolves through the creature it controls");
        _service.Get(Creature).Should().BeSameAs(saved);
    }

    [Test]
    public void Update_SavesNothingWhenTheCDKeyCannotBeRead()
    {
        var called = false;

        var saved = _service.Update(Npc, _ => called = true);

        saved.Should().BeFalse();
        called.Should().BeFalse();
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void Update_CreatesAndSavesPreferencesForADMWithNoneSaved()
    {
        _world.AddPlayer(DM, isDM: true);

        var saved = _service.Update(DM, preferences => preferences.EmoteStyle = EmoteStyle.Novel);

        saved.Should().BeTrue();
        var record = _db.Saved.Should().ContainSingle().Subject.Should().BeOfType<DMPreferences>().Subject;
        record.Id.Should().Be(DMKey);
        record.EmoteStyle.Should().Be(EmoteStyle.Novel);
        record.ShowHelmet.Should().BeTrue("a field the change did not touch keeps its default");
    }

    [Test]
    public void Update_ChangesTheSavedPreferencesAndKeepsTheOtherFields()
    {
        _world.AddPlayer(DM, isDM: true);
        var existing = new DMPreferences { Id = DMKey, ShowCloak = false };
        _db.With(existing);

        _service.Update(DM, preferences => preferences.CommsFilter = DMCommsFilter.OwnParty);

        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(existing);
        existing.CommsFilter.Should().Be(DMCommsFilter.OwnParty);
        existing.ShowCloak.Should().BeFalse();
    }

    [Test]
    public void Update_FromAPossessedCreatureSavesUnderTheSameDMKey()
    {
        _world.AddPlayer(DM, isDM: true);
        _world.Possess(DM, Creature, DMPossess);

        _service.Update(Creature, preferences => preferences.ShowHelmet = false).Should().BeTrue();

        _db.Saved.Should().ContainSingle().Which.Id.Should().Be(DMKey);
    }
}
