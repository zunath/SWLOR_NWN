using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// Players traveling to and from a running event: joining from a regular area or from an instance, going back, the
/// host sending everyone back, the announcements, and the time zone each viewer sees. The engine is a recording fake
/// with one player online, the database an in-memory one, and the neighbouring services scripted stand-ins.
/// </summary>
[TestFixture]
public class HostedEventsTravelTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Dm = 1001;
    private const uint Alice = 2001;
    private const uint Bob = 2002;
    private const uint StartArea = 3001;
    private const uint EventArea = 3002;
    private const uint Instance = 3003;

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private InMemoryDBService _db = null!;
    private HostedEventsService _service = null!;
    private ScriptedProxy _presence = null!;
    private ScriptedProxy _area = null!;
    private ScriptedProxy _enmity = null!;
    private ScriptedProxy _space = null!;
    private ScriptedProxy _death = null!;
    private ScriptedProxy _preferences = null!;
    private readonly List<uint> _online = new();
    private readonly Dictionary<uint, uint> _areaOf = new();
    private readonly Dictionary<uint, string> _uuids = new();
    private readonly HashSet<uint> _dead = new();
    private bool _landingAvailable;
    private float _distanceToDestination;
    private int _nextPc;
    private string _localEventId = string.Empty;
    private int _refreshes;

    [SetUp]
    public void SetUp()
    {
        _online.Clear();
        _areaOf.Clear();
        _uuids.Clear();
        _dead.Clear();
        _landingAvailable = true;
        _distanceToDestination = 0f;
        _localEventId = string.Empty;
        _refreshes = 0;

        _online.Add(Alice);
        _uuids[Alice] = "alice";
        _uuids[Bob] = "bob";
        _areaOf[Alice] = StartArea;
        _areaOf[Bob] = StartArea;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetFirstPC", _ =>
        {
            _nextPc = 1;
            return _online.Count > 0 ? _online[0] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetNextPC", _ => _nextPc < _online.Count ? _online[_nextPc++] : ObjectInvalid);
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid && (uint)arguments[0]! != 0);
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! is Alice or Bob);
        _engine.SetReturnHandler("GetIsDead", arguments => _dead.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetObjectUUID", arguments => _uuids.GetValueOrDefault((uint)arguments[0]!, string.Empty));
        _engine.SetReturnHandler("GetName", arguments => (uint)arguments[0]! switch { Alice => "Alice", Bob => "Bob", _ => "Area Name" });
        _engine.SetReturnHandler("GetArea", arguments => _areaOf.GetValueOrDefault((uint)arguments[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetResRef", arguments => (uint)arguments[0]! switch
        {
            StartArea => "area_start",
            EventArea => "area_event",
            Instance => "area_instance",
            _ => string.Empty,
        });
        _engine.SetReturnHandler("GetAreaFromLocation", _ => StartArea);
        _engine.SetReturnHandler("GetDistanceBetweenLocations", _ => _distanceToDestination);
        _engine.SetReturnHandler("GetLocalString", _ => _localEventId);
        _scope = NWScriptRuntime.Override(_engine);

        _db = new InMemoryDBService();
        _db.With(new Player { Id = "alice", LocationAreaResref = "saved_area" });
        _db.With(new Player { Id = "bob", LocationAreaResref = "saved_area" });

        var gui = ScriptedProxy.Create<IGuiRefreshPublisher>(out var guiScript);
        guiScript.On("PublishRefreshEvent", _ =>
        {
            _refreshes++;
            return null;
        });
        var presence = ScriptedProxy.Create<IDMPresenceService>(out _presence);
        _presence.On("DescribeDM", _ => "DM Test");
        _presence.On("GetDM", arguments => arguments[0]);
        _presence.On("GetControlledCreature", arguments => arguments[0]);
        _presence.On("IsControlledByDM", arguments => (uint)arguments[0]! == Dm);
        _presence.On("TryGetLanding", arguments =>
        {
            arguments[2] = EngineStructures.Create<Location>(77);
            return _landingAvailable;
        });
        var area = ScriptedProxy.Create<IAreaService>(out _area);
        _area.On("GetAreaByResref", arguments => (string)arguments[0]! switch
        {
            "area_start" => StartArea,
            "area_event" => EventArea,
            _ => ObjectInvalid,
        });
        var enmity = ScriptedProxy.Create<IEnmityService>(out _enmity);
        _enmity.On("HasEnmity", _ => false);
        var space = ScriptedProxy.Create<ISpaceModeQuery>(out _space);
        _space.On("IsPlayerInSpaceMode", _ => false);
        var death = ScriptedProxy.Create<IDeathService>(out _death);
        _death.On("GetRespawnLocation", _ => EngineStructures.Create<Location>(88));
        var preferences = ScriptedProxy.Create<IDMPreferenceService>(out _preferences);
        _preferences.On("Get", _ => new DMPreferences { TimeZoneId = "America/New_York" });

        _service = new HostedEventsService(
            _db, gui, presence, area, enmity, space, ScriptedProxy.Create<IBackgroundJobService>(out _), death, preferences);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    private HostedEvent RunningEvent(bool announce = false)
    {
        var error = _service.SaveDetails(Dm, string.Empty, "Race night", "Host", "d", "2026-11-20 19:30", null, PlayerTimeZones.Utc, true, announce, out var saved);
        error.Should().BeEmpty();
        saved.ArrivalAreaResref = "area_event";
        _service.Start(Dm, saved.Id).Should().BeEmpty();
        return saved;
    }

    private IEnumerable<string> Messages() => _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!);

    [Test]
    public void JoiningFromARegularAreaRemembersTheSpotAndListsThePlayer()
    {
        var hostedEvent = RunningEvent();

        _service.Join(Alice, hostedEvent.Id).Should().BeEmpty();

        var returnPoint = _db.Get<Player>("alice").EventReturnPoint;
        returnPoint.EventId.Should().Be(hostedEvent.Id);
        returnPoint.EventName.Should().Be("Race night");
        returnPoint.AreaResref.Should().Be("area_start");
        hostedEvent.ParticipantPlayerIds.Should().BeEquivalentTo(new[] { "alice" });
        _engine.CountOf("AssignCommand").Should().Be(1);
        _engine.CountOf("SetLocalLocation").Should().Be(1);
        Messages().Should().ContainSingle(text => text.Contains("Traveling to Race night"));
        _service.GetReturnPoint(Alice).Should().BeSameAs(returnPoint);
        _service.CountParticipants(hostedEvent).Should().Be((1, 1));
    }

    [Test]
    public void JoiningFromAnInstanceFallsBackToThePlayersLastSavedLocation()
    {
        var hostedEvent = RunningEvent();
        _areaOf[Alice] = Instance;
        var player = _db.Get<Player>("alice");
        player.LocationX = 4f;
        player.LocationOrientation = 90f;

        _service.Join(Alice, hostedEvent.Id).Should().BeEmpty();

        var returnPoint = _db.Get<Player>("alice").EventReturnPoint;
        returnPoint.AreaResref.Should().Be("saved_area");
        returnPoint.X.Should().Be(4f);
        returnPoint.Facing.Should().Be(90f);
    }

    [Test]
    public void JoiningIsRefusedWhenTheEventIsClosed_TheArrivalIsGone_OrTheArrivalIsBlocked()
    {
        var hostedEvent = RunningEvent();

        _service.Join(Alice, "missing").Should().Be(HostedEventRules.EventNotFound);

        hostedEvent.ArrivalAreaResref = "area_gone";
        _service.Join(Alice, hostedEvent.Id).Should().Be("The arrival point no longer exists. Let the host know.");

        hostedEvent.ArrivalAreaResref = "area_event";
        _landingAvailable = false;
        _service.Join(Alice, hostedEvent.Id).Should().Be("The arrival point is blocked. Let the host know.");
        _db.Get<Player>("alice").EventReturnPoint.Should().BeNull();
    }

    [Test]
    public void ADeadPlayerOrADMCannotJoin()
    {
        var hostedEvent = RunningEvent();
        _dead.Add(Alice);

        _service.Join(Alice, hostedEvent.Id).Should().Be(HostedEventRules.DeadCannotJoin);
        _service.Join(Dm, hostedEvent.Id).Should().Be(HostedEventRules.DMsUseDMTools);
        _service.GetReturnPoint(Dm).Should().BeNull();
    }

    [Test]
    public void ThePlayersTravelStateReportsWhereTheyAre()
    {
        var hostedEvent = RunningEvent();
        _service.Join(Alice, hostedEvent.Id);
        _areaOf[Alice] = EventArea;
        _engine.SetReturnHandler("GetResRef", arguments => (uint)arguments[0]! == EventArea ? "ooc_area" : "x");
        _dead.Add(Alice);
        _space.On("IsPlayerInSpaceMode", _ => true);
        _enmity.On("HasEnmity", _ => true);

        var state = _service.GetTravelState(Alice);

        state.IsDM.Should().BeFalse();
        state.IsDead.Should().BeTrue();
        state.IsInSpace.Should().BeTrue();
        state.IsInCombat.Should().BeTrue();
        state.IsInOutOfCharacterArea.Should().BeTrue();
        state.CurrentEventId.Should().Be(hostedEvent.Id);
        _service.GetTravelState(Dm).IsDM.Should().BeTrue();
    }

    [Test]
    public void LeavingSendsThePlayerBackAndClearsTheReturnPointOnceTheyArrive()
    {
        var hostedEvent = RunningEvent();
        _service.Join(Alice, hostedEvent.Id);
        _engine.ClearCalls();

        _service.Leave(Alice).Should().BeEmpty();

        _engine.CountOf("AssignCommand").Should().Be(1);
        _db.Get<Player>("alice").EventReturnPoint.Should().BeNull();
        hostedEvent.ParticipantPlayerIds.Should().BeEmpty();
        _engine.CountOf("DeleteLocalLocation").Should().Be(1);
        _engine.CountOf("DeleteLocalString").Should().Be(1);
    }

    [Test]
    public void ALeavingPlayerWhoHasNotArrivedKeepsTheirReturnPoint()
    {
        var hostedEvent = RunningEvent();
        _service.Join(Alice, hostedEvent.Id);
        _distanceToDestination = 100f;

        _service.Leave(Alice).Should().BeEmpty();

        _db.Get<Player>("alice").EventReturnPoint.Should().NotBeNull();
        hostedEvent.ParticipantPlayerIds.Should().Contain("alice");
    }

    [Test]
    public void LeavingTakesTheExactSpotRememberedThisSession()
    {
        var hostedEvent = RunningEvent();
        _service.Join(Alice, hostedEvent.Id);
        _localEventId = hostedEvent.Id;

        _service.Leave(Alice).Should().BeEmpty();

        _engine.CountOf("GetLocalLocation").Should().Be(1);
        _death.Calls.Should().BeEmpty("the remembered spot was usable");
    }

    [Test]
    public void LeavingFallsBackToTheRespawnLocationWhenTheReturnPointIsGone()
    {
        var hostedEvent = RunningEvent();
        _service.Join(Alice, hostedEvent.Id);
        _db.Get<Player>("alice").EventReturnPoint.AreaResref = "area_gone";

        _service.Leave(Alice).Should().BeEmpty();

        _death.Calls.Should().ContainSingle(call => call.Member == "GetRespawnLocation");
    }

    [Test]
    public void LeavingIsRefusedWhenNotAtAnEventOrWhenEveryWayBackIsBlocked()
    {
        _service.Leave(Alice).Should().Be(HostedEventRules.NotAtEvent);

        var hostedEvent = RunningEvent();
        _service.Join(Alice, hostedEvent.Id);
        _landingAvailable = false;

        _service.Leave(Alice).Should().StartWith("Your way back is blocked");
        _db.Get<Player>("alice").EventReturnPoint.Should().NotBeNull();
    }

    [Test]
    public void TheHostSendingEveryoneBackReportsWhoWentAndWhoCouldNot()
    {
        var hostedEvent = RunningEvent();
        _service.Join(Alice, hostedEvent.Id);
        _online.Add(Bob);
        _service.Join(Bob, hostedEvent.Id);
        hostedEvent.ParticipantPlayerIds.Add("offline-player");
        _dead.Add(Bob);

        var summary = _service.SendEveryoneBack(Dm, hostedEvent.Id);

        summary.Should().Contain("Sent 1 player back.");
        summary.Should().Contain($"Not sent: Bob ({HostedEventRules.DeadCannotLeave}).");
        summary.Should().Contain("1 offline player keeps /leaveevent.");
        Messages().Should().Contain(text => text.Contains("The host sent you back from Race night."));
        _service.SendEveryoneBack(Dm, "missing").Should().Be(HostedEventRules.EventNotFound);
    }

    [Test]
    public void StartingAnAnnouncingEventTellsPlayersWhoWantAnnouncementsAndNoOneElse()
    {
        _online.Add(Bob);
        _db.Get<Player>("bob").Settings.DisplayEventAnnouncements = false;
        _db.Get<Player>("alice").Settings.DisplayEventAnnouncements = null;

        RunningEvent(announce: true);

        var announced = _engine.CallsTo("SendMessageToPC").Where(call => ((string)call.Arguments[1]!).Contains("Event started: Race night")).ToList();
        announced.Should().ContainSingle();
        announced[0].Arguments[0].Should().Be(Alice);
        _refreshes.Should().BeGreaterThan(0);
    }

    [Test]
    public void EndingARunningEventTellsItsOnlineParticipants()
    {
        var hostedEvent = RunningEvent();
        _service.Join(Alice, hostedEvent.Id);
        _engine.ClearCalls();

        _service.End(Dm, hostedEvent.Id).Should().BeEmpty();

        Messages().Should().ContainSingle(text => text.Contains("Race night has ended"));
    }

    [Test]
    public void EachViewerSeesTheirOwnTimeZone()
    {
        _db.Get<Player>("alice").Settings.TimeZoneId = "Asia/Tokyo";

        _service.GetPlayerTimeZone(Alice).Id.Should().Be("Asia/Tokyo");
        _service.GetPlayerTimeZone(Bob).Id.Should().Be(PlayerTimeZones.UtcId);
        _service.GetPlayerTimeZone(Dm).Id.Should().Be("America/New_York");
    }

    [Test]
    public void RefreshingOnePlayersWindowsPublishesOneRefresh()
    {
        _service.RefreshWindows(Alice);

        _refreshes.Should().Be(1);
    }

    [Test]
    public void TheArrivalAreaNameIsEmptyUntilTheAreaExists()
    {
        var hostedEvent = RunningEvent();
        _service.GetArrivalAreaName(hostedEvent).Should().Be("Area Name");

        hostedEvent.ArrivalAreaResref = "area_gone";
        _service.GetArrivalAreaName(hostedEvent).Should().BeEmpty();
        _service.GoToArrivalPoint(Dm, hostedEvent.Id).Should().Be(HostedEventRules.NoArrivalPoint);
    }

    [Test]
    public void ADMGoesToTheArrivalPoint_AndSetsItWhereTheyStand()
    {
        var hostedEvent = RunningEvent();
        _presence.On("JumpTo", _ => null);
        _areaOf[Dm] = StartArea;

        _service.GoToArrivalPoint(Dm, hostedEvent.Id).Should().BeEmpty();
        _presence.Calls.Should().ContainSingle(call => call.Member == "JumpTo");

        _service.SetArrivalPoint(Dm, hostedEvent.Id).Should().BeEmpty();
        hostedEvent.ArrivalAreaResref.Should().Be("area_start");
    }
}
