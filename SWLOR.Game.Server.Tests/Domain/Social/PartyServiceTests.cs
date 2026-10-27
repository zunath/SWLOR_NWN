using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// Unit tests of the party instance: the engine (NWScript and the events plugin) is replaced by recording fakes, so no
/// server is needed. The party bars and the native faction are engine state; what these tests prove is the cache the
/// server keeps beside them: who is in whose party and who leads it.
/// </summary>
[TestFixture]
public class PartyServiceTests
{
    private const uint Leader = 101;
    private const uint Second = 102;
    private const uint Third = 103;
    private const uint Fourth = 104;
    private const uint AreaOne = 900;
    private const uint AreaTwo = 901;

    private FakeNWScript _engine = null!;
    private FakeEventsPlugin _events = null!;
    private PartyService _service = null!;
    private readonly Dictionary<string, uint> _eventObjects = new();
    private readonly Dictionary<uint, int> _factions = new();
    private readonly Dictionary<uint, uint> _areas = new();
    private readonly Dictionary<(uint, uint), float> _distances = new();
    private IDisposable _engineScope = null!;
    private IDisposable _pluginScope = null!;
    private IDisposable _containerScope = null!;
    private ServiceProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _eventObjects.Clear();
        _factions.Clear();
        _areas.Clear();
        _distances.Clear();

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("StringToObject", arguments => Convert.ToUInt32((string)arguments[0]!, 16));
        _engine.SetReturnHandler("GetFactionEqual", arguments =>
            _factions.GetValueOrDefault((uint)arguments[0]!, -1) == _factions.GetValueOrDefault((uint)arguments[1]!, -2));
        _engine.SetReturnHandler("GetArea", arguments => _areas.GetValueOrDefault((uint)arguments[0]!));
        _engine.SetReturnHandler("GetDistanceBetween", arguments =>
            _distances.GetValueOrDefault(((uint)arguments[0]!, (uint)arguments[1]!)));
        _events = new FakeEventsPlugin();
        _events.SetReturnHandler("GetEventData", arguments => _eventObjects[(string)arguments[0]!].ToString("x"));

