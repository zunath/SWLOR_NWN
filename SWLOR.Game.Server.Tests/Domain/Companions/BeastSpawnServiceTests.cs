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
/// Unit tests of calling a beast: the one-companion check, the spawn with its name, appearance, stats, feats and event scripts,
/// and the delayed hit point correction. They run on the world of <see cref="BeastTestBase"/>, where
/// <c>_service.Spawn</c> is the real <see cref="BeastSpawnService"/>.
/// </summary>
[TestFixture]
[NonParallelizable]
public class BeastSpawnServiceTests : BeastTestBase
{
    // ---- companion slot

    [Test]
    public void A_player_without_a_companion_may_spawn_one()
    {
        _service.Spawn.GetCompanionSlotValidationError(Player).Should().BeEmpty();
    }

    [Test]
    public void A_player_with_a_companion_may_not_spawn_another()
    {
        _henchman = BeastObject;

        _service.Spawn.GetCompanionSlotValidationError(Player).Should().Be("Only one companion may be active at a time.");
    }

    // ---- spawning

    [Test]
    public void Spawning_a_beast_is_refused_while_a_companion_is_out()
    {
        _henchman = BeastObject;

        _service.Spawn.SpawnBeast(Player, BeastId, 100);

        Messages().Should().Equal("Only one companion may be active at a time.");
        _engine.CallsTo("CreateObject").Should().BeEmpty();
    }

    [Test]
    public void Spawning_a_beast_that_is_not_in_the_database_tells_the_player()
    {
        _service.Spawn.SpawnBeast(Player, "missing", 100);

        Messages().Should().Equal("Unable to locate beast in DB. Notify an admin.");
        _engine.CallsTo("CreateObject").Should().BeEmpty();
    }

    [Test]
    public void Spawning_a_beast_creates_it_names_it_and_makes_it_the_henchman()
    {
        StoredBeast(level: 2);
        _engine.SetReturnHandler("GetEventScript", _ => "beast_spawn");

        _service.Spawn.SpawnBeast(Player, BeastId, 100);

        var spawn = _engine.CallsTo("CreateObject").Single();
        spawn.Arguments[0].Should().Be(ObjectType.Creature);
        spawn.Arguments[1].Should().Be("pc_beast");
        var beast = FirstCreatedObject;
        _engine.CallsTo("SetName").Single().Arguments.Should().Equal(beast, "Rex");
        _locals.String(beast, "BEAST_ID").Should().Be(BeastId);
        _locals.Int(beast, "BEAST_TYPE").Should().Be((int)BeastType.Aradile);
        _engine.CallsTo("SetCreatureAppearanceType").Single().Arguments.Should().Equal(beast, AppearanceType.LizardGiantBrownCCP);
        _engine.CallsTo("SetObjectVisualTransform").Single().Arguments.Take(3).Should().Equal(beast, ObjectVisualTransform.Scale, 1.5f);
        _engine.CallsTo("AddHenchman").Single().Arguments.Should().Equal(Player, beast);
    }

    [Test]
    public void A_beast_uses_the_portrait_and_sound_set_of_its_species_unless_its_owner_picked_others()
    {
        StoredBeast(level: 1);
        _service.Spawn.SpawnBeast(Player, BeastId, 100);
        _engine.CallsTo("SetPortraitId").Single().Arguments[1].Should().Be(4279);
        _engine.CallsTo("SetSoundset").Single().Arguments[1].Should().Be(442);

        _locals.ClearCalls();
        _henchman = 0;
        var custom = StoredBeast(level: 1);
        custom.PortraitId = 12;
        custom.SoundSetId = 13;
        _service.Spawn.SpawnBeast(Player, BeastId, 100);

        _engine.CallsTo("SetPortraitId").Single().Arguments[1].Should().Be(12);
        _engine.CallsTo("SetSoundset").Single().Arguments[1].Should().Be(13);
    }

    [Test]
    public void A_spawned_beast_gets_its_stats_its_ai_profile_and_the_feats_of_its_perks()
    {
        var stored = StoredBeast(level: 2);
        stored.Perks[PerkType.Tame] = 3;
        stored.Perks[PerkType.DNAManipulation] = 1;

        _service.Spawn.SpawnBeast(Player, BeastId, 100);

        var beast = FirstCreatedObject;
        Added(Skin).Should().Contain((ItemPropertyType.NPCLevel, -1, 2));
        _service.Calls.Should().Contain($"SetNPCMaxHitPoints {beast} 200");
        _service.Calls.Should().Contain($"SyncGrantedFeats {beast} {PerkType.Tame} 3 False");
        _service.Calls.Should().Contain($"SyncGrantedFeats {beast} {PerkType.DNAManipulation} 1 False");
        _locals.String(beast, "AI_PROFILE").Should().NotBeEmpty();
    }

