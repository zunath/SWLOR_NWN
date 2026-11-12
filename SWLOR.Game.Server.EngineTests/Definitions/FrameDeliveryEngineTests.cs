using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>
/// Frame equivalence of the two entries (P4-10). The native entry (<c>ServerManager.Bootstrap</c>) runs its frame work
/// (<see cref="Scheduler"/>, <c>NwTask</c>) inside NWNX's main-loop callback with <c>OBJECT_SELF</c> invalid, from the first
/// frame, and NWNX refuses a closure aimed at that invalid object, which is then dropped without an exception. The host
/// entry (<c>SWLOR.Game.Host</c>) must do the same: the package's inline frame delivery and dropped rejected closures.
/// The probes record what scheduled work sees, at boot (<c>mod_load</c>, started by <see cref="EngineTest"/>) and during
/// the run; the tests pass on both entries, and the boot test's detail line gives the frame numbers, which match between
/// the two entries' runs.
/// </summary>
public static class FrameDeliveryEngineTests
{
    private static readonly Probe BootProbe = new();

    /// <summary>
    /// Schedules the boot probe's work. <see cref="EngineTest.ScheduleEngineTests"/> calls it from <c>mod_load</c>, while
    /// the module finishes loading.
    /// </summary>
    internal static void StartBootProbe() => BootProbe.Schedule();

    [EngineTest("Boot-time scheduled work runs on the next frame with OBJECT_SELF invalid, and its DelayCommand is dropped", Category = "FrameDelivery", TimeoutSeconds = 30f)]
    public static async Task BootScheduledWorkMatchesTheNativeEntry(EngineTestContext ctx)
    {
        ctx.Assert(BootProbe.Scheduled, "The engine-test boot handler scheduled the probe's work at mod_load.");
        ctx.Assert(BootProbe.Ran, $"The work scheduled at mod_load (frame {BootProbe.ScheduledFrame}) ran.");

        // The refused closure would have run within a frame or two; give it far longer.
        await ctx.DelaySecondsAsync(1f);

        BootProbe.AssertMatchesTheNativeEntry(ctx, "the work scheduled at mod_load");
        ctx.SetResultDetail(
            $"mod_load ran at frame {BootProbe.ScheduledFrame} (the frame NWNX last delivered before the module finished loading); " +
            $"its scheduled work ran at frame {BootProbe.RunFrame} with OBJECT_SELF invalid; DelayCommand on OBJECT_SELF dropped without an exception.");
    }

    [EngineTest("Scheduled work during the run sees OBJECT_SELF invalid, its DelayCommand is dropped, and a module closure still runs", Category = "FrameDelivery", TimeoutSeconds = 30f)]
    public static async Task RunTimeScheduledWorkMatchesTheNativeEntry(EngineTestContext ctx)
    {
        var probe = new Probe();
        var moduleClosureRan = false;
        probe.Schedule();
        AssignCommand(GetModule(), () => DelayCommand(0.1f, () => moduleClosureRan = true));

        await ctx.WaitUntilAsync(() => probe.Ran && moduleClosureRan, 10f, "the scheduled work and the module closure");
        await ctx.DelaySecondsAsync(1f);

        probe.AssertMatchesTheNativeEntry(ctx, "scheduled work");
        ctx.SetResultDetail(
            $"Work scheduled at frame {probe.ScheduledFrame} ran at frame {probe.RunFrame} with OBJECT_SELF invalid; its DelayCommand was dropped; " +
            "a closure assigned to the module ran.");
    }

    /// <summary>Schedules one <see cref="Scheduler"/> item and records what it sees when it runs.</summary>
    private sealed class Probe
    {
        public bool Scheduled { get; private set; }
        public ulong ScheduledFrame { get; private set; }
        public bool Ran { get; private set; }
        public ulong RunFrame { get; private set; }
        public uint ObjectSelf { get; private set; }
        public string DelayException { get; private set; }
        public bool DelayedRan { get; private set; }

        public void Schedule()
        {
            if (Scheduled)
                return;

            Scheduled = true;
            ScheduledFrame = ServerFrame.Current;
            Scheduler.Schedule(Run, TimeSpan.Zero);
        }

        public void AssertMatchesTheNativeEntry(EngineTestContext ctx, string what)
        {
            ctx.Assert(Ran, $"{what} ran.");
            ctx.AssertEqual(OBJECT_INVALID, ObjectSelf, $"OBJECT_SELF seen by {what}");
            ctx.AssertEqual(null, DelayException, $"Exception from DelayCommand on OBJECT_SELF in {what}");
            ctx.Assert(!DelayedRan, $"The DelayCommand on OBJECT_SELF from {what} never ran: NWNX refused it.");
            ctx.Assert(RunFrame >= ScheduledFrame && RunFrame - ScheduledFrame <= 1,
                $"{what} was scheduled at frame {ScheduledFrame} and ran at frame {RunFrame}: frame work runs on the first frame " +
                "after it is scheduled.");
        }

        private void Run()
        {
            Ran = true;
            RunFrame = ServerFrame.Current;
            ObjectSelf = OBJECT_SELF;
            try
            {
                DelayCommand(0f, () => DelayedRan = true);
            }
            catch (Exception exception)
            {
                DelayException = exception.GetType().Name + ": " + exception.Message;
            }
        }
    }
}
