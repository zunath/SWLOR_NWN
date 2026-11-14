using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Unit tests of the disguise instance over <see cref="DisguiseWorld"/>: every rule of creating, customizing,
/// activating, retiring and deleting a disguise, the activation delay, and the facade.
/// </summary>
[TestFixture]
public class DisguiseServiceTests
{
    private const uint Owner = DisguiseWorld.Owner;

    private DisguiseWorld _world = null!;
    private DisguiseWorld.BenchDisguises Disguises => _world.Disguises;

    [SetUp]
    public void SetUp()
    {
        _world = new DisguiseWorld();
        Disguises.StatAdjustments[StatType.AdditionalDisguiseSlots] = 2;
    }

    [TearDown]
    public void TearDown() => _world.Dispose();

    private PlayerDisguise Create() => Disguises.CreateDisguise(Owner)!;

    private PlayerDisguise StoreDisguise(string id, string playerId = DisguiseWorld.OwnerId, bool retired = false,
        DateTime? lastActivated = null)
    {
        var disguise = new PlayerDisguise
        {
            Id = id, PlayerId = playerId, IsRetired = retired, PrivateName = id, Descriptor = "Desc " + id,
            DateLastActivated = lastActivated, PortraitInternalId = 20, SoundSetId = 21
        };
        _world.Db.With(disguise);
        return disguise;
    }

    // ---- identity

    [Test]
    public void The_identity_key_is_the_player_id_until_a_disguise_is_active()
    {
        Disguises.GetIdentityKey(Owner).Should().Be("owner-id");

        var disguise = Create();
        Disguises.Activate(Owner, disguise.Id);

        Disguises.GetIdentityKey(Owner).Should().Be("disguise:" + disguise.Id);
        Disguises.BuildIdentityKey("abc").Should().Be("disguise:abc");
    }

    [TestCase("disguise:abc", true)]
    [TestCase("DISGUISE:abc", true)]
    [TestCase("owner-id", false)]
    [TestCase("", false)]
    [TestCase("  ", false)]
    [TestCase(null, false)]
    public void Only_keys_with_the_prefix_are_disguise_identity_keys(string? key, bool expected)
    {
        Disguises.IsDisguiseIdentityKey(key!).Should().Be(expected);
    }

    [Test]
    public void The_display_descriptor_falls_back_to_the_base_descriptor_when_the_disguise_has_none()
    {
        _world.DbPlayer.UnknownDisplayName = "Base";
        Disguises.GetDisplayDescriptor(Owner).Should().Be("Base");

        var disguise = Create();
        disguise.Descriptor = "Masked";
        _world.Db.Set(disguise);
        Disguises.Activate(Owner, disguise.Id);
        Disguises.GetDisplayDescriptor(Owner).Should().Be("Masked");

        disguise.Descriptor = " ";
        Disguises.GetDisplayDescriptor(Owner).Should().Be("Base");
    }

    [Test]
    public void The_account_name_is_scrambled_by_the_disguise_or_the_player_setting()
    {
        _world.DbPlayer.Settings.ScrambleAccountName = false;
        Disguises.ShouldScrambleAccountName(Owner).Should().BeFalse();
        _world.DbPlayer.Settings.ScrambleAccountName = true;
        Disguises.ShouldScrambleAccountName(Owner).Should().BeTrue();

        var disguise = Create();
        disguise.ScrambleAccountId = false;
        Disguises.Activate(Owner, disguise.Id);
        Disguises.ShouldScrambleAccountName(Owner).Should().BeFalse();

        // No saved player: scrambled by default.
        Disguises.ShouldScrambleAccountName(DisguiseWorld.Staff).Should().BeTrue();
    }

    [Test]
    public void Only_valid_player_characters_have_an_active_disguise()
    {
        var disguise = Create();
        Disguises.Activate(Owner, disguise.Id);

        Disguises.GetActiveDisguise(Owner)!.Id.Should().Be(disguise.Id);
        Disguises.GetActiveDisguise(DisguiseWorld.ObjectInvalid).Should().BeNull();
        Disguises.GetActiveDisguise(DisguiseWorld.Possessed).Should().BeNull("not a player character");
        Disguises.GetActiveDisguise(DisguiseWorld.Dm).Should().BeNull("a DM");
        Disguises.GetActiveDisguise(99u).Should().BeNull("an invalid object");
        Disguises.GetActiveDisguise(DisguiseWorld.Staff).Should().BeNull("a player with no saved record");
    }

