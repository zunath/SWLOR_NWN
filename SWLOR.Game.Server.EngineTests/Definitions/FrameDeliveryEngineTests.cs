using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>
/// Frame equivalence of the two entries (P4-10). The native entry (<c>ServerManager.Bootstrap</c>) runs its frame work
/// (<see cref="Scheduler"/>, <c>NwTask</c>) inside NWNX's main-loop callback with <c>OBJECT_SELF</c> invalid, from the first
/// frame, and NWNX refuses a closure aimed at that invalid object, which is then dropped without an exception. The host
/// entry (<c>SWLOR.Game.Host</c>) must do the same: the package's inline frame delivery and dropped rejected closures.
/// The probes below record, at boot and again during the run, what scheduled work sees; the test passes on both entries,
/// and its detail line gives the boot frame numbers, which match between the two entries' runs.
/// </summary>
public static class FrameDeliveryEngineTests
{
    private static readonly BootProbe PreloadProbe = new("mod_preload");
    private static readonly BootProbe LoadProbe = new("mod_load");

    /// <summary>Scheduled at <c>mod_preload</c>, before the module starts loading: frames during the load run it.</summary>
    [NWNEventHandler(ScriptName.OnModulePreload)]
    public static void ScheduleFromModulePreload() => PreloadProbe.Schedule();

    /// <summary>Scheduled at <c>mod_load</c>, while the module finishes loading.</summary>
    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void ScheduleFromModuleLoad() => LoadProbe.Schedule();

    [EngineTest("Boot-time scheduled work runs on the next frame with OBJECT_SELF invalid, and its DelayCommand is dropped", Category = "FrameDelivery", TimeoutSeconds = 30f)]
    public static async Task BootScheduledWorkMatchesTheNativeEntry(EngineTestContext ctx)
    {
        foreach (var probe in new[] { PreloadProbe, LoadProbe })
        {
            ctx.Assert(probe.Scheduled, $"The {probe.Script} probe scheduled its work at boot.");
            ctx.Assert(probe.Ran, $"The work {probe.Script} scheduled at frame {probe.ScheduledFrame} ran.");
            ctx.AssertEqual(OBJECT_INVALID, probe.ObjectSelf, $"OBJECT_SELF seen by the work {probe.Script} scheduled");
            ctx.AssertEqual(null, probe.DelayException, $"Exception from DelayCommand on OBJECT_SELF in the work {probe.Script} scheduled");
            ctx.Assert(probe.RunFrame - probe.ScheduledFrame <= 1,
                $"The work {probe.Script} scheduled at frame {probe.ScheduledFrame} ran at frame {probe.RunFrame}: frame work runs from the " +
                "first frame after it is scheduled, also while the module is still loading.");
        }

        // The refused closures would have run within a frame or two; give them far longer.
        await ctx.DelaySecondsAsync(1f);
        foreach (var probe in new[] { PreloadProbe, LoadProbe })
            ctx.Assert(!probe.DelayedRan, $"The DelayCommand on OBJECT_SELF from the work {probe.Script} scheduled never ran: NWNX refused it.");

        ctx.SetResultDetail(
            $"mod_preload scheduled at frame {PreloadProbe.ScheduledFrame}, ran at {PreloadProbe.RunFrame}; " +
            $"mod_load scheduled at frame {LoadProbe.ScheduledFrame}, ran at {LoadProbe.RunFrame}; " +
            "OBJECT_SELF invalid in both; each DelayCommand on OBJECT_SELF dropped without an exception.");
    }

    [EngineTest("Scheduled work during the run sees OBJECT_SELF invalid, its DelayCommand is dropped, and a module closure still runs", Category = "FrameDelivery", TimeoutSeconds = 30f)]
    public static async Task RunTimeScheduledWorkMatchesTheNativeEntry(EngineTestContext ctx)
    {
        var probe = new BootProbe("run");
        var moduleClosureRan = false;
        probe.Schedule();
        AssignCommand(GetModule(), () => DelayCommand(0.1f, () => moduleClosureRan = true));

        await ctx.WaitUntilAsync(() => probe.Ran && moduleClosureRan, 10f, "the scheduled work and the module closure");
        await ctx.DelaySecondsAsync(1f);

        ctx.AssertEqual(OBJECT_INVALID, probe.ObjectSelf, "OBJECT_SELF seen by scheduled work");
        ctx.AssertEqual(null, probe.DelayException, "Exception from DelayCommand on OBJECT_SELF in scheduled work");
        ctx.Assert(!probe.DelayedRan, "The DelayCommand on OBJECT_SELF from scheduled work never ran: NWNX refused it.");
        ctx.Assert(probe.RunFrame - probe.ScheduledFrame <= 1,
            $"Work scheduled at frame {probe.ScheduledFrame} ran at frame {probe.RunFrame}.");
        ctx.SetResultDetail("Scheduled work ran on the next frame with OBJECT_SELF invalid; its DelayCommand was dropped; a closure assigned to the module ran.");
    }

    /// <summary>Schedules one <see cref="Scheduler"/> item and records what it sees when it runs.</summary>
    private sealed class BootProbe
    {
        public BootProbe(string script)
        {
            Script = script;
        }

        public string Script { get; }
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