        _service = new PartyService();
        // Aura code that runs when the party changes asks the Party facade, which resolves this instance.
        _provider = ContainerHost.Create(new ServiceCollection().AddSingleton<IPartyService>(_service));
        _containerScope = CompositionRoot.InstallForTests(_provider);
        _engineScope = NWScriptRuntime.Override(_engine);
        _pluginScope = EventsPluginRuntime.Override(_events);
    }

    [TearDown]
    public void TearDown()
    {
        _pluginScope.Dispose();
        _engineScope.Dispose();
        _containerScope.Dispose();
        _provider.Dispose();
    }

    // The engine puts an invited creature in its inviter's party before the After event the server handles.
    private void Accept(uint invited, uint inviter)
    {
        _engine.SetReturn("OBJECT_SELF", invited);
        _eventObjects["INVITED_BY"] = inviter;
        _factions[invited] = _factions.GetValueOrDefault(inviter, (int)inviter);
        _factions[inviter] = _factions.GetValueOrDefault(inviter, (int)inviter);
        _service.JoinParty();
    }

    private Dictionary<Guid, uint> Leaders() =>
        (Dictionary<Guid, uint>)typeof(PartyService).GetField("_partyLeaders", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_service)!;

    private uint LeaderOf(uint member)
    {
        var parties = (Dictionary<uint, Guid>)typeof(PartyService).GetField("_creatureToParty", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_service)!;
        return Leaders()[parties[member]];
    }

    // ---- joining by invitation

    [Test]
    public void A_creature_outside_any_party_is_listed_alone()
    {
        _service.GetAllPartyMembers(Leader).Should().Equal(Leader);
        _service.IsInParty(Leader, Leader).Should().BeTrue();
        _service.IsInParty(Leader, Second).Should().BeFalse();
    }

    [Test]
    public void Accepting_an_invitation_starts_a_party_led_by_the_inviter()
    {
        Accept(Second, Leader);

        _service.GetAllPartyMembers(Leader).Should().Equal(Leader, Second);
        _service.GetAllPartyMembers(Second).Should().Equal(Leader, Second);
        LeaderOf(Second).Should().Be(Leader);
        Leaders().Should().ContainSingle();
    }

    [Test]
    public void A_later_acceptance_joins_the_existing_party()
    {
        Accept(Second, Leader);
        Accept(Third, Second);

        _service.GetAllPartyMembers(Third).Should().BeEquivalentTo(new[] { Leader, Second, Third });
        _service.IsInParty(Leader, Third).Should().BeTrue();
        LeaderOf(Third).Should().Be(Leader);
        Leaders().Should().ContainSingle();
    }

    [Test]
    public void An_acceptance_the_engine_refused_changes_nothing()
    {
        // The engine put no faction link between them, so the Before-event acceptance did not happen.
        _engine.SetReturn("OBJECT_SELF", Second);
        _factions[Second] = 1;
        _factions[Leader] = 2;
        _eventObjects["INVITED_BY"] = Leader;

        _service.JoinParty();

        _service.GetAllPartyMembers(Leader).Should().Equal(Leader);
        _service.IsInParty(Leader, Second).Should().BeFalse();
    }

    [Test]
    public void An_acceptance_by_someone_already_in_the_party_changes_nothing()
    {
        Accept(Second, Leader);

        Accept(Second, Leader);

        _service.GetAllPartyMembers(Leader).Should().Equal(Leader, Second);
    }

    [Test]
    public void Accepting_another_invitation_moves_the_player_out_of_their_old_party()
    {
        Accept(Second, Leader);
        Accept(Third, Leader);
        var otherLeader = Fourth;

        // Second accepts the invitation of a lone player elsewhere.
        _factions[otherLeader] = 50;
        _engine.SetReturn("OBJECT_SELF", Second);
        _eventObjects["INVITED_BY"] = otherLeader;
        _factions[Second] = 50;
        _service.JoinParty();

        _service.GetAllPartyMembers(Leader).Should().Equal(Leader, Third);
        _service.GetAllPartyMembers(Second).Should().Equal(otherLeader, Second);
        LeaderOf(Second).Should().Be(otherLeader);
    }

    // ---- associates

    [Test]
    public void An_associate_joins_its_owners_party_and_leaves_it()
    {
        _engine.SetReturn("OBJECT_SELF", Leader);
        _eventObjects["ASSOCIATE_OBJECT_ID"] = Second;

        _service.AssociateJoinParty();

        _service.GetAllPartyMembers(Leader).Should().Equal(Leader, Second);
        LeaderOf(Second).Should().Be(Leader);

        _service.AssociateLeaveParty();

        _service.GetAllPartyMembers(Leader).Should().Equal(Leader);
        _service.GetAllPartyMembers(Second).Should().Equal(Second);
    }

    // ---- leaving

    [Test]
    public void Leaving_a_party_of_two_disbands_it()
    {
        Accept(Second, Leader);
        _eventObjects["LEAVING"] = Second;

        _service.LeaveParty();

        _service.GetAllPartyMembers(Leader).Should().Equal(Leader);
        _service.GetAllPartyMembers(Second).Should().Equal(Second);
        Leaders().Should().BeEmpty();
    }

    [Test]
    public void A_member_leaving_a_larger_party_leaves_the_rest_together()
    {
        Accept(Second, Leader);
        Accept(Third, Leader);
        _eventObjects["LEAVING"] = Second;

        _service.LeaveParty();

        _service.GetAllPartyMembers(Leader).Should().Equal(Leader, Third);
        _service.GetAllPartyMembers(Second).Should().Equal(Second);
        LeaderOf(Third).Should().Be(Leader);
    }

    [Test]
    public void When_the_leader_leaves_the_next_member_in_the_list_leads()
    {
        Accept(Second, Leader);
        Accept(Third, Leader);
        _eventObjects["LEAVING"] = Leader;

        _service.LeaveParty();

        _service.GetAllPartyMembers(Second).Should().Equal(Second, Third);
        LeaderOf(Second).Should().Be(Second);
    }

    [Test]
    public void Leaving_when_in_no_party_is_ignored()
    {
        _eventObjects["LEAVING"] = Fourth;

        var leave = () => _service.LeaveParty();

        leave.Should().NotThrow();
    }

    [Test]
    public void A_player_leaving_the_server_is_removed_from_the_party()
    {
        Accept(Second, Leader);
        _engine.SetReturn("GetExitingObject", Second);

        _service.LeaveServer();

        _service.GetAllPartyMembers(Leader).Should().Equal(Leader);
    }

    [Test]
    public void Removing_a_player_by_script_tells_the_engine_and_updates_the_caches()
    {
        Accept(Second, Leader);
        Accept(Third, Leader);

        _service.RemoveFromCurrentParty(Third);

        _engine.CallsTo("RemoveFromParty").Should().ContainSingle().Which.Arguments[0].Should().Be(Third);
        _service.GetAllPartyMembers(Leader).Should().Equal(Leader, Second);
    }

    // ---- kicks

    [Test]
    public void A_kick_the_engine_accepted_removes_the_kicked_player()
    {
        Accept(Second, Leader);
        Accept(Third, Leader);
        _engine.SetReturn("OBJECT_SELF", Leader);
        _eventObjects["KICKED"] = Third;
        // After a real kick the engine has moved the kicked player to a faction of their own.
        _factions[Third] = 999;

        _service.KickFromParty();

        _service.GetAllPartyMembers(Leader).Should().Equal(Leader, Second);
        _service.IsInParty(Leader, Third).Should().BeFalse();
    }

    [Test]
    public void A_kick_the_engine_refused_keeps_the_player_in_the_party()
    {
        Accept(Second, Leader);
        Accept(Third, Second);
        // Only the leader may kick; the engine left everyone in the same faction.
        _engine.SetReturn("OBJECT_SELF", Second);
        _eventObjects["KICKED"] = Third;

        _service.KickFromParty();

        _service.GetAllPartyMembers(Leader).Should().BeEquivalentTo(new[] { Leader, Second, Third });
    }

    [Test]
    public void A_kick_of_someone_outside_the_party_is_ignored()
    {
        Accept(Second, Leader);
        _engine.SetReturn("OBJECT_SELF", Leader);
        _eventObjects["KICKED"] = Fourth;
        _factions[Fourth] = 999;

        _service.KickFromParty();

        _service.GetAllPartyMembers(Leader).Should().Equal(Leader, Second);
    }

    // ---- leadership

    [Test]
    public void A_leadership_transfer_event_makes_the_new_leader_lead()
    {
        Accept(Second, Leader);
        _eventObjects["NEW_LEADER"] = Second;

        _service.TransferLeadership();

        LeaderOf(Leader).Should().Be(Second);
    }

    [Test]
    public void A_leadership_transfer_to_someone_in_no_party_fails_as_it_always_did()
    {
        _eventObjects["NEW_LEADER"] = Fourth;

        var transfer = () => _service.TransferLeadership();

        transfer.Should().Throw<KeyNotFoundException>();
    }

    // ---- range

    [Test]
    public void Party_members_in_range_are_those_in_the_same_area_close_enough_plus_the_creature()
    {
        Accept(Second, Leader);
        Accept(Third, Leader);
        Accept(Fourth, Leader);
        _areas[Leader] = AreaOne;
        _areas[Second] = AreaOne;
        _areas[Third] = AreaOne;
        _areas[Fourth] = AreaTwo;
        _distances[(Second, Leader)] = 4f;
        _distances[(Third, Leader)] = 30f;
        _distances[(Fourth, Leader)] = 1f;

        _service.GetAllPartyMembersWithinRange(Leader, 10f).Should().Equal(Leader, Second);
    }

    [Test]
    public void A_range_of_zero_or_less_counts_as_zero()
    {
        Accept(Second, Leader);
        _areas[Leader] = AreaOne;
        _areas[Second] = AreaOne;
        _distances[(Second, Leader)] = 0.5f;

        _service.GetAllPartyMembersWithinRange(Leader, -5f).Should().Equal(Leader);
        _distances[(Second, Leader)] = 0f;
        _service.GetAllPartyMembersWithinRange(Leader, -5f).Should().Equal(Leader, Second);
    }

    [Test]
    public void The_range_of_a_creature_with_no_party_is_the_creature()
    {
        _service.GetAllPartyMembersWithinRange(Fourth, 10f).Should().Equal(Fourth);
    }

    // ---- hand-out

    [Test]
    public void The_member_list_is_a_copy()
    {
        Accept(Second, Leader);

        _service.GetAllPartyMembers(Leader).Clear();

        _service.GetAllPartyMembers(Leader).Should().Equal(Leader, Second);
    }

    [Test]
    public void Each_instance_keeps_its_own_caches()
    {
        Accept(Second, Leader);
        var other = new PartyService();

        other.GetAllPartyMembers(Leader).Should().Equal(Leader);
    }
}