    [Test]
    public void The_active_disguise_of_a_record_must_exist_belong_to_the_player_and_not_be_retired()
    {
        Disguises.GetActiveDisguise((Player)null!).Should().BeNull();
        Disguises.GetActiveDisguise(new Player("x") { ActiveDisguiseId = "" }).Should().BeNull();
        Disguises.GetActiveDisguise(new Player("x") { ActiveDisguiseId = "missing" }).Should().BeNull();

        StoreDisguise("retired", retired: true);
        StoreDisguise("foreign", playerId: "other");
        StoreDisguise("mine");
        Disguises.GetActiveDisguise(new Player(DisguiseWorld.OwnerId) { ActiveDisguiseId = "retired" }).Should().BeNull();
        Disguises.GetActiveDisguise(new Player(DisguiseWorld.OwnerId) { ActiveDisguiseId = "foreign" }).Should().BeNull();
        Disguises.GetActiveDisguise(new Player(DisguiseWorld.OwnerId) { ActiveDisguiseId = "mine" })!.Id.Should().Be("mine");
    }

    // ---- listing and slots

    [Test]
    public void Disguises_are_listed_by_private_name_filtered_by_retirement()
    {
        StoreDisguise("b").PrivateName = "Bravo";
        StoreDisguise("a").PrivateName = "Alpha";
        StoreDisguise("r", retired: true).PrivateName = "Retired";
        StoreDisguise("x", playerId: "other");

        Disguises.GetDisguises(DisguiseWorld.OwnerId, false).Select(d => d.PrivateName).Should().Equal("Alpha", "Bravo");
        Disguises.GetDisguises(DisguiseWorld.OwnerId, true).Select(d => d.PrivateName).Should().Equal("Retired");
    }

    [Test]
    public void A_full_page_of_disguises_asks_for_the_next_page()
    {
        var db = ScriptedProxy.Create<SWLOR.Game.Server.Domain.Foundation.IDBService>(out var script);
        var pages = new Queue<List<PlayerDisguise>>();
        pages.Enqueue(Enumerable.Range(0, 50).Select(i => new PlayerDisguise { Id = "a" + i, PrivateName = "n" + (99 - i) }).ToList());
        pages.Enqueue(new List<PlayerDisguise> { new() { Id = "last", PrivateName = "n00" } });
        script.On("Search", _ => pages.Dequeue());
        var service = new DisguiseWorld.BenchDisguises(db, _world.Cache, _world.Gui, _world.DmPresence, _world.PlayerName,
            _world.Descriptors, _world.Logs);

        var all = service.GetDisguises("p", false);

        all.Should().HaveCount(51);
        all.First().PrivateName.Should().Be("n00");
        pages.Should().BeEmpty();
    }

    [Test]
    public void The_slot_limit_is_the_saved_limit_plus_bonus_slots_never_below_the_default()
    {
        var dbPlayer = new Player("x") { DisguiseSlotLimit = 4 };
        Disguises.GetDisguiseSlotLimit(Owner, dbPlayer).Should().Be(6);

        dbPlayer.DisguiseSlotLimit = 0;
        Disguises.GetDisguiseSlotLimit(Owner, dbPlayer).Should().Be(Player.DefaultDisguiseSlotLimit + 2);
        Disguises.GetDisguiseSlotLimit(Owner, null!).Should().Be(Player.DefaultDisguiseSlotLimit + 2);

        Disguises.CalculateDisguiseSlotLimit(1, -5).Should().Be(Player.DefaultDisguiseSlotLimit);
        Disguises.CalculateDisguiseSlotLimit(3, 2).Should().Be(5);
    }

    [TestCase(0, 30)]
    [TestCase(50, 15)]
    [TestCase(90, 5)]
    [TestCase(100, 5)]
    [TestCase(250, 5)]
    [TestCase(-20, 30)]
    public void The_activation_delay_shrinks_with_the_reduction_but_never_below_the_minimum(int percent, double minutes)
    {
        Disguises.CalculateActivationDelay(percent).Should().Be(TimeSpan.FromMinutes(minutes));
    }

    [Test]
    public void The_activation_delay_of_a_player_uses_their_cooldown_reduction()
    {
        Disguises.StatAdjustments[StatType.DisguiseSwapCooldownReductionPercent] = 50;

        Disguises.GetActivationDelay(Owner).Should().Be(TimeSpan.FromMinutes(15));
    }

