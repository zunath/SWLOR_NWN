using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Service.ChatPortraitTellService;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// Unit tests of the portrait-tell instance. The engine's list of connected clients exists only inside a running server,
/// so a subclass answers the two lookups from a table; everything else (the guard, the character ids, the handlers and
/// the messages) is the production code.
/// </summary>
[TestFixture]
public class ChatPortraitTellsServiceTests
{
    // Login objects and the slots (player IDs) the clients use.
    private const uint Observer = 1001;
    private const uint FirstSpeaker = 1002;
    private const uint SecondSpeaker = 1003;
    private const uint ObserverSlot = 1;
    private const uint SharedSlot = 5;
    private const uint OtherSlot = 6;

    private FakeNWScript _engine = null!;
    private TestableService _service = null!;
    private IDisposable _engineScope = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetObjectUUID", arguments => $"uuid-{(uint)arguments[0]!}");
        _engine.SetReturn("GetIsPC", true);
        _service = new TestableService();
        _engineScope = NWScriptRuntime.Override(_engine);

        _service.Connect(Observer, ObserverSlot);
        Enter(Observer);
    }

    [TearDown]
    public void TearDown()
    {
        _engineScope.Dispose();
    }

    private void Enter(uint loginObject)
    {
        _engine.SetReturn("GetEnteringObject", loginObject);
        _service.RememberEnteringPlayer();
    }

    private void Exit(uint loginObject)
    {
        _engine.SetReturn("GetExitingObject", loginObject);
        _service.ForgetExitingPlayer();
        _service.Disconnect(loginObject);
    }

    private void Speaks(uint speaker)
    {
        _service.RecordChatLine(ObserverSlot, speaker);
    }

    private void ClicksPortrait(uint holder)
    {
        _engine.SetReturn("GetLastGuiEventType", GuiEventType.ChatLogPortraitClick);
        _engine.SetReturn("GetLastGuiEventPlayer", Observer);
        _engine.SetReturn("GetLastGuiEventObject", holder);
        _service.CheckPortraitClick();
    }

    private IReadOnlyList<string> Messages() =>
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    private class TestableService : ChatPortraitTellsService
    {
        private readonly Dictionary<uint, ChatPortraitTellsClient> _byObject = new();
        private readonly Dictionary<uint, ChatPortraitTellsClient> _bySlot = new();

        public void Connect(uint loginObject, uint slot)
        {
            var client = new ChatPortraitTellsClient(slot, loginObject);
            _byObject[loginObject] = client;
            _bySlot[slot] = client;
        }

        public void Disconnect(uint loginObject)
        {
            if (_byObject.Remove(loginObject, out var client))
                _bySlot.Remove(client.PlayerId);
        }

        protected override ChatPortraitTellsClient? FindClientByObjectId(uint objectId) =>
            _byObject.TryGetValue(objectId, out var client) ? client : null;

        protected override ChatPortraitTellsClient? FindClientByPlayerId(uint playerId) =>
            _bySlot.TryGetValue(playerId, out var client) ? client : null;
    }

    // ---- recording lines

    [Test]
    public void A_line_from_another_player_is_counted_for_the_receiving_client_only()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);

        Speaks(FirstSpeaker);
        Speaks(FirstSpeaker);

        _service.CountLinesReceived(Observer).Should().Be(2);
        _service.CountLinesReceived(FirstSpeaker).Should().Be(0);
    }

    [Test]
    public void A_speakers_own_line_is_not_recorded_for_them()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);

        _service.RecordChatLine(SharedSlot, FirstSpeaker);

        _service.CountLinesReceived(FirstSpeaker).Should().Be(0);
    }

    [Test]
    public void A_line_from_an_unknown_speaker_or_to_an_unknown_slot_is_ignored()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);

        _service.RecordChatLine(ObserverSlot, 4040);
        _service.RecordChatLine(777, FirstSpeaker);

        _service.CountLinesReceived(Observer).Should().Be(0);
    }

    [Test]
    public void The_line_count_of_an_unknown_client_is_zero()
    {
        _service.CountLinesReceived(4040).Should().Be(0);
    }

    // ---- classifying portrait clicks

    [Test]
    public void A_portrait_of_the_player_who_spoke_is_the_current_holder()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);

        _service.ClickPortrait(Observer, FirstSpeaker).Should().Be(ChatPortraitClick.CurrentHolder);
    }

    [Test]
    public void A_portrait_with_no_recorded_line_is_unrecorded()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);

        _service.ClickPortrait(Observer, FirstSpeaker).Should().Be(ChatPortraitClick.Unrecorded);
    }

    [Test]
    public void A_click_by_or_on_a_client_the_server_does_not_know_is_unrecorded()
    {
        _service.ClickPortrait(4040, Observer).Should().Be(ChatPortraitClick.Unrecorded);
        _service.ClickPortrait(Observer, 4040).Should().Be(ChatPortraitClick.Unrecorded);
    }

    [Test]
    public void A_slot_taken_by_a_new_player_after_the_speaker_left_is_a_former_holder()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);
        Exit(FirstSpeaker);
        _service.Connect(SecondSpeaker, SharedSlot);
        Enter(SecondSpeaker);

        _service.ClickPortrait(Observer, SecondSpeaker).Should().Be(ChatPortraitClick.FormerHolder);
    }

    [Test]
    public void A_slot_whose_old_and_new_holder_both_spoke_is_ambiguous()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);
        Exit(FirstSpeaker);
        _service.Connect(SecondSpeaker, SharedSlot);
        Enter(SecondSpeaker);
        Speaks(SecondSpeaker);

        _service.ClickPortrait(Observer, SecondSpeaker).Should().Be(ChatPortraitClick.Ambiguous);
    }

    [Test]
    public void A_character_that_logs_back_in_is_still_the_same_speaker()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);

        // Same character, same login object, a new connection: the character ID is the account-independent UUID.
        _service.Disconnect(FirstSpeaker);
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);

        _service.ClickPortrait(Observer, FirstSpeaker).Should().Be(ChatPortraitClick.CurrentHolder);
    }

    [Test]
    public void A_speaker_the_server_never_saw_enter_is_identified_by_their_object()
    {
        _engine.SetReturn("GetIsPC", false);
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);

        _service.ClickPortrait(Observer, FirstSpeaker).Should().Be(ChatPortraitClick.CurrentHolder);
    }

    // ---- the handlers

    [Test]
    public void A_non_player_entering_is_not_remembered()
    {
        _engine.SetReturn("GetIsPC", false);
        _service.Connect(FirstSpeaker, SharedSlot);
        Speaks(FirstSpeaker);
        _service.CountLinesReceived(Observer).Should().Be(1);

        Enter(Observer);

        // Entering would have forgotten the observer's history had the object been a player.
        _service.CountLinesReceived(Observer).Should().Be(1);
    }

    [Test]
    public void A_player_entering_forgets_what_the_login_objects_previous_client_saw()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Speaks(FirstSpeaker);
        _service.CountLinesReceived(Observer).Should().Be(1);

        Enter(Observer);

        _service.CountLinesReceived(Observer).Should().Be(0);
    }

    [Test]
    public void A_player_exiting_forgets_what_their_client_saw()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Speaks(FirstSpeaker);

        _engine.SetReturn("GetExitingObject", Observer);
        _service.ForgetExitingPlayer();

        _service.CountLinesReceived(Observer).Should().Be(0);
    }

    [Test]
    public void Clicking_the_portrait_of_a_former_holder_warns_the_player()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);
        Exit(FirstSpeaker);
        _service.Connect(SecondSpeaker, SharedSlot);
        Enter(SecondSpeaker);

        ClicksPortrait(SecondSpeaker);

        Messages().Should().ContainSingle().Which.Should().Contain("A tell started from it will not be sent");
        _engine.CallsTo("SendMessageToPC").Single().Arguments[0].Should().Be(Observer);
    }

    [Test]
    public void Clicking_an_ambiguous_portrait_warns_the_player()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);
        Exit(FirstSpeaker);
        _service.Connect(SecondSpeaker, SharedSlot);
        Enter(SecondSpeaker);
        Speaks(SecondSpeaker);

        ClicksPortrait(SecondSpeaker);

        Messages().Should().ContainSingle().Which.Should().Contain("may belong to a player who has logged out");
    }

    [Test]
    public void Clicking_the_portrait_of_the_current_holder_says_nothing()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);

        ClicksPortrait(FirstSpeaker);

        Messages().Should().BeEmpty();
    }

    [Test]
    public void Other_gui_events_are_ignored()
    {
        _engine.SetReturn("GetLastGuiEventType", GuiEventType.ChatBarFocus);

        _service.CheckPortraitClick();

        _engine.CountOf("GetLastGuiEventPlayer").Should().Be(0);
        Messages().Should().BeEmpty();
    }

    // ---- refusing the tell a stale click started

    [Test]
    public void A_direct_tell_from_the_player_list_is_never_refused()
    {
        _service.RefuseStalePortraitTell(Observer, FirstSpeaker, true).Should().BeFalse();
        Messages().Should().BeEmpty();
    }

    [Test]
    public void A_tell_from_an_unknown_sender_is_not_refused()
    {
        _service.RefuseStalePortraitTell(4040, FirstSpeaker, false).Should().BeFalse();
    }

    [Test]
    public void A_tell_with_no_portrait_click_before_it_is_not_refused()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);

        _service.RefuseStalePortraitTell(Observer, FirstSpeaker, false).Should().BeFalse();
    }

    [Test]
    public void A_tell_started_from_a_stale_portrait_is_refused_and_the_sender_is_told_why()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);
        Exit(FirstSpeaker);
        _service.Connect(SecondSpeaker, SharedSlot);
        Enter(SecondSpeaker);
        ClicksPortrait(SecondSpeaker);
        _engine.ClearCalls();

        _service.RefuseStalePortraitTell(Observer, SecondSpeaker, false).Should().BeTrue();

        Messages().Should().ContainSingle().Which.Should().Contain("Tell not sent");
        _engine.CallsTo("SendMessageToPC").Single().Arguments[0].Should().Be(Observer);
    }

    [Test]
    public void A_refused_click_refuses_only_one_tell()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);
        Exit(FirstSpeaker);
        _service.Connect(SecondSpeaker, SharedSlot);
        Enter(SecondSpeaker);
        ClicksPortrait(SecondSpeaker);

        _service.RefuseStalePortraitTell(Observer, SecondSpeaker, false).Should().BeTrue();
        _service.RefuseStalePortraitTell(Observer, SecondSpeaker, false).Should().BeFalse();
    }

    [Test]
    public void A_tell_to_a_different_player_than_the_clicked_slot_is_not_refused()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);
        Exit(FirstSpeaker);
        _service.Connect(SecondSpeaker, SharedSlot);
        Enter(SecondSpeaker);
        ClicksPortrait(SecondSpeaker);
        const uint third = 1004;
        _service.Connect(third, OtherSlot);
        Enter(third);

        _service.RefuseStalePortraitTell(Observer, third, false).Should().BeFalse();
    }

    [Test]
    public void A_tell_to_a_player_who_took_the_slot_after_a_good_click_is_refused()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);
        ClicksPortrait(FirstSpeaker);
        Exit(FirstSpeaker);
        _service.Connect(SecondSpeaker, SharedSlot);
        Enter(SecondSpeaker);

        _service.RefuseStalePortraitTell(Observer, SecondSpeaker, false).Should().BeTrue();
    }

    [Test]
    public void A_tell_to_a_good_click_target_who_is_still_there_is_not_refused()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);
        ClicksPortrait(FirstSpeaker);

        _service.RefuseStalePortraitTell(Observer, FirstSpeaker, false).Should().BeFalse();
        Messages().Should().BeEmpty();
    }

    [Test]
    public void A_tell_to_someone_who_has_since_left_is_checked_against_no_slot()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Enter(FirstSpeaker);
        Speaks(FirstSpeaker);
        Exit(FirstSpeaker);
        _service.Connect(SecondSpeaker, SharedSlot);
        Enter(SecondSpeaker);
        ClicksPortrait(SecondSpeaker);

        // The target is no longer connected, so it cannot match the clicked slot.
        _service.RefuseStalePortraitTell(Observer, 4040, false).Should().BeFalse();
    }

    [Test]
    public void Each_instance_keeps_its_own_history()
    {
        _service.Connect(FirstSpeaker, SharedSlot);
        Speaks(FirstSpeaker);

        new TestableService().CountLinesReceived(Observer).Should().Be(0);
    }
}
