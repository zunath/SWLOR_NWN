using System.Numerics;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Unit tests of the name override part: the overrides applied on entering and after a refresh, the AFK flag and its movement check, the
/// chat overrides and the tell handler. They run over the world of <see cref="PlayerNameTestBase"/>, with the part built on the player name
/// instance.
/// </summary>
[TestFixture]
public class PlayerNameOverrideServiceTests : PlayerNameTestBase
{
    // ---- name overrides

    [Test]
    public void Entering_as_a_player_applies_the_overrides_now_and_again_a_second_later()
    {
        _engine.SetReturnHandler("GetEnteringObject", _ => Alice);

        _service.Overrides.ApplyNameOverridesOnEnter();

        _service.EnsuredDescriptors.Should().Equal(Alice);
        Overrides().Should().NotBeEmpty();
        var delayed = _engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        delayed.Arguments[0].Should().Be(1.0f);

        _rename.ClearCalls();
        ((Action)delayed.Arguments[1]!)();

        Overrides().Should().NotBeEmpty();
    }

    [Test]
    public void Entering_as_staff_gives_the_true_names_of_everyone_and_repeats_it()
    {
        _engine.SetReturnHandler("GetEnteringObject", _ => Staff);

        _service.Overrides.ApplyNameOverridesOnEnter();

        var targets = Overrides().Select(call => (uint)call.Arguments[0]!).ToList();
        targets.Should().BeEquivalentTo(new[] { Alice, Bob, Carol });
        Overrides().Should().OnlyContain(call => (uint)call.Arguments[5]! == Staff);
        Overrides().Select(Name).Should().OnlyContain(name => name.Contains("[") && name.Contains("Tall Stranger"));
        var delayed = _engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        _rename.ClearCalls();
        ((Action)delayed.Arguments[1]!)();
        Overrides().Should().HaveCount(3);
    }

    [Test]
    public void A_reload_applies_the_overrides_of_everyone_online_again_whose_restoring_closures_it_dropped()
    {
        // Hot reload: a tell's chat-name override is put back by a closure the reload drops.
        _service.Overrides.OnCarriedStateAdopted();

        Overrides().Select(call => (uint)call.Arguments[5]!).Distinct().Should().Contain(new[] { Alice, Bob, Carol, Staff },
            "each player's view of every name, and the staff's true names, are derived again");
    }

    [Test]
    public void Entering_as_something_that_is_not_a_player_does_nothing()
    {
        _engine.SetReturnHandler("GetEnteringObject", _ => Npc);

        _service.Overrides.ApplyNameOverridesOnEnter();

        _service.EnsuredDescriptors.Should().BeEmpty();
        Overrides().Should().BeEmpty();
    }

    [Test]
    public void Refreshing_a_player_sets_their_own_two_names_and_those_every_other_player_sees()
    {
        _service.Overrides.RefreshNameOverridesForPlayer(Alice);

        _queueRefreshes.Should().Be(1);
        var forSelf = Overrides().Where(call => (uint)call.Arguments[0]! == Alice && (uint)call.Arguments[5]! == Alice).Single();
        Name(forSelf).Should().Be("Alice [" + Unknown("Tall Stranger") + "]");
        var forEveryone = Overrides().First(call => (uint)call.Arguments[0]! == Alice && (uint)call.Arguments[5]! == Nobody);
        Name(forEveryone).Should().Be("Tall Stranger");
        forEveryone.Arguments[2].Should().Be(ColorToken.TokenStart(127, 127, 127));
        forEveryone.Arguments[3].Should().Be(ColorToken.TokenEnd());
        Overrides().Should().Contain(call => (uint)call.Arguments[0]! == Bob && (uint)call.Arguments[5]! == Alice);
        Overrides().Should().Contain(call => (uint)call.Arguments[0]! == Alice && (uint)call.Arguments[5]! == Bob);
        Overrides().Should().Contain(call => (uint)call.Arguments[0]! == Alice && (uint)call.Arguments[5]! == Staff);
        _rename.CallsTo("ClearPCNameOverride").Should().ContainSingle("the account name is not scrambled");
    }

    [Test]
    public void A_player_who_hides_their_own_descriptor_sees_just_their_name()
    {
        _db.With(new Player("alice-id") { Settings = { ShowOwnDescriptor = false } });

        _service.Overrides.RefreshNameOverridesForPlayer(Alice);

        Name(Overrides().Single(call => (uint)call.Arguments[0]! == Alice && (uint)call.Arguments[5]! == Alice)).Should().Be("Alice");
    }