    [Test]
    public void Used_slots_count_retired_disguises_too()
    {
        StoreDisguise("a");
        StoreDisguise("b", retired: true);
        StoreDisguise("c", playerId: "other");

        Disguises.CountUsedSlots(DisguiseWorld.OwnerId).Should().Be(2);
    }

    // ---- creating

    [Test]
    public void A_new_disguise_starts_with_the_players_current_portrait_sound_and_base_descriptor()
    {
        _world.DbPlayer.UnknownDisplayName = "Base";

        var disguise = Create();

        disguise.PrivateName.Should().Be("Disguise #1");
        disguise.Descriptor.Should().Be("Base");
        disguise.PlayerId.Should().Be("owner-id");
        disguise.PortraitInternalId.Should().Be(10);
        disguise.SoundSetId.Should().Be(10);
        disguise.ScrambleAccountId.Should().BeTrue();
        disguise.Biography.Should().BeEmpty();
        _world.Db.Get<PlayerDisguise>(disguise.Id).Should().BeSameAs(disguise);
    }

    [Test]
    public void No_disguise_is_created_past_the_slot_limit()
    {
        Create();
        Create();
        Create();
        _world.ClearLogs();

        Disguises.CreateDisguise(Owner).Should().BeNull();

        _world.Logs.Should().ContainSingle().Which.Template.Should().StartWith("Disguise creation rejected");
        Disguises.CountUsedSlots(DisguiseWorld.OwnerId).Should().Be(3);
    }

    [Test]
    public void The_portrait_falls_back_to_the_engine_portrait_id_and_then_to_the_first_portrait()
    {
        _world.CacheScript.On("GetPortraitInternalIdByResref", _ => 0).On("GetPortraitInternalId", _ => 7);
        Create().PortraitInternalId.Should().Be(7);

        _world.CacheScript.On("GetPortraitInternalId", _ => 0);
        Create().PortraitInternalId.Should().Be(1);

        _world.CacheScript.On("GetPortraitInternalId", _ => throw new KeyNotFoundException());
        Create().PortraitInternalId.Should().Be(1);
    }

    // ---- saving

    [Test]
    public void Saving_stores_sanitized_values_and_clamps_the_portrait_and_sound()
    {
        var disguise = Create();

        Disguises.SaveDisguise(Owner, disguise.Id, "  Mask ", " Hooded ", null!, 5000, 999, false).IsSuccessful.Should().BeTrue();

        var saved = _world.Db.Get<PlayerDisguise>(disguise.Id)!;
        saved.PrivateName.Should().Be("Mask");
        saved.Descriptor.Should().Be("Hooded");
        saved.Biography.Should().BeEmpty();
        saved.PortraitInternalId.Should().Be(100, "clamped to the portrait count");
        saved.SoundSetId.Should().Be(-1, "an unknown sound set");
        saved.ScrambleAccountId.Should().BeFalse();
    }

    [Test]
    public void Saving_an_active_disguise_reapplies_it_and_refreshes_the_display()
    {
        var disguise = Create();
        Disguises.Activate(Owner, disguise.Id);
        _world.NameRefreshes.Clear();
        _world.Refreshes.Clear();

        Disguises.SaveDisguise(Owner, disguise.Id, "Mask", "Hooded", "Bio", 20, 21, true).IsSuccessful.Should().BeTrue();

        _world.NameRefreshes.Should().Equal(Owner);
        _world.Refreshes.Select(r => r.Payload).Should().ContainSingle().Which.Should().BeOfType<DisguiseChangedRefreshEvent>();
        _world.Engine.CallsTo("SetDescription").Last().Arguments[1].Should().Be("Bio");
    }

    [Test]
    public void Saving_an_inactive_disguise_does_not_touch_the_character()
    {
        var disguise = Create();
        _world.Engine.ClearCalls();

        Disguises.SaveDisguise(Owner, disguise.Id, "Mask", "Hooded", "Bio", 20, 21, true);

        _world.Engine.CallsTo("SetDescription").Should().BeEmpty();
        _world.NameRefreshes.Should().BeEmpty();
    }

