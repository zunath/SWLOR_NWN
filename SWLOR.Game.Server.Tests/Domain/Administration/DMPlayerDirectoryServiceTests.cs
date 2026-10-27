using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Tests.Support.DMPossessionEngine;

namespace SWLOR.Game.Server.Tests.Domain.Administration;

/// <summary>
/// Unit tests of the DM player directory instance. Online characters come from the engine model of DM possession and
/// offline ones from an in-memory database, which answers every search with all the characters it holds, so a test
/// stores only the characters a query would find and the tests of paging rely on that.
/// </summary>
[TestFixture]
public class DMPlayerDirectoryServiceTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Aria = 10;
    private const uint Brent = 11;
    private const uint Cantina = 100;
    private const string OfflineId = "6f9619ff-8b86-d011-b42d-00c04fc964ff";

    private DMPossessionEngine _world = null!;
    private InMemoryDBService _db = null!;
    private DMPlayerDirectoryService _service = null!;
    private FacadeContainer _container = null!;
    private IDisposable _containerScope = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new DMPossessionEngine();
        _db = new InMemoryDBService();
        _service = new DMPlayerDirectoryService(_db, new DMPresenceService());
        // The disguise and descriptor lookups read a player's record from the static database when the object is a PC.
        // An object that is not a PC takes their no-database path, which is enough to list the online characters here.
        _world.Engine.SetReturnHandler("GetIsPC", _ => false);
        _world.Name(Cantina, "Cantina");
        // The service reads the area cache through the Area facade, which resolves its instance from a container.
        _container = new FacadeContainer();
        _containerScope = _container.Install();
    }

    [TearDown]
    public void TearDown()
    {
        _containerScope.Dispose();
        _container.Dispose();
        _world.Dispose();
    }

    private static Player OfflinePlayer(string id, string name, string account = "offlineacct", string cdKey = "OFFKEY01", bool isDeleted = false) =>
        new()
        {
            Id = id,
            Name = name,
            AccountName = account,
            CDKey = cdKey,
            IsDeleted = isDeleted,
            LocationAreaResref = "dm_test_unknown_area",
            LastLoginDate = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc)
        };

    // ---- online characters

    [Test]
    public void GetOnlinePlayer_FindsACharacterByPlayerId()
    {
        _world.AddPlayer(Aria, name: "Aria Voss");
        _world.AddPlayer(Brent, name: "Brent Hale");

        _service.GetOnlinePlayer(Brent.ToString()).Should().Be(Brent);
    }

    [Test]
    public void GetOnlinePlayer_ReturnsInvalidForAnOfflineCharacter()
    {
        _world.AddPlayer(Aria, name: "Aria Voss");

        _service.GetOnlinePlayer(OfflineId).Should().Be(Invalid);
    }

    [Test]
    public void GetOnlinePlayer_SkipsDMsAndTheCreaturesTheyPossess()
    {
        _world.AddPlayer(DM, isDM: true);
        _world.Possess(DM, Creature, DMPossess);

        _service.GetOnlinePlayer(DM.ToString()).Should().Be(Invalid);
        _service.GetOnlinePlayer(Creature.ToString()).Should().Be(Invalid);
    }

    [Test]
    public void Search_ListsEveryOnlineCharacterWhenNoTextIsGiven()
    {
        _world.AddPlayer(DM, isDM: true);
        _world.AddPlayer(Brent, name: "Brent Hale", area: Cantina);
        _world.AddPlayer(Aria, name: "Aria Voss", area: Cantina);

        var results = _service.Search(string.Empty, DMPlayerSearchScope.All, out var isTruncated);

        isTruncated.Should().BeFalse();
        results.Select(result => result.Name).Should().Equal("Aria Voss", "Brent Hale");
        var aria = results[0];
        aria.IsOnline.Should().BeTrue();
        aria.PlayerId.Should().Be(Aria.ToString());
        aria.AccountName.Should().Be("account10");
        aria.CDKey.Should().Be("KEY10");
        aria.LocationName.Should().Be("Cantina");
        aria.IsDisguised.Should().BeFalse();
    }

    [Test]
    public void Search_MatchesTheStartOfAWordInTheNameAccountOrCDKey()
    {
        _world.AddPlayer(Aria, name: "Aria Voss");
        _world.AddPlayer(Brent, name: "Brent Hale");

        _service.Search("vo", DMPlayerSearchScope.Online, out _).Select(result => result.Name).Should().Equal("Aria Voss");
        _service.Search("account11", DMPlayerSearchScope.Online, out _).Select(result => result.Name).Should().Equal("Brent Hale");
        _service.Search("KEY10", DMPlayerSearchScope.Online, out _).Select(result => result.Name).Should().Equal("Aria Voss");
        _service.Search("oss", DMPlayerSearchScope.Online, out _).Should().BeEmpty("only the start of a word matches");
    }

    [Test]
    public void Search_MatchesAWholePlayerIdOnline()
    {
        const uint character = 0x100;
        _world.AddPlayer(character, name: "Cleo Marsh");
        _world.Engine.SetReturnHandler("GetObjectUUID", _ => OfflineId);

        var results = _service.Search(OfflineId.ToUpperInvariant(), DMPlayerSearchScope.Online, out _);

        results.Should().ContainSingle().Which.PlayerId.Should().Be(OfflineId);
    }

    [Test]
    public void Search_ReturnsNothingForTextWithNoQueryableWords()
    {
        _world.AddPlayer(Aria, name: "Aria Voss");

        var results = _service.Search(", . *", DMPlayerSearchScope.All, out var isTruncated);

        results.Should().BeEmpty();
        isTruncated.Should().BeFalse();
        _db.Queries.Should().BeEmpty();
    }

    [Test]
    public void Search_ReadsTheOnlineCharactersRecordForTheLastLogin()
    {
        var login = new DateTime(2026, 10, 2, 8, 30, 0, DateTimeKind.Utc);
        _world.AddPlayer(Aria, name: "Aria Voss");
        _db.With(new Player { Id = Aria.ToString(), LastLoginDate = login });

        var results = _service.Search(string.Empty, DMPlayerSearchScope.Online, out _);

        results.Should().ContainSingle().Which.LastLoginDate.Should().Be(login);
    }

    // ---- offline characters

    [Test]
    public void Search_DoesNotListOfflineCharactersWhenNoTextIsGiven()
    {
        _db.With(OfflinePlayer(OfflineId, "Dara Quill"));

        var results = _service.Search(string.Empty, DMPlayerSearchScope.All, out _);

        results.Should().BeEmpty();
        _db.Queries.Should().BeEmpty();
    }

    [Test]
    public void Search_FindsAnOfflineCharacterThroughTheIndex()
    {
        _db.With(OfflinePlayer(OfflineId, "Dara Quill"));

        var results = _service.Search("dara", DMPlayerSearchScope.All, out var isTruncated);

        isTruncated.Should().BeFalse();
        var result = results.Should().ContainSingle().Subject;
        result.IsOnline.Should().BeFalse();
        result.PlayerId.Should().Be(OfflineId);
        result.Name.Should().Be("Dara Quill");
        result.AccountName.Should().Be("offlineacct");
        result.CDKey.Should().Be("OFFKEY01");
        result.LocationName.Should().Be("dm_test_unknown_area", "an area that no longer exists shows its resref");
        _db.Queries.Should().NotBeEmpty();
        _db.Queries.OfType<DBQuery<Player>>().Select(query => query.BuildQuery().QueryString)
            .Should().Contain(query => query.Contains("@IsDeleted:0"), "deleted characters are never searched");
    }

    [Test]
    public void Search_SkipsOfflineCharactersWhoDoNotMatchEveryWord()
    {
        _db.With(OfflinePlayer(OfflineId, "Dara Quill"));
        _db.With(OfflinePlayer("00000000-0000-0000-0000-000000000002", "Dara Voss"));

        var results = _service.Search("dara quill", DMPlayerSearchScope.All, out _);

        results.Select(result => result.Name).Should().Equal("Dara Quill");
    }

    [Test]
    public void Search_SkipsDeletedCharacters()
    {
        _db.With(OfflinePlayer(OfflineId, "Dara Quill", isDeleted: true));

        _service.Search("dara", DMPlayerSearchScope.All, out _).Should().BeEmpty();
    }

    [Test]
    public void Search_ListsACharacterOnlineRatherThanOffline()
    {
        _world.AddPlayer(Aria, name: "Dara Quill");
        _db.With(OfflinePlayer(Aria.ToString(), "Dara Quill"));

        var results = _service.Search("dara", DMPlayerSearchScope.All, out _);

        results.Should().ContainSingle().Which.IsOnline.Should().BeTrue();
    }

    [Test]
    public void Search_InTheOnlineScopeNeverSearchesOffline()
    {
        _db.With(OfflinePlayer(OfflineId, "Dara Quill"));

        _service.Search("dara", DMPlayerSearchScope.Online, out _).Should().BeEmpty();

        _db.Queries.Should().BeEmpty();
    }

    [Test]
    public void Search_InTheOfflineScopeListsOnlyOfflineCharacters()
    {
        _world.AddPlayer(Aria, name: "Dara Voss");
        _db.With(OfflinePlayer(OfflineId, "Dara Quill"));

        var results = _service.Search("dara", DMPlayerSearchScope.Offline, out _);

        results.Select(result => result.Name).Should().Equal("Dara Quill");
    }

    [Test]
    public void Search_FindsAnOfflineCharacterByWholePlayerId()
    {
        _db.With(OfflinePlayer(OfflineId, "Dara Quill"));

        var results = _service.Search(OfflineId.ToUpperInvariant(), DMPlayerSearchScope.All, out _);

        results.Should().ContainSingle().Which.PlayerId.Should().Be(OfflineId);
        _db.Reads.Should().Contain(OfflineId, "player records are keyed by the lowercase GUID");
    }

    [Test]
    public void Search_ReadsTheCharactersOfADisguiseThatIsNoLongerTheirActiveOne()
    {
        _db.With(OfflinePlayer(OfflineId, "Dara Quill"));
        _db.With(new PlayerDisguise { Id = "disguise-1", PlayerId = OfflineId, Descriptor = "Hooded Stranger" });

        var results = _service.Search("hooded", DMPlayerSearchScope.All, out var isTruncated);

        results.Should().BeEmpty("the character's active disguise is not this one");
        isTruncated.Should().BeFalse();
        _db.Queries.OfType<DBQuery<PlayerDisguise>>().Should().NotBeEmpty("disguise descriptors are searched too");
    }

    [Test]
    public void Search_OrdersOnlineCharactersFirstThenByName()
    {
        _world.AddPlayer(Brent, name: "Zed Online");
        _db.With(OfflinePlayer(OfflineId, "Zed Alpha"));
        _db.With(OfflinePlayer("00000000-0000-0000-0000-000000000003", "Zed Beta"));

        var results = _service.Search("zed", DMPlayerSearchScope.All, out _);

        results.Select(result => result.Name).Should().Equal("Zed Online", "Zed Alpha", "Zed Beta");
    }

    [Test]
    public void Search_StopsAtTheResultLimitAndSaysSo()
    {
        for (var index = 0; index < DMPlayerDirectoryService.MaxResults + 20; index++)
            _db.With(OfflinePlayer(Guid.NewGuid().ToString(), "Zed" + index));

        var results = _service.Search("zed", DMPlayerSearchScope.All, out var isTruncated);

        results.Should().HaveCount(DMPlayerDirectoryService.MaxResults);
        isTruncated.Should().BeTrue();
    }

    [Test]
    public void Search_StopsAtTheCandidateLimitWhenNothingMatches()
    {
        for (var index = 0; index < 200; index++)
            _db.With(OfflinePlayer(Guid.NewGuid().ToString(), "Zed" + index));

        // The in-memory database ignores the paging window, so every page is a full page of characters who do not match.
        var results = _service.Search("quill", DMPlayerSearchScope.All, out var isTruncated);

        results.Should().BeEmpty();
        isTruncated.Should().BeTrue("the search gave up at the candidate limit");
    }

    // ---- where an offline character was last saved

    [Test]
    public void TryGetLastLocation_IsFalseForACharacterThatIsNotSaved()
    {
        _service.TryGetLastLocation(OfflineId, out var location).Should().BeFalse();

        location.Should().BeNull();
    }

    [Test]
    public void TryGetLastLocation_IsFalseWhenTheCharacterHasNoSavedArea()
    {
        var saved = OfflinePlayer(OfflineId, "Dara Quill");
        saved.LocationAreaResref = " ";
        _db.With(saved);

        _service.TryGetLastLocation(OfflineId, out _).Should().BeFalse();
    }

    [Test]
    public void TryGetLastLocation_IsFalseWhenTheAreaNoLongerExists()
    {
        _db.With(OfflinePlayer(OfflineId, "Dara Quill"));

        _service.TryGetLastLocation(OfflineId, out _).Should().BeFalse();
    }

    [Test]
    public void TryGetLastLocation_ReturnsTheSavedPosition()
    {
        const string resref = "dm_test_cantina";
        _world.Engine.SetReturnSequence("GetFirstArea", Cantina);
        _world.Engine.SetReturnHandler("GetResRef", _ => resref);
        Area.CacheData();
        var spot = EngineStructures.Create<Location>(21);
        _world.Engine.SetReturn("Location", spot);
        var saved = OfflinePlayer(OfflineId, "Dara Quill");
        saved.LocationAreaResref = resref;
        saved.LocationX = 1.5f;
        saved.LocationY = 2.5f;
        saved.LocationZ = 3.5f;
        saved.LocationOrientation = 90f;
        _db.With(saved);

        _service.TryGetLastLocation(OfflineId, out var location).Should().BeTrue();

        location.Should().BeSameAs(spot);
        var call = _world.Engine.CallsTo("Location").Should().ContainSingle().Subject;
        call.Arguments[0].Should().Be(Cantina);
        call.Arguments[1].Should().Be(new System.Numerics.Vector3(1.5f, 2.5f, 3.5f));
        call.Arguments[2].Should().Be(90f);
    }
}
