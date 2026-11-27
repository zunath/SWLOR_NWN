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
/// Unit tests of the player name instance: the display names, the known name caches and the audit names. The known name validation and
/// writes are in <see cref="PlayerKnownNameServiceTests"/>; the name overrides, AFK and the tell handler are in
/// <see cref="PlayerNameOverrideServiceTests"/>.
/// </summary>
[TestFixture]
public class PlayerNameServiceTests : PlayerNameTestBase
{
    // ---- sanitizing

    [TestCase(null, "")]
    [TestCase("   ", "")]
    [TestCase("  Kira   Vos  ", "Kira Vos")]
    [TestCase("<c\u007f\u007f\u007f>Kira</c>", "Kira")]
    [TestCase("Ki\u0001ra", "Kira")]
    [TestCase("Kira     Vos", "Kira Vos")]
    public void Names_are_stripped_of_colors_control_characters_and_repeated_spaces(string? raw, string expected)
    {
        _service.Names.SanitizeKnownName(raw!).Should().Be(expected);
    }

    [Test]
    public void A_known_name_that_is_blank_counts_as_unknown()
    {
        Knows(Alice, Bob, " ");

        _service.Names.TryGetKnownName(Alice, Bob, out _).Should().BeFalse();
    }

    [Test]
    public void Known_names_are_cached_until_the_player_known_name_table_is_written()
    {
        Knows(Alice, Bob, "Kira");
        _service.Names.TryGetKnownName(Alice, Bob, out _).Should().BeTrue();
        _service.Names.TryGetKnownName(Alice, Bob, out _).Should().BeTrue();

        _db.Store.Queries.Should().HaveCount(1, "the second lookup is served from the cache");

        Knows(Alice, Bob, "Robert");
        _service.Names.TryGetKnownName(Alice, Bob, out var known).Should().BeTrue();

        known.Should().Be("Robert");
        _db.Store.Queries.Should().HaveCount(2);
    }

    [Test]
    public void Leaving_drops_the_cached_known_names_of_the_player()
    {
        Knows(Alice, Bob, "Kira");
        _service.Names.TryGetKnownName(Alice, Bob, out _);
        _world[Alice].Uuid = "alice-id";
        _engine.SetReturnHandler("GetExitingObject", _ => Alice);

        _service.Names.ReleaseKnownNamesOnExit();
        _service.Names.TryGetKnownName(Alice, Bob, out _);

        _db.Store.Queries.Should().HaveCount(2);
    }

    [Test]
    public void Leaving_without_an_id_changes_nothing()
    {
        _engine.SetReturnHandler("GetExitingObject", _ => Npc);

        var act = () => _service.Names.ReleaseKnownNamesOnExit();

        act.Should().NotThrow();
    }

    // ---- display names

    [Test]
    public void A_target_that_is_not_valid_has_no_name()
    {
        _service.Names.GetDisplayName(Alice, Nobody).Should().BeEmpty();
        _service.Overrides.GetChatDisplayName(Alice, Nobody).Should().BeEmpty();
    }

    [TestCase(Npc)]
    [TestCase(Staff)]
    [TestCase(Possessed)]
    public void An_NPC_staff_or_possessed_target_shows_its_own_name(uint target)
    {
        _service.Names.GetDisplayName(Alice, target).Should().Be(_world[target].Name);
        _service.Overrides.GetChatDisplayName(Alice, target).Should().Be(_world[target].Name);
    }

    [Test]
    public void A_player_sees_themselves_and_invalid_observers_see_the_real_name()
    {
        _service.Names.GetDisplayName(Alice, Alice).Should().Be("Alice");
        _service.Names.GetDisplayName(Nobody, Bob).Should().Be("Bob");
        _service.Overrides.GetChatDisplayName(Alice, Alice).Should().Be("Alice");
        _service.Overrides.GetChatDisplayName(Nobody, Bob).Should().Be("Bob");
        _service.Overrides.GetChatDisplayName(Npc, Bob).Should().Be("Bob");
        _service.Names.GetDisplayName(Npc, Bob).Should().Be("Bob");
    }