    [Test]
    public void A_save_is_rejected_for_a_missing_foreign_or_retired_disguise_and_for_invalid_text()
    {
        var own = Create();
        StoreDisguise("foreign", playerId: "other");
        StoreDisguise("retired", retired: true);

        Disguises.SaveDisguise(Owner, "missing", "N", "D", "B", 1, 1, true).ErrorMessage.Should().Be("Unable to locate that disguise.");
        Disguises.SaveDisguise(Owner, "foreign", "N", "D", "B", 1, 1, true).ErrorMessage.Should().Be("Unable to locate that disguise.");
        Disguises.SaveDisguise(Owner, "retired", "N", "D", "B", 1, 1, true).ErrorMessage.Should().Be("Retired disguises cannot be edited.");
        Disguises.SaveDisguise(Owner, own.Id, " ", "D", "B", 1, 1, true).ErrorMessage.Should().Be("Please enter a private disguise name.");
        Disguises.SaveDisguise(Owner, own.Id, new string('x', 33), "D", "B", 1, 1, true).ErrorMessage
            .Should().Be("Private disguise names may be no longer than 32 characters.");
        Disguises.SaveDisguise(Owner, own.Id, "<c=red>x</c>", "D", "B", 1, 1, true).ErrorMessage
            .Should().Be("Private disguise names may not contain color codes.");
        Disguises.SaveDisguise(Owner, own.Id, "N", "", "B", 1, 1, true).ErrorMessage.Should().Be("Required");
        Disguises.SaveDisguise(Owner, own.Id, "N", "D", new string('b', 5001), 1, 1, true).ErrorMessage
            .Should().Be("Disguise biographies may be no longer than 5000 characters.");
        _world.Db.Get<PlayerDisguise>(own.Id)!.PrivateName.Should().Be("Disguise #1");
    }

    // ---- activating

    [Test]
    public void Activating_snapshots_the_undisguised_look_once_and_applies_the_disguise()
    {
        var disguise = Create();
        disguise.Biography = "My bio";
        disguise.PortraitInternalId = 20;

        Disguises.Activate(Owner, disguise.Id).IsSuccessful.Should().BeTrue();

        var player = _world.DbPlayer;
        player.ActiveDisguiseId.Should().Be(disguise.Id);
        player.UndisguisedPortraitId.Should().Be(10);
        player.UndisguisedPortraitResref.Should().Be("baseportrait");
        player.UndisguisedSoundSetId.Should().Be(10);
        player.UndisguisedDescription.Should().Be("Current description");
        player.HasUndisguisedDescriptionSnapshot.Should().BeTrue();
        disguise.DateLastActivated.Should().Be(Disguises.Now);
        _world.Engine.CallsTo("SetPortraitId").Last().Arguments[1].Should().Be(520);
        _world.Engine.CallsTo("SetDescription").Last().Arguments[1].Should().Be("My bio");
    }

    [Test]
    public void A_blank_biography_shows_the_placeholder_text()
    {
        var disguise = Create();

        Disguises.Activate(Owner, disguise.Id);

        _world.Engine.CallsTo("SetDescription").Last().Arguments[1].Should().Be("No description is available.");
    }

    [Test]
    public void Activating_the_active_disguise_again_only_reapplies_it()
    {
        var disguise = Create();
        Disguises.Activate(Owner, disguise.Id);
        var stamp = disguise.DateLastActivated;
        Disguises.Now = Disguises.Now.AddMinutes(1);

        Disguises.Activate(Owner, disguise.Id).IsSuccessful.Should().BeTrue();

        disguise.DateLastActivated.Should().Be(stamp);
        _world.Logs.Last().Template.Should().StartWith("Active disguise reapplied");
    }

    [Test]
    public void A_second_activation_waits_for_the_delay_and_names_the_minutes_left()
    {
        var first = Create();
        var second = Create();
        Disguises.Activate(Owner, first.Id);
        Disguises.Now = Disguises.Now.AddMinutes(10);

        var result = Disguises.Activate(Owner, second.Id);

        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("There is a 30-minute delay").And.Contain("You must wait 20 minutes")
            .And.Contain("Deactivation is available immediately.");
        _world.DbPlayer.ActiveDisguiseId.Should().Be(first.Id);

        Disguises.Now = Disguises.Now.AddMinutes(19).AddSeconds(30);
        Disguises.Activate(Owner, second.Id).ErrorMessage.Should().Contain("less than 1 minute");

        Disguises.Now = Disguises.Now.AddMinutes(1);
        Disguises.Activate(Owner, second.Id).IsSuccessful.Should().BeTrue();
        _world.DbPlayer.ActiveDisguiseId.Should().Be(second.Id);
    }