    [Test]
    public void A_scrambled_account_name_is_obfuscated_instead_of_cleared()
    {
        _service.ScrambledAccounts.Add(Alice);

        _service.Overrides.RefreshNameOverridesForPlayer(Alice);

        _rename.CallsTo("ClearPCNameOverride").Should().BeEmpty();
        Overrides().Should().Contain(call => (PlayerNameOverrideType)call.Arguments[4]! == PlayerNameOverrideType.Obfuscate);
    }

    [TestCase(Npc)]
    [TestCase(Staff)]
    [TestCase(Possessed)]
    [TestCase(Nobody)]
    public void Refreshing_something_that_is_not_a_plain_player_sets_nothing(uint player)
    {
        _service.Overrides.RefreshNameOverridesForPlayer(player);

        Overrides().Should().BeEmpty();
        _queueRefreshes.Should().Be(1);
    }

    [Test]
    public void Refreshing_an_observer_reapplies_every_other_players_name_and_forgets_the_cached_setting()
    {
        _db.With(new Player("alice-id"));
        _service.Names.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeTrue();
        _db.With(new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } });

        _service.Overrides.RefreshNameOverridesForObserver(Alice);

        Overrides().Select(call => (uint)call.Arguments[0]!).Should().BeEquivalentTo(new[] { Bob, Carol });
        _queueRefreshes.Should().Be(1);
        _service.Names.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeFalse();
    }

    [TestCase(Npc)]
    [TestCase(Staff)]
    [TestCase(Possessed)]
    [TestCase(Nobody)]
    public void Refreshing_an_observer_that_is_not_a_plain_player_does_nothing(uint observer)
    {
        _service.Overrides.RefreshNameOverridesForObserver(observer);

        Overrides().Should().BeEmpty();
        _queueRefreshes.Should().Be(0);
    }

    [Test]
    public void Staff_see_the_disguise_marker_and_nobody_else_does()
    {
        Wears(Bob, "d1", "Masked Wanderer");

        _service.Overrides.RefreshNameOverridesForPlayer(Bob);

        var forStaff = Overrides().Single(call => (uint)call.Arguments[0]! == Bob && (uint)call.Arguments[5]! == Staff);
        forStaff.Arguments[3].Should().Be(" " + ColorToken.Orange("[Disguised]"));
        Overrides().Where(call => (uint)call.Arguments[5]! != Staff).Select(call => (string)call.Arguments[3]!)
            .Should().NotContain(suffix => suffix.Contains("Disguised"));
    }

    [Test]
    public void An_override_is_not_applied_when_the_observer_or_target_is_not_a_player_of_the_right_kind()
    {
        _service.Overrides.RefreshNameOverridesForPlayer(Bob);

        Overrides().Should().NotContain(call => (uint)call.Arguments[0]! == Npc || (uint)call.Arguments[5]! == Npc);
    }

    // ---- AFK

    [Test]
    public void Toggling_afk_marks_the_player_and_shows_the_tag_in_their_names()
    {
        _service.Overrides.ToggleAfk(Alice).Should().BeTrue();

        _service.Overrides.IsAfk(Alice).Should().BeTrue();
        Overrides().Should().Contain(call => ((string)call.Arguments[3]!).Contains("[AFK]"));
        _service.Overrides.RepeatingTask.Should().NotBeNull();
        _service.ContextSubscriptions.Should().Be(0);
    }

    [Test]
    public void Toggling_afk_again_clears_it_and_stops_the_movement_check()
    {
        _service.Overrides.ToggleAfk(Alice);

        _service.Overrides.ToggleAfk(Alice).Should().BeFalse();

        _service.Overrides.IsAfk(Alice).Should().BeFalse();
        _service.Overrides.DisposedChecks.Should().Be(1);
    }

    [Test]
    public void The_movement_check_runs_once_for_all_afk_players()
    {
        _service.Overrides.ToggleAfk(Alice);
        var task = _service.Overrides.RepeatingTask;

        _service.Overrides.ToggleAfk(Bob);
        _service.Overrides.ToggleAfk(Alice);

        _service.Overrides.RepeatingTask.Should().BeSameAs(task);
        _service.Overrides.DisposedChecks.Should().Be(0, "Bob is still afk");
    }

    [TestCase(Npc)]
    [TestCase(Staff)]
    [TestCase(Possessed)]
    [TestCase(Nobody)]
    public void Only_plain_players_can_go_afk(uint player)
    {
        _service.Overrides.ToggleAfk(player).Should().BeFalse();

        _service.Overrides.IsAfk(player).Should().BeFalse();
        _service.Overrides.RepeatingTask.Should().BeNull();
    }

    [Test]
    public void Moving_ends_afk_and_tells_the_player()
    {
        _service.Overrides.ToggleAfk(Alice);
        _world[Alice].Position = new Vector3(1, 0, 0);
        _engine.ClearCalls();

        _service.Overrides.RepeatingTask!();

        _service.Overrides.IsAfk(Alice).Should().BeFalse();
        _engine.CallsTo("SendMessageToPC").Should().ContainSingle().Which.Arguments.Should().Equal(Alice, "You are no longer AFK.");
        Overrides().Should().NotBeEmpty();
    }

    [Test]
    public void Changing_area_ends_afk()
    {
        _service.Overrides.ToggleAfk(Alice);
        _world[Alice].Area = AreaTwo;

        _service.Overrides.RepeatingTask!();

        _service.Overrides.IsAfk(Alice).Should().BeFalse();
    }

    [Test]
    public void Standing_still_stays_afk()
    {
        _service.Overrides.ToggleAfk(Alice);

        _service.Overrides.RepeatingTask!();

        _service.Overrides.IsAfk(Alice).Should().BeTrue();
    }

    [Test]
    public void A_player_who_left_is_dropped_from_the_afk_list_without_a_message()
    {
        _service.Overrides.ToggleAfk(Alice);
        _world.Remove(Alice);
        _engine.ClearCalls();

        _service.Overrides.RepeatingTask!();

        _service.Overrides.IsAfk(Alice).Should().BeFalse();
        _engine.CallsTo("SendMessageToPC").Should().BeEmpty();
    }

    [Test]
    public void Leaving_the_module_clears_afk()
    {
        _service.Overrides.ToggleAfk(Alice);
        _engine.SetReturnHandler("GetExitingObject", _ => Alice);

        _service.Overrides.ClearAfkOnExit();

        _service.Overrides.IsAfk(Alice).Should().BeFalse();
    }

    [Test]
    public void Entering_the_module_clears_a_stale_afk_flag()
    {
        _service.Overrides.ToggleAfk(Alice);
        _engine.SetReturnHandler("GetEnteringObject", _ => Alice);

        _service.Overrides.ApplyNameOverridesOnEnter();

        _service.Overrides.IsAfk(Alice).Should().BeFalse();
    }

    // ---- chat overrides

    [Test]
    public void A_chat_message_is_sent_between_the_observers_view_of_the_name_and_the_restored_name()
    {
        var order = new List<string>();

        _service.Overrides.SendChatMessageWithChatNameOverride(Alice, Bob, () => order.Add("sent: " + string.Join("|", Overrides().Select(Name))));

        order.Should().ContainSingle().Which.Should().Be("sent: Tall Stranger");
        Overrides().Select(Name).Should().Equal("Tall Stranger", "Tall Stranger");
    }

    [Test]
    public void The_name_is_restored_even_when_sending_fails()
    {
        var act = () => _service.Overrides.SendChatMessageWithChatNameOverride(Alice, Bob, () => throw new InvalidOperationException("boom"));

        act.Should().Throw<InvalidOperationException>();
        Overrides().Should().HaveCount(2);
    }

    [Test]
    public void A_chat_override_for_a_staff_observer_carries_the_staff_suffix_and_is_restored_as_the_true_name()
    {
        Wears(Bob, "d1", "Masked Wanderer");

        _service.Overrides.SendChatMessageWithChatNameOverride(Staff, Bob, () => { });

        var calls = Overrides().ToList();
        calls[0].Arguments[3].Should().Be(" " + ColorToken.Orange("[Disguised]"));
        Name(calls[0]).Should().StartWith("Bob [");
        Name(calls[1]).Should().Contain("Bob [");
    }

    [Test]
    public void Restoring_ones_own_name_uses_the_own_view()
    {
        _service.Overrides.SendChatMessageWithChatNameOverride(Alice, Alice, () => { });

        Overrides().Select(Name).Should().Equal("Alice", "Alice [" + Unknown("Tall Stranger") + "]");
    }

    [Test]
    public void A_chat_override_needs_two_players_and_a_plain_target()
    {
        _service.Overrides.SendChatMessageWithChatNameOverride(Alice, Npc, () => { });
        _service.Overrides.SendChatMessageWithChatNameOverride(Npc, Alice, () => { });
        _service.Overrides.SendChatMessageWithChatNameOverride(Alice, Staff, () => { });
        _service.Overrides.SendChatMessageWithChatNameOverride(Alice, Possessed, () => { });
        _service.Overrides.SendChatMessageWithChatNameOverride(Alice, Nobody, () => { });

        Overrides().Should().BeEmpty();
    }

    // ---- tells

    private void Tell(ChatChannel channel, uint sender, uint target, bool direct = false)
    {
        _chat.SetReturn("GetChannel", channel);
        _chat.SetReturn("GetSender", sender);
        _chat.SetReturn("GetTarget", target);
        _variables.ClearCalls();
        if (direct)
            _engine.SetReturnHandler("GetLocalBool", _ => true);
    }

    [Test]
    public void A_stale_portrait_tell_is_skipped_for_any_tell_channel()
    {
        _portraitTellRefused = true;
        Tell(ChatChannel.DMTell, Alice, Bob);

        _service.Overrides.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(1);
        Overrides().Should().BeEmpty();
    }

    [Test]
    public void Chat_that_is_not_a_tell_is_left_alone()
    {
        Tell(ChatChannel.PlayerTalk, Alice, Bob);

        _service.Overrides.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(0);
        _portraitScript.Calls.Should().BeEmpty();
    }

    [TestCase(Nobody, Bob)]
    [TestCase(Alice, Nobody)]
    [TestCase(Npc, Bob)]
    [TestCase(Alice, Npc)]
    [TestCase(Staff, Bob)]
    [TestCase(Alice, Staff)]
    [TestCase(Alice, Possessed)]
    [TestCase(Possessed, Bob)]
    public void Tells_that_do_not_involve_two_plain_players_are_left_alone(uint sender, uint target)
    {
        Tell(ChatChannel.PlayerTell, sender, target);

        _service.Overrides.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(0);
        Overrides().Should().BeEmpty();
    }

    [Test]
    public void A_tell_to_a_name_that_matches_one_player_overrides_both_names_and_restores_them_soon_after()
    {
        _service.Descriptors[Carol] = "Short Stranger";
        Tell(ChatChannel.PlayerTell, Alice, Bob);

        _service.Overrides.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(0);
        Overrides().Should().HaveCount(2);
        var restores = _engine.CallsTo("DelayCommand").ToList();
        restores.Should().HaveCount(2);
        restores.Should().OnlyContain(call => (float)call.Arguments[0]! == 0.1f);
        _rename.ClearCalls();
        foreach (var restore in restores)
            ((Action)restore.Arguments[1]!)();
        Overrides().Should().HaveCount(2);
    }

    [Test]
    public void A_tell_addressed_to_a_character_directly_is_never_ambiguous()
    {
        Tell(ChatChannel.PlayerTell, Alice, Bob, direct: true);

        _service.Overrides.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(0);
        Overrides().Should().HaveCount(2);
    }

    [Test]
    public void A_tell_to_a_name_that_two_players_share_is_refused_with_a_hint()
    {
        Knows(Alice, Bob, "Kira");
        Knows(Alice, Carol, "Kira");
        _db.With(new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } });
        Tell(ChatChannel.PlayerTell, Alice, Bob);

        _service.Overrides.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(1);
        _engine.CallsTo("SendMessageToPC").Should().ContainSingle().Which.Arguments[1].Should().Be(
            ColorToken.Red("'Kira' is ambiguous. Use /name on the intended player before sending tells."));
        Overrides().Should().BeEmpty();
    }

    [Test]
    public void A_tell_to_a_shared_name_from_a_confirmed_portrait_is_not_ambiguous()
    {
        Knows(Alice, Bob, "Kira");
        Knows(Alice, Carol, "Kira");
        _db.With(new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } });
        _portraitTellConfirmed = true;
        Tell(ChatChannel.PlayerTell, Alice, Bob);

        _service.Overrides.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(0);
        Overrides().Should().NotBeEmpty();
    }

    [Test]
    public void A_tell_with_an_empty_display_name_falls_back_to_the_overrides()
    {
        _world[Bob].Name = "";
        Wears(Bob, "d1", "Masked Wanderer");
        Tell(ChatChannel.PlayerTell, Alice, Bob);

        _service.Overrides.PreventAmbiguousTellTargets();

        _chat.CountOf("SkipMessage").Should().Be(0);
    }
}
