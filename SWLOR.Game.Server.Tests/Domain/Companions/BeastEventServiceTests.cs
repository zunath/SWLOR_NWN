using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Composition.GameServices;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>
/// Unit tests of the beast event handlers: the despawn, the item check, the twelve beast scripts and the stables terminal. They run on the
/// world of <see cref="BeastTestBase"/>, where <c>_service.Events</c> is the real <see cref="BeastEventService"/> and the calls into the
/// companion control, the stat service, the status effects and the windows are answered by <see cref="TestBeastEventService"/>.
/// </summary>
[TestFixture]
[NonParallelizable]
public class BeastEventServiceTests : BeastTestBase
{
    // ---- creature event handlers

    [Test]
    public void Leaving_the_party_or_entering_space_despawns_the_beast()
    {
        _henchman = BeastObject;
        _engine.SetReturn("OBJECT_SELF", Player);

        _service.Events.RemoveAssociate();

        _service.Calls.Should().Equal($"Clear {BeastObject}");
        _engine.CallsTo("DestroyObject").Single().Arguments[0].Should().Be(BeastObject);
    }

    [Test]
    public void A_beast_may_pick_up_its_own_natural_weapons()
    {
        ActiveBeast();
        _engine.SetReturn("GetModuleItemAcquiredBy", BeastObject);
        _engine.SetReturn("GetModuleItemAcquired", ItemObject);

        foreach (var type in new[]
                 {
                     BaseItem.CreatureBludgeonWeapon, BaseItem.CreaturePierceWeapon, BaseItem.CreatureSlashPierceWeapon,
                     BaseItem.CreatureSlashWeapon, BaseItem.CreatureItem
                 })
        {
            _engine.SetReturn("GetBaseItemType", type);

            _service.Events.OnAcquireItem();
        }

        Messages().Should().BeEmpty();
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_beast_that_picks_up_another_item_gives_it_back_to_its_master()
    {
        ActiveBeast();
        _engine.SetReturn("GetModuleItemAcquiredBy", BeastObject);
        _engine.SetReturn("GetModuleItemAcquired", ItemObject);
        _engine.SetReturn("GetBaseItemType", BaseItem.Longsword);

        _service.Events.OnAcquireItem();

        Messages().Should().Equal("Beasts cannot hold items.");
        _service.Calls.Should().Equal($"ReturnItem {Player} {ItemObject}");
        _engine.CallsTo("AssignCommand").Single().Arguments[0].Should().Be(BeastObject);
        RunScheduledCommands();
        _engine.CountOf("ClearAllActions").Should().Be(1);
    }

    [Test]
    public void A_creature_that_is_not_a_player_beast_may_acquire_anything()
    {
        _engine.SetReturn("GetModuleItemAcquiredBy", Player);
        _engine.SetReturn("GetModuleItemAcquired", ItemObject);
        _engine.SetReturn("GetBaseItemType", BaseItem.Longsword);

        _service.Events.OnAcquireItem();

        Messages().Should().BeEmpty();
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_beast_blocked_by_a_door_runs_the_henchman_block_script()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnBlocked();

        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("x0_ch_hen_block", BeastObject);
    }

    [Test]
    public void A_beast_runs_the_other_henchman_scripts_for_the_events_the_companion_control_does_not_own()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnDisturbed();
        _service.Events.BeastOnUserDefined();

        _engine.CallsTo("ExecuteScript").Select(call => (string)call.Arguments[0]!)
            .Should().Equal("x0_ch_hen_distrb", "x0_ch_hen_usrdef");
    }

    [Test]
    public void A_beast_ending_a_combat_round_lets_the_companion_control_run_it()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnEndCombatRound();