    [Test]
    public void Switching_between_disguises_keeps_the_original_baseline()
    {
        var first = Create();
        var second = Create();
        Disguises.Activate(Owner, first.Id);
        _world.DbPlayer.UndisguisedDescription = "baseline";
        Disguises.Now = Disguises.Now.AddHours(1);

        Disguises.Activate(Owner, second.Id);

        _world.DbPlayer.UndisguisedDescription.Should().Be("baseline");
    }

    [Test]
    public void The_activation_delay_looks_at_the_latest_activation_of_every_disguise_across_pages()
    {
        var disguise = Create();
        StoreDisguise("old", lastActivated: Disguises.Now.AddHours(-5));
        StoreDisguise("recent", lastActivated: Disguises.Now.AddMinutes(-1));
        StoreDisguise("never");

        Disguises.Activate(Owner, disguise.Id).ErrorMessage.Should().Contain("You must wait 29 minutes");
    }

    [Test]
    public void Activation_is_rejected_for_missing_foreign_retired_or_unsaved_players()
    {
        StoreDisguise("foreign", playerId: "other");
        StoreDisguise("retired", retired: true);

        Disguises.Activate(Owner, "missing").ErrorMessage.Should().Be("Unable to activate that disguise.");
        Disguises.Activate(Owner, "foreign").ErrorMessage.Should().Be("Unable to activate that disguise.");
        Disguises.Activate(Owner, "retired").ErrorMessage.Should().Be("Unable to activate that disguise.");
        Disguises.Activate(DisguiseWorld.Staff, "foreign").ErrorMessage.Should().Be("Unable to activate that disguise.");
    }

    [Test]
    public void A_stale_applied_disguise_description_is_snapshotted_while_applying()
    {
        var disguise = Create();
        _world.DbPlayer.ActiveDisguiseId = "stale-id";
        _world.DbPlayer.HasUndisguisedDescriptionSnapshot = false;

        Disguises.Activate(Owner, disguise.Id);

        _world.DbPlayer.HasUndisguisedDescriptionSnapshot.Should().BeTrue();
        _world.DbPlayer.UndisguisedDescription.Should().Be("Current description");
    }

    [Test]
    public void An_invalid_legacy_portrait_is_ignored_and_an_unknown_sound_set_is_left_alone()
    {
        var disguise = Create();
        disguise.SoundSetId = 777;
        _world.CacheScript.On("GetPortraitByInternalId", _ => throw new KeyNotFoundException());

        Disguises.Activate(Owner, disguise.Id).IsSuccessful.Should().BeTrue();

        _world.Engine.CallsTo("SetPortraitId").Should().BeEmpty();
        _world.Engine.CallsTo("SetSoundset").Should().BeEmpty();
    }

    [Test]
    public void A_disguise_without_a_portrait_leaves_the_portrait_alone()
    {
        var disguise = Create();
        disguise.PortraitInternalId = 0;

        Disguises.Activate(Owner, disguise.Id);

        _world.Engine.CallsTo("SetPortraitId").Should().BeEmpty();
        _world.Engine.CallsTo("SetSoundset").Should().ContainSingle();
    }

    // ---- logging in

    [Test]
    public void Entering_reapplies_the_active_disguise_for_players_only()
    {
        var disguise = Create();
        Disguises.Activate(Owner, disguise.Id);
        _world.Engine.ClearCalls();
        _world.ClearLogs();

        foreach (var entering in new[] { DisguiseWorld.Dm, DisguiseWorld.Possessed, DisguiseWorld.Staff })
        {
            _world.Entering = entering;
            Disguises.ApplyActiveDisguiseOnEnter();
        }

        _world.Engine.CallsTo("SetDescription").Should().BeEmpty();
        _world.Logs.Should().BeEmpty();

        _world.Entering = Owner;
        Disguises.ApplyActiveDisguiseOnEnter();
        _world.Engine.CallsTo("SetDescription").Should().ContainSingle();
        _world.Logs.Should().ContainSingle().Which.Template.Should().StartWith("Disguise reapplied on login");
    }

    // ---- deactivating

