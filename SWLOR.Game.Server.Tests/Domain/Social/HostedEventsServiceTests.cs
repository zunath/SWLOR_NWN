using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// The event list a DM keeps: creating, editing, starting and ending events, the arrival point, and the listing of
/// the Discord server's events. The engine is a recording fake with nobody online, the database an in-memory one,
/// and the neighbouring services scripted stand-ins.
/// </summary>
[TestFixture]
public class HostedEventsServiceTests
{
    private const uint Dm = 1001;
    private const string SnapshotKey = "swlor:discord:scheduled-events";

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private InMemoryDBService _db = null!;
    private HostedEventsService _service = null!;
    private int _refreshes;

    [SetUp]
    public void SetUp()
    {
        _refreshes = 0;
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", _ => false);
        _engine.SetReturnHandler("GetName", _ => "Controlled Creature");
        _scope = NWScriptRuntime.Override(_engine);

        _db = new InMemoryDBService();
        var gui = ScriptedProxy.Create<IGuiRefreshPublisher>(out var guiScript);
        guiScript.On("PublishRefreshEvent", _ =>
        {
            _refreshes++;
            return null;
        });
        var presence = ScriptedProxy.Create<IDMPresenceService>(out var presenceScript);
        presenceScript.On("DescribeDM", _ => "DM Test");
        presenceScript.On("GetDM", arguments => arguments[0]);
        presenceScript.On("GetControlledCreature", arguments => arguments[0]);

        _service = new HostedEventsService(
            _db,
            gui,
            presence,
            ScriptedProxy.Create<IAreaService>(out _),
            ScriptedProxy.Create<IEnmityService>(out _),
            ScriptedProxy.Create<ISpaceModeQuery>(out _),
            ScriptedProxy.Create<IBackgroundJobService>(out _),
            ScriptedProxy.Create<IDeathRespawnService>(out _),
            ScriptedProxy.Create<IDMPreferenceService>(out _));
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    private HostedEvent Save(string name = "Race night", string start = "2026-11-20 19:30", bool join = true, bool announce = false)
    {
        var error = _service.SaveDetails(Dm, string.Empty, name, "Host", "Come along", start, null, PlayerTimeZones.Utc, join, announce, out var saved);
        error.Should().BeEmpty();
        return saved;
    }

    private static string Snapshot(string id, string name, string start = "2026-11-21T19:00:00Z") =>
        "{\"events\":[{\"id\":\"" + id + "\",\"status\":1,\"name\":\"" + name + "\",\"description\":\"d\"," +
        "\"scheduled_start_time\":\"" + start + "\",\"creator\":{\"global_name\":\"Jess\"}}]}";

    [Test]
    public void ANewEventIsSavedListedAndFoundById()
    {
        var saved = Save();

        saved.Name.Should().Be("Race night");
        saved.HostName.Should().Be("Host");
        saved.CreatedBy.Should().Be("DM Test");
        saved.ScheduledStartUtc.Should().Be(new DateTime(2026, 11, 20, 19, 30, 0, DateTimeKind.Utc));
        _service.GetEvent(saved.Id).Should().BeSameAs(saved);
        _service.GetEvents().Should().ContainSingle();
        _db.Saved.Should().Contain(saved);
        _refreshes.Should().Be(0, "nobody is online to refresh");
    }

    [Test]
    public void ABlankHostNameUsesTheNameOfTheDMsCreature()
    {
        var error = _service.SaveDetails(Dm, string.Empty, "Race", " ", "d", "2026-11-20 19:30", null, null, true, true, out var saved);

        error.Should().BeEmpty();
        saved.HostName.Should().Be("Controlled Creature");
    }

    [Test]
    public void BadDetailsAreRefusedAndNothingIsSaved()
    {
        var error = _service.SaveDetails(Dm, string.Empty, " ", "h", "d", "2026-11-20 19:30", null, null, true, true, out var saved);

        error.Should().StartWith("Enter a name");
        saved.Should().BeNull();
        _service.GetEvents().Should().BeEmpty();
    }

    [Test]
    public void EditingAnEventKeepsItsStartWhileTheShownTextIsUnchanged()
    {
        var first = Save();
        var shown = first.ScheduledStartUtc;

        var error = _service.SaveDetails(Dm, first.Id, "Renamed", "Host", "d", "2026-11-20 19:30", shown, PlayerTimeZones.Utc, false, false, out var saved);

        error.Should().BeEmpty();
        saved.Id.Should().Be(first.Id);
        saved.Name.Should().Be("Renamed");
        saved.IsJoinOpen.Should().BeFalse();
        saved.ScheduledStartUtc.Should().Be(shown);
        _service.GetEvents().Should().ContainSingle();
    }

    [Test]
    public void EditingAnEventThatHasEndedIsRefused()
    {
        var error = _service.SaveDetails(Dm, "missing", "Race", "h", "d", "2026-11-20 19:30", null, null, true, true, out var saved);

        error.Should().Be(HostedEventRules.EventNotFound);
        saved.Should().BeNull();
    }

    [Test]
    public void StartingAnEventMarksItRunning_AndStartingItAgainIsRefused()
    {
        var saved = Save();

        _service.Start(Dm, saved.Id).Should().BeEmpty();
        saved.Status.Should().Be(HostedEventStatus.Running);
        saved.StartedUtc.Should().NotBeNull();

        _service.Start(Dm, saved.Id).Should().Be("This event is already running.");
        _service.Start(Dm, "missing").Should().Be(HostedEventRules.EventNotFound);
    }

    [Test]
    public void StartingAnEventThatAnnouncesItself_ReachesNobodyWhenNobodyIsOnline()
    {
        var saved = Save(announce: true);

        _service.Start(Dm, saved.Id).Should().BeEmpty();
        saved.Status.Should().Be(HostedEventStatus.Running);
    }

    [Test]
    public void EndingAnEventRemovesItFromTheListAndTheDatabase()
    {
        var saved = Save();
        _service.Start(Dm, saved.Id);

        _service.End(Dm, saved.Id).Should().BeEmpty();

        _service.GetEvent(saved.Id).Should().BeNull();
        _db.Deleted.Should().Contain(saved.Id);
        _service.End(Dm, saved.Id).Should().Be(HostedEventRules.EventNotFound);
    }

    [Test]
    public void ClearingTheArrivalPointRemovesIt()
    {
        var saved = Save();
        saved.ArrivalAreaResref = "area_a";

        _service.ClearArrivalPoint(Dm, saved.Id).Should().BeEmpty();

        saved.ArrivalAreaResref.Should().BeEmpty();
        _service.ClearArrivalPoint(Dm, "missing").Should().Be(HostedEventRules.EventNotFound);
        _service.SetArrivalPoint(Dm, "missing").Should().Be(HostedEventRules.EventNotFound);
        _service.GoToArrivalPoint(Dm, "missing").Should().Be(HostedEventRules.EventNotFound);
        _service.GoToArrivalPoint(Dm, saved.Id).Should().Be(HostedEventRules.NoArrivalPoint);
    }

    [Test]
    public void SettingTheArrivalPointWhereNoPlayerCanBeSentIsRefused()
    {
        var saved = Save();

        _service.SetArrivalPoint(Dm, saved.Id).Should().StartWith("Players cannot be sent here");
    }

    [Test]
    public void AnEventWithoutParticipantsCountsNobody()
    {
        _service.CountParticipants(Save()).Should().Be((0, 0));
    }

    [Test]
    public void EventsStoredBeforeAReloadAreReadBack()
    {
        var stored = new HostedEvent { Id = "stored", Name = "Stored" };
        _db.With(stored);
        _db.With(new DiscordEventDismissal { Id = "d9", ScheduledStartUtc = new DateTime(2026, 11, 21, 19, 0, 0, DateTimeKind.Utc) });

        _service.LoadEvents();

        _service.GetEvent("stored").Should().BeSameAs(stored);
        _service.GetEvent(null).Should().BeNull();
        _service.GetEvent(string.Empty).Should().BeNull();
    }

    [Test]
    public void LoadingListsNewDiscordEventsAndSkipsDismissedOnes()
    {
        _db.Strings[SnapshotKey] = Snapshot("d1", "Movie night");
        _db.With(new DiscordEventDismissal { Id = "d1", ScheduledStartUtc = new DateTime(2026, 11, 21, 19, 0, 0, DateTimeKind.Utc) });
        _service.LoadEvents();
        _service.GetEvents().Should().BeEmpty("a dismissed occurrence stays off the list");

        _db.Strings[SnapshotKey] = Snapshot("d2", "Quiz");
        _service.LoadEvents();

        var listed = _service.GetEvents().Should().ContainSingle().Subject;
        listed.Name.Should().Be("Quiz");
        listed.DiscordEventId.Should().Be("d2");
        listed.CreatedBy.Should().Be("Discord");
        listed.HostName.Should().Be("Jess");
    }

    [Test]
    public void ADiscordEventTheDMHasNotStartedKeepsDiscordsDetails_ButTakesTheDMsOptions()
    {
        _db.Strings[SnapshotKey] = Snapshot("d1", "Movie night");
        _service.LoadEvents();
        var listed = _service.GetEvents().Single();

        var error = _service.SaveDetails(Dm, listed.Id, "Ignored", "Ignored", "Ignored", "garbage", null, null, false, false, out var saved);

        error.Should().BeEmpty();
        saved.Should().BeSameAs(listed);
        saved.Name.Should().Be("Movie night");
        saved.IsJoinOpen.Should().BeFalse();
        saved.AnnounceOnStart.Should().BeFalse();
    }

    [Test]
    public void AChangeOnDiscordReachesTheListedEvent_AndItsRemovalRemovesIt()
    {
        _db.Strings[SnapshotKey] = Snapshot("d1", "Movie night");
        _service.LoadEvents();
        var id = _service.GetEvents().Single().Id;

        _db.Strings[SnapshotKey] = Snapshot("d1", "Film night");
        _service.LoadEvents();
        _service.GetEvent(id).Name.Should().Be("Film night");

        _db.Strings[SnapshotKey] = Snapshot("other", "Other");
        _service.LoadEvents();
        _service.GetEvent(id).Should().BeNull();
        _db.Deleted.Should().Contain(id);
    }

    [Test]
    public void AnUnreadableSnapshotChangesNothing()
    {
        _db.Strings[SnapshotKey] = Snapshot("d1", "Movie night");
        _service.LoadEvents();

        _db.Strings[SnapshotKey] = "not json";
        _service.LoadEvents();

        _service.GetEvents().Should().ContainSingle();
    }

    [Test]
    public void EndingAListedDiscordEventDismissesItsOccurrence()
    {
        _db.Strings[SnapshotKey] = Snapshot("d1", "Movie night");
        _service.LoadEvents();
        var listed = _service.GetEvents().Single();

        _service.End(Dm, listed.Id).Should().BeEmpty();

        _db.Saved.OfType<DiscordEventDismissal>().Should().ContainSingle(d => d.Id == "d1")
            .Which.ScheduledStartUtc.Should().Be(new DateTime(2026, 11, 21, 19, 0, 0, DateTimeKind.Utc));
        _service.LoadEvents();
        _service.GetEvents().Should().BeEmpty("the next sync does not list it again");
    }

    [Test]
    public void ARunningDiscordEventIsLeftAloneWhenDiscordStopsListingIt()
    {
        _db.Strings[SnapshotKey] = Snapshot("d1", "Movie night");
        _service.LoadEvents();
        var listed = _service.GetEvents().Single();
        _service.Start(Dm, listed.Id);

        _db.Strings[SnapshotKey] = Snapshot("other", "Other");
        _service.LoadEvents();

        _service.GetEvent(listed.Id).Should().BeSameAs(listed);
    }
}
