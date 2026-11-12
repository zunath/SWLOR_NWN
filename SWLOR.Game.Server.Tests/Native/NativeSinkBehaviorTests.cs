using System.Reflection;
using System.Runtime.InteropServices;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Tests.Native;

/// <summary>
/// P4-9: what a sink does without an engine. A hook must never let a managed failure escape into the engine and must
/// run the engine function exactly as before, so these call the sinks with null pointers (every engine read fails) and a
/// stand-in for the engine function, and check what runs and what comes back.
/// </summary>
[TestFixture]
public unsafe class NativeSinkBehaviorTests
{
    private static int _calls;

    [SetUp]
    public void ResetCalls() => _calls = 0;

    [UnmanagedCallersOnly]
    private static int EngineReturns7(void* a, void* b)
    {
        _calls++;
        return 7;
    }

    [UnmanagedCallersOnly]
    private static int EngineReturns9WithFourPointersAndNumber(void* a, void* b, void* c, void* d, int e)
    {
        _calls++;
        return 9;
    }

    [UnmanagedCallersOnly]
    private static int EngineChatLine(void* message, uint recipient, uint speaker, void* text)
    {
        _calls++;
        return 5;
    }

    [UnmanagedCallersOnly]
    private static uint EngineValidates(void* server, void* client, void* data, uint size)
    {
        _calls++;
        return 1234;
    }

    [UnmanagedCallersOnly]
    private static uint EngineJump(void* creature, void* node)
    {
        _calls++;
        return 3;
    }

    [UnmanagedCallersOnly]
    private static int EngineDrive(void* creature, ushort group, void* start, int bearing, int path, int flags, int generate)
    {
        _calls++;
        return 11;
    }

    [UnmanagedCallersOnly]
    private static short EngineMaximumHitPoints(void* creature, int includeToughness)
    {
        _calls++;
        return 321;
    }

    [UnmanagedCallersOnly]
    private static sbyte EngineModifier(void* stats, byte score)
    {
        _calls++;
        return -2;
    }

    [UnmanagedCallersOnly]
    private static void EngineBaseScore(void* stats, byte score, int recalculate) => _calls++;

    [UnmanagedCallersOnly]
    private static void EngineQuickButtons(void* creature, void* resource, void* structure) => _calls++;

    [UnmanagedCallersOnly]
    private static int EngineSave(void* creature, void* resource, void* structure, int a, int b, int c, int d)
    {
        _calls++;
        return 1;
    }

    [UnmanagedCallersOnly]
    private static uint EngineReadStats(void* stats, void* resource, void* structure, void* appearance, int a, int b, int c, int d)
    {
        _calls++;
        return 0;
    }

    [UnmanagedCallersOnly]
    private static int EngineLoadItem(void* item, void* resource, void* structure, int a, int b)
    {
        _calls++;
        return 1;
    }

    [Test]
    public void Chat_line_runs_the_engine_function_even_when_the_tracker_fails()
    {
        delegate* unmanaged<void*, uint, uint, void*, int> engine = &EngineChatLine;

        new ChatPortraitSpeakersSink().Send(null, 1, 2, null, (nint)engine).Should().Be(5);
        _calls.Should().Be(1);
    }

    [Test]
    public void An_engine_rejection_of_a_new_character_is_returned_without_recording_anything()
    {
        delegate* unmanaged<void*, void*, void*, uint, uint> engine = &EngineValidates;

        new PlayerCreationSink().Validate(null, null, null, 0, (nint)engine).Should().Be(1234u);
        _calls.Should().Be(1);
    }

    [Test]
    public void A_saved_stack_comparison_is_refused_only_while_a_player_file_loads()
    {
        delegate* unmanaged<void*, void*, int> engine = &EngineReturns7;
        var sink = new SavedPlayerInventorySink();
        sink.CompareItem(null, null, (nint)engine).Should().Be(7, "ordinary gameplay merging is the engine's");
        _calls.Should().Be(1);

        var inventory = typeof(SWLOR.Game.Server.Native.ChatPortraitSpeakers).Assembly.GetType("SWLOR.Game.Server.Native.SavedPlayerInventory")!;
        var scopeType = inventory.GetNestedType("LoadScope", BindingFlags.NonPublic)!;
        var scope = (IDisposable)Activator.CreateInstance(scopeType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [true], null)!;
        try
        {
            sink.CompareItem(null, null, (nint)engine).Should().Be(0, "a player file keeps each saved stack");
            _calls.Should().Be(1, "the engine function is not called while the file loads");
        }
        finally
        {
            scope.Dispose();
        }

        sink.CompareItem(null, null, (nint)engine).Should().Be(7);
        _calls.Should().Be(2);
    }

    [Test]
    public void A_jump_and_a_drive_path_still_reach_the_engine_when_the_creature_cannot_be_read()
    {
        delegate* unmanaged<void*, void*, uint> jump = &EngineJump;
        delegate* unmanaged<void*, ushort, void*, int, int, int, int, int> drive = &EngineDrive;
        var sink = new StaleDriveAfterJumpSink();

        sink.JumpToPoint(null, null, (nint)jump).Should().Be(3u);
        sink.AddDriveAction(null, 0, null, 0, 0, 0, 0, (nint)drive).Should().Be(11);
        _calls.Should().Be(2);
    }

    [Test]
    public void The_vitality_hooks_return_the_engine_values_when_the_creature_cannot_be_read()
    {
        delegate* unmanaged<void*, int, short> maximum = &EngineMaximumHitPoints;
        delegate* unmanaged<void*, byte, sbyte> modifier = &EngineModifier;
        delegate* unmanaged<void*, byte, int, void> setBase = &EngineBaseScore;
        var sink = new VitalityHitPointsSink();

        sink.GetMaximum(null, 1, (nint)maximum).Should().Be(321);
        sink.CalculateModifier(null, 10, (nint)modifier).Should().Be(-2);
        sink.SetBase(null, 10, 1, (nint)setBase);
        _calls.Should().Be(3);
    }

    [Test]
    public void Quickbar_loading_still_runs_the_engine_function_when_restoring_preferences_fails()
    {
        delegate* unmanaged<void*, void*, void*, void> engine = &EngineQuickButtons;

        new QuickbarFeatLoadingSink().LoadQuickButtons(null, null, null, (nint)engine);
        _calls.Should().Be(1);
    }

    [Test]
    public void A_character_file_that_cannot_be_read_fails_the_checkpoint_hooks_without_throwing()
    {
        delegate* unmanaged<void*, void*, void*, int, int, int, int, int> save = &EngineSave;
        delegate* unmanaged<void*, void*, void*, void*, int, int, int, int, uint> readStats = &EngineReadStats;
        var sink = new PlayerFileCheckpointSink();

        sink.Save(null, null, null, 0, 0, 0, 0, (nint)save).Should().Be(0, "a failed checkpoint write reports a failed save");
        sink.ReadStats(null, null, null, null, 0, 0, 0, 0, (nint)readStats).Should().Be(0u, "a creature that is not a player has no checkpoint to restore");
        sink.ReadStats(null, null, null, null, 0, 1, 0, 0, (nint)readStats).Should().Be(uint.MaxValue, "a failed restore reports the failure");
        _calls.Should().Be(3);
    }

    [Test]
    public void An_item_file_that_cannot_be_read_fails_the_load_without_calling_the_engine()
    {
        delegate* unmanaged<void*, void*, void*, int, int, int> engine = &EngineLoadItem;

        new LegacyItemPropertiesSink().LoadItem(null, null, null, 0, 0, (nint)engine).Should().Be(0);
        _calls.Should().Be(0);
    }
}