    [Test]
    public void Deactivating_restores_the_snapshot_and_clears_it()
    {
        var disguise = Create();
        Disguises.Activate(Owner, disguise.Id);
        _world.Engine.ClearCalls();

        Disguises.Deactivate(Owner).Should().BeTrue();

        var player = _world.DbPlayer;
        player.ActiveDisguiseId.Should().BeEmpty();
        player.UndisguisedPortraitId.Should().Be(-1);
        player.UndisguisedPortraitResref.Should().BeEmpty();
        player.UndisguisedSoundSetId.Should().Be(-1);
        player.UndisguisedDescription.Should().BeEmpty();
        player.HasUndisguisedDescriptionSnapshot.Should().BeFalse();
        _world.Engine.CallsTo("SetPortraitResRef").Single().Arguments[1].Should().Be("baseportrait");
        _world.Engine.CallsTo("SetSoundset").Single().Arguments[1].Should().Be(10);
        _world.Engine.CallsTo("SetDescription").Single().Arguments[1].Should().Be("Current description");
        _world.NameRefreshes.Should().Contain(Owner);
    }

    [Test]
    public void Deactivating_without_a_resref_uses_the_portrait_id_and_without_a_snapshot_the_original_description()
    {
        var disguise = Create();
        Disguises.Activate(Owner, disguise.Id);
        _world.DbPlayer.UndisguisedPortraitResref = string.Empty;
        _world.DbPlayer.UndisguisedPortraitId = 12;
        _world.DbPlayer.HasUndisguisedDescriptionSnapshot = false;
        _world.Engine.ClearCalls();

        Disguises.Deactivate(Owner);

        _world.Engine.CallsTo("SetPortraitResRef").Should().BeEmpty();
        _world.Engine.CallsTo("SetPortraitId").Single().Arguments[1].Should().Be(12);
        _world.Engine.CallsTo("SetDescription").Single().Arguments[1].Should().Be("Original description");
    }

    [Test]
    public void Deactivating_with_nothing_saved_changes_no_portrait_or_sound()
    {
        var disguise = Create();
        Disguises.Activate(Owner, disguise.Id);
        _world.DbPlayer.UndisguisedPortraitResref = string.Empty;
        _world.DbPlayer.UndisguisedPortraitId = -1;
        _world.DbPlayer.UndisguisedSoundSetId = -1;
        _world.Engine.ClearCalls();

        Disguises.Deactivate(Owner);

        _world.Engine.CallsTo("SetPortraitId").Should().BeEmpty();
        _world.Engine.CallsTo("SetPortraitResRef").Should().BeEmpty();
        _world.Engine.CallsTo("SetSoundset").Should().BeEmpty();
    }

    [Test]
    public void Deactivating_with_no_active_disguise_or_no_record_is_refused()
    {
        Disguises.Deactivate(Owner).Should().BeFalse();
        Disguises.Deactivate(DisguiseWorld.Staff).Should().BeFalse();

        _world.Logs.Should().HaveCount(2);
    }

    // ---- retiring and restoring

    [Test]
    public void Retiring_the_active_disguise_deactivates_it_first()
    {
        var disguise = Create();
        Disguises.Activate(Owner, disguise.Id);

        Disguises.Retire(Owner, disguise.Id).Should().BeTrue();

        disguise.IsRetired.Should().BeTrue();
        disguise.DateRetired.Should().Be(Disguises.Now);
        _world.DbPlayer.ActiveDisguiseId.Should().BeEmpty();
    }

    [Test]
    public void Retiring_and_restoring_are_refused_for_the_wrong_owner_or_state()
    {
        var own = Create();
        StoreDisguise("foreign", playerId: "other");
        StoreDisguise("retired", retired: true);

        Disguises.Retire(Owner, "foreign").Should().BeFalse();
        Disguises.Retire(Owner, "retired").Should().BeFalse();
        Disguises.Unretire(Owner, "foreign").Should().BeFalse();
        Disguises.Unretire(Owner, own.Id).Should().BeFalse("it is not retired");

        Disguises.Unretire(Owner, "retired").Should().BeTrue();
        _world.Db.Get<PlayerDisguise>("retired")!.IsRetired.Should().BeFalse();
        _world.Db.Get<PlayerDisguise>("retired")!.DateRetired.Should().BeNull();
    }

    // ---- deleting

    [Test]
    public void Deleting_a_retired_disguise_with_credits_takes_the_credits_and_forgets_the_name()
    {
        var disguise = StoreDisguise("old", retired: true);

        Disguises.DeleteRetiredDisguise(Owner, "old", DisguisePaymentMethod.Credits).Should().Be(DeleteRetiredDisguiseResult.Success);

        _world.Engine.CallsTo("TakeGoldFromCreature").Single().Arguments[0].Should().Be(100000);
        _world.DeletedReferences.Should().Equal("disguise:old");
        _world.Db.Get<PlayerDisguise>(disguise.Id).Should().BeNull();
    }