    [Test]
    public void A_spawned_beast_runs_the_beast_scripts_and_the_spawn_script_runs_once()
    {
        StoredBeast(level: 1);
        _engine.SetReturnHandler("GetEventScript", _ => "beast_spawn_script");

        _service.Spawn.SpawnBeast(Player, BeastId, 100);

        var beast = FirstCreatedObject;
        var scripts = _engine.CallsTo("SetEventScript").Select(call => (EventScript)call.Arguments[1]!).ToList();
        scripts.Should().BeEquivalentTo(new[]
        {
            EventScript.Creature_OnBlockedByDoor, EventScript.Creature_OnEndCombatRound, EventScript.Creature_OnDialogue,
            EventScript.Creature_OnDamaged, EventScript.Creature_OnDeath, EventScript.Creature_OnDisturbed,
            EventScript.Creature_OnHeartbeat, EventScript.Creature_OnMeleeAttacked, EventScript.Creature_OnRested,
            EventScript.Creature_OnSpawnIn, EventScript.Creature_OnSpellCastAt, EventScript.Creature_OnUserDefined
        });
        _engine.CallsTo("SetEventScript").Select(call => (string)call.Arguments[2]!).Should().Contain(new[]
        {
            ScriptName.OnBeastBlocked, ScriptName.OnBeastRoundEnd, ScriptName.OnBeastConversation, ScriptName.OnBeastDamaged,
            ScriptName.OnBeastDeath, ScriptName.OnBeastDisturbed, ScriptName.OnBeastHeartbeat, ScriptName.OnBeastAttacked,
            ScriptName.OnBeastRest, ScriptName.OnBeastSpawn, ScriptName.OnBeastSpellCast, ScriptName.OnBeastUserDefined
        });
        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("beast_spawn_script", beast);
    }

    [Test]
    public void A_beast_that_is_called_at_full_health_is_restored_before_the_delayed_correction()
    {
        StoredBeast(level: 1);
        _engine.SetReturn("GetMaxHitPoints", 150);

        _service.Spawn.SpawnBeast(Player, BeastId, 100);

        var beast = FirstCreatedObject;
        var calls = _engine.Calls.Select(call => call.Member).ToList();
        calls.Should().Contain("SetCurrentHitPoints");
        _engine.CallsTo("SetCurrentHitPoints").Single().Arguments.Should().Equal(beast, 150);
        calls.IndexOf("SetCurrentHitPoints").Should().BeLessThan(calls.IndexOf("AssignCommand"),
            "the full-health branch restores the beast at once, before the delayed correction is scheduled");
        _engine.CallsTo("DelayCommand").Should().BeEmpty("the delay is scheduled from inside the command assigned to the module");
        _engine.CallsTo("AssignCommand").Single().Arguments[0].Should().Be(Module);
    }

    [Test]
    public void A_beast_called_below_full_health_waits_for_the_delayed_correction()
    {
        StoredBeast(level: 1);
        _engine.SetReturn("GetMaxHitPoints", 150);

        _service.Spawn.SpawnBeast(Player, BeastId, 50);

        _engine.CallsTo("SetCurrentHitPoints").Should().BeEmpty("only a full-health spawn is restored at once");
        RunScheduledCommands();
        _engine.CallsTo("DelayCommand").Single().Arguments[0].Should().Be(4f);
        _engine.CallsTo("SetCurrentHitPoints").Single().Arguments.Should().Equal(FirstCreatedObject, 1);
        _engine.CallsTo("EffectHeal").Single().Arguments.Should().Equal((int)(150 * (50 * 0.01f)));
        var heal = _engine.CallsTo("ApplyEffectToObject").Single();
        heal.Arguments[0].Should().Be(DurationType.Instant);
        heal.Arguments[2].Should().Be(FirstCreatedObject);
    }

    [Test]
    public void A_beast_spawned_with_no_heal_is_left_at_one_hit_point_after_the_delay()
    {
        StoredBeast(level: 1);
        _engine.SetReturn("GetMaxHitPoints", 150);

        _service.Spawn.SpawnBeast(Player, BeastId, 0);
        RunScheduledCommands();

        _engine.CallsTo("SetCurrentHitPoints").Single().Arguments.Should().Equal(FirstCreatedObject, 1);
        _engine.CallsTo("EffectHeal").Should().BeEmpty();
        _engine.CallsTo("ApplyEffectToObject").Should().BeEmpty();
    }

}