        _service.Calls.Should().Equal($"CombatRound {BeastObject}");
    }

    [Test]
    public void A_beast_falls_back_to_the_henchman_conversation_when_the_companion_control_does_not_handle_it()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.ConversationHandled = true;
        _service.Events.BeastOnConversation();
        _engine.CallsTo("ExecuteScript").Should().BeEmpty();

        _service.Events.ConversationHandled = false;
        _service.Events.BeastOnConversation();
        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("x0_ch_hen_conv", BeastObject);
        _service.Calls.Should().Equal($"Conversation {BeastObject}", $"Conversation {BeastObject}");
    }

    [Test]
    public void A_damaged_or_attacked_beast_registers_the_threat()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);
        _engine.SetReturnHandler("GetLastDamager", _ => Damager);
        _engine.SetReturnHandler("GetLastAttacker", _ => Killer);

        _service.Events.BeastOnDamaged();
        _service.Events.BeastOnPhysicalAttacked();

        _service.Calls.Should().Equal($"Threat {BeastObject} {Damager}", $"Threat {BeastObject} {Killer}");
    }

    [Test]
    public void A_beast_only_defends_against_harmful_spells()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);
        _engine.SetReturn("GetLastSpellCaster", Damager);

        _engine.SetReturn("GetLastSpellHarmful", false);
        _service.Events.BeastOnSpellCastAt();
        _service.Calls.Should().BeEmpty();

        _engine.SetReturn("GetLastSpellHarmful", true);
        _service.Events.BeastOnSpellCastAt();
        _service.Calls.Should().Equal($"Threat {BeastObject} {Damager}");
    }

    [Test]
    public void A_beast_heartbeat_restores_the_beast_stats_then_runs_the_companion_control()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnHeartbeat();

        _service.Calls.Should().Equal("RestoreBeastStats", $"Heartbeat {BeastObject}");
    }

    [Test]
    public void A_resting_beast_stops_what_it_is_doing_and_gets_the_rest_effect()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnRested();

        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("x0_ch_hen_rest", BeastObject);
        _service.Calls.Should().Equal($"Rest {BeastObject}");
        _engine.CallsTo("AssignCommand").Single().Arguments[0].Should().Be(BeastObject);
    }

    [Test]
    public void A_beast_that_spawns_is_made_destroyable_and_handed_to_the_companion_control()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnSpawn();
        RunScheduledCommands();

        _service.Calls.Should().Equal("LoadNPCStats", $"MovementRate {BeastObject}", $"Initialize {BeastObject}");
        _engine.CallsTo("SetIsDestroyable").Single().Arguments.Take(3).Should().Equal(true, false, false);
    }

    [Test]
    public void A_dying_beast_is_recorded_on_its_master_and_marked_dead()
    {
        var stored = ActiveBeast();
        _engine.SetReturn("OBJECT_SELF", BeastObject);
        _engine.SetReturn("GetLastKiller", Killer);

        _service.Events.BeastOnDeath();

        _service.Calls.Should().Equal($"RecordDeath {Player} {BeastId} {Killer}", $"Clear {BeastObject}");
        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("x2_hen_death", BeastObject);
        stored.IsDead.Should().BeTrue();
        _db.Store.Saved.Should().ContainSingle().Which.Should().BeSameAs(stored);
    }

    [Test]
    public void A_beast_with_no_killer_is_recorded_against_the_last_hostile_actor()
    {
        ActiveBeast();
        _engine.SetReturn("OBJECT_SELF", BeastObject);
        _engine.SetReturn("GetLastKiller", ObjectInvalid);
        _engine.SetReturnHandler("GetLastHostileActor", _ => Damager);

        _service.Events.BeastOnDeath();

        _service.Calls[0].Should().Be($"RecordDeath {Player} {BeastId} {Damager}");
    }

    [Test]
    public void A_dying_beast_with_no_entity_leaves_the_database_alone()
    {
        _service.Beasts.SetBeastId(BeastObject, "missing");
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnDeath();

        _service.Calls.Should().Contain($"Clear {BeastObject}");
        _db.Store.Saved.Should().BeEmpty();
    }

    // ---- terminals

    [Test]
    public void A_player_using_the_stables_terminal_opens_the_stables_window()
    {
        _engine.SetReturn("GetLastUsedBy", Player);
        _engine.SetReturn("OBJECT_SELF", ItemObject);

        _service.Events.OpenStablesMenu();

        _service.Calls.Should().Equal($"Window {Player} {GuiWindowType.Stables} {ItemObject}");
        _service.Payload.Should().BeNull();
        Messages().Should().BeEmpty();
    }

    [Test]
    public void The_stables_terminal_is_for_players_only()
    {
        _engine.SetReturn("GetLastUsedBy", Npc);

        _service.Events.OpenStablesMenu();

        Messages().Should().ContainSingle().Which.Should().Contain("Only players may use this terminal.");
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_dm_or_a_possessing_dm_may_not_use_the_stables_terminal()
    {
        _engine.SetReturn("GetLastUsedBy", Player);
        _isDM = true;
        _service.Events.OpenStablesMenu();

        _isDM = false;
        _isPossessed = true;
        _service.Events.OpenStablesMenu();

        Messages().Should().HaveCount(2);
        _service.Calls.Should().BeEmpty();
    }

    // ---- the seams

    private BeastEventService PlainEvents(List<string> calls) => new(
        _db,
        RecordingServiceProxy.For<ICompanionControlService>("Control", calls),
        RecordingServiceProxy.For<ICreatureStatService>("CreatureStat", calls),
        RecordingServiceProxy.For<IStatusEffectApplicationService>("StatusEffect", calls),
        RecordingServiceProxy.For<IItemService>("Item", calls),
        RecordingServiceProxy.For<IGuiService>("Gui", calls),
        _service);

    [Test]
    public void The_beast_event_seams_forward_to_the_companion_control_the_stat_service_and_the_status_effects()
    {
        ActiveBeast();
        var calls = new List<string>();
        var events = PlainEvents(calls);
        _engine.SetReturn("OBJECT_SELF", BeastObject);
        _engine.SetReturnHandler("GetLastDamager", _ => Damager);
        _engine.SetReturnHandler("GetMaster", _ => ObjectInvalid);

        events.BeastOnSpawn();
        events.BeastOnEndCombatRound();
        events.BeastOnHeartbeat();
        events.BeastOnConversation();
        events.BeastOnDamaged();
        events.BeastOnRested();
        events.BeastOnDeath();

        calls.Should().Equal(
            "CreatureStat.LoadNPCStats()",
            $"CreatureStat.ApplyCreatureMovementRate({BeastObject})",
            $"Control.Initialize({BeastObject}, False)",
            $"Control.ProcessCombatRound({BeastObject}, False)",
            "CreatureStat.RestoreBeastStats()",
            $"Control.ProcessHeartbeat({BeastObject})",
            $"Control.HandleConversation({BeastObject})",
            $"Control.RegisterDefensiveThreat({BeastObject}, {Damager})",
            $"StatusEffect.ApplyStatusEffect({BeastObject}, {BeastObject}, RestStatusEffect, 0, Invalid)",
            $"Control.Clear({BeastObject})");
    }

    [Test]
    public void The_beast_event_seams_forward_the_despawn_the_returned_item_and_the_window()
    {
        ActiveBeast();
        var calls = new List<string>();
        var events = PlainEvents(calls);
        _engine.SetReturn("OBJECT_SELF", Player);
        events.RemoveAssociate();

        _engine.SetReturn("GetModuleItemAcquiredBy", BeastObject);
        _engine.SetReturn("GetModuleItemAcquired", ItemObject);
        _engine.SetReturn("GetBaseItemType", BaseItem.Longsword);
        events.OnAcquireItem();

        _engine.SetReturn("GetLastUsedBy", Player);
        _engine.SetReturn("OBJECT_SELF", ItemObject);
        events.OpenStablesMenu();

        calls.Should().Equal(
            $"Control.Clear({BeastObject})",
            $"Item.ReturnItem({Player}, {ItemObject})",
            $"Gui.TogglePlayerWindow({Player}, Stables, , {ItemObject}, {ObjectInvalid})");
    }

    // ---- the container

    [Test]
    public void The_container_builds_the_event_part_from_the_converted_services_it_needs()
    {
        using var container = new FacadeContainer();

        container.Get<IBeastEventService>().Should().BeOfType<BeastEventService>();
    }
}