    [Test]
    public void Deleting_with_roleplay_xp_spends_the_xp_and_refreshes_the_window()
    {
        StoreDisguise("old", retired: true);
        _world.DbPlayer.UnallocatedXP = 30000;

        Disguises.DeleteRetiredDisguise(Owner, "old", DisguisePaymentMethod.RoleplayXP).Should().Be(DeleteRetiredDisguiseResult.Success);

        _world.DbPlayer.UnallocatedXP.Should().Be(5000);
        _world.Refreshes.Select(r => r.Payload).Should().ContainSingle().Which.Should().BeOfType<RPXPRefreshEvent>();
    }

    [Test]
    public void Deleting_is_refused_without_payment_or_for_the_wrong_disguise()
    {
        StoreDisguise("old", retired: true);
        StoreDisguise("foreign", playerId: "other", retired: true);
        var live = StoreDisguise("live");
        var active = StoreDisguise("active", retired: true);
        _world.DbPlayer.ActiveDisguiseId = active.Id;

        Disguises.DeleteRetiredDisguise(Owner, "missing", DisguisePaymentMethod.Credits).Should().Be(DeleteRetiredDisguiseResult.NotFound);
        Disguises.DeleteRetiredDisguise(Owner, "foreign", DisguisePaymentMethod.Credits).Should().Be(DeleteRetiredDisguiseResult.NotOwner);
        Disguises.DeleteRetiredDisguise(Owner, live.Id, DisguisePaymentMethod.Credits).Should().Be(DeleteRetiredDisguiseResult.NotRetired);
        Disguises.DeleteRetiredDisguise(Owner, "active", DisguisePaymentMethod.Credits).Should().Be(DeleteRetiredDisguiseResult.IsActive);

        _world.Gold = 99999;
        Disguises.DeleteRetiredDisguise(Owner, "old", DisguisePaymentMethod.Credits).Should().Be(DeleteRetiredDisguiseResult.InsufficientCredits);

        _world.DbPlayer.UnallocatedXP = 24999;
        Disguises.DeleteRetiredDisguise(Owner, "old", DisguisePaymentMethod.RoleplayXP).Should().Be(DeleteRetiredDisguiseResult.InsufficientRoleplayXP);
        Disguises.DeleteRetiredDisguise(DisguiseWorld.Staff, "old", DisguisePaymentMethod.RoleplayXP).Should().Be(DeleteRetiredDisguiseResult.NotOwner);

        Disguises.DeleteRetiredDisguise(Owner, "old", (DisguisePaymentMethod)99).Should().Be(DeleteRetiredDisguiseResult.InvalidPaymentMethod);
        _world.Db.Get<PlayerDisguise>("old").Should().NotBeNull();
    }

    [Test]
    public void Paying_roleplay_xp_without_a_saved_player_is_refused()
    {
        StoreDisguise("old", playerId: "staff-id", retired: true);

        Disguises.DeleteRetiredDisguise(DisguiseWorld.Staff, "old", DisguisePaymentMethod.RoleplayXP)
            .Should().Be(DeleteRetiredDisguiseResult.InsufficientRoleplayXP);
    }

    // ---- staff cooldown reset

    [Test]
    public void Resetting_cooldowns_clears_every_activation_stamp_across_pages_and_counts_them()
    {
        StoreDisguise("a", lastActivated: Disguises.Now.AddMinutes(-3));
        StoreDisguise("b", lastActivated: Disguises.Now.AddMinutes(-9));
        StoreDisguise("c");

        Disguises.ResetActivationCooldowns(Owner).Should().Be(2);

        _world.Db.Get<PlayerDisguise>("a")!.DateLastActivated.Should().BeNull();
        _world.Logs.Should().HaveCount(2);
        _world.Logs[0].Values.Last().Should().Be("Canonical Player", "without an actor the player is the actor");
    }

    [Test]
    public void Resetting_cooldowns_walks_every_page()
    {
        var db = ScriptedProxy.Create<SWLOR.Game.Server.Domain.Foundation.IDBService>(out var script);
        var pages = new Queue<List<PlayerDisguise>>();
        pages.Enqueue(Enumerable.Range(0, 50).Select(i => new PlayerDisguise { Id = "a" + i, DateLastActivated = DateTime.UtcNow }).ToList());
        pages.Enqueue(new List<PlayerDisguise> { new() { Id = "last", DateLastActivated = DateTime.UtcNow } });
        script.On("Search", _ => pages.Dequeue()).On("Set", _ => null);
        var service = new DisguiseWorld.BenchDisguises(db, _world.Cache, _world.Gui, _world.DmPresence, _world.PlayerName,
            _world.Descriptors, _world.Logs);

        service.ResetActivationCooldowns(Owner, DisguiseWorld.Staff).Should().Be(51);
    }