    [Test]
    public void Staff_see_the_real_name_with_the_descriptor()
    {
        _service.Names.GetDisplayName(Staff, Bob).Should().Be("Bob [" + Unknown("Tall Stranger") + "]");
        _service.Names.GetDisplayName(Possessed, Bob).Should().Be("Bob [" + Unknown("Tall Stranger") + "]");
        _service.Overrides.GetChatDisplayName(Staff, Bob).Should().Be("Bob [" + Unknown("Tall Stranger") + "]");
    }

    [Test]
    public void A_stranger_is_shown_by_descriptor()
    {
        _service.Names.GetDisplayName(Alice, Bob).Should().Be("Tall Stranger");
        _service.Overrides.GetChatDisplayName(Alice, Bob).Should().Be("Tall Stranger");
    }

    [Test]
    public void A_known_player_is_shown_by_the_name_the_observer_gave_with_or_without_the_descriptor()
    {
        Knows(Alice, Bob, "Kira");
        _db.With(new Player("alice-id"));

        _service.Names.GetDisplayName(Alice, Bob).Should().Be("Kira [" + Unknown("Tall Stranger") + "]");
        _service.Overrides.GetChatDisplayName(Alice, Bob).Should().Be("Kira");
    }

    [Test]
    public void The_observer_can_turn_off_the_descriptor_beside_names_they_gave()
    {
        Knows(Alice, Bob, "Kira");
        _db.With(new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } });

        _service.Names.GetDisplayName(Alice, Bob).Should().Be("Kira");
    }

    [Test]
    public void The_names_a_disguised_player_shows_follow_the_disguise_identity()
    {
        Wears(Bob, "d1", "Masked Wanderer");
        Knows(Alice, Bob, "Original");

        _service.Names.GetDisplayName(Alice, Bob).Should().Be("Masked Wanderer");
    }

    [Test]
    public void The_database_display_name_covers_every_observer_kind()
    {
        Knows(Alice, Bob, "Kira");
        _db.With(new Player("alice-id"));
        _service.DescriptorsById["bob-id"] = "Tall Stranger";
        _service.DescriptorsById["unknown-id"] = "Short Stranger";

        _service.Names.GetDisplayNameByPlayerId(Alice, " ", "Fallback").Should().Be("Someone");
        _service.Names.GetDisplayNameByPlayerId(Alice, "x", " ").Should().Be("Someone");
        _service.Names.GetDisplayNameByPlayerId(Nobody, "bob-id", "Bob").Should().Be("Bob");
        _service.Names.GetDisplayNameByPlayerId(Npc, "bob-id", "Bob").Should().Be("Bob");
        _service.Names.GetDisplayNameByPlayerId(Alice, "alice-id", "Alice").Should().Be("Alice");
        _service.Names.GetDisplayNameByPlayerId(Staff, "bob-id", "Bob").Should().Be("Bob [" + Unknown("Tall Stranger") + "]");
        _service.Names.GetDisplayNameByPlayerId(Possessed, "bob-id", "Bob").Should().Be("Bob [" + Unknown("Tall Stranger") + "]");
        _service.Names.GetDisplayNameByPlayerId(Alice, "unknown-id", "Nope").Should().Be("Short Stranger");
    }

    [Test]
    public void The_database_display_name_of_a_known_player_follows_the_descriptor_setting()
    {
        _db.With(new PlayerKnownName("alice-id") { KnownNames = { ["bob-id"] = "Kira" } });
        _db.With(new Player("alice-id"));
        _service.DescriptorsById["bob-id"] = "Tall Stranger";

        _service.Names.GetDisplayNameByPlayerId(Alice, "bob-id", "Bob").Should().Be("Kira [" + Unknown("Tall Stranger") + "]");

        var quiet = new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } };
        _db.With(quiet);
        _service.Overrides.RefreshNameOverridesForObserver(Alice);

        _service.Names.GetDisplayNameByPlayerId(Alice, "bob-id", "Bob").Should().Be("Kira");
    }

    [Test]
    public void The_plain_display_name_has_no_color_tokens()
    {
        _db.With(new PlayerKnownName("alice-id") { KnownNames = { ["bob-id"] = "Kira" } });
        _service.DescriptorsById["bob-id"] = "Tall Stranger";
        _service.DescriptorsById["carol-id"] = "Short Stranger";

        _service.Names.GetPlainDisplayNameByPlayerId(Alice, "", "x").Should().Be("Someone");
        _service.Names.GetPlainDisplayNameByPlayerId(Alice, "bob-id", " ").Should().Be("Kira");
        _service.Names.GetPlainDisplayNameByPlayerId(Alice, "carol-id", "x").Should().Be("Short Stranger");
        _service.Names.GetPlainDisplayNameByPlayerId(Nobody, "bob-id", "<c\u007f\u007f\u007f>Bob</c>").Should().Be("Bob");
        _service.Names.GetPlainDisplayNameByPlayerId(Alice, "alice-id", "Alice").Should().Be("Alice");
        _service.Names.GetPlainDisplayNameByPlayerId(Staff, "bob-id", "Bob").Should().Be("Bob [Tall Stranger]");
        _service.Names.GetPlainDisplayNameByPlayerId(Alice, "nobody-id", null!).Should().Be("Someone");
    }

    [Test]
    public void The_known_name_or_fallback_is_for_permission_screens()
    {
        _db.With(new PlayerKnownName("alice-id") { KnownNames = { ["bob-id"] = "Kira" } });

        _service.Names.GetKnownNameOrFallbackByPlayerId(Alice, " ", "Fallback").Should().Be("Fallback");
        _service.Names.GetKnownNameOrFallbackByPlayerId(Alice, "", " ").Should().Be("Someone");
        _service.Names.GetKnownNameOrFallbackByPlayerId(Alice, "bob-id", "Bob").Should().Be("Kira");
        _service.Names.GetKnownNameOrFallbackByPlayerId(Alice, "carol-id", "Carol").Should().Be("Carol");
        _service.Names.GetKnownNameOrFallbackByPlayerId(Alice, "carol-id", "").Should().Be("Someone");
        _service.Names.GetKnownNameOrFallbackByPlayerId(Alice, "alice-id", "Alice").Should().Be("Alice");
        _service.Names.GetKnownNameOrFallbackByPlayerId(Staff, "bob-id", "Bob").Should().Be("Bob");
        _service.Names.GetKnownNameOrFallbackByPlayerId(Nobody, "bob-id", "Bob").Should().Be("Bob");
    }

    // ---- colored names

    [Test]
    public void Colored_names_use_the_npc_color_for_anything_that_is_not_a_plain_player()
    {
        var expected = ColorToken.GetNameNPCColor(Npc);

        _service.Names.GetColoredDisplayName(Alice, Npc).Should().Be(expected);
        _service.Overrides.GetColoredChatDisplayName(Alice, Npc).Should().Be(expected);
        _service.Names.GetColoredDisplayName(Alice, Staff).Should().Be(ColorToken.GetNameNPCColor(Staff));
        _service.Overrides.GetColoredChatDisplayName(Alice, Possessed).Should().Be(ColorToken.GetNameNPCColor(Possessed));
    }

    [Test]
    public void A_stranger_is_gray_and_a_known_player_has_the_player_color()
    {
        _service.Names.GetColoredDisplayName(Alice, Bob).Should().Be(ColorToken.Gray("Tall Stranger"));
        _service.Overrides.GetColoredChatDisplayName(Alice, Bob).Should().Be(ColorToken.Gray("Tall Stranger"));

        Knows(Alice, Bob, "Kira");
        _db.With(new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } });
        _service.Overrides.RefreshNameOverridesForObserver(Alice);

        _service.Names.GetColoredDisplayName(Alice, Bob).Should().Be(ColorToken.GetPCColor("Kira"));
        _service.Overrides.GetColoredChatDisplayName(Alice, Bob).Should().Be(ColorToken.GetPCColor("Kira"));
    }

    [Test]
    public void A_known_player_is_colored_with_the_descriptor_when_the_observer_wants_it()
    {
        Knows(Alice, Bob, "Kira");
        _db.With(new Player("alice-id"));

        _service.Names.GetColoredDisplayName(Alice, Bob)
            .Should().Be(ColorToken.GetPCColor("Kira") + " [" + ColorToken.Gray("Tall Stranger") + "]");
    }

    [Test]
    public void Staff_see_the_true_name_with_the_descriptor_in_color()
    {
        _service.Names.GetColoredDisplayName(Staff, Bob)
            .Should().Be(ColorToken.GetPCColor("Bob") + " [" + ColorToken.Gray("Tall Stranger") + "]");
        _service.Names.GetColoredDisplayName(Alice, Alice).Should().Be(ColorToken.GetPCColor("Alice"));
        _service.Overrides.GetColoredChatDisplayName(Staff, Bob).Should().Be(ColorToken.GetPCColor("Bob [" + ColorToken.Gray("Tall Stranger") + "]"));
    }

    // ---- audit names

    [Test]
    public void Audit_names_add_the_current_descriptor_for_players_only()
    {
        _service.Names.GetAuditName(Alice).Should().Be("Alice [Tall Stranger]");
        _service.Names.GetAuditName(Npc).Should().Be("Guard");
        _service.Names.GetAuditName(Staff).Should().Be("Staff");
        _service.Names.GetAuditName(Possessed).Should().Be("Possessed Guard");
        _service.Names.GetAuditName(Nobody).Should().BeEmpty();
    }

    [Test]
    public void Audit_identities_name_the_disguise_when_one_is_worn()
    {
        _service.Names.GetAuditIdentity(Alice).Should().Be("Alice [Tall Stranger] [PlayerId=alice-id IdentityKey=alice-id Disguised=False DisguiseId=none DisguiseName=none]");
        Wears(Bob, "d1", "Masked Wanderer");

        var identity = _service.Names.GetAuditIdentity(Bob);

        identity.Should().StartWith("Bob [Masked Wanderer] [PlayerId=bob-id IdentityKey=");
        identity.Should().Contain("Disguised=True DisguiseId=d1 DisguiseName=Private d1]");
        _service.Names.GetAuditIdentity(Npc).Should().Be("Guard");
    }

    // ---- descriptor settings cache

    [Test]
    public void The_descriptor_settings_default_to_on_for_anyone_who_is_not_a_plain_player()
    {
        _service.Names.ShouldShowDescriptorForNamedPlayers(Staff).Should().BeTrue();
        _service.Names.ShouldShowDescriptorForNamedPlayers(Possessed).Should().BeTrue();
        _service.Names.ShouldShowDescriptorForNamedPlayers(Npc).Should().BeTrue();
        _service.Names.ShouldShowDescriptorForNamedPlayers(Nobody).Should().BeTrue();
        _world[Alice].Uuid = " ";
        _service.Names.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeTrue();
        _db.Store.Reads.Should().BeEmpty();
    }

    [Test]
    public void The_descriptor_setting_is_read_once_per_script_context()
    {
        _db.With(new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } });

        _service.Names.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeFalse();
        _service.Names.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeFalse();

        _db.Store.Reads.Should().HaveCount(1);
        _service.ContextSubscriptions.Should().Be(1);

        _db.With(new Player("alice-id"));
        _service.ScriptContextEnd!();

        _service.Names.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeTrue();
        _service.ContextSubscriptions.Should().Be(1, "the hook is registered once");
    }

    [Test]
    public void Forgetting_the_descriptor_setting_makes_the_next_read_come_from_the_record()
    {
        _db.With(new Player("alice-id") { Settings = { ShowDescriptorsForNamedPlayers = false } });
        _service.Names.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeFalse();

        _db.With(new Player("alice-id"));
        _service.Names.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeFalse("the cached setting is kept until it is forgotten");
        _service.Names.ForgetDescriptorSetting("alice-id");

        _service.Names.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeTrue();
        _service.Names.ForgetDescriptorSetting("nobody-id");
    }

    [Test]
    public void A_player_without_a_record_shows_descriptors()
    {
        _service.Names.ShouldShowDescriptorForNamedPlayers(Alice).Should().BeTrue();
    }

    [Test]
    public void The_own_descriptor_setting_is_read_each_time()
    {
        _service.Names.ShouldShowOwnDescriptor(Alice).Should().BeTrue();
        _db.With(new Player("alice-id") { Settings = { ShowOwnDescriptor = false } });
        _service.Names.ShouldShowOwnDescriptor(Alice).Should().BeFalse();
        _service.Names.ShouldShowOwnDescriptor(Staff).Should().BeFalse();
        _service.Names.ShouldShowOwnDescriptor(Possessed).Should().BeFalse();
        _service.Names.ShouldShowOwnDescriptor(Npc).Should().BeFalse();
        _world[Bob].Uuid = "";
        _service.Names.ShouldShowOwnDescriptor(Bob).Should().BeTrue();
    }

}
