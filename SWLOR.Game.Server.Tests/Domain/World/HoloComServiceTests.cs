using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Service.PlayerListService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the HoloCom instance: the engine (NWScript, the player and creature plugins) is replaced by recording
/// fakes with real local variables, and the player list is a recording proxy, so no server is needed. The call
/// participants are creatures the engine does not report as players, which keeps the player-only lookups (the space
/// check and the display names) off the database; those lookups are exercised in the engine tests.
/// </summary>
[TestFixture]
public class HoloComServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Alice = 1;
    private const uint Bob = 2;
    private const uint Carol = 3;
    private const uint Dm = 9;
    private const uint FirstHologram = 501;
    private const uint SecondHologram = 502;

    private const string Connected = "HOLOCOM_CALL_CONNECTED";
    private const string ConnectedWith = "HOLOCOM_CALL_CONNECTED_WITH";
    private const string SenderFlag = "HOLOCOM_CALL_SENDER";
    private const string ReceiverFlag = "HOLOCOM_CALL_RECEIVER";
    private const string SenderObject = "HOLOCOM_CALL_SENDER_OBJECT";
    private const string ReceiverObject = "HOLOCOM_CALL_RECEIVER_OBJECT";
    private const string Attempt = "HOLOCOM_CALL_ATTEMPT";
    private const string Token = "HOLOCOM_CALL_TOKEN";
    private const string HologramVariable = "HOLOCOM_HOLOGRAM";
    private const string HologramOwnerVariable = "HOLOGRAM_OWNER";
    private const string IsHologramVariable = "HOLOCOM_IS_HOLOGRAM";

    // A server's facades resolve from a container; the services this one reaches through their static facades
    // (the DM presence check, say) may be converted, so the fixture installs the server's registrations.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private FakePlayerPlugin _playerPlugin = null!;
    private FakeCreaturePlugin _creaturePlugin = null!;
    private RecordingProxy _playerListCalls = null!;
    private HoloComService _service = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _playerScope = null!;
    private IDisposable _creatureScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _facadeScope = _facades.Install();
        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! == Dm);
        _engine.SetReturnHandler("GetName", arguments => (uint)arguments[0]! switch
        {
            Alice => "Alice",
            Bob => "Bob",
            Carol => "Carol",
            Dm => "The DM",
            _ => "Somebody"
        });
        _playerPlugin = new FakePlayerPlugin();
        _creaturePlugin = new FakeCreaturePlugin();
        var playerList = (IPlayerListService)RecordingProxy.Create(typeof(IPlayerListService), out _playerListCalls);
        _service = new HoloComService(playerList);

        _engineScope = NWScriptRuntime.Override(_engine);
        _playerScope = PlayerPluginRuntime.Override(_playerPlugin);
        _creatureScope = CreaturePluginRuntime.Override(_creaturePlugin);
    }

    [TearDown]
    public void TearDown()
    {
        _creatureScope.Dispose();
        _playerScope.Dispose();
        _engineScope.Dispose();
        _facadeScope.Dispose();
    }

    private List<string> MessagesTo(uint player) =>
        _engine.CallsTo("SendMessageToPC")
            .Where(call => (uint)call.Arguments[0]! == player)
            .Select(call => (string)call.Arguments[1]!)
            .ToList();

    private int PlayerListRefreshes => _playerListCalls.Calls.Count(call => call.Method.Name == nameof(IPlayerListService.QueueRefresh));

    private Action LastDelayed() => (Action)_engine.CallsTo("DelayCommand").Last().Arguments[1]!;

    private void RunLastDelayed() => LastDelayed()();

    private void RunAssigned(uint subject)
    {
        foreach (var action in _engine.CallsTo("AssignCommand")
                     .Where(call => (uint)call.Arguments[0]! == subject)
                     .Select(call => (Action)call.Arguments[1]!)
                     .ToList())
        {
            action();
        }
    }

    /// <summary>Alice rings Bob and the first ring is delivered: Bob's HoloCom buzzes.</summary>
    private void AliceIsRingingBob()
    {
        _service.StartCall(Alice, Bob);
        RunLastDelayed();
    }

    /// <summary>Alice rings Bob and Bob answers: a connected call, with Bob's hologram first.</summary>
    private void AliceAndBobAreConnected()
    {
        AliceIsRingingBob();
        _engine.SetReturnSequence("CopyObject", FirstHologram, SecondHologram);
        _service.AnswerCall(Bob);
        _locals.ClearCalls();
    }

    // ---- state of a player

    [Test]
    public void A_player_with_no_variables_is_in_no_call()
    {
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
        _service.IsInCall(Alice).Should().BeFalse();
        _service.IsCallSender(Alice).Should().BeFalse();
        _service.IsCallReceiver(Alice).Should().BeFalse();
        _service.GetCallAttempt(Alice).Should().Be(0);
        _service.GetCallSender(Alice).Should().Be(ObjectInvalid);
        _service.GetCallReceiver(Alice).Should().Be(ObjectInvalid);
        _service.GetHoloGram(Alice).Should().Be(ObjectInvalid);
        _service.GetTargetForActiveCall(Alice).Should().Be(ObjectInvalid);
    }

    [Test]
    public void The_setters_write_the_variables_the_getters_read()
    {
        _service.SetIsCallSender(Alice);
        _service.SetIsCallReceiver(Bob);
        _service.SetCallSender(Bob, Alice);
        _service.SetCallReceiver(Alice, Bob);
        _service.SetCallAttempt(Alice, 4);

        _service.IsCallSender(Alice).Should().BeTrue();
        _service.IsCallReceiver(Bob).Should().BeTrue();
        _service.GetCallSender(Bob).Should().Be(Alice);
        _service.GetCallReceiver(Alice).Should().Be(Bob);
        _service.GetCallAttempt(Alice).Should().Be(4);
        _locals.Int(Alice, SenderFlag).Should().Be(1);
        _locals.Int(Bob, ReceiverFlag).Should().Be(1);
        _locals.Object(Bob, SenderObject).Should().Be(Alice);
        _locals.Object(Alice, ReceiverObject).Should().Be(Bob);
        _locals.Int(Alice, Attempt).Should().Be(4);
    }

    [Test]
    public void The_flags_can_be_switched_off_and_the_attempt_resets_by_default()
    {
        _service.SetIsCallSender(Alice);
        _service.SetIsCallReceiver(Alice);
        _service.SetCallAttempt(Alice, 9);

        _service.SetIsCallSender(Alice, false);
        _service.SetIsCallReceiver(Alice, false);
        _service.SetCallAttempt(Alice);

        _service.IsCallSender(Alice).Should().BeFalse();
        _service.IsCallReceiver(Alice).Should().BeFalse();
        _service.GetCallAttempt(Alice).Should().Be(0);
    }

    [Test]
    public void GetCallState_ranks_connected_above_incoming_above_outgoing()
    {
        _service.SetIsCallSender(Alice);
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.Outgoing);

        _service.SetIsCallReceiver(Alice);
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.Incoming);

        _engine.SetLocalBool(Alice, Connected, true);
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.Connected);
    }

    [Test]
    public void Holograms_are_recognized_by_their_marker_and_know_their_owner()
    {
        _service.IsHologram(FirstHologram).Should().BeFalse();
        _engine.SetLocalBool(FirstHologram, IsHologramVariable, true);
        _engine.SetLocalObject(FirstHologram, HologramOwnerVariable, Alice);

        _service.IsHologram(FirstHologram).Should().BeTrue();
        _service.GetHoloGramOwner(FirstHologram).Should().Be(Alice);
    }

    [Test]
    public void The_hologram_faction_is_the_neutral_faction_of_the_module()
    {
        HoloComService.HologramFactionId.Should().Be(6);
    }

    // ---- starting a call

    [Test]
    public void StartCall_marks_the_caller_as_ringing_and_delivers_the_first_ring_after_a_second()
    {
        _service.StartCall(Alice, Bob);

        _service.GetCallState(Alice).Should().Be(PlayerListCallState.Outgoing);
        _locals.Int(Alice, Token).Should().Be(1);
        PlayerListRefreshes.Should().Be(1);
        var delayed = _engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        delayed.Arguments[0].Should().Be(1.0f);
        _service.GetCallState(Bob).Should().Be(PlayerListCallState.None, "nobody rings until the delayed ring is delivered");
    }

    [Test]
    public void Every_attempt_gets_its_own_token()
    {
        _service.StartCall(Alice, Bob);
        _service.StartCall(Carol, Bob);

        _locals.Int(Alice, Token).Should().Be(1);
        _locals.Int(Carol, Token).Should().Be(2);
    }

    [Test]
    public void Each_instance_counts_its_own_tokens()
    {
        _service.StartCall(Alice, Bob);

        var other = new HoloComService((IPlayerListService)RecordingProxy.Create(typeof(IPlayerListService), out _));
        other.StartCall(Carol, Bob);

        _locals.Int(Carol, Token).Should().Be(1, "the token counter lives on the instance, not in a static field");
    }

    [Test]
    public void StartCall_is_refused_when_the_caller_is_a_DM()
    {
        _service.StartCall(Dm, Bob);

        _service.GetCallState(Dm).Should().Be(PlayerListCallState.None);
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
        PlayerListRefreshes.Should().Be(0);
    }

    [Test]
    public void StartCall_is_refused_when_the_receiver_is_a_DM()
    {
        _service.StartCall(Alice, Dm);

        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
    }

    [Test]
    public void StartCall_is_refused_when_the_caller_is_already_ringing_or_in_a_call()
    {
        _service.SetIsCallSender(Alice);

        _service.StartCall(Alice, Bob);

        _engine.CallsTo("DelayCommand").Should().BeEmpty();
    }

    [Test]
    public void StartCall_is_refused_when_the_receiver_is_busy()
    {
        _service.SetIsCallReceiver(Bob);

        _service.StartCall(Alice, Bob);

        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
    }

    // ---- ringing

    [Test]
    public void The_first_ring_tells_both_players_and_marks_the_receiver_as_being_rung()
    {
        AliceIsRingingBob();

        MessagesTo(Alice).Should().Equal("You wait for Bob to answer their HoloCom.");
        MessagesTo(Bob).Should().ContainSingle().Which.Should().StartWith("Your HoloCom buzzes as you are receiving a call.");
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.Outgoing);
        _service.GetCallState(Bob).Should().Be(PlayerListCallState.Incoming);
        _service.GetCallSender(Bob).Should().Be(Alice);
        _service.GetCallSender(Alice).Should().Be(Alice);
        _service.GetCallReceiver(Alice).Should().Be(Bob);
        _service.GetCallReceiver(Bob).Should().Be(Bob);
        _service.GetCallAttempt(Alice).Should().Be(1);
        PlayerListRefreshes.Should().Be(2, "once when the call starts and once when the receiver is rung");
    }

    [Test]
    public void The_first_ring_also_floats_the_buzz_over_the_receiver_and_rings_again_in_five_seconds()
    {
        AliceIsRingingBob();

        var floating = _engine.CallsTo("FloatingTextStringOnCreature").Should().ContainSingle().Subject;
        floating.Arguments[0].Should().Be(MessagesTo(Bob).Single());
        floating.Arguments[1].Should().Be(Bob);
        _engine.CallsTo("DelayCommand").Last().Arguments[0].Should().Be(5.0f);
    }

    [Test]
    public void One_ring_in_ten_adds_a_nudge_to_the_buzz()
    {
        _engine.SetReturn("Random", 1);

        AliceIsRingingBob();

        _engine.CallsTo("Random").Should().ContainSingle().Which.Arguments.Should().Equal(10);
        MessagesTo(Bob).Single().Should().Contain("Maybe you should answer it.");
    }

    [Test]
    public void The_other_rings_add_no_nudge()
    {
        _engine.SetReturn("Random", 0);

        AliceIsRingingBob();

        MessagesTo(Bob).Single().Should().NotContain("Maybe you should answer it.");
    }

    [Test]
    public void The_buzz_floats_over_the_receiver_only_on_every_fifth_attempt()
    {
        AliceIsRingingBob();
        for (var ring = 1; ring < 10; ring++)
            RunLastDelayed();

        // Attempts 0 and 5 float: ten rings were delivered in all (attempt 0 through 9).
        _engine.CallsTo("FloatingTextStringOnCreature").Should().HaveCount(2);
        MessagesTo(Bob).Should().HaveCount(10);
        _service.GetCallAttempt(Alice).Should().Be(10);
    }

    [Test]
    public void After_sixteen_rings_the_call_goes_unanswered_and_everything_is_cleaned_up()
    {
        AliceIsRingingBob();
        for (var ring = 1; ring <= 16; ring++)
            RunLastDelayed();

        MessagesTo(Alice).Should().Contain("Your HoloCom call went unanswered.");
        MessagesTo(Bob).Last().Should().Be("Your HoloCom stops buzzing.");
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
        _service.GetCallState(Bob).Should().Be(PlayerListCallState.None);
        _locals.Has(Alice, Attempt).Should().BeFalse();
        _locals.Has(Alice, Token).Should().BeFalse();
        _engine.CallsTo("DelayCommand").Should().HaveCount(17, "the start, then one per delivery except the last");
    }

    [Test]
    public void A_ring_from_a_cancelled_attempt_does_nothing()
    {
        _service.StartCall(Alice, Bob);
        var staleRing = LastDelayed();
        _service.EndCall(Alice);
        _locals.ClearCalls();

        staleRing();

        _engine.Calls.Should().NotContain(call => call.Member == "SendMessageToPC");
        _service.GetCallState(Bob).Should().Be(PlayerListCallState.None);
    }

    [Test]
    public void A_ring_for_a_caller_who_left_does_nothing()
    {
        _service.StartCall(Alice, Bob);
        var ring = LastDelayed();
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid && (uint)arguments[0]! != Alice);

        ring();

        MessagesTo(Bob).Should().BeEmpty();
    }

    [Test]
    public void A_ring_for_a_receiver_who_left_tells_the_caller_it_went_unanswered()
    {
        _service.StartCall(Alice, Bob);
        var ring = LastDelayed();
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid && (uint)arguments[0]! != Bob);

        ring();

        MessagesTo(Alice).Should().Equal("Your HoloCom call went unanswered.");
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
    }

    [Test]
    public void A_ring_for_a_caller_who_is_no_longer_ringing_does_nothing()
    {
        _service.StartCall(Alice, Bob);
        var ring = LastDelayed();
        _service.SetIsCallSender(Alice, false);

        ring();

        MessagesTo(Bob).Should().BeEmpty();
        MessagesTo(Alice).Should().BeEmpty();
    }

    [Test]
    public void A_ring_for_a_caller_who_is_already_in_a_call_does_nothing()
    {
        _service.StartCall(Alice, Bob);
        var ring = LastDelayed();
        _engine.SetLocalBool(Alice, Connected, true);

        ring();

        MessagesTo(Bob).Should().BeEmpty();
    }

    [Test]
    public void A_ring_to_a_receiver_on_another_line_is_a_busy_signal()
    {
        _service.StartCall(Alice, Bob);
        var ring = LastDelayed();
        _service.SetIsCallSender(Bob);

        ring();

        MessagesTo(Alice).Should().Equal("Their HoloCom line is busy.");
        MessagesTo(Bob).Should().BeEmpty();
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
        _service.GetCallState(Bob).Should().Be(PlayerListCallState.Outgoing, "the receiver's own call is not disturbed");
    }

    [Test]
    public void A_ring_to_a_receiver_who_is_being_rung_by_someone_else_is_a_busy_signal()
    {
        _service.StartCall(Alice, Bob);
        var ring = LastDelayed();
        _service.SetIsCallReceiver(Bob);
        _service.SetCallSender(Bob, Carol);

        ring();

        MessagesTo(Alice).Should().Equal("Their HoloCom line is busy.");
        _service.GetCallState(Bob).Should().Be(PlayerListCallState.Incoming);
        _service.GetCallSender(Bob).Should().Be(Carol);
    }

    [Test]
    public void A_ring_that_finds_a_DM_on_the_line_ends_the_attempt_silently()
    {
        _service.StartCall(Alice, Bob);
        var ring = LastDelayed();
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! is Dm or Bob);

        ring();

        MessagesTo(Alice).Should().BeEmpty();
        MessagesTo(Bob).Should().BeEmpty();
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
    }

    // ---- answering and declining

    [Test]
    public void AnswerCall_connects_the_two_players_and_tells_both()
    {
        AliceIsRingingBob();
        _engine.SetReturnSequence("CopyObject", FirstHologram, SecondHologram);
        _locals.ClearCalls();

        _service.AnswerCall(Bob);

        _service.IsInCall(Alice).Should().BeTrue();
        _service.IsInCall(Bob).Should().BeTrue();
        _service.GetTargetForActiveCall(Bob).Should().Be(Alice);
        _service.GetTargetForActiveCall(Alice).Should().Be(Bob);
        const string connected = "Call Connected. (Use End Call in the Player List or the chat command /endcall to terminate the call)";
        MessagesTo(Bob).Should().Equal(connected);
        MessagesTo(Alice).Should().Equal(connected);
    }

    [Test]
    public void AnswerCall_gives_each_player_the_hologram_of_the_other_end_and_makes_it_harmless()
    {
        AliceIsRingingBob();
        _engine.SetReturnSequence("CopyObject", FirstHologram, SecondHologram);

        _service.AnswerCall(Bob);

        _service.GetHoloGram(Bob).Should().Be(FirstHologram);
        _service.GetHoloGram(Alice).Should().Be(SecondHologram);
        _service.GetHoloGramOwner(FirstHologram).Should().Be(Bob);
        _service.GetHoloGramOwner(SecondHologram).Should().Be(Alice);
        _service.IsHologram(FirstHologram).Should().BeTrue();
        _service.IsHologram(SecondHologram).Should().BeTrue();
        _engine.CallsTo("CopyObject").Select(call => call.Arguments[0]).Should().Equal(new object[] { Bob, Alice });
        _engine.CallsTo("SetName").Select(call => (call.Arguments[0], call.Arguments[1])).Should().Equal(
            ((object)FirstHologram, (object)"HoloCom Hologram"),
            ((object)SecondHologram, (object)"HoloCom Hologram"));
        _engine.CallsTo("SetPlotFlag").Select(call => call.Arguments[0]).Should().BeEquivalentTo(new object[] { FirstHologram, SecondHologram });
        _creaturePlugin.CallsTo("SetFaction").Select(call => (call.Arguments[0], call.Arguments[1])).Should().BeEquivalentTo(new[]
        {
            ((object?)FirstHologram, (object?)HoloComService.HologramFactionId),
            ((object?)SecondHologram, (object?)HoloComService.HologramFactionId)
        });
    }

    [Test]
    public void AnswerCall_immobilizes_both_players_while_the_call_lasts()
    {
        AliceIsRingingBob();
        _locals.ClearCalls();

        _service.AnswerCall(Bob);

        var permanent = _engine.CallsTo("ApplyEffectToObject")
            .Where(call => (DurationType)call.Arguments[0]! == DurationType.Permanent)
            .Select(call => call.Arguments[2])
            .ToList();
        permanent.Should().Contain(new object?[] { Alice, Bob }, "the tagged immobilize goes on both players");
        permanent.Should().HaveCount(4, "two immobilizes and the two hologram visual effects");
    }

    [Test]
    public void AnswerCall_plays_the_hologram_sound_for_both_players()
    {
        AliceIsRingingBob();
        _locals.ClearCalls();

        _service.AnswerCall(Bob);
        RunAssigned(Alice);
        RunAssigned(Bob);

        _engine.CallsTo("PlaySound").Select(call => call.Arguments[0]).Should().Equal(new object[] { "hologram_on", "hologram_on" });
    }

    [Test]
    public void AnswerCall_ignores_a_player_who_is_not_being_rung()
    {
        _service.AnswerCall(Bob);

        _service.IsInCall(Bob).Should().BeFalse();
        MessagesTo(Bob).Should().BeEmpty();
    }

    [Test]
    public void AnswerCall_ignores_a_player_who_is_already_in_a_call()
    {
        AliceAndBobAreConnected();

        _service.AnswerCall(Bob);

        _engine.Calls.Should().NotContain(call => call.Member == "CopyObject");
    }

    [Test]
    public void AnswerCall_is_refused_for_a_DM()
    {
        _service.SetIsCallReceiver(Dm);

        _service.AnswerCall(Dm);

        _service.IsInCall(Dm).Should().BeFalse();
    }

    [Test]
    public void AnswerCall_tells_both_players_when_the_caller_is_gone()
    {
        AliceIsRingingBob();
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid && (uint)arguments[0]! != Alice);
        _locals.ClearCalls();

        _service.AnswerCall(Bob);

        MessagesTo(Bob).Should().Equal("Your HoloCom stops buzzing.");
        _service.IsInCall(Bob).Should().BeFalse();
        _service.GetCallState(Bob).Should().Be(PlayerListCallState.None);
    }

    [Test]
    public void AnswerCall_tells_both_players_when_the_caller_is_now_a_DM()
    {
        AliceIsRingingBob();
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! == Alice);
        _locals.ClearCalls();

        _service.AnswerCall(Bob);

        MessagesTo(Alice).Should().Equal("Your HoloCom call went unanswered.");
        MessagesTo(Bob).Should().Equal("Your HoloCom stops buzzing.");
        _service.IsInCall(Bob).Should().BeFalse();
    }

    [Test]
    public void DeclineCall_tells_the_caller_and_ends_the_attempt()
    {
        AliceIsRingingBob();
        _locals.ClearCalls();

        _service.DeclineCall(Bob);

        MessagesTo(Alice).Should().Equal("Your HoloCom call was declined.");
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
        _service.GetCallState(Bob).Should().Be(PlayerListCallState.None);
        PlayerListRefreshes.Should().BeGreaterThan(0);
    }

    [Test]
    public void DeclineCall_ignores_a_player_who_is_not_being_rung_or_is_in_a_call()
    {
        _service.DeclineCall(Bob);
        AliceAndBobAreConnected();
        _service.DeclineCall(Bob);

        _engine.Calls.Should().NotContain(call => call.Member == "SendMessageToPC");
        _service.IsInCall(Bob).Should().BeTrue();
    }

    [Test]
    public void DeclineCall_is_refused_for_a_DM()
    {
        _service.SetIsCallReceiver(Dm);
        _service.SetCallSender(Dm, Alice);

        _service.DeclineCall(Dm);

        MessagesTo(Alice).Should().BeEmpty();
    }

    // ---- ending a call

    [Test]
    public void EndCall_hangs_up_a_connected_call_for_both_players()
    {
        AliceAndBobAreConnected();

        _service.EndCall(Bob).Should().BeTrue();

        MessagesTo(Bob).Should().Equal("You end your HoloCom call.");
        _service.IsInCall(Alice).Should().BeFalse();
        _service.IsInCall(Bob).Should().BeFalse();
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
    }

    [Test]
    public void Hanging_up_removes_the_immobilize_destroys_the_holograms_and_forgets_every_variable()
    {
        AliceAndBobAreConnected();

        _service.EndCall(Alice);
        RunAssigned(Alice);
        RunAssigned(Bob);

        _engine.CallsTo("RemoveEffectByTag").Select(call => call.Arguments[0]).Should().Equal(new object[] { Alice, Bob });
        _engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().BeEquivalentTo(new object[] { FirstHologram, SecondHologram });
        _engine.CallsTo("PlaySound").Select(call => call.Arguments[0]).Should().Equal(new object[] { "hologram_off", "hologram_off" });
        foreach (var player in new[] { Alice, Bob })
        {
            foreach (var variable in new[] { Connected, ConnectedWith, SenderFlag, ReceiverFlag, SenderObject, ReceiverObject, Attempt, Token, HologramVariable })
                _locals.Has(player, variable).Should().BeFalse($"{variable} is deleted for player {player}");
        }
    }

    [Test]
    public void EndCall_cancels_an_outgoing_ring_and_quiets_the_receivers_phone()
    {
        AliceIsRingingBob();
        _locals.ClearCalls();

        _service.EndCall(Alice).Should().BeTrue();

        MessagesTo(Bob).Should().Equal("Your HoloCom stops buzzing.");
        MessagesTo(Alice).Should().Equal("You cancel your HoloCom call.");
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
        _service.GetCallState(Bob).Should().Be(PlayerListCallState.None);
    }

    [Test]
    public void EndCall_cancels_a_ring_whose_receiver_has_not_been_reached_yet()
    {
        _service.StartCall(Alice, Bob);
        _locals.ClearCalls();

        _service.EndCall(Alice).Should().BeTrue();

        MessagesTo(Alice).Should().Equal("You cancel your HoloCom call.");
        MessagesTo(Bob).Should().BeEmpty();
    }

    [Test]
    public void EndCall_returns_false_when_there_is_nothing_to_end()
    {
        _service.EndCall(Alice).Should().BeFalse();
        MessagesTo(Alice).Should().BeEmpty();
    }

    [Test]
    public void SetIsInCall_false_for_a_call_that_never_connected_still_cleans_both_players()
    {
        AliceIsRingingBob();

        _service.SetIsInCall(Alice, Bob, false);

        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
        _service.GetCallState(Bob).Should().Be(PlayerListCallState.None);
    }

    // ---- module events

    [Test]
    public void OnModuleDeath_ends_the_call_of_the_player_who_died()
    {
        AliceAndBobAreConnected();
        _engine.SetReturn("GetLastPlayerDied", Alice);

        _service.OnModuleDeath();

        _service.IsInCall(Alice).Should().BeFalse();
        _service.IsInCall(Bob).Should().BeFalse();
    }

    [Test]
    public void OnModuleDeath_leaves_a_player_who_was_not_in_a_call_alone()
    {
        _engine.SetReturn("GetLastPlayerDied", Alice);

        _service.OnModuleDeath();

        _engine.Calls.Should().NotContain(call => call.Member == "RemoveEffectByTag");
    }

    [Test]
    public void OnModuleEnter_removes_a_stale_immobilize_from_the_player_who_entered()
    {
        _engine.SetReturn("GetEnteringObject", Alice);

        _service.OnModuleEnter();

        var removal = _engine.CallsTo("RemoveEffectByTag").Should().ContainSingle().Subject;
        removal.Arguments[0].Should().Be(Alice);
        removal.Arguments[1].Should().BeEquivalentTo(new[] { "HOLOCOM_CALL_IMMOBILIZE" });
    }

    [Test]
    public void OnModuleLeave_hangs_up_a_connected_call()
    {
        AliceAndBobAreConnected();
        _engine.SetReturn("GetExitingObject", Alice);

        _service.OnModuleLeave();

        _service.IsInCall(Alice).Should().BeFalse();
        _service.IsInCall(Bob).Should().BeFalse();
    }

    [Test]
    public void CleanupAllHoloComState_for_a_caller_quiets_the_receiver_and_clears_both()
    {
        AliceIsRingingBob();
        _locals.ClearCalls();

        _service.CleanupAllHoloComState(Alice);

        MessagesTo(Bob).Should().Equal("Your HoloCom stops buzzing.");
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
        _service.GetCallState(Bob).Should().Be(PlayerListCallState.None);
        _locals.Has(Bob, SenderObject).Should().BeFalse();
        _locals.Has(Bob, Attempt).Should().BeFalse();
    }

    [Test]
    public void CleanupAllHoloComState_for_a_caller_whose_receiver_is_gone_clears_only_the_caller()
    {
        AliceIsRingingBob();
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid && (uint)arguments[0]! != Bob);
        _locals.ClearCalls();

        _service.CleanupAllHoloComState(Alice);

        MessagesTo(Bob).Should().BeEmpty();
        _service.IsCallSender(Alice).Should().BeFalse();
        _service.GetCallReceiver(Alice).Should().Be(ObjectInvalid);
    }

    [Test]
    public void CleanupAllHoloComState_for_a_receiver_tells_the_caller_it_went_unanswered()
    {
        AliceIsRingingBob();
        _locals.ClearCalls();

        _service.CleanupAllHoloComState(Bob);

        MessagesTo(Alice).Should().Equal("Your HoloCom call went unanswered.");
        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
        _service.GetCallState(Bob).Should().Be(PlayerListCallState.None);
    }

    [Test]
    public void CleanupAllHoloComState_for_a_receiver_whose_caller_is_gone_clears_only_the_receiver()
    {
        AliceIsRingingBob();
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid && (uint)arguments[0]! != Alice);
        _locals.ClearCalls();

        _service.CleanupAllHoloComState(Bob);

        MessagesTo(Alice).Should().BeEmpty();
        _service.IsCallReceiver(Bob).Should().BeFalse();
    }

    [Test]
    public void CleanupAllHoloComState_for_a_connected_player_ends_the_call_for_the_other_end_too()
    {
        AliceAndBobAreConnected();

        _service.CleanupAllHoloComState(Alice);

        _service.IsInCall(Alice).Should().BeFalse();
        _service.IsInCall(Bob).Should().BeFalse();
    }

    [Test]
    public void CleanupAllHoloComState_for_a_connected_player_whose_partner_is_gone_clears_only_that_player()
    {
        AliceAndBobAreConnected();
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid && (uint)arguments[0]! != Bob);

        _service.CleanupAllHoloComState(Alice);

        _service.IsInCall(Alice).Should().BeFalse();
        _locals.Has(Alice, ConnectedWith).Should().BeFalse();
        _locals.Has(Alice, HologramVariable).Should().BeFalse();
        _service.IsInCall(Bob).Should().BeTrue("the other end is only cleaned when it is still in the module");
    }

    [Test]
    public void CleanupCallAttempt_clears_the_attempt_state_and_refreshes_the_player_list()
    {
        AliceIsRingingBob();
        var refreshes = PlayerListRefreshes;

        _service.CleanupCallAttempt(Alice, Bob);

        _service.GetCallState(Alice).Should().Be(PlayerListCallState.None);
        _service.GetCallState(Bob).Should().Be(PlayerListCallState.None);
        _locals.Has(Alice, Token).Should().BeFalse();
        PlayerListRefreshes.Should().Be(refreshes + 1);
    }

    [Test]
    public void CleanupCallAttempt_without_a_receiver_clears_only_the_caller()
    {
        _service.SetIsCallSender(Alice);
        _service.SetCallAttempt(Alice, 3);

        _service.CleanupCallAttempt(Alice, ObjectInvalid);

        _service.IsCallSender(Alice).Should().BeFalse();
        _service.GetCallAttempt(Alice).Should().Be(0);
    }

    // ---- hologram chat

    private void ChatSpeaker(uint speaker, string text, TalkVolume volume = TalkVolume.Talk)
    {
        _engine.SetReturn("GetPCChatSpeaker", speaker);
        _engine.SetReturn("GetPCChatVolume", volume);
        _engine.SetReturn("GetPCChatMessage", text);
    }

    [Test]
    public void OnModuleChat_makes_the_speakers_hologram_say_the_same_thing()
    {
        _engine.SetLocalObject(Alice, HologramVariable, FirstHologram);
        ChatSpeaker(Alice, "  Hello there.  ");

        _service.OnModuleChat();
        RunAssigned(FirstHologram);

        _engine.CallsTo("SetCommandable").Should().ContainSingle().Which.Arguments.Should().Equal(true, FirstHologram);
        _engine.CallsTo("ClearAllActions").Should().HaveCount(1);
        _engine.CallsTo("ActionPlayAnimation").Should().ContainSingle().Which.Arguments[0].Should().Be(Animation.LoopingTalkNormal);
        _engine.CallsTo("ActionSpeakString").Should().ContainSingle().Which.Arguments.Should().Equal("Hello there.", TalkVolume.Talk);
    }

    [TestCase("Stop!", Animation.LoopingTalkForceful)]
    [TestCase("Really?", Animation.LoopingTalkPleading)]
    [TestCase("Really?!", Animation.LoopingTalkPleading)]
    [TestCase("Fine.", Animation.LoopingTalkNormal)]
    public void The_hologram_gestures_to_match_the_punctuation(string text, Animation expected)
    {
        _engine.SetLocalObject(Alice, HologramVariable, FirstHologram);
        ChatSpeaker(Alice, text);

        _service.OnModuleChat();
        RunAssigned(FirstHologram);

        _engine.CallsTo("ActionPlayAnimation").Should().ContainSingle().Which.Arguments[0].Should().Be(expected);
    }

    [Test]
    public void The_hologram_whispers_when_its_speaker_whispers()
    {
        _engine.SetLocalObject(Alice, HologramVariable, FirstHologram);
        ChatSpeaker(Alice, "psst", TalkVolume.Whisper);

        _service.OnModuleChat();
        RunAssigned(FirstHologram);

        _engine.CallsTo("ActionSpeakString").Should().ContainSingle().Which.Arguments.Should().Equal("psst", TalkVolume.Whisper);
    }

    [TestCase(TalkVolume.Shout)]
    [TestCase(TalkVolume.Tell)]
    [TestCase(TalkVolume.SilentShout)]
    [TestCase(TalkVolume.SilentTalk)]
    public void OnModuleChat_ignores_the_channels_that_are_not_spoken_aloud(TalkVolume volume)
    {
        _engine.SetLocalObject(Alice, HologramVariable, FirstHologram);
        ChatSpeaker(Alice, "hello", volume);

        _service.OnModuleChat();

        _engine.Calls.Should().NotContain(call => call.Member == "SetCommandable" || call.Member == "AssignCommand");
    }

    [Test]
    public void OnModuleChat_ignores_a_speaker_with_no_hologram()
    {
        ChatSpeaker(Alice, "hello");

        _service.OnModuleChat();

        _engine.Calls.Should().NotContain(call => call.Member == "SetCommandable" || call.Member == "AssignCommand");
    }

    [Test]
    public void OnModuleChat_ignores_chat_commands()
    {
        _engine.SetLocalObject(Alice, HologramVariable, FirstHologram);
        ChatSpeaker(Alice, "/endcall");

        _service.OnModuleChat();

        _engine.Calls.Should().NotContain(call => call.Member == "SetCommandable" || call.Member == "AssignCommand");
    }
}