    // ---- portraits and sounds

    [Test]
    public void The_portrait_resref_of_a_disguise_is_the_large_variant_or_empty()
    {
        Disguises.GetPortraitResref(null!).Should().BeEmpty();
        Disguises.GetPortraitResref(new PlayerDisguise { PortraitInternalId = 5 }).Should().Be("portrait5l");

        _world.CacheScript.On("GetPortraitResrefByInternalId", _ => throw new KeyNotFoundException());
        Disguises.GetPortraitResref(new PlayerDisguise { PortraitInternalId = 5 }).Should().BeEmpty();
    }

    [Test]
    public void A_sound_set_has_its_label_or_Unknown()
    {
        Disguises.GetSoundSetName(21).Should().Be("Twenty-one");
        Disguises.GetSoundSetName(5).Should().Be("Unknown");
    }

    // ---- the facade

    [Test]
    public void The_facade_forwards_every_member_and_repeats_the_constants()
    {
        using var container = new FacadeContainer(services =>
            Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.Replace(
                services,
                Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton<IDisguiseService>(Disguises)));
        using var scope = container.Install();

        DisguiseService.IdentityKeyPrefix.Should().Be("disguise:");
        DisguiseService.WipeCreditCost.Should().Be(100000);
        DisguiseService.WipeRoleplayXPCost.Should().Be(25000);
        DisguiseService.ActivationDelayMinutes.Should().Be(30);
        DisguiseService.MinimumActivationDelayMinutes.Should().Be(5);
        DisguiseService.MaxPrivateNameLength.Should().Be(32);
        DisguiseService.MaxBiographyLength.Should().Be(5000);

        var disguise = Disguise.CreateDisguise(Owner)!;
        Disguise.CountUsedSlots(DisguiseWorld.OwnerId).Should().Be(1);
        Disguise.GetDisguises(DisguiseWorld.OwnerId, false).Should().ContainSingle();
        Disguise.SaveDisguise(Owner, disguise.Id, "Mask", "Hooded", "Bio", 20, 21, true).IsSuccessful.Should().BeTrue();
        Disguise.Activate(Owner, disguise.Id).IsSuccessful.Should().BeTrue();
        Disguise.GetActiveDisguise(Owner)!.Id.Should().Be(disguise.Id);
        Disguise.GetActiveDisguise(_world.DbPlayer)!.Id.Should().Be(disguise.Id);
        Disguise.GetIdentityKey(Owner).Should().Be(Disguise.BuildIdentityKey(disguise.Id));
        Disguise.IsDisguiseIdentityKey("disguise:x").Should().BeTrue();
        Disguise.GetDisplayDescriptor(Owner).Should().Be("Hooded");
        Disguise.ShouldScrambleAccountName(Owner).Should().BeTrue();
        Disguise.GetDisguiseSlotLimit(Owner, _world.DbPlayer).Should().Be(3);
        Disguise.CalculateDisguiseSlotLimit(1, 1).Should().Be(2);
        Disguise.GetActivationDelay(Owner).Should().Be(TimeSpan.FromMinutes(30));
        Disguise.CalculateActivationDelay(0).Should().Be(TimeSpan.FromMinutes(30));
        Disguise.GetPortraitResref(disguise).Should().Be("portrait20l");
        Disguise.GetSoundSetName(21).Should().Be("Twenty-one");
        Disguise.ResetActivationCooldowns(Owner, DisguiseWorld.Staff).Should().Be(1);
        Disguise.Deactivate(Owner).Should().BeTrue();
        Disguise.Retire(Owner, disguise.Id).Should().BeTrue();
        Disguise.Unretire(Owner, disguise.Id).Should().BeTrue();
        Disguise.Retire(Owner, disguise.Id).Should().BeTrue();
        Disguise.DeleteRetiredDisguise(Owner, disguise.Id, DisguisePaymentMethod.Credits).Should().Be(DeleteRetiredDisguiseResult.Success);
        _world.Entering = DisguiseWorld.Dm;
        Disguise.ApplyActiveDisguiseOnEnter();
    }
}
