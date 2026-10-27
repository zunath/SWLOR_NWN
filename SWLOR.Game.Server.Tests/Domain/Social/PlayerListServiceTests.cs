using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PlayerListService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// Unit tests of the player list instance. The engine (NWScript and the NWNX chat and player plugins) is replaced by
/// recording fakes and the database by an in-memory one. Two reads of native engine data (a creature's party faction
/// and its PvP attitude) are answered by a subclass. Characters in these tests are not player characters as far as the
/// engine fake says, so the code that looks up a player's disguise or settings in the real database is not reached.
/// </summary>
[TestFixture]
public class PlayerListServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint First = 2001;
    private const uint Second = 2002;
    private const uint Third = 2003;
    private const uint Fourth = 2004;
    private const uint DmAvatar = 3001;
    private const uint OtherDm = 3002;
    private const uint PossessedCreature = 4001;

    private FakeNWScript _engine = null!;
    private FakeChatPlugin _chat = null!;
    private FakePlayerPlugin _playerPlugin = null!;
    private InMemoryDBService _db = null!;
    private RecordingParty _party = null!;
    private TestableService _service = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _chatScope = null!;
    private IDisposable _playerScope = null!;
    private FacadeContainer _container = null!;
    private IDisposable _containerScope = null!;

    private readonly List<uint> _online = new();
    private int _cursor;
    private readonly HashSet<uint> _dms = new();
    private readonly HashSet<uint> _possessed = new();
    private readonly Dictionary<uint, uint> _masters = new();
    private readonly Dictionary<uint, uint> _factionLeaders = new();
    private readonly HashSet<uint> _inCall = new();

    [SetUp]
    public void SetUp()
    {
        _online.Clear();
        _dms.Clear();
        _possessed.Clear();
        _masters.Clear();
        _factionLeaders.Clear();
        _inCall.Clear();

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetFirstPC", _ =>
        {
            _cursor = 0;
            return _online.Count > 0 ? _online[0] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetNextPC", _ =>
        {
            _cursor++;
            return _cursor < _online.Count ? _online[_cursor] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetIsDM", arguments => _dms.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetIsDMPossessed", arguments => _possessed.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetMaster", arguments => _masters.GetValueOrDefault((uint)arguments[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetName", arguments => $"Name{(uint)arguments[0]!}");
        _engine.SetReturnHandler("GetObjectUUID", arguments => $"uuid-{(uint)arguments[0]!}");
        _engine.SetReturnHandler("GetFactionLeader", arguments => _factionLeaders.GetValueOrDefault((uint)arguments[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetLocalBool", arguments => _inCall.Contains((uint)arguments[0]!));
        _chat = new FakeChatPlugin();
        _playerPlugin = new FakePlayerPlugin();
        _db = new InMemoryDBService();
        _party = new RecordingParty();
        _service = new TestableService(_db, _party);
        _engineScope = NWScriptRuntime.Override(_engine);
        _chatScope = ChatPluginRuntime.Override(_chat);
        _playerScope = PlayerPluginRuntime.Override(_playerPlugin);
        // The service calls the DMPresence facade, which resolves its instance from a container.
        _container = new FacadeContainer();
        _containerScope = _container.Install();
    }

    [TearDown]
    public void TearDown()
    {
        // A refresh the test queued sits in the shared scheduler; run it here, with the fake engine, so it never
        // reaches another test.
        Scheduler.Process();
        _containerScope.Dispose();
        _container.Dispose();
        _playerScope.Dispose();
        _chatScope.Dispose();
        _engineScope.Dispose();
    }

    private void Online(params uint[] players)
    {
        _online.Clear();
        _online.AddRange(players);
    }

    private void InSameParty(int factionId, uint leader, params uint[] members)
    {
        foreach (var member in members)
        {
            _service.PartyFactions[member] = factionId;
            _factionLeaders[member] = leader;
        }
    }

    private void Dm(params uint[] dms)
    {
        foreach (var dm in dms)
            _dms.Add(dm);
    }

    private static PlayerListSubject Subject(uint player, string name, uint leader = 0, int partyCount = 1, bool isDm = false, string descriptor = "Someone") =>
        new()
        {
            Player = player,
            CanonicalName = name,
            Descriptor = isDm ? PlayerListService.DMDescriptor : descriptor,
            IsDM = isDm,
            Flags = Array.Empty<PlayerListFlagType>(),
            PartyLeader = leader == 0 ? player : leader,
            PartyPlayerCount = partyCount,
        };

    private class TestableService : PlayerListService
    {
        public TestableService(IDBService db, IPartyService party)
            : base(db, party)
        {
        }

        public Dictionary<uint, int> PartyFactions { get; } = new();
        public HashSet<(uint Viewer, uint Target)> Hostile { get; } = new();

        protected override int? GetPartyFactionId(uint creature) =>
            PartyFactions.TryGetValue(creature, out var faction) ? faction : null;

        public override bool IsHostileTo(uint viewer, uint target) => Hostile.Contains((viewer, target));
    }

    private sealed class RecordingParty : IPartyService
    {
        public List<uint> Removed { get; } = new();
        public List<(uint Leader, uint Member)> Transfers { get; } = new();
        public bool AcceptTransfers { get; set; } = true;

        public void RemoveFromCurrentParty(uint player) => Removed.Add(player);

        public bool TransferLeadership(uint currentLeader, uint newLeader)
        {
            Transfers.Add((currentLeader, newLeader));
            return AcceptTransfers;
        }

        public void JoinParty() => throw new NotSupportedException();
        public void AssociateJoinParty() => throw new NotSupportedException();
        public void AssociateLeaveParty() => throw new NotSupportedException();
        public void LeaveParty() => throw new NotSupportedException();
        public void KickFromParty() => throw new NotSupportedException();
        public void TransferLeadership() => throw new NotSupportedException();
        public void LeaveServer() => throw new NotSupportedException();
        public List<uint> GetAllPartyMembers(uint creature) => throw new NotSupportedException();
        public List<uint> GetAllPartyMembersWithinRange(uint creature, float distance) => throw new NotSupportedException();
        public bool IsInParty(uint creature, uint toCheck) => throw new NotSupportedException();
    }

    // ---- flags

    [Test]
    public void The_flags_of_an_invalid_object_or_a_dm_are_empty()
    {
        Dm(DmAvatar);

        _service.GetFlags(ObjectInvalid).Should().BeEmpty();
        _service.GetFlags(DmAvatar).Should().BeEmpty();
    }

    [Test]
    public void The_flags_of_a_player_are_their_known_flags_in_flag_order()
    {
        _db.With(new Player("uuid-2001") { PlayerListFlags = new List<PlayerListFlagType> { PlayerListFlagType.Crafting, PlayerListFlagType.GeneralRP, (PlayerListFlagType)99 } });

        _service.GetFlags(First).Should().Equal(PlayerListFlagType.GeneralRP, PlayerListFlagType.Crafting);
    }

    [Test]
    public void A_player_with_no_record_or_no_flags_has_none()
    {
        _service.GetFlags(First).Should().BeEmpty();

        _db.With(new Player("uuid-2002"));
        _service.GetFlags(Second).Should().BeEmpty();
    }

    [Test]
    public void Turning_a_flag_on_saves_the_player_once_and_turning_it_off_saves_again()
    {
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! == First);
        var player = new Player("uuid-2001");
        _db.With(player);

        _service.SetFlag(First, PlayerListFlagType.PvP, true);

        player.PlayerListFlags.Should().Equal(PlayerListFlagType.PvP);
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(player);

        _service.SetFlag(First, PlayerListFlagType.PvP, false);

        player.PlayerListFlags.Should().BeEmpty();
        _db.Saved.Should().HaveCount(2);
    }

    [Test]
    public void Setting_a_flag_to_the_value_it_already_has_saves_nothing()
    {
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! == First);
        _db.With(new Player("uuid-2001") { PlayerListFlags = new List<PlayerListFlagType> { PlayerListFlagType.DM } });

        _service.SetFlag(First, PlayerListFlagType.DM, true);
        _service.SetFlag(First, PlayerListFlagType.PvP, false);

        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void A_player_record_without_a_flag_list_gets_one()
    {
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! == First);
        var player = new Player("uuid-2001") { PlayerListFlags = null! };
        _db.With(player);

        _service.SetFlag(First, PlayerListFlagType.Crafting, true);

        player.PlayerListFlags.Should().Equal(PlayerListFlagType.Crafting);
    }

    [Test]
    public void A_flag_is_not_set_for_a_non_player_a_dm_a_missing_record_or_an_unknown_flag()
    {
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! is First or Second or DmAvatar);
        Dm(DmAvatar);
        _db.With(new Player("uuid-2001"));
        _db.With(new Player("uuid-3001"));

        _service.SetFlag(Third, PlayerListFlagType.PvP, true);
        _service.SetFlag(DmAvatar, PlayerListFlagType.PvP, true);
        _service.SetFlag(Second, PlayerListFlagType.PvP, true);
        _service.SetFlag(First, (PlayerListFlagType)99, true);

        _db.Saved.Should().BeEmpty();
    }

    // ---- who is online

    [Test]
    public void Nobody_online_means_no_subjects()
    {
        _service.BuildSubjects().Should().BeEmpty();
    }

    [Test]
    public void Subjects_describe_each_player_their_party_and_the_dm()
    {
        Online(First, Second, DmAvatar, Third);
        Dm(DmAvatar);
        InSameParty(10, Second, First, Second);
        _db.With(new Player("uuid-2001") { PlayerListFlags = new List<PlayerListFlagType> { PlayerListFlagType.PvP } });

        var subjects = _service.BuildSubjects();

        subjects.Select(subject => subject.Player).Should().Equal(First, Second, DmAvatar, Third);
        var first = subjects[0];
        first.CanonicalName.Should().Be("Name2001");
        first.Descriptor.Should().Be("Someone");
        first.IsDM.Should().BeFalse();
        first.IsDisguised.Should().BeFalse();
        first.IsAfk.Should().BeFalse();
        first.Flags.Should().Equal(PlayerListFlagType.PvP);
        first.PartyLeader.Should().Be(Second);
        first.PartyPlayerCount.Should().Be(2);
        subjects[1].PartyLeader.Should().Be(Second);
        subjects[1].PartyPlayerCount.Should().Be(2);
        subjects[1].Flags.Should().BeEmpty();

        var dm = subjects[2];
        dm.IsDM.Should().BeTrue();
        dm.Descriptor.Should().Be("Dungeon Master");
        dm.Flags.Should().BeEmpty();
        dm.PartyLeader.Should().Be(DmAvatar);
        dm.PartyPlayerCount.Should().Be(1);

        subjects[3].PartyLeader.Should().Be(Third);
        subjects[3].PartyPlayerCount.Should().Be(1);
    }

    [Test]
    public void A_dm_possessing_a_creature_is_still_a_dm_subject()
    {
        Online(DmAvatar);
        _possessed.Add(DmAvatar);

        _service.BuildSubjects().Should().ContainSingle().Which.IsDM.Should().BeTrue();
    }

    [Test]
    public void A_party_is_counted_by_the_player_characters_in_it()
    {
        Online(First, Second, DmAvatar);
        Dm(DmAvatar);
        InSameParty(10, First, First, Second, DmAvatar);

        var subjects = _service.BuildSubjects();

        subjects[0].PartyPlayerCount.Should().Be(2);
        subjects[2].PartyPlayerCount.Should().Be(1);
    }

    [Test]
    public void A_party_is_led_by_the_first_member_when_the_engines_leader_is_not_in_it()
    {
        Online(First, Second);
        InSameParty(10, ObjectInvalid, First, Second);

        _service.GetParty(Second).Should().Be((First, 2));
    }

    [Test]
    public void A_character_outside_the_party_system_leads_a_party_of_one()
    {
        Online(First, Second);

        _service.GetParty(First).Should().Be((First, 1));
        _service.GetParty(Fourth).Should().Be((Fourth, 1));
    }

    [Test]
    public void A_party_is_the_leader_and_the_count()
    {
        Online(First, Second, Third);
        InSameParty(10, Second, First, Second, Third);

        _service.GetParty(First).Should().Be((Second, 3));
    }

    // ---- relations

    [Test]
    public void A_target_who_is_not_online_has_no_relation()
    {
        Online(First);

        var relation = _service.GetRelation(First, Second);

        relation.HasTarget.Should().BeFalse();
        relation.ViewerIsDM.Should().BeFalse();
    }

    [Test]
    public void A_dm_is_invisible_to_a_player_viewer()
    {
        Online(First, DmAvatar);
        Dm(DmAvatar);

        _service.GetRelation(First, DmAvatar).HasTarget.Should().BeFalse();
    }

    [Test]
    public void A_dm_viewer_sees_players_and_dms()
    {
        Online(DmAvatar, First, OtherDm);
        Dm(DmAvatar, OtherDm);

        var player = _service.GetRelation(DmAvatar, First);
        var dm = _service.GetRelation(DmAvatar, OtherDm);

        player.HasTarget.Should().BeTrue();
        player.ViewerIsDM.Should().BeTrue();
        player.TargetIsDM.Should().BeFalse();
        player.ViewerIsInParty.Should().BeFalse();
        dm.TargetIsDM.Should().BeTrue();
        dm.TargetIsInOtherParty.Should().BeFalse();
    }

    [Test]
    public void A_viewer_and_target_in_the_same_party_are_related_through_it()
    {
        Online(First, Second);
        InSameParty(10, First, First, Second);

        var relation = _service.GetRelation(First, Second);

        relation.HasTarget.Should().BeTrue();
        relation.TargetIsSelf.Should().BeFalse();
        relation.ViewerIsInParty.Should().BeTrue();
        relation.ViewerIsPartyLeader.Should().BeTrue();
        relation.TargetIsInViewerParty.Should().BeTrue();
        relation.TargetIsPartyLeader.Should().BeFalse();
        relation.TargetIsInOtherParty.Should().BeFalse();
    }

    [Test]
    public void A_target_in_another_party_is_grouped_and_a_leader_is_marked()
    {
        Online(First, Second, Third, Fourth);
        InSameParty(10, First, First, Second);
        InSameParty(11, Third, Third, Fourth);

        var relation = _service.GetRelation(First, Third);

        relation.TargetIsInOtherParty.Should().BeTrue();
        relation.TargetIsInViewerParty.Should().BeFalse();
        relation.TargetIsPartyLeader.Should().BeTrue();
        relation.ViewerIsPartyLeader.Should().BeTrue();
    }

    [Test]
    public void A_viewers_relation_to_themselves_is_self()
    {
        Online(First);

        var relation = _service.GetRelation(First, First);

        relation.TargetIsSelf.Should().BeTrue();
        relation.ViewerDislikesTarget.Should().BeFalse();
    }

    [Test]
    public void A_viewer_who_marked_the_target_hostile_dislikes_them()
    {
        Online(First, Second);
        _service.Hostile.Add((First, Second));

        _service.GetRelation(First, Second).ViewerDislikesTarget.Should().BeTrue();
        _service.GetRelation(Second, First).ViewerDislikesTarget.Should().BeFalse();
    }

    [Test]
    public void A_dm_viewer_never_dislikes_a_target()
    {
        Online(DmAvatar, First);
        Dm(DmAvatar);
        _service.Hostile.Add((DmAvatar, First));

        _service.GetRelation(DmAvatar, First).ViewerDislikesTarget.Should().BeFalse();
    }

    [Test]
    public void A_target_in_a_holocom_call_is_marked_so()
    {
        Online(First, Second);
        _inCall.Add(Second);

        var relation = _service.GetRelation(First, Second);

        relation.TargetIsInCall.Should().BeTrue();
        relation.ViewerCallState.Should().Be(PlayerListCallState.None);
    }

    [Test]
    public void A_viewer_in_a_holocom_call_is_connected()
    {
        Online(First, Second);
        _inCall.Add(First);

        _service.GetRelation(First, Second).ViewerCallState.Should().Be(PlayerListCallState.Connected);
    }

    [Test]
    public void A_possessing_dms_creature_and_avatar_are_the_same_viewer()
    {
        Online(DmAvatar, First);
        Dm(DmAvatar);
        _possessed.Add(PossessedCreature);
        _masters[PossessedCreature] = DmAvatar;

        var relation = _service.GetRelation(PossessedCreature, First);

        relation.ViewerIsDM.Should().BeTrue();
        relation.HasTarget.Should().BeTrue();
    }

    // ---- rows for a viewer

    [Test]
    public void A_dm_viewer_gets_every_row_with_canonical_names_and_their_own_first()
    {
        Dm(DmAvatar, OtherDm);
        var subjects = new List<PlayerListSubject>
        {
            Subject(Third, "Zed"),
            Subject(OtherDm, "Dm Two", isDm: true),
            Subject(First, "alpha"),
            Subject(DmAvatar, "Dm One", isDm: true),
            Subject(Second, "Bravo"),
        };

        var entries = _service.BuildEntries(DmAvatar, subjects);

        entries.Select(entry => entry.Player).Should().Equal(DmAvatar, First, Second, Third, OtherDm);
        entries.Select(entry => entry.Name).Should().Equal("Dm One", "alpha", "Bravo", "Zed", "Dm Two");
        entries.Should().OnlyContain(entry => !string.IsNullOrEmpty(entry.Descriptor));
        entries[0].Relation.TargetIsSelf.Should().BeTrue();
        entries[0].Status.Should().Be(PlayerListStatus.DM);
        entries[1].Status.Should().Be(PlayerListStatus.None);
    }

    [Test]
    public void A_dm_viewer_sees_which_players_are_disguised()
    {
        Dm(DmAvatar);
        var disguised = Subject(First, "Real Name");
        disguised.IsDisguised = true;
        var subjects = new List<PlayerListSubject> { Subject(DmAvatar, "Dm One", isDm: true), disguised };

        _service.BuildEntries(DmAvatar, subjects).Single(entry => entry.Player == First).IsDisguised.Should().BeTrue();
    }

    [Test]
    public void A_player_viewer_never_gets_a_row_for_a_dm()
    {
        Dm(DmAvatar);
        var subjects = new List<PlayerListSubject>
        {
            Subject(DmAvatar, "Dm One", isDm: true),
            Subject(First, "Real Name", descriptor: "Tall Stranger"),
        };

        var entries = _service.BuildEntries(First, subjects);

        var self = entries.Should().ContainSingle().Subject;
        self.Player.Should().Be(First);
        self.Name.Should().Be("Real Name");
        self.Relation.TargetIsSelf.Should().BeTrue();
        self.Status.Should().Be(PlayerListStatus.None);
        // This viewer has not asked to see their own descriptor.
        self.Descriptor.Should().BeEmpty();
    }

    [Test]
    public void A_possessing_dms_window_belongs_to_the_avatar()
    {
        Dm(DmAvatar);
        _possessed.Add(PossessedCreature);
        _masters[PossessedCreature] = DmAvatar;
        var subjects = new List<PlayerListSubject> { Subject(DmAvatar, "Dm One", isDm: true), Subject(First, "Alpha") };

        var entries = _service.BuildEntries(PossessedCreature, subjects);

        entries.Select(entry => entry.Player).Should().Equal(DmAvatar, First);
        entries[0].Relation.TargetIsSelf.Should().BeTrue();
    }

    // ---- hostility, tells and party controls

    [Test]
    public void Marking_hostile_dislikes_the_target_and_clearing_it_likes_them_again()
    {
        _service.SetHostile(First, Second, true);
        _service.SetHostile(First, Second, false);

        _engine.CallsTo("SetPCDislike").Should().ContainSingle().Which.Arguments.Should().Equal(First, Second);
        _engine.CallsTo("SetPCLike").Should().ContainSingle().Which.Arguments.Should().Equal(First, Second);
    }

    [Test]
    public void An_empty_tell_is_refused()
    {
        _service.SendTell(First, Second, "  \t ").Should().Be("Enter a message to send.");
        _service.SendTell(First, Second, null!).Should().Be("Enter a message to send.");

        _chat.CountOf("SendMessage").Should().Be(0);
    }

    [Test]
    public void A_tell_to_someone_who_is_not_online_is_refused()
    {
        Online(First);

        _service.SendTell(First, Second, "hello").Should().Be("That player is no longer online.");

        _chat.CountOf("SendMessage").Should().Be(0);
    }

    [Test]
    public void A_tell_to_a_hidden_dm_gets_the_answer_for_an_absent_player()
    {
        Online(First, DmAvatar);
        Dm(DmAvatar);

        _service.SendTell(First, DmAvatar, "hello").Should().Be("That player is no longer online.");
    }

    [Test]
    public void A_players_tell_is_sent_with_its_spaces_collapsed_and_the_direct_flag_set_around_it()
    {
        Online(First, Second);

        var error = _service.SendTell(First, Second, "  hello    there\tfriend ");

        error.Should().BeEmpty();
        var sent = _chat.CallsTo("SendMessage").Should().ContainSingle().Subject;
        sent.Arguments[0].Should().Be(ChatChannel.PlayerTell);
        sent.Arguments[1].Should().Be("hello there friend");
        sent.Arguments[2].Should().Be(First);
        sent.Arguments[3].Should().Be(Second);
        var order = _engine.Calls.Select(call => call.Member).ToList();
        order.IndexOf("SetLocalBool").Should().BeLessThan(order.IndexOf("DeleteLocalBool"));
        _engine.CallsTo("SetLocalBool").Should().ContainSingle().Which.Arguments.Should().Equal(First, "PLAYER_NAME_DIRECT_TELL", true);
        _engine.CallsTo("DeleteLocalBool").Should().ContainSingle().Which.Arguments.Should().Equal(First, "PLAYER_NAME_DIRECT_TELL");
    }

    [Test]
    public void The_direct_flag_is_cleared_even_when_the_chat_plugin_fails()
    {
        Online(First, Second);
        _chat.SetReturnHandler("SendMessage", _ => throw new InvalidOperationException("chat failed"));

        var send = () => _service.SendTell(First, Second, "hello");

        send.Should().Throw<InvalidOperationException>();
        _engine.CallsTo("DeleteLocalBool").Should().ContainSingle();
    }

    [Test]
    public void A_dms_tell_uses_the_dm_channel_and_speaks_as_the_avatar()
    {
        Online(DmAvatar, First);
        Dm(DmAvatar);

        var error = _service.SendTell(DmAvatar, First, "hello");

        error.Should().BeEmpty();
        var sent = _chat.CallsTo("SendMessage").Should().ContainSingle().Subject;
        sent.Arguments[0].Should().Be(ChatChannel.DMTell);
        sent.Arguments[2].Should().Be(DmAvatar);
        sent.Arguments[3].Should().Be(First);
    }

    [Test]
    public void Jumping_sends_the_dm_to_the_targets_location()
    {
        Online(DmAvatar, First);
        Dm(DmAvatar);

        _service.JumpTo(DmAvatar, First);

        _engine.CallsTo("GetLocation").Should().ContainSingle().Which.Arguments[0].Should().Be(First);
        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(DmAvatar);
    }

    [Test]
    public void Inviting_asks_the_player_plugin_to_send_the_invite()
    {
        _service.Invite(First, Second);

        _playerPlugin.CallsTo("SendPartyInvite").Should().ContainSingle().Which.Arguments.Take(2).Should().Equal(Second, First);
    }

    [Test]
    public void Removing_from_the_party_goes_through_the_party_service()
    {
        _service.RemoveFromParty(Second);

        _party.Removed.Should().Equal(Second);
    }

    [Test]
    public void Making_a_leader_returns_whether_the_engine_accepted_it()
    {
        _service.MakeLeader(First, Second).Should().BeTrue();
        _party.AcceptTransfers = false;
        _service.MakeLeader(First, Third).Should().BeFalse();

        _party.Transfers.Should().Equal((First, Second), (First, Third));
    }

    // ---- refreshing the open windows

    [Test]
    public void Requests_in_one_frame_refresh_once()
    {
        _service.QueueRefresh();
        _service.QueueRefresh();
        _service.RefreshOnEnter();
        _service.RefreshOnPartyChange();

        Scheduler.Process();

        _engine.CountOf("GetFirstPC").Should().Be(1);
    }

    [Test]
    public void A_refresh_can_be_queued_again_after_it_ran()
    {
        _service.QueueRefresh();
        Scheduler.Process();
        _engine.ClearCalls();

        _service.QueueRefresh();
        Scheduler.Process();

        _engine.CountOf("GetFirstPC").Should().Be(1);
    }

    [Test]
    public void Changing_a_flag_or_hostility_queues_a_refresh()
    {
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! == First);
        _db.With(new Player("uuid-2001"));

        _service.SetFlag(First, PlayerListFlagType.PvP, true);
        _service.SetHostile(First, Second, true);
        _service.RemoveFromParty(Second);
        _service.MakeLeader(First, Second);
        Scheduler.Process();

        _engine.CountOf("GetFirstPC").Should().Be(1);
    }

    [Test]
    public void A_player_who_is_leaving_is_off_the_roster_until_the_refresh_runs()
    {
        Online(First, Second);
        _engine.SetReturn("GetExitingObject", Second);

        _service.RefreshOnExit();

        _service.GetRelation(First, Second).HasTarget.Should().BeFalse();
        _service.BuildSubjects().Select(subject => subject.Player).Should().Equal(First);

        Scheduler.Process();

        _service.BuildSubjects().Select(subject => subject.Player).Should().Equal(First, Second);
    }

    [Test]
    public void A_refresh_reads_the_roster_once_for_each_open_window()
    {
        Online(First, Second);
        _engine.SetReturnHandler("NuiFindWindow", arguments => (uint)arguments[0]! == First ? 7 : 0);
        _service.QueueRefresh();

        Scheduler.Process();

        _engine.CallsTo("NuiFindWindow").Select(call => call.Arguments[0]).Should().Equal(First, Second);
        // A window is open for the first player, so the subjects are built once, for everyone online.
        _engine.CallsTo("GetName").Select(call => call.Arguments[0]).Should().Equal(First, Second);
    }

    [Test]
    public void A_refresh_skips_players_nobody_has_a_window_open_for()
    {
        Online(First, Second);
        _service.QueueRefresh();

        Scheduler.Process();

        _engine.CountOf("GetName").Should().Be(0);
    }

    [Test]
    public void A_refresh_reaches_the_windows_a_dm_has_open_on_the_creature_they_control()
    {
        Online(DmAvatar);
        Dm(DmAvatar);
        _engine.SetReturnHandler("NuiFindWindow", _ => 9);
        _service.QueueRefresh();

        Scheduler.Process();

        _engine.CallsTo("NuiFindWindow").Should().NotBeEmpty();
        _engine.CallsTo("GetName").Should().NotBeEmpty();
    }

    // ---- possession

    [Test]
    public void Possession_of_a_window_is_ignored_for_anyone_who_is_not_a_dm()
    {
        _engine.SetReturn("OBJECT_SELF", First);

        _service.MoveWindowOnPossession();

        _engine.CountOf("NuiFindWindow").Should().Be(0);
        _engine.CountOf("DelayCommand").Should().Be(0);
    }

    [Test]
    public void Possession_leaves_a_dm_without_an_open_list_alone()
    {
        Dm(DmAvatar);
        _engine.SetReturn("OBJECT_SELF", DmAvatar);
        _engine.SetReturn("NuiFindWindow", 0);

        _service.MoveWindowOnPossession();

        _engine.CountOf("DelayCommand").Should().Be(0);
    }

    [Test]
    public void Possession_reopens_an_open_list_on_the_next_tick()
    {
        _possessed.Add(PossessedCreature);
        _masters[PossessedCreature] = DmAvatar;
        _engine.SetReturn("OBJECT_SELF", PossessedCreature);
        _engine.SetReturn("NuiFindWindow", 5);

        _service.MoveWindowOnPossession();

        var delayed = _engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        delayed.Arguments[0].Should().Be(0.1f);

        // Run what was delayed: the list is closed and shown again for the avatar.
        _engine.ClearCalls();
        ((Action)delayed.Arguments[1]!).Invoke();

        _engine.CallsTo("NuiFindWindow").Should().NotBeEmpty();
        _engine.CallsTo("NuiDestroy").Should().NotBeEmpty();
    }
}
